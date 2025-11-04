using System;
using _Main.Scripts.Core.Services;
using Cysharp.Threading.Tasks;
using PlatformCore.Core;
using PlatformCore.Infrastructure.Lifecycle;
using PlatformCore.Services.Audio;
using PlatformCore.Services.UI;
using UnityEngine;

public interface ISettingsWindow
{
	UniTask ShowFrontendAsync();
	UniTask ShowInGameAsync();
	void RequestClose();
}

public class SettingsController : IBaseController, IPreloadable, IActivatable, ISettingsWindow
{
	private readonly IUIService _ui;
	private readonly IAudioService _audio;
	private readonly ICursorService _cursor;
	private readonly IInputService _input;

	private UISettingsAudio _view;
	private UniTaskCompletionSource _tcs;

	public SettingsController(ServiceLocator sl)
	{
		_ui = sl.Get<IUIService>();
		_audio = sl.Get<IAudioService>();
		_cursor = sl.Get<ICursorService>();
		_input = sl.Get<IInputService>();
	}

	public async UniTask PreloadAsync()
	{
		await _ui.PreloadAsync<UISettingsAudio>();
	}

	public void Activate()
	{
		_view = _ui.Get<UISettingsAudio>();
	}

	public void Deactivate()
	{
	}

	public async UniTask ShowFrontendAsync()
	{
		using (HookView())
		using (HookInput()) // 👈 Esc закрывает окно
		{
			InitValues();
			await _ui.ShowAsync<UISettingsAudio>();
			_tcs = new UniTaskCompletionSource();
			await _tcs.Task;
			await _ui.HideAsync<UISettingsAudio>();
		}
	}

	public async UniTask ShowInGameAsync()
	{
		using (HookView())
		using (HookInput()) // 👈 Esc закрывает окно
		{
			float prevTS = Time.timeScale;
			_cursor.UnlockCursor();
			Time.timeScale = 0f;

			InitValues();
			await _ui.ShowAsync<UISettingsAudio>();
			_tcs = new UniTaskCompletionSource();
			await _tcs.Task;

			await _ui.HideAsync<UISettingsAudio>();
			Time.timeScale = prevTS;
			_cursor.LockCursor();
		}
	}

	public void RequestClose()
	{
		_tcs?.TrySetResult(); // 👈 можно закрыть извне (например, из PauseController)
	}

	private void InitValues()
	{
		_view.SetValues(master: 1f, music: 1f, sfx: 1f, mute: _audio.IsMuted, fullscreen: Screen.fullScreen);
	}

	private IDisposable HookView()
	{
		void Master(float v) => _audio.SetMasterVolume(v);
		void Music(float v) => _audio.SetMusicVolume(v);
		void Sfx(float v) => _audio.SetSfxVolume(v);
		void Close() => _tcs?.TrySetResult();

		_view.OnMasterChanged += Master;
		_view.OnMusicChanged += Music;
		_view.OnSfxChanged += Sfx;
		_view.OnCloseClicked += Close;

		return new Disposer(() =>
		{
			_view.OnMasterChanged -= Master;
			_view.OnMusicChanged -= Music;
			_view.OnSfxChanged -= Sfx;
			_view.OnCloseClicked -= Close;
		});
	}

	private IDisposable HookInput()
	{
		void OnPausePressed() => _tcs?.TrySetResult(); // 👈 Esc/Пауз — закрыть

		_input.OnPausePressed += OnPausePressed;
		return new Disposer(() => _input.OnPausePressed -= OnPausePressed);
	}

	private sealed class Disposer : IDisposable
	{
		private readonly Action _onDispose;
		public Disposer(Action onDispose) => _onDispose = onDispose;
		public void Dispose() => _onDispose?.Invoke();
	}
}