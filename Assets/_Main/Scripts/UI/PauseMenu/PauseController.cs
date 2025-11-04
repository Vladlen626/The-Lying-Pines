using _Main.Scripts.Core.Services;
using Cysharp.Threading.Tasks;
using PlatformCore.Core;
using PlatformCore.Infrastructure.Lifecycle;
using PlatformCore.Services.Audio;
using PlatformCore.Services.UI;
using UnityEngine;

namespace _Main.Scripts.UI
{
	public class PauseController : IBaseController, IActivatable, IPreloadable
	{
		private readonly GameStateModel _gameStateModel;
		private readonly IInputService _inputService;
		private readonly ICursorService _cursorService;
		private readonly IUIService _uiService;
		private readonly IAudioService _audio;

		private UIPauseMenu _menu;

		// если хочешь хранить текущие значения локально (пока у аудиосервиса нет геттеров)
		private float _master = 1f, _music = 1f, _sfx = 1f;

		public PauseController(GameStateModel model, ServiceLocator sl)
		{
			_gameStateModel = model;
			_inputService = sl.Get<IInputService>();
			_cursorService = sl.Get<ICursorService>();
			_uiService = sl.Get<IUIService>();
			_audio = sl.Get<IAudioService>();
		}

		public async UniTask PreloadAsync()
		{
			await _uiService.PreloadAsync<UIPauseMenu>();
		}

		public void Activate()
		{
			_menu = _uiService.Get<UIPauseMenu>();
			SubscribeMenu();

			_inputService.OnPausePressed += TogglePause;
		}

		public void Deactivate()
		{
			_inputService.OnPausePressed -= TogglePause;
			UnsubscribeMenu();
		}

		private void SubscribeMenu()
		{
			if (_menu == null) return;

			_menu.OnMasterChanged += OnMasterChanged;
			_menu.OnMusicChanged += OnMusicChanged;
			_menu.OnSfxChanged += OnSfxChanged;
			_menu.OnMuteChanged += OnMuteChanged;
			_menu.OnFullscreenChanged += OnFullscreenChanged;
			_menu.OnQuitClicked += OnQuitClicked;
		}

		private void UnsubscribeMenu()
		{
			if (_menu == null) return;

			_menu.OnMasterChanged -= OnMasterChanged;
			_menu.OnMusicChanged -= OnMusicChanged;
			_menu.OnSfxChanged -= OnSfxChanged;
			_menu.OnMuteChanged -= OnMuteChanged;
			_menu.OnFullscreenChanged -= OnFullscreenChanged;
			_menu.OnQuitClicked -= OnQuitClicked;

			// опционально, чтобы гарантированно не осталось внешних слушателей:
			// _menu.ClearListeners();
		}

		private void TogglePause()
		{
			if (_gameStateModel.isPaused) ResumeGame();
			else PauseGame();
		}

		private void PauseGame()
		{
			_gameStateModel.isPaused = true;
			Time.timeScale = 0f;
			_cursorService.UnlockCursor();

			_uiService.ShowAsync<UIPauseMenu>();
			_menu.SetValues(_master, _music, _sfx, _audio.IsMuted, Screen.fullScreen);
		}

		private void ResumeGame()
		{
			_gameStateModel.isPaused = false;
			Time.timeScale = 1f;
			_cursorService.LockCursor();

			_uiService.HideAsync<UIPauseMenu>();
		}

		// ===== handlers =====
		private void OnMasterChanged(float v)
		{
			_master = Mathf.Clamp01(v);
			_audio.SetMasterVolume(_master);
		}

		private void OnMusicChanged(float v)
		{
			_music = Mathf.Clamp01(v);
			_audio.SetMusicVolume(_music);
		}

		private void OnSfxChanged(float v)
		{
			_sfx = Mathf.Clamp01(v);
			_audio.SetSfxVolume(_sfx);
		}

		private void OnMuteChanged(bool m)
		{
			_audio.SetMuted(m);
		}

		private void OnFullscreenChanged(bool on)
		{
			Screen.fullScreen = on;
		}

		private void OnQuitClicked()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
		}
	}
}