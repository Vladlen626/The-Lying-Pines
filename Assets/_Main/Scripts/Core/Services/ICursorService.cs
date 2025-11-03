using System;

namespace _Main.Scripts.Core.Services
{
	public interface ICursorService : IService
	{
		public event Action OnCursorStateChanged;
		void LockCursor();
		void UnlockCursor();
		bool IsCursorLocked { get; }
	}
}