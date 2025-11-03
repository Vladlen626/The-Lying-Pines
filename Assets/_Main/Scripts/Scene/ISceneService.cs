using System.Threading;
using Cysharp.Threading.Tasks;
using PlatformCore.Core;
using UnityEngine.SceneManagement;

namespace PlatformCore.Services
{
	public interface ISceneService
	{
		// _____________ Base _____________

		UniTask LoadSceneAsync(string sceneName, CancellationToken ct = default);
		UniTask LoadGlobalSceneAsync(string sceneName, CancellationToken ct = default);
		UniTask ReloadCurrentSceneAsync(CancellationToken ct = default);
		string GetActiveSceneName();
		bool IsSceneLoaded(string sceneName);
		bool TryGetSceneContext(string sceneName, out SceneContext sceneContext);
		
		UniTask ActivatePreloadedScene(string sceneName);
		UniTask PreloadSceneAsync(string sceneName, CancellationToken ct = default);
		UniTask UnloadSceneAsync(string sceneName, CancellationToken ct = default);
	}
}