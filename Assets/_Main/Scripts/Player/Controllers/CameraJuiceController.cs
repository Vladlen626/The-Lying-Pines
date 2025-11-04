using PlatformCore.Core;
using PlatformCore.Infrastructure.Lifecycle;
using PlatformCore.Services;
using UnityEngine;

namespace _Main.Scripts.CameraFX
{
	public sealed class CameraJuiceController : IBaseController, ILateUpdatable
	{
		private readonly ICameraService _cam;
		private readonly Transform _follow;

		// Настройки
		private readonly float _baseFov = 60f;
		private readonly float _fovFromSpeed = 8f;
		private readonly float _fovFromFall = 3f;
		private readonly float _speedForMaxFov = 8f;
		private readonly float _fovLerp = 8f;

		private readonly float _maxDutch = 0.5f;
		private readonly float _dutchLerp = 8f;
		private float _yawToDutchGain = 0.03f;

		private Vector3 _lastCamFwd;
		private Vector3 _lastFollowPos;
		private float _currentFov;
		private float _currentDutch;


		public CameraJuiceController(ICameraService cameraService, Transform followTarget)
		{
			_cam = cameraService;
			_follow = followTarget;
			_lastFollowPos = followTarget ? followTarget.position : Vector3.zero;
			_currentFov = Mathf.Approximately(_cam.GetFOV(), 0f) ? _baseFov : _cam.GetFOV();
			var fwd = Camera.main ? Camera.main.transform.forward : Vector3.forward;
			fwd.y = 0;
			_lastCamFwd = fwd.normalized;
		}

		public void OnLateUpdate(float dt)
		{
			if (!_follow)
			{
				return;
			}

			var pos = _follow.position;
			var vel = (pos - _lastFollowPos) / Mathf.Max(dt, 1e-4f);
			_lastFollowPos = pos;

			var hVel = vel;
			hVel.y = 0f;
			float speed = hVel.magnitude;
			float speed01 = Mathf.Clamp01(speed / Mathf.Max(0.01f, _speedForMaxFov));

			float fallAdd = Mathf.Clamp01(-vel.y * 0.15f) * _fovFromFall;
			float targetFov = _baseFov + _fovFromSpeed * speed01 + fallAdd;
			_currentFov = Mathf.Lerp(_currentFov, targetFov, 1f - Mathf.Exp(-_fovLerp * dt));
			_cam.SetFOV(_currentFov);

			var camFwd = Camera.main.transform.forward;
			camFwd.y = 0;
			camFwd.Normalize();

			float prevYaw = Mathf.Atan2(_lastCamFwd.x, _lastCamFwd.z) * Mathf.Rad2Deg;
			float currYaw = Mathf.Atan2(camFwd.x, camFwd.z) * Mathf.Rad2Deg;
			float yawRate = Mathf.DeltaAngle(prevYaw, currYaw) / Mathf.Max(dt, 1e-4f);
			_lastCamFwd = camFwd;
			float targetDutch = Mathf.Clamp(-yawRate * _yawToDutchGain, -_maxDutch, _maxDutch);
			_currentDutch = Mathf.Lerp(_currentDutch, targetDutch, 1f - Mathf.Exp(-_dutchLerp * dt));
			_cam.SetDutch(_currentDutch);
		}
	}
}