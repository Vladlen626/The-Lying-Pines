using System.Collections.Generic;
using _Main.Scripts.Inventory;

namespace _Main.Scripts.Collectibles
{
	// TODO[JAM]: если понадобится «не только ресурсы» (квесты/флаги) — расширим до IRequirement.Evaluate(ctx); сейчас оставляем просто мультивалюту.
	public static class PurchaseUtils
	{
		public static bool CanAfford(IInventoryService inv, IReadOnlyList<Cost> costs)
		{
			for (int i = 0; i < costs.Count; i++)
			{
				var c = costs[i];
				if (c.Amount > 0 && inv.Get(c.Kind) < c.Amount) return false;
			}

			return true;
		}

		public static List<(CollectibleKind kind, int amount)> ToPairs(IReadOnlyList<Cost> costs)
		{
			var list = new List<(CollectibleKind, int)>(costs.Count);
			for (int i = 0; i < costs.Count; i++)
			{
				list.Add((costs[i].Kind, costs[i].Amount));
			}
			return list;
		}

		public static string MissingText(IInventoryService inv, IReadOnlyList<Cost> costs)
		{
			var parts = new List<string>(costs.Count);
			for (int i = 0; i < costs.Count; i++)
			{
				var c = costs[i];
				int have = inv.Get(c.Kind);
				int need = c.Amount - have;
				if (need > 0) parts.Add($"{c.Kind}: {need}");
			}

			return parts.Count == 0 ? "" : "Not enough: " + string.Join(", ", parts);
		}
	}
}