// CollectibleController.cs

using System;
using PlatformCore.Infrastructure.Lifecycle;
using _Main.Scripts.Inventory;
using _Main.Scripts.Player;
using PlatformCore.Core;
using UnityEngine;

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

			// длительность и высота дуги зависят от дистанции (коротко — низкая дуга и быстро)
			float k = Mathf.Clamp01(currentDist / Mathf.Max(0.01f, _view.MagnetRadius));
			_dur = Mathf.Lerp(MinFlyTime, MaxFlyTime, k);
		}

		private void TickFlying(float dt)
		{
			_t += dt / Mathf.Max(_dur, 1e-4f);
			if (_t > 1f) _t = 1f;

			// ease-out к концу (быстро стартуем, мягко садимся)
			float te = EaseOutCubic(_t);

			// дуга: контрольная точка зависит от текущей дистанции
			var target = _smoothedTarget;
			float distNow = Vector3.Distance(_startPos, target);
			float arc = Mathf.Lerp(0.15f, MaxArcHeight, Mathf.Clamp01(distNow / Mathf.Max(0.01f, _view.MagnetRadius)));

			var mid = (_startPos + target) * 0.5f;
			var side = Vector3.Cross(Vector3.up, (target - _startPos).normalized);
			var ctrl = mid + Vector3.up * arc + side * (Mathf.Sin(te * Mathf.PI) * Swirl);

			var a = Vector3.Lerp(_startPos, ctrl, te);
			var b = Vector3.Lerp(ctrl, target, te);
			var pos = Vector3.Lerp(a, b, te);
			_view.WorldPos = pos;

			// «мягкая посадка»: чуть уменьшаем масштаб, когда близко к цели
			float d = Vector3.Distance(target, pos);
			if (d <= EndShrinkDist)
			{
				float shrinkK = Mathf.InverseLerp(EndShrinkDist, 0f, d);
				float scale = Mathf.Lerp(1f, EndShrinkMin, Smooth01(shrinkK));
				_view.SetScale01(scale);
			}

			// финиш
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
			_view.DestroySelf();
		}

		// ——— util easing ———
		private static float Smooth01(float x) => x * x * (3f - 2f * x); // Hermite
		private static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);
	}
}