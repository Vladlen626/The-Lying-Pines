// Assets/_Main/Scripts/UI/MainMenuController.cs

using Cysharp.Threading.Tasks;
using PlatformCore.Core;
using PlatformCore.Infrastructure.Lifecycle;
using PlatformCore.Services.UI;

namespace _Main.Scripts.UI
{
	public sealed class MainMenuController : IBaseController, IPreloadable, IActivatable
	{
		private readonly IUIService _ui;
		private readonly ISettingsWindow _settings; // твой SettingsController как сервис

		private UIMainMenu _menu;
		private UniTaskCompletionSource _startTcs;

		public MainMenuController(ServiceLocator sl, ISettingsWindow settings)
		{
			_ui = sl.Get<IUIService>();
			_settings = settings;
		}

		public async UniTask PreloadAsync()
		{
			await _ui.PreloadAsync<UIMainMenu>();
		}

		public void Activate()
		{
			_menu = _ui.Get<UIMainMenu>();
			_menu.OnStartClicked += HandleStart;
			_menu.OnSettingsClicked += HandleSettings;

			_ui.ShowAsync<UIMainMenu>(0.15f).Forget();
		}

		public void Deactivate()
		{
			if (_menu == null) return;
			_menu.OnStartClicked -= HandleStart;
			_menu.OnSettingsClicked -= HandleSettings;
		}

		public UniTask WaitForStartAsync()
		{
			_startTcs = new UniTaskCompletionSource();
			return _startTcs.Task;
		}

		private void HandleStart()
		{
			_ui.HideAsync<UIMainMenu>().Forget();
			_startTcs?.TrySetResult();
		}

		private async void HandleSettings()
		{
			// Настройки поверх фронтенда (без паузы игры)
			await _settings.ShowFrontendAsync();
		}

		private void HandleQuit()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
		}
	}
}