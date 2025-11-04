using _Main.Scripts;
using _Main.Scripts.Core.Services;
using Cysharp.Threading.Tasks;
using PlatformCore.Core;
using PlatformCore.Infrastructure.Lifecycle;

public class PauseController : IBaseController, IActivatable
{
	private readonly GameStateModel _gameStateModel;
	private readonly IInputService _input;
	private readonly ISettingsWindow _settingsWindow;

	public PauseController(GameStateModel model, ServiceLocator sl, ISettingsWindow SettingsController)
	{
		_gameStateModel = model;
		_input = sl.Get<IInputService>();
		_settingsWindow = SettingsController;
	}

	public void Activate()
	{
		_input.OnPausePressed += OnPausePressed;
	}

	public void Deactivate()
	{
		_input.OnPausePressed -= OnPausePressed;
	}

	private void OnPausePressed()
	{
		ShowPauseAsync().Forget();
	}

	private async UniTaskVoid ShowPauseAsync()
	{
		if (_gameStateModel.isPaused) return;

		_gameStateModel.SetPaused(true);
		
		await _settingsWindow.ShowInGameAsync();

		_gameStateModel.SetPaused(false);
	}
}