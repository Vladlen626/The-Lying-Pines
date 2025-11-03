using System.Collections.Generic;
using System.Linq;

namespace _Main.Scripts.Collectibles
{
	public class PurchaseRequirements
	{
		public IReadOnlyList<Cost> Costs { get; }

		public PurchaseRequirements(params Cost[] costs)
		{
			Costs = costs ?? System.Array.Empty<Cost>();
		}

		public string ToShortText() =>
			Costs.Count == 0
				? "Free"
				: string.Join(", ", Costs.Select(c => $"{c.Kind}: {c.Amount}"));
	}
}