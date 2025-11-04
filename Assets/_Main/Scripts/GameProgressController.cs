using _Main.Scripts.Home;
using PlatformCore.Core;
using PlatformCore.Infrastructure.Lifecycle;
using UnityEngine;

namespace _Main.Scripts
{
	public sealed class GameProgressController : IBaseController, IActivatable
	{
		private readonly GameProgressModel _model;
		private readonly HomeModel[] _homes;
		private readonly PlayerView _playerView;

		public GameProgressController(GameProgressModel model, HomeModel[] homes, PlayerView playerView)
		{
			_model = model;
			_homes = homes;
			_playerView = playerView;
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
			if (_model.Completed)
			{
				_playerView.ShowCigar();
			}
		}
	}
}