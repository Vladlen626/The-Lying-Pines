using Cysharp.Threading.Tasks;
using UnityEngine;

namespace PlatformCore.Services
{
	public interface ICameraShakeService : IService
	{
		UniTask ShakeAsync(float intensity, float duration);
		void StopShake();
		bool IsShaking { get; }
	}
}