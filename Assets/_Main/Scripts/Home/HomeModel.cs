using _Main.Scripts.Collectibles;
using _Main.Scripts.Inventory;

namespace _Main.Scripts.Home
{
	public class HomeModel
	{
		public HomeState State { get; private set; } = HomeState.Broken;
		public PurchaseRequirements Requirements { get; } // сейчас как config внутри модели

		public event System.Action<HomeState> Changed;

		public HomeModel(PurchaseRequirements reqs) => Requirements = reqs;

		public bool TryBuild(IInventoryService inv)
		{
			if (State != HomeState.Broken)
			{
				return false;
			}

			if (!PurchaseUtils.CanAfford(inv, Requirements.Costs))
			{
				return false;
			}

			if (!inv.TrySpend(PurchaseUtils.ToPairs(Requirements.Costs)))
			{
				return false;
			}

			State = HomeState.Built;
			Changed?.Invoke(State);
			return true;
		}

		// TODO[JAM] Requirements сейчас в модели ради скорости.
		// PLAN: после джема вынести в HomeConfig (readonly), в модели оставить только State.
	}
}