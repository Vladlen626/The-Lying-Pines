using System.Threading;
using Cysharp.Threading.Tasks;

namespace PlatformCore.Services.UI
{
	public interface IUIService
	{
		UniTask<T> ShowAsync<T>(float duration = 0) where T : BaseUIElement;
		UniTask HideAsync<T>(float duration = 0) where T : BaseUIElement;

		void Hide<T>() where T : BaseUIElement;

		T Get<T>() where T : BaseUIElement;

		bool IsShowed<T>() where T : BaseUIElement;

		void Unload<T>() where T : BaseUIElement;

		UniTask PreloadAsync<T>() where T : BaseUIElement;
	}
}