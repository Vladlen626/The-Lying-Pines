using System;

namespace _Main.Scripts
{
	using System;

	public class GameStateModel
	{
		// (isInMenu, isPaused)
		public event Action<bool, bool> GameStateChanged;

		public bool isPaused  { get; private set; }
		public bool isInMenu  { get; private set; }

		public void SetPaused(bool v)
		{
			if (isPaused == v) return;
			isPaused = v;
			GameStateChanged?.Invoke(isInMenu, isPaused);
		}

		public void SetInMenu(bool v)
		{
			if (isInMenu == v) return;
			isInMenu = v;
			GameStateChanged?.Invoke(isInMenu, isPaused);
		}
	}


	public class GameProgressModel
	{
		public int BuiltHomes { get; private set; }
		public int TargetHomes { get; }
		public bool Completed => BuiltHomes >= TargetHomes;
		public event Action<int> BuiltChanged;

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