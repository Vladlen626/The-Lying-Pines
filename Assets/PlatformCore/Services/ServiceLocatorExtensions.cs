using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace PlatformCore.Core
{
	public static class ServiceLocatorExtensions
	{
		public static async UniTask InitializeAllAsync(this ServiceLocator locator, CancellationToken ct)
		{
			var asyncServices = locator.All.OfType<IAsyncInitializable>().ToList();
			var syncServices = locator.All.OfType<ISyncInitializable>().ToList();

			// === 1. Параллельный PreInit ===
			await UniTask.WhenAll(asyncServices.Select(s => s.PreInitializeAsync(ct)));

			// === 2. Последовательный PostInit (зависимости уже готовы) ===
			foreach (var svc in asyncServices)
				await svc.PostInitializeAsync(ct);

			// === 3. Синхронные сервисы ===
			foreach (var svc in syncServices)
				svc.Initialize();
		}
	}
}