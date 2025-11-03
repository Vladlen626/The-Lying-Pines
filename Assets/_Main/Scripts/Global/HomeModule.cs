using _Main.Scripts.Core.Services;
using _Main.Scripts.Home;
using _Main.Scripts.Inventory;
using Cysharp.Threading.Tasks;
using PlatformCore.Core;
using PlatformCore.Infrastructure.Lifecycle;

namespace _Main.Scripts.Collectibles
{
	public static class HomeModule
	{
		public static async UniTask BindFromContext(
			LifecycleManager lifecycle,
			ServiceLocator services,
			SceneContext ctx,
			int defaultCrumbsCost = 50)
		{
			var inv = services.Get<IInventoryService>();
			var input = services.Get<IInputService>();

			if (ctx == null || ctx.Homes == null) return;

			foreach (var slot in ctx.Homes)
			{
				if (!slot.Home) continue;

				var cost = slot.CrumbsCost > 0 ? slot.CrumbsCost : defaultCrumbsCost;
				var req = new PurchaseRequirements(new Cost(CollectibleKind.Crumb, cost));

				// JAM-версия: требования лежат в модели
				var model = new HomeModel(req);

				var homeCtrl = new HomeController(model, slot.Home);
				await lifecycle.RegisterAsync(homeCtrl);

				if (slot.Builder)
				{
					var roachCtrl = new CockroachController(slot.Builder, model, inv, input);
					await lifecycle.RegisterAsync(roachCtrl);
				}
			}
		}
	}
}