using System;
using System.Collections.Generic;
using _Main.Scripts.Collectibles;

namespace _Main.Scripts.Inventory
{
	public interface IInventoryService
	{
		event Action<CollectibleKind, int, int> Changed;
		void Add(CollectibleKind kind, int amount);
		bool TrySpend(CollectibleKind kind, int amount);
		bool TrySpend(IReadOnlyList<(CollectibleKind kind, int amount)> costs);
		int Get(CollectibleKind kind);
		int Total { get; }
	}
}