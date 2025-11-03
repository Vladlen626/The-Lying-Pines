using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace PlatformCore.Services.UI
{
	public abstract class BaseUIElement : MonoBehaviour
	{
		[Header("Canvas Performance Settings")] [SerializeField]
		private UICanvasType _canvasType = UICanvasType.Static;

		public UICanvasType CanvasType => _canvasType;

		public virtual UniTask OnShowAsync(float duration, CancellationToken ct)
		{
			OnShow();
			return UniTask.CompletedTask;
		}

		public virtual UniTask OnHideAsync(float duration, CancellationToken ct)
		{
			OnHide();
			return UniTask.CompletedTask;
		}

		public virtual void OnShow()
		{
		}

		public virtual void OnHide()
		{
		}
	}
}