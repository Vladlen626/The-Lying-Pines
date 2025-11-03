using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PlatformCore.Infrastructure.Lifecycle;
using UnityEngine;

namespace PlatformCore.Core
{
	public abstract class BaseGameRoot : IDisposable
	{
#if UNITY_EDITOR
		public ServiceLocator EditorServices => Services;
#endif
		protected ServiceLocator Services { get; private set; }
		protected LifecycleManager Lifecycle { get; private set; }

		private readonly ApplicationLifetimeService _lifetimeService;
		protected CancellationToken ApplicationCancellationToken => _lifetimeService.ApplicationLifetime;

		protected BaseGameRoot()
		{
			Services = new ServiceLocator();
			Lifecycle = new LifecycleManager();
			_lifetimeService = new ApplicationLifetimeService();
		}

		public async UniTask LaunchAsync(GameContext context)
		{
			try
			{
				RegisterServices(context);
				await InitializeServicesAsync();
				await LaunchGameAsync(context);
			}
			catch (OperationCanceledException)
			{
				Debug.LogWarning("[GameRoot] Launch cancelled (application closing)");
			}
			catch (Exception ex)
			{
				Debug.LogError($"[GameRoot] Launch failed: {ex}");
				throw;
			}
		}

		protected virtual async UniTask InitializeServicesAsync()
		{
			await Services.InitializeAllAsync(ApplicationCancellationToken);
		}

		protected abstract void RegisterServices(GameContext gameContext);

		protected abstract UniTask LaunchGameAsync(GameContext gameContext);


		public void OnUpdate(float delta)
		{
			Lifecycle.Update(delta);
		}

		public void OnFixedUpdate(float delta)
		{
			Lifecycle.FixedUpdate(delta);
		}

		public void OnLateUpdate(float delta)
		{
			Lifecycle.LateUpdate(delta);
		}

		public void Dispose()
		{
			try
			{
				_lifetimeService?.Dispose();
				Lifecycle?.Dispose();
				Services?.Dispose();

				Debug.Log("[GameRoot] Disposed successfully");
			}
			catch (Exception ex)
			{
				Debug.LogError($"[GameRoot] Dispose error: {ex}");
			}
		}
	}
}