using Cysharp.Threading.Tasks;
using PlatformCore.Core;
using PlatformCore.Infrastructure.Lifecycle;
using PlatformCore.Services.Audio;

namespace _Main.Scripts.Audio
{
	public class AudioController : IBaseController, IActivatable
	{
		private readonly GameStateModel _gameState;
		private const string MainMenuTheme = "event:/MainMenuTheme";
		private const string GameplayTheme = "event:/GameplayTheme";
		private readonly IAudioService _audio;
		private float _savedMusicVol = 1f;

		private string _currentTheme;
		private bool _alive;
		public AudioController(IAudioService audio, GameStateModel gameState)
		{
			_audio = audio;
			_gameState = gameState;
		}

		public void Activate()
		{
			_alive = true;
			_gameState.onMenuChanged += OnMenuChanged;
			
			PlayDeferred(_gameState.isInMenu ? MainMenuTheme : GameplayTheme, 1f).Forget();
		}

		public void Deactivate()
		{
			_gameState.onMenuChanged -= OnMenuChanged;
			_audio.StopMusicAsync(0.2f).Forget();
		}

		private async UniTaskVoid PlayDeferred(string theme, float fade)
		{
			await UniTask.Yield();
			if (!_alive) return;
			SwitchTo(theme, fade);
		}
		
		private void OnMenuChanged(bool isInMenu)
		{
			SwitchTo(isInMenu ? MainMenuTheme : GameplayTheme, 0.35f);
		}
		
		private void SwitchTo(string theme, float fade)
		{
			if (!_alive) return;
			if (_currentTheme == theme) return;          // не перезапускаем ту же тему
			_currentTheme = theme;
			_audio.PlayMusicAsync(theme, fade).Forget(); // твой сервис сам стопает предыдущее
		}
	}
}