using System;

namespace _Main.Scripts.Collectibles
{
	//todo: надо убрать после джема
	public static class Signals
	{
		public static event Action<int> CrumbCollected;
		public static void RaiseCrumbCollected(int amount) => CrumbCollected?.Invoke(amount);
	}
}