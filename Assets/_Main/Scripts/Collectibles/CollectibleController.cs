// CollectibleController.cs

using System;
using PlatformCore.Infrastructure.Lifecycle;
using _Main.Scripts.Inventory;
using _Main.Scripts.Player;
using PlatformCore.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _Main.Scripts.Collectibles
{
	public sealed class CollectibleController : IBaseController, IActivatable, IUpdatable, IDeactivatable
	{
		private enum State
		{
			Idle,
			Flying,
			Collected
		}

		private readonly CollectibleView _view;
		private readonly IInventoryService _inventory;
		private readonly IAnchor _target;

		// Idle (затухают возле игрока)
		private const float IdleBobAmp = 0.06f; // было 0.08
		private const float IdleBobFreq = 2.2f;
		private const float IdleSpinDps = 55f; // было 70

		// Полёт
		private const float MinFlyTime = 0.20f; // чуть быстрее реакции
		private const float MaxFlyTime = 0.40f;
		private const float MaxArcHeight = 0.60f; // высота дуги на большой дистанции
		private const float Swirl = 0.5f;

		// Сглаживание цели
		private const float TargetSmooth = 14f; // exp-smooth к якорю
		private const float EndShrinkDist = 0.35f; // когда начинаем «сжиматься»
		private const float EndShrinkMin = 0.78f; // минимум масштаба при касании

		private State _state;
		private float _phase; // синус idle
		private Vector3 _startPos; // начало полёта
		private float _t; // 0..1 прогресс
		private float _dur; // длительность
		private Vector3 _smoothedTarget; // сглаженная цель
		private readonly System.Random _rng = new System.Random();

		private const float OvershootDist = 0.25f; // перелёт цели (м)
		private const float OvershootEase = 0.6f; // 0..1 — сколько “держим” перелёт
		private const float OrbitTime = 0.14f; // сколько “крутимся” у игрока перед влетом
		private const float OrbitRadius = 0.25f; // радиус орбиты
		private const float OrbitDps = 360f; // скорость орбиты (deg/s)
		private const float WobbleAmp = 0.12f; // боковой синус
		private const float WobbleFreq = 2.5f; // его частота
		private const float StartPunch = 1.12f; // стартовый пульс масштаба

		private float _orbitT; // таймер орбиты
		private Vector3 _flySide; // боковая ось для синуса
		private float _wobblePhase; // фаза синуса

		public CollectibleController(CollectibleView view, IInventoryService inventory, IAnchor target)
		{
			_view = view ?? throw new ArgumentNullException(nameof(view));
			_inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
			_target = target ?? throw new ArgumentNullException(nameof(target));
		}

		public void Activate()
		{
			_state = State.Idle;
			_view.EnableCollider(true);
			_smoothedTarget = _target.Position;

			// случайная фаза, чтобы не синхронизировались
			_phase = (float)(_rng.NextDouble() * Math.PI * 2.0);
			_view.ResetScale();
		}

		public void Deactivate()
		{
		}

		public void OnUpdate(float dt)
		{
			if (!_view.canBeCollected)
			{
				return;
			}

			// экспоненциальное сглаживание цели
			_smoothedTarget = Vector3.Lerp(_smoothedTarget, _target.Position, 1f - Mathf.Exp(-TargetSmooth * dt));

			switch (_state)
			{
				case State.Idle: TickIdle(dt); break;
				case State.Flying: TickFlying(dt); break;
			}
		}

		private void TickIdle(float dt)
		{
			var to = _smoothedTarget - _view.WorldPos;
			var dist = to.magnitude;

			// коэффициент «далеко/близко»: 1 — далеко, 0 — у цели
			float nearK = Mathf.InverseLerp(_view.ContactRadius, _view.MagnetRadius, dist);
			nearK = Smooth01(nearK);

			// меньше вертикальных «прыжков» около игрока
			_phase += dt * (IdleBobFreq * Mathf.PI * 2f);
			_view.SetIdleOffset(Mathf.Sin(_phase) * (IdleBobAmp * nearK));

			// и спин тише рядом с игроком
			_view.AddYaw((IdleSpinDps * (0.4f + 0.6f * nearK)) * dt);

			// старт полёта: как только вошли в магнит-радиус
			if (dist <= _view.MagnetRadius)
				BeginFlight(dist);
		}

		private void BeginFlight(float currentDist)
		{
			_state = State.Flying;
			_startPos = _view.WorldPos;
			_view.EnableCollider(false);

			// длительность и высота дуги — как у тебя было
			float k = Mathf.Clamp01(currentDist / Mathf.Max(0.01f, _view.MagnetRadius));
			_dur = Mathf.Lerp(MinFlyTime, MaxFlyTime, k);

			// боковая ось перпендикулярно направлению к цели
			var toTarget = (_smoothedTarget - _startPos);
			var dir = toTarget.sqrMagnitude > 1e-6f ? toTarget.normalized : Vector3.forward;
			_flySide = Vector3.Cross(Vector3.up, dir).normalized;
			_wobblePhase = (float)(_rng.NextDouble() * Math.PI * 2.0);

			// лёгкий стартовый “пульс” масштаба
			_view.SetScale01(StartPunch);
		}

		private void TickFlying(float dt)
		{
			_t += dt / Mathf.Max(_dur, 1e-4f);
			if (_t > 1f) _t = 1f;

			// ease-out к концу
			float te = EaseOutCubic(_t);

			// базовая дуга Бэзье (как у тебя)
			var target = _smoothedTarget;
			float distNow = Vector3.Distance(_startPos, target);
			float arc = Mathf.Lerp(0.15f, MaxArcHeight, Mathf.Clamp01(distNow / Mathf.Max(0.01f, _view.MagnetRadius)));

			var mid = (_startPos + target) * 0.5f;
			var sideBase = _flySide; // сохранённая боковая ось
			var ctrl = mid + Vector3.up * arc;

			var a = Vector3.Lerp(_startPos, ctrl, te);
			var b = Vector3.Lerp(ctrl, target, te);
			var pos = Vector3.Lerp(a, b, te);

			// 1) Боковой синус, который затухает к концу
			_wobblePhase += dt * (WobbleFreq * Mathf.PI * 2f);
			float wobbleK = Smooth01(1f - te);
			pos += sideBase * (Mathf.Sin(_wobblePhase) * (WobbleAmp * wobbleK));

			// 2) Небольшой перелёт цели и возврат (даёт "магнитное" втягивание)
			Vector3 toT = (target - pos);
			float dNow = toT.magnitude;
			if (dNow < EndShrinkDist * 1.8f) // уже рядом — начинаем “перелёт”
			{
				var overDir = toT.sqrMagnitude > 1e-6f ? toT.normalized : Vector3.forward;
				float overK = Mathf.Clamp01((EndShrinkDist * 1.8f - dNow) / (EndShrinkDist * 1.8f));
				pos += overDir * (OvershootDist * overK * OvershootEase);
			}

			// 3) Мини-орбита на самом финише
			if (dNow <= EndShrinkDist * 0.9f)
			{
				_orbitT += dt;
				float ang = OrbitDps * _orbitT;
				var orbitOffset = Quaternion.AngleAxis(ang, Vector3.up) * (sideBase * OrbitRadius);
				pos = target + orbitOffset * Mathf.Clamp01(1f - (_orbitT / OrbitTime));
				if (_orbitT >= OrbitTime)
				{
					pos = target;
				}
			}

			_view.WorldPos = pos;

			// Мягкая посадка: у тебя уже было — оставляем
			float d = Vector3.Distance(target, pos);
			if (d <= EndShrinkDist)
			{
				float shrinkK = Mathf.InverseLerp(EndShrinkDist, 0f, d);
				float scale = Mathf.Lerp(1f, EndShrinkMin, Smooth01(shrinkK));
				_view.SetScale01(scale);
			}

			// Финиш: близко/дошли по времени
			if (d <= _view.ContactRadius || _t >= 1f)
			{
				Collect();
			}
		}

		private void Collect()
		{
			if (_state == State.Collected) return;
			_state = State.Collected;
			_inventory.Add(_view.Kind, _view.Amount);
			_view.PlayFx();
			_view.DestroySelf();
		}

		// ——— util easing ———
		private static float Smooth01(float x) => x * x * (3f - 2f * x); // Hermite
		private static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);
	}
}