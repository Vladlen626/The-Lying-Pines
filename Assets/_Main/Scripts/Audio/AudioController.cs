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

		public AudioController(IAudioService audio, GameStateModel gameState)
		{
			_audio = audio;
			_gameState = gameState;
		}

		public void Activate()
		{
			_gameState.onMenuChanged += OnMenuChangedHandler;
			// стартуем с меню-темы, если мы сейчас в меню
			if (_gameState.isInMenu)
				_audio.PlayMusicAsync(MainMenuTheme, 0.4f).Forget();
			else
				_audio.PlayMusicAsync(GameplayTheme, 0.4f).Forget();
		}

		public void Deactivate()
		{
			_gameState.onMenuChanged -= OnMenuChangedHandler;
			_audio.StopMusicAsync(0.2f).Forget();
		}


		private void OnMenuChangedHandler(bool isInMenu)
		{
			if (isInMenu)
				_audio.PlayMusicAsync(MainMenuTheme, 0.4f).Forget();
			else
				_audio.PlayMusicAsync(GameplayTheme, 0.4f).Forget();
		}
	}
}