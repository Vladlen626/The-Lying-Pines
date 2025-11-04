using _Main.Scripts.Core.Services;
using PlatformCore.Core;
using PlatformCore.Infrastructure.Lifecycle;
using PlatformCore.Services.UI;

namespace _Main.Scripts.Global.Controllers
{
	public class GameStateController : IBaseController, IActivatable
	{
		private readonly GameStateModel _gameStateModel;
		private readonly IInputService _inputService;

		public GameStateController(GameStateModel gameStateModel, IInputService inputService)
		{
			_gameStateModel = gameStateModel;
			_inputService = inputService;
		}

		public void Activate()
		{
			_gameStateModel.onPauseChanged += GameStateChangedHandler;
		}

		public void Deactivate()
		{
			_gameStateModel.onPauseChanged -= GameStateChangedHandler;
		}

		private void GameStateChangedHandler(bool wasPaused)
		{
			if (wasPaused)
			{
				_inputService.DisablePlayerInputs();
			}
			else
			{
				_inputService.EnablePlayerInputs();
			}
		}
	}
}