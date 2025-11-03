using System;
using System.Threading;
using _Main.Scripts.Core.Services;
using Cysharp.Threading.Tasks;
using PlatformCore.Services.UI;
using Unity.VisualScripting;
using UnityEngine;

namespace PlatformCore.Infrastructure
{
	public class CursorService : BaseService, ICursorService
	{
		public event Action OnCursorStateChanged;
		private readonly IUIService _uiService;
		public bool IsCursorLocked => Cursor.lockState == CursorLockMode.Locked;

		public CursorService(IUIService uiService)
		{
			_uiService = uiService;
		}

		protected override UniTask InitializeServiceAsync()
		{
			LockCursor();
			_uiService.PreloadAsync<UIPlayerCrosshair>();
			return UniTask.CompletedTask;
		}

		public void LockCursor()
		{
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
			OnCursorStateChanged?.Invoke();
		}

		public void UnlockCursor()
		{
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
			OnCursorStateChanged?.Invoke();
		}
	}
}