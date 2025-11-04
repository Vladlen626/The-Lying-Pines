using System;
using _Main.Scripts.Core;
using Cysharp.Threading.Tasks;
using _Main.Scripts.Core.Services;
using _Main.Scripts.Player;
using PlatformCore.Core;
using PlatformCore.Infrastructure.Lifecycle;
using PlatformCore.Services;
using PlatformCore.Services.Audio;
using UnityEngine;

namespace _Main.Scripts.Player
{
	/// <summary>
	/// E = Dive -> Impact -> Bounce. Цепочка "распрыжек-ударов".
	/// Требует в PlayerMovementController:
	///   - RequestVerticalOverride(float y)
	///   - AddImpulseXZ(Vector3 v)
	///   - SuppressJumpFor(float sec)
	/// </summary>
	public class PlayerSlamBounceController : IBaseController, IUpdatable
	{
		private readonly PlayerMovementController _movement;
		private readonly PlayerView _view;
		private readonly IInputService _input;
		private readonly ICameraShakeService _shake;
		private readonly PlayerModel _model;
		private readonly IAudioService _audio;

		// сколько игнорим коллизию с разрушенным объектом, чтобы пролететь сквозь
		private const float _ignoreDestructibleSecs = 0.20f;

		// маска земли = всё, КРОМЕ ломаемых слоёв (чтобы не "считать" ящик как землю)
		private LayerMask GroundMask => ~_cfg.ImpactMask;

		// Состояние
		private bool _diving; // активное пикирование вниз
		private bool _awaitLand; // ждём событие касания земли
		private bool _prevE; // edge detect для E
		private float _airTime; // время в воздухе
		private float _cooldown; // антиспам между ударами

		// Джем-модель параметров внутри контроллера
		private class Model
		{
			public float MinAirTime = 0.03f; // E доступна, если уже чутка в воздухе
			public float DiveDownSpeed = -38f; // мгновенная вертикалка вниз при старте
			public float DiveExtra = -48f; // подпинываем вниз каждый кадр во время dive

			public float ImpactRadius = 1f;
			public LayerMask ImpactMask = 1 << LayerMask.NameToLayer("Destructible");
			public float ImpactDelay = 0.03f; // микро-лаг до нанесения урона/FX

			public float AfterImpactCooldown = 0.5f; // прежде чем можно снова жать E
			public float SuppressJumpTime = 0.15f; // глушим обычный Space вокруг удара

			public float ForwardBoost = 5.5f; // горизонтальный толчок вперёд при отскоке
			public float ShakeAmp = 1.1f, ShakeDur = 0.18f;
			public string AudioImpact = "event:/ground_slam_impact";
		}

		private readonly Model _cfg = new();

		public PlayerSlamBounceController(
			IInputService input,
			PlayerMovementController movement,
			PlayerView view,
			ICameraShakeService shake,
			PlayerModel model,
			IAudioService audio = null)
		{
			_input = input;
			_movement = movement;
			_view = view;
			_shake = shake;
			_model = model;
			_audio = audio;

			_view.OnLand += OnLand; // детерминированный триггер удара
		}

		public void OnUpdate(float dt)
		{
			if (_cooldown > 0f) _cooldown -= dt;

			// учёт "в воздухе"
			if (!_view.IsGrounded) _airTime += dt;
			else _airTime = 0f;

			// нажатие E
			bool ePressed = _input.IsInteract && !_prevE;
			_prevE = _input.IsInteract;

			// старт дайва только в воздухе (включая воздух после предыдущего отскока)
			if (!_diving && _airTime >= _cfg.MinAirTime && ePressed && _cooldown <= 0f)
				StartDive();

			// во время дайва каждый кадр "прижимаем" вертикалку вниз
			if (_diving)
			{
				_movement.RequestVerticalOverride(_cfg.DiveDownSpeed + _cfg.DiveExtra);

				// страховка: вдруг событие OnLand потерялось, но контроллер уже считает, что на земле
				if (_view.IsGrounded && _awaitLand == false)
				{
					_diving = false;
					DoImpactAndBounce().Forget();
				}
			}
		}

		private void StartDive()
		{
			_diving = true;
			_awaitLand = true;
		
			// стартовый "пинок" вниз для отзывчивости
			_movement.RequestVerticalOverride(_cfg.DiveDownSpeed);

			// лёгкий стартовый шейк
			_audio?.PlaySound(AudioEvents.SlamDive);
			_shake?.ShakeAsync(0.35f, 0.08f).Forget();
		}

		private void OnLand()
		{
			if (!_awaitLand) return;
			_awaitLand = false;
			_diving = false;
			DoImpactAndBounce().Forget();
		}

		private async UniTaskVoid DoImpactAndBounce()
		{
			// моментальный фидбек удара
			_shake?.ShakeAsync(_cfg.ShakeAmp * 0.6f, _cfg.ShakeDur * 0.5f).Forget();


			// микро-задержка для "ощущения" удара
			await UniTask.Delay(TimeSpan.FromSeconds(_cfg.ImpactDelay));

			// ломаем окружение (ящики и т.д.)
			_audio?.PlaySound(AudioEvents.SlamPunch);
			DoAreaImpact();

			// ⚠️ теперь ждём, пока реально коснёмся земли
			await WaitUntilTouchGround();

			// FX (в этот момент жаба уже реально на полу)
			_view.PlaySlamFx();

			// === ОТСКОК ===
			float maxJumpVy = Mathf.Sqrt(_model.jumpHeight * -2f * _model.gravity) * 1.1f;
			_movement.SuppressJumpFor(_cfg.SuppressJumpTime);
			_movement.RequestVerticalOverride(maxJumpVy);

			var fwd = _view.CameraRoot.forward;
			fwd.y = 0f;
			fwd.Normalize();
			_movement.AddImpulseXZ(fwd * _cfg.ForwardBoost);

			_cooldown = _cfg.AfterImpactCooldown;
		}

		private async UniTask WaitUntilTouchGround()
		{
			const float maxWait = 1.0f; // защита, если не нашли землю
			float timer = 0f;

			while (timer < maxWait)
			{
				// Проверяем, есть ли настоящая земля под жабой
				var origin = _view.Position + Vector3.up * 0.2f;
				if (Physics.Raycast(origin, Vector3.down, out var hit, 1.5f, GroundMask))
				{
					// проверяем, что слой не в ImpactMask (т.е. не Destructible)
					if (((1 << hit.collider.gameObject.layer) & _cfg.ImpactMask) == 0)
					{
						return; // вот тогда реально касание земли
					}
				}

				timer += Time.deltaTime;
				await UniTask.Yield();
			}
		}


		private void DoAreaImpact()
		{
			var pos = _view.Position;

			// кого бьём
			var hits = Physics.OverlapSphere(
				pos,
				_cfg.ImpactRadius,
				_cfg.ImpactMask,
				QueryTriggerInteraction.Collide
			);

			var ctx = new ImpactCtx
			{
				Position = pos,
				Radius = _cfg.ImpactRadius,
				Force = Mathf.Abs(_cfg.DiveDownSpeed),
				Source = _view.PlayerTransform
			};

			// 1) Ломаем то, что под маской
			for (int i = 0; i < hits.Length; i++)
			{
				var go = hits[i].attachedRigidbody ? hits[i].attachedRigidbody.gameObject : hits[i].gameObject;
				if (go.TryGetComponent<ISlamImpactReceiver>(out var r))
					r.OnSlamImpact(ctx);
			}

			// 2) Временно игнорим коллизию с разбитыми объектами — чтобы пролететь сквозь них
			TemporarilyIgnoreDestructibles(hits, _view.Controller, 0.20f).Forget();

			// 3) Мягко доснэпаться к настоящей земле под объектом (чтобы оттолкнуться именно от пола)
			SnapDownToGroundAsync(~_cfg.ImpactMask).Forget();
		}

		private async UniTaskVoid TemporarilyIgnoreDestructibles(Collider[] cols, CharacterController cc, float seconds)
		{
			if (cc == null || cols == null || cols.Length == 0) return;

			for (int i = 0; i < cols.Length; i++)
			{
				var col = cols[i];
				if (col == null) continue;

				Physics.IgnoreCollision(cc, col, true);
				_ = RestoreCollisionLater(cc, col, seconds);
			}
		}

		private async UniTask RestoreCollisionLater(Collider a, Collider b, float seconds)
		{
			await UniTask.Delay(TimeSpan.FromSeconds(seconds));
			if (a != null && b != null)
				Physics.IgnoreCollision(a, b, false);
		}

		private async UniTask SnapDownToGroundAsync(LayerMask groundMask)
		{
			var origin = _view.Position + Vector3.up * 0.20f;
			float radius = 0.30f;
			float maxDist = 2.50f;

			if (Physics.SphereCast(origin, radius, Vector3.down, out var hit, maxDist, groundMask,
				    QueryTriggerInteraction.Ignore))
			{
				// небольшая подстройка вниз (оставим микро-зазор, чтобы не зажёвывало)
				float dist = Mathf.Max(0f, hit.distance - 0.05f);
				if (dist > 0.001f)
				{
					// ApplyMovement умножает на dt, конвертим дистанцию в скорость на 1 кадр
					var v = Vector3.down * (dist / Mathf.Max(Time.deltaTime, 0.0001f));
					_view.ApplyMovement(v);
					await UniTask.Yield(); // дать контроллеру обновить grounded на следующий кадр
				}
			}
		}
	}
}