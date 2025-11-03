using _Main.Scripts.Core.Services;
using Cysharp.Threading.Tasks;
using PlatformCore.Core;
using PlatformCore.Infrastructure.Lifecycle;
using PlatformCore.Services.UI;

namespace _Main.Scripts.UI
{
	public class PauseController : IBaseController, IActivatable, IPreloadable
	{
		private readonly GameStateModel _gameStateModel;
		private readonly IInputService _inputService;
		private readonly ICursorService _cursorService;
		private readonly IUIService _uiService;

		private UIPauseMenu _uiPauseMenu;

		public PauseController(GameStateModel gameStateModel, ServiceLocator serviceLocator)
		{
			_gameStateModel = gameStateModel;
			_inputService = serviceLocator.Get<IInputService>();
			_uiService = serviceLocator.Get<IUIService>();
			_cursorService = serviceLocator.Get<ICursorService>();
		}

		public async UniTask PreloadAsync()
		{
			await _uiService.PreloadAsync<UIPauseMenu>();
		}

		public void Activate()
		{
			_uiPauseMenu = _uiService.Get<UIPauseMenu>();
			_uiPauseMenu.OnResumeClicked += ResumeGame;
			_inputService.OnPausePressed += TogglePause;
		}

		public void Deactivate()
		{
			_inputService.OnPausePressed -= TogglePause;
			_uiPauseMenu.OnResumeClicked -= ResumeGame;
		}

		private void TogglePause()
		{
			if (_gameStateModel.isPaused)
			{
				ResumeGame();
			}
			else
			{
				PauseGame();
			}
		}

		private void PauseGame()
		{
			_gameStateModel.isPaused = true;
			_uiService.ShowAsync<UIPauseMenu>();
			_cursorService.UnlockCursor();
		}

		private void ResumeGame()
		{
			_gameStateModel.isPaused = false;
			_uiService.HideAsync<UIPauseMenu>();
			_cursorService.LockCursor();
		}
	}
}