using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PlatformCore.Services.Factory;
using Unity.Cinemachine;
using UnityEngine;

namespace PlatformCore.Services
{
	public class CameraAsyncService : BaseAsyncService, ICameraService, ICameraShakeService
	{
		private const string PlayerCamera = "PlayerCamera";
		private readonly IObjectFactory _objectFactory;
		private CinemachineBasicMultiChannelPerlin _noise;
		private CinemachineCamera _camera;
		private CancellationTokenSource _shakeCts;
		public bool IsShaking { get; private set; }

		public CameraAsyncService(IObjectFactory objectFactory)
		{
			_objectFactory = objectFactory;
		}

		protected override async UniTask OnPostInitializeAsync(CancellationToken ct)
		{
			_camera = await _objectFactory.CreateAsync<CinemachineCamera>(ResourcePaths.Main.CinemachineCamera,
				Vector3.zero, Quaternion.identity);
			_noise = (CinemachineBasicMultiChannelPerlin)_camera.GetCinemachineComponent(CinemachineCore.Stage.Noise);
			_camera.name = PlayerCamera;
		}

		public override void Dispose()
		{
			if (!_camera)
			{
				return;
			}

			_objectFactory.Destroy(_camera.gameObject);
		}

		public void AttachTo(Transform target)
		{
			if (_camera == null || target == null)
			{
				return;
			}

			_camera.Follow = target;
			_camera.LookAt = target;
		}

		public Transform GetCameraTransform()
		{
			return _camera.transform;
		}

		public void SetFOV(float fov)
		{
			_camera.Lens.FieldOfView = fov;
		}
		
		public float GetFOV()
		{
			return _camera != null ? _camera.Lens.FieldOfView : 60f;
		}

		public void SetDutch(float degrees)
		{
			if (_camera)
			{
				_camera.Lens.Dutch = degrees;
			}
		}

		public float GetDutch()
		{
			return _camera != null ? _camera.Lens.Dutch : 0f;
		}

		public async UniTask ShakeAsync(float intensity, float duration)
		{
			if (!_noise || IsShaking)
			{
				return;
			}

			IsShaking = true;
			_shakeCts?.Cancel();
			_shakeCts = new CancellationTokenSource();
			_noise.AmplitudeGain = intensity;
			_noise.FrequencyGain = intensity * 1.5f;

			try
			{
				await UniTask.WaitForSeconds(duration, cancellationToken: _shakeCts.Token);
			}
			catch (OperationCanceledException)
			{
				// Shake прерван вручную
			}
			finally
			{
				StopShake();
			}
		}

		public void StopShake()
		{
			if (!_noise)
			{
				return;
			}


			_shakeCts?.Cancel();
			_noise.AmplitudeGain = 0;
			_noise.FrequencyGain = 0;
			IsShaking = false;
		}
	}
}