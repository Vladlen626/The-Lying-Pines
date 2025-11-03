using Cysharp.Threading.Tasks;
using UnityEngine;

namespace PlatformCore.Services
{
	public interface IResourceService : IService
	{
		UniTask<T>  LoadAsync<T>(string path) where T : Object;
		void Unload(Object obj);
		UniTask UnloadUnusedAssetsAsync();
	}
}