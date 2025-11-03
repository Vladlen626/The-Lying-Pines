using _Main.Scripts.Core.Services;
using Cysharp.Threading.Tasks;
using PlatformCore.Core;
using PlatformCore.Infrastructure.Lifecycle;
using PlatformCore.Services;
using Unity.Cinemachine;
using UnityEngine;

namespace _Main.Scripts.Player
{
	public class PlayerCameraController : IBaseController, IActivatable
	{
		private readonly IInputService _inputService;
		private readonly ICursorService _cursorService;
		private readonly PlayerView _playerView;
		private readonly ICameraShakeService _cameraShakeService;

		public PlayerCameraController(ServiceLocator serviceLocator, PlayerView playerView)
		{
			_inputService = serviceLocator.Get<IInputService>();
			_cameraShakeService = serviceLocator.Get<ICameraShakeService>();
			_playerView = playerView;
		}

		public void Activate()
		{
			_inputService.OnJumpPressed += OnJumpHandler;
			_playerView.OnLand += OnJumpHandler;
		}

		public void Deactivate()
		{
			_inputService.OnJumpPressed -= OnJumpHandler;
			_playerView.OnLand -= OnJumpHandler;
		}

		private void OnJumpHandler()
		{
			_cameraShakeService.ShakeAsync(1f, 0.1f);
		}
	}
}