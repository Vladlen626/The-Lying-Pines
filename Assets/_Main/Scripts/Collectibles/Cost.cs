namespace _Main.Scripts.Collectibles
{
	public readonly struct Cost
	{
		public readonly CollectibleKind Kind;
		public readonly int Amount;
		public Cost(CollectibleKind kind, int amount)
		{
			Kind = kind; Amount = amount < 0 ? 0 : amount;
		}
	}
}