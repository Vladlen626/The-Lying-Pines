// CollectibleController.cs

using System;
using _Main.Scripts.Core;
using PlatformCore.Infrastructure.Lifecycle;
using _Main.Scripts.Inventory;
using _Main.Scripts.Player;
using PlatformCore.Core;
using PlatformCore.Services.Audio;
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
		private readonly IAudioService _audioService;
		private readonly IAnchor _target;

		// Idle (затухают возле игрока)
		private const float IdleBobAmp = 0.06f; // было 0.08
		private const float IdleBobFreq = 2.2f;
		private const float IdleSpinDps = 55f; // было 70

		// Полёт
		private const float MinFlyTime = 0.20f; // чуть быстрее реакции
		private const float MaxFlyTime = 0.40f;
		private const float MaxArcHeight = 0.60f; // высота дуги на большой дистанции
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
                private Vector3 _cachedTargetPos;
                private Vector3 _toTarget;
                private Vector3 _bezierMid;
                private Vector3 _bezierFirst;
                private Vector3 _currentPos;
                private Vector3 _orbitOffset;
                private readonly System.Random _rng = new System.Random();

		private const float OvershootDist = 0.25f; // перелёт цели (м)
		private const float OvershootEase = 0.6f; // 0..1 — сколько “держим” перелёт
		private const float OrbitTime = 0.14f; // сколько “крутимся” у игрока перед влетом
		private const float OrbitRadius = 0.25f; // радиус орбиты
                private const float OrbitDps = 360f; // скорость орбиты (deg/s)
                private const float WobbleAmp = 0.12f; // боковой синус
                private const float WobbleFreq = 2.5f; // его частота
                private const float StartPunch = 1.12f; // стартовый пульс масштаба
                private const float TwoPi = Mathf.PI * 2f;

		private float _orbitT; // таймер орбиты
		private Vector3 _flySide; // боковая ось для синуса
		private float _wobblePhase; // фаза синуса

		public CollectibleController(CollectibleView view, IInventoryService inventory, IAnchor target, IAudioService audioService)
		{
			_view = view ?? throw new ArgumentNullException(nameof(view));
			_inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
			_target = target ?? throw new ArgumentNullException(nameof(target));
			_audioService = audioService;
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
                        _cachedTargetPos = _target.Position;
                        float smoothK = 1f - Mathf.Exp(-TargetSmooth * dt);
                        _smoothedTarget.x += (_cachedTargetPos.x - _smoothedTarget.x) * smoothK;
                        _smoothedTarget.y += (_cachedTargetPos.y - _smoothedTarget.y) * smoothK;
                        _smoothedTarget.z += (_cachedTargetPos.z - _smoothedTarget.z) * smoothK;

			switch (_state)
			{
				case State.Idle: TickIdle(dt); break;
				case State.Flying: TickFlying(dt); break;
			}
		}

		private void TickIdle(float dt)
		{
                        Vector3 worldPos = _view.WorldPos;
                        _toTarget.x = _smoothedTarget.x - worldPos.x;
                        _toTarget.y = _smoothedTarget.y - worldPos.y;
                        _toTarget.z = _smoothedTarget.z - worldPos.z;
                        float distSq = _toTarget.sqrMagnitude;
                        float dist = distSq > 0f ? Mathf.Sqrt(distSq) : 0f;

			// коэффициент «далеко/близко»: 1 — далеко, 0 — у цели
			float nearK = Mathf.InverseLerp(_view.ContactRadius, _view.MagnetRadius, dist);
			nearK = Smooth01(nearK);

			// меньше вертикальных «прыжков» около игрока
                        _phase += dt * (IdleBobFreq * TwoPi);
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
                        _toTarget.x = _smoothedTarget.x - _startPos.x;
                        _toTarget.y = _smoothedTarget.y - _startPos.y;
                        _toTarget.z = _smoothedTarget.z - _startPos.z;
                        float toTargetMagSq = _toTarget.sqrMagnitude;
                        if (toTargetMagSq < 1e-6f)
                        {
                                _toTarget = Vector3.forward;
                                toTargetMagSq = 1f;
                        }

                        float invLen = 1f / Mathf.Sqrt(toTargetMagSq);
                        Vector3 dir;
                        dir.x = _toTarget.x * invLen;
                        dir.y = _toTarget.y * invLen;
                        dir.z = _toTarget.z * invLen;

                        _flySide = Vector3.Cross(Vector3.up, dir);
                        float sideMag = _flySide.sqrMagnitude;
                        if (sideMag > 1e-6f)
                        {
                                float invSide = 1f / Mathf.Sqrt(sideMag);
                                _flySide.x *= invSide;
                                _flySide.y *= invSide;
                                _flySide.z *= invSide;
                        }

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
                        Vector3 target = _smoothedTarget;
                        _toTarget.x = target.x - _startPos.x;
                        _toTarget.y = target.y - _startPos.y;
                        _toTarget.z = target.z - _startPos.z;
                        float distNowSq = _toTarget.sqrMagnitude;
                        float distNow = distNowSq > 0f ? Mathf.Sqrt(distNowSq) : 0f;
                        float arc = Mathf.Lerp(0.15f, MaxArcHeight, Mathf.Clamp01(distNow / Mathf.Max(0.01f, _view.MagnetRadius)));

                        _bezierMid.x = (_startPos.x + target.x) * 0.5f;
                        _bezierMid.y = (_startPos.y + target.y) * 0.5f + arc;
                        _bezierMid.z = (_startPos.z + target.z) * 0.5f;

                        _bezierFirst.x = Mathf.Lerp(_startPos.x, _bezierMid.x, te);
                        _bezierFirst.y = Mathf.Lerp(_startPos.y, _bezierMid.y, te);
                        _bezierFirst.z = Mathf.Lerp(_startPos.z, _bezierMid.z, te);

                        _currentPos.x = Mathf.Lerp(_bezierMid.x, target.x, te);
                        _currentPos.y = Mathf.Lerp(_bezierMid.y, target.y, te);
                        _currentPos.z = Mathf.Lerp(_bezierMid.z, target.z, te);

                        _currentPos.x = Mathf.Lerp(_bezierFirst.x, _currentPos.x, te);
                        _currentPos.y = Mathf.Lerp(_bezierFirst.y, _currentPos.y, te);
                        _currentPos.z = Mathf.Lerp(_bezierFirst.z, _currentPos.z, te);

                        // 1) Боковой синус, который затухает к концу
                        _wobblePhase += dt * (WobbleFreq * TwoPi);
                        float wobbleK = Smooth01(1f - te);
                        float wobbleOffset = Mathf.Sin(_wobblePhase) * (WobbleAmp * wobbleK);
                        _currentPos.x += _flySide.x * wobbleOffset;
                        _currentPos.y += _flySide.y * wobbleOffset;
                        _currentPos.z += _flySide.z * wobbleOffset;

                        // 2) Небольшой перелёт цели и возврат (даёт "магнитное" втягивание)
                        _toTarget.x = target.x - _currentPos.x;
                        _toTarget.y = target.y - _currentPos.y;
                        _toTarget.z = target.z - _currentPos.z;
                        float dNowSq = _toTarget.sqrMagnitude;
                        float dNow = dNowSq > 0f ? Mathf.Sqrt(dNowSq) : 0f;
                        if (dNow < EndShrinkDist * 1.8f) // уже рядом — начинаем “перелёт”
                        {
                                Vector3 overDir;
                                if (dNowSq > 1e-6f)
                                {
                                        float invD = 1f / Mathf.Sqrt(dNowSq);
                                        overDir.x = _toTarget.x * invD;
                                        overDir.y = _toTarget.y * invD;
                                        overDir.z = _toTarget.z * invD;
                                }
                                else
                                {
                                        overDir = Vector3.forward;
                                }

                                float overK = Mathf.Clamp01((EndShrinkDist * 1.8f - dNow) / (EndShrinkDist * 1.8f));
                                float overAmt = OvershootDist * overK * OvershootEase;
                                _currentPos.x += overDir.x * overAmt;
                                _currentPos.y += overDir.y * overAmt;
                                _currentPos.z += overDir.z * overAmt;
                        }

                        // 3) Мини-орбита на самом финише
                        if (dNow <= EndShrinkDist * 0.9f)
                        {
                                _orbitT += dt;
                                float ang = OrbitDps * _orbitT;
                                float angRad = ang * Mathf.Deg2Rad;
                                float sin = Mathf.Sin(angRad);
                                float cos = Mathf.Cos(angRad);
                                float orbitScale = Mathf.Clamp01(1f - (_orbitT / OrbitTime));

                                _orbitOffset.x = (_flySide.x * cos - _flySide.z * sin) * OrbitRadius;
                                _orbitOffset.y = _flySide.y * OrbitRadius;
                                _orbitOffset.z = (_flySide.x * sin + _flySide.z * cos) * OrbitRadius;

                                _currentPos.x = target.x + _orbitOffset.x * orbitScale;
                                _currentPos.y = target.y + _orbitOffset.y * orbitScale;
                                _currentPos.z = target.z + _orbitOffset.z * orbitScale;
                                if (_orbitT >= OrbitTime)
                                {
                                        _currentPos = target;
                                }
                        }

                        _view.WorldPos = _currentPos;

                        // Мягкая посадка: у тебя уже было — оставляем
                        _toTarget.x = target.x - _currentPos.x;
                        _toTarget.y = target.y - _currentPos.y;
                        _toTarget.z = target.z - _currentPos.z;
                        float dSq = _toTarget.sqrMagnitude;
                        float d = dSq > 0f ? Mathf.Sqrt(dSq) : 0f;
			if (d <= EndShrinkDist)
			{
				_audioService.PlaySound(AudioEvents.GetCrumb);
				_view.PlayFx(_target.Position);
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
			_view.DestroySelf();
		}

		// ——— util easing ———
		private static float Smooth01(float x) => x * x * (3f - 2f * x); // Hermite
		private static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);
	}
}