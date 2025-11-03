using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _Main.Scripts.Collectibles;

namespace _Main.Scripts.Inventory
{
	public sealed class InventoryService : IInventoryService, IService
	{
		// TODO[JAM] Inventory: сейчас состояние живёт в памяти без сохранения.

		// PLAN: после джема добавить Save/Load снапшота и транзакции с откатом (если появятся внешние ошибки при оплате).
		public event Action<CollectibleKind, int, int> Changed;

		private readonly Dictionary<CollectibleKind, int> _counts = new()
		{
			{ CollectibleKind.Crumb, 0 },
			{ CollectibleKind.Croissant, 0 },
			{ CollectibleKind.Baguette, 0 },
		};

		public void Add(CollectibleKind kind, int amount)
		{
			if (amount == 0) return;
			_counts[kind] = Math.Max(0, _counts[kind] + amount);
			Changed?.Invoke(kind, _counts[kind], amount);
		}

		public bool TrySpend(CollectibleKind kind, int amount)
		{
			if (amount <= 0)
			{
				return true;
			}

			var current = _counts[kind];
			if (current < amount)
			{
				return false;
			}

			var newValue = current - amount;
			_counts[kind] = newValue;

			Changed?.Invoke(kind, newValue, -amount);
			return true;
		}

		public bool TrySpend(IReadOnlyList<(CollectibleKind kind, int amount)> costs)
		{
			foreach (var (kind, value) in costs)
			{
				if (value > 0 && _counts[kind] < value)
				{
					return false;
				}
			}

			foreach (var (kind, value) in costs)
			{
				if (value <= 0) continue;

				var newValue = _counts[kind] - value;
				_counts[kind] = newValue;
				Changed?.Invoke(kind, newValue, -value);
			}

			return true;
		}


		public int Get(CollectibleKind kind) => _counts[kind];

		public int Total =>
			_counts[CollectibleKind.Crumb] + _counts[CollectibleKind.Croissant] +
			_counts[CollectibleKind.Baguette];

		public void Dispose()
		{
			Changed = null;
		}
	}
}