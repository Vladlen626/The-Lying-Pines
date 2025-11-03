using System;
using Cysharp.Threading.Tasks;

namespace PlatformCore.Services.UI.SplashScreen
{
	public class SplashScreenService : BaseService, ISplashScreenService
	{
		private readonly IUIService _uiService;

		public bool IsTransitioning { get; private set; }

		public SplashScreenService(IUIService uiService)
		{
			_uiService = uiService;
		}

		protected override UniTask InitializeServiceAsync()
		{
			return _uiService.PreloadAsync<SplashScreenElement>();
		}

		public async UniTask FadeInAsync(float duration = 1f)
		{
			IsTransitioning = true;
			await _uiService.ShowAsync<SplashScreenElement>(duration);
			IsTransitioning = false;
		}

		public async UniTask FadeOutAsync(float duration = 1f)
		{
			IsTransitioning = true;
			await _uiService.HideAsync<SplashScreenElement>(duration);
			IsTransitioning = false;
		}

		public async UniTask ShowSplashAsync(float duration = 2f, float fadeIn = 0.5f, float fadeOut = 0.5f)
		{
			await FadeInAsync(fadeIn);
			await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: ServiceToken);
			await FadeOutAsync(fadeOut);
		}
	}
}