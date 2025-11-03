using System;

namespace _Main.Scripts
{
	public class GameStateModel
	{
		public event Action GameStateChanged;
		public bool isPaused;
	}

	public class GameProgressModel
	{
		public int BuiltHomes { get; private set; }
		public int TargetHomes { get; }
		public bool Completed => BuiltHomes >= TargetHomes;
		public event System.Action<int> BuiltChanged;

		public GameProgressModel(int targetHomes)
		{
			TargetHomes = targetHomes;
		}

		public void MarkBuilt()
		{
			BuiltHomes++;
			BuiltChanged?.Invoke(BuiltHomes);
		}
	}
}