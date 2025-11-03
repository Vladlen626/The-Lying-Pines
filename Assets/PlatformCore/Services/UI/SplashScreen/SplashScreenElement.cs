using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace PlatformCore.Services.UI.SplashScreen
{
	public class SplashScreenElement : BaseUIElement
	{
		[SerializeField] private CanvasGroup _canvasGroup;

		public override async UniTask OnShowAsync(float duration, CancellationToken ct)
		{
			await _canvasGroup.DOFade(1, duration).AsyncWaitForCompletion();
		}

		public override async UniTask OnHideAsync(float duration, CancellationToken ct)
		{
			await _canvasGroup.DOFade(0, duration).AsyncWaitForCompletion();
		}
	}
}