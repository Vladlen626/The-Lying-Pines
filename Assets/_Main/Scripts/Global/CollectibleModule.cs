// Assets/_Main/Scripts/Collectibles/CollectibleModule.cs
using System.Linq;
using Cysharp.Threading.Tasks;
using PlatformCore.Infrastructure.Lifecycle;
using _Main.Scripts.Inventory;
using _Main.Scripts.Player;
using UnityEngine;

namespace _Main.Scripts.Collectibles
{
	public static class CollectibleModule
	{
		public static async UniTask BindSceneCollectibles(
			LifecycleManager lifecycle,
			IInventoryService inventory,
			PlayerView playerView)
		{
			if (!playerView) return;

			var anchor = new PlayerPickupAnchor(playerView);
			var views = Object.FindObjectsOfType<CollectibleView>(true);

			foreach (var v in views.Where(v => v != null && v.gameObject.activeInHierarchy))
			{
				var c = new CollectibleController(v, inventory, anchor);
				await lifecycle.RegisterAsync(c);
			}
		}
	}
}