using _Main.Scripts.Core;
using _Main.Scripts.Home;
using Cysharp.Threading.Tasks;
using PlatformCore.Core;
using PlatformCore.Infrastructure.Lifecycle;
using PlatformCore.Services.Audio;
using PlatformCore.Services.UI;
using UnityEngine;

namespace _Main.Scripts
{
	public sealed class GameProgressController : IBaseController, IActivatable
	{
		private readonly GameProgressModel _model;
		private readonly HomeModel[] _homes;
		private readonly PlayerView _playerView;
		private readonly IAudioService _audioService;
		private readonly IUIService _uiService;

		public GameProgressController(GameProgressModel model, HomeModel[] homes, PlayerView playerView,
			IUIService uiService, IAudioService audioService)
		{
			_model = model;
			_homes = homes;
			_playerView = playerView;
			_uiService = uiService;
			_audioService = audioService;
		}

		public void Activate()
		{
			foreach (var homeModel in _homes)
			{
				homeModel.Changed += OnHomeChanged;
			}
		}

		public void Deactivate()
		{
			foreach (var homeModel in _homes)
			{
				homeModel.Changed -= OnHomeChanged;
			}
		}

		private void OnHomeChanged(HomeState s)
		{
			if (s != HomeState.Built) return;
			_model.MarkBuilt();
			_audioService.PlaySound(AudioEvents.HomeBuild);
			if (_model.Completed)
			{
				_audioService.PlaySound(AudioEvents.Congratulations);
				ShowCongratulations().Forget();
				_playerView.ShowCigar();
			}
		}

		private async UniTask ShowCongratulations()
		{
			await _uiService.ShowAsync<UICongratulations>(0.15f);
			await UniTask.Delay(3000);
			await _uiService.HideAsync<UICongratulations>(0.15f);
		}
	}
}