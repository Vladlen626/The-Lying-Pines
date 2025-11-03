using _Main.Scripts.Collectibles;
using _Main.Scripts.Inventory;
using Cysharp.Threading.Tasks;
using PlatformCore.Core;
using PlatformCore.Infrastructure.Lifecycle;

namespace _Main.Scripts.Home
{
	public class HomeController : IBaseController, IActivatable
	{
		private readonly HomeModel _model;
		private readonly HomeView _view;

		public HomeController(HomeModel model, HomeView view)
		{
			_model = model;
			_view = view;
		}

		public void Activate()
		{
			_model.Changed += OnChanged;
			Apply();
		}

		public void Deactivate()
		{
			_model.Changed -= OnChanged;
		}

		private void OnChanged(HomeState s) => Apply();
		private void Apply() => _view.SetState(_model.State);
	}
}