using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace PlatformCore.Services
{
	public class LoggerService : ILoggerService
	{
		private const string LogPrefix = "[GAME]";

		public UniTask InitializeAsync(CancellationToken ct)
		{
			Debug.Log($"{LogPrefix} Logger initialized");
			return UniTask.CompletedTask;
		}

		public void Log(string message)
		{
			Debug.Log($"{LogPrefix} {message}");
		}

		public void LogWarning(string message)
		{
			Debug.LogWarning($"{LogPrefix} {message}");
		}

		public void LogError(string message)
		{
			Debug.LogError($"{LogPrefix} {message}");
		}

		public void LogException(Exception exception)
		{
			Debug.LogException(exception);
		}

#if UNITY_EDITOR || DEVELOPMENT_BUILD
		public void LogDebug(string message)
		{
			Debug.Log($"{LogPrefix} [DEBUG] {message}");
		}
#endif

		public void Dispose()
		{
			Debug.Log($"{LogPrefix} Logger disposed");
		}
	}
}