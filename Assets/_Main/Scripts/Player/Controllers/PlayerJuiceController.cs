using System;
using DG.Tweening;
using PlatformCore.Core;
using PlatformCore.Infrastructure.Lifecycle;
using UnityEngine;

namespace _Main.Scripts.FX
{
	public sealed class PlayerJuiceController : IBaseController, IActivatable, ILateUpdatable, IDisposable
	{
		private readonly PlayerView _view;
		private readonly Transform _visual;

		// --- Тюн ---
		private readonly float _landSquashY = 0.86f;
		private readonly float _landStretchXZ = 1.08f;
		private readonly float _landTime = 0.10f;

		private readonly float _startLow = 0.10f; // ниже этого считаем “стояли”
		private readonly float _startHigh = 0.80f; // выше этого считаем “побежали”
		private readonly float _startStretchZ = 1.06f;
		private readonly float _startSquashX = 0.96f;
		private readonly float _startTime = 0.08f;

		private readonly float _turnAngle = 110f;
		private readonly float _turnSquashY = 0.92f;
		private readonly float _turnTime = 0.07f;

		private readonly float _returnTime = 0.10f;
		private readonly Ease _ease = Ease.InOutSine;

		// --- Антиконфликты ---
		private const float LandLockDuration = 0.12f; // время, в течение которого ничего не перебивает приземление
		private float _landLockTimer;
		private int _landFrame; // кадр, в котором произошло приземление (на всякий)

		// --- Рантайм ---
		private Vector3 _lastPos;
		private Vector3 _lastDir;
		private float _prevSpeed;
		private Sequence _seq;

		public PlayerJuiceController(PlayerView view)
		{
			_view = view;
			_visual = view.PlayerTransform;
			_lastPos = _view.Position;
			_lastDir = _view.transform.forward;
		}

		public void Activate()
		{
			_view.OnLand += OnLand;
			_lastPos = _view.Position;
			_lastDir = _view.transform.forward;
			_prevSpeed = 0f;
			_landLockTimer = 0f;
			_landFrame = -1;
		}

		public void Deactivate()
		{
			_view.OnLand -= OnLand;
			_seq?.Kill();
		}

		public void Dispose() => _seq?.Kill();

		public void OnLateUpdate(float dt)
		{
			// тик локера
			if (_landLockTimer > 0f) _landLockTimer -= dt;

			var pos = _view.Position;
			var vel = (pos - _lastPos) / Mathf.Max(dt, 1e-4f);
			vel.y = 0f;
			var speed = vel.magnitude;

			bool sameFrameAsLand = (Time.frameCount == _landFrame);

			// --- старт-бёрст: пересечение порога с гистерезисом (и не в кадр приземления, и не под лочком) ---
			if (!sameFrameAsLand && _landLockTimer <= 0f && _view.IsGrounded)
			{
				if (_prevSpeed < _startLow && speed > _startHigh)
					DoStartBurst();
			}

			// --- резкий разворот (не перебиваем приземление) ---
			if (!sameFrameAsLand && _landLockTimer <= 0f && speed > 0.35f)
			{
				var dir = vel.sqrMagnitude > 1e-6f ? vel.normalized : _lastDir;
				float ang = Vector3.Angle(_lastDir, dir);
				if (ang >= _turnAngle) DoTurnSquash();
				_lastDir = dir;
			}

			_lastPos = pos;
			_prevSpeed = speed; 
		}

		private void OnLand()
		{
			_landLockTimer = LandLockDuration;
			_landFrame = Time.frameCount;

			_seq?.Kill();
			_seq = DOTween.Sequence();
			var to = new Vector3(_landStretchXZ, _landSquashY, _landStretchXZ);
			_seq.Append(_visual.DOScale(to, _landTime).SetEase(_ease));
			_seq.Append(_visual.DOScale(Vector3.one, _returnTime).SetEase(_ease));
		}

		private void DoStartBurst()
		{
			_seq?.Kill();
			_seq = DOTween.Sequence();
			var to = new Vector3(_startSquashX, 1f, _startStretchZ);
			_seq.Append(_visual.DOScale(to, _startTime).SetEase(_ease));
			_seq.Append(_visual.DOScale(Vector3.one, _returnTime).SetEase(_ease));
		}

		private void DoTurnSquash()
		{
			_seq?.Kill();
			_seq = DOTween.Sequence();
			var to = new Vector3(1.04f, _turnSquashY, 1.04f);
			_seq.Append(_visual.DOScale(to, _turnTime).SetEase(_ease));
			_seq.Append(_visual.DOScale(Vector3.one, _returnTime).SetEase(_ease));
		}
	}
}