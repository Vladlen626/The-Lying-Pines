using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PlatformCore.Services.UI
{
	public sealed class UIBaseService : BaseAsyncService, IUIService
	{
		private readonly ILoggerService _logger;
		private readonly IResourceService _resources;

		private readonly Transform _staticCanvas;
		private readonly Transform _dynamicCanvas;

		private readonly Dictionary<Type, BaseUIElement> _windows = new();

		// локальный токен для отмены
		private CancellationTokenSource _cts;
		private CancellationToken _token;

		public UIBaseService(ILoggerService logger, IResourceService resources,
			Transform staticCanvas, Transform dynamicCanvas)
		{
			_logger = logger;
			_resources = resources;
			_staticCanvas = staticCanvas;
			_dynamicCanvas = dynamicCanvas;
		}

		protected override UniTask OnPreInitializeAsync(CancellationToken ct)
		{
			_cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			_token = _cts.Token;
			_logger?.Log("[UIService] Initialized");
			return UniTask.CompletedTask;
		}

		// === SHOW ===
		public async UniTask<T> ShowAsync<T>(float duration) where T : BaseUIElement
		{
			var type = typeof(T);
			if (!_windows.TryGetValue(type, out var window))
				window = await LoadAsync<T>(_token);

			if (window == null)
			{
				_logger?.LogError($"[UIService] Failed to show: {type.Name}");
				return null;
			}

			window.gameObject.SetActive(true);
			await window.OnShowAsync(duration, _token);
			return (T)window;
		}

		// === HIDE ===
		public async UniTask HideAsync<T>(float duration) where T : BaseUIElement
		{
			if (!_windows.TryGetValue(typeof(T), out var window) || window == null)
				return;

			await window.OnHideAsync(duration, _token);
			window.gameObject.SetActive(false);
		}

		public void Hide<T>() where T : BaseUIElement
		{
			if (_windows.TryGetValue(typeof(T), out var window) && window != null)
			{
				window.OnHide();
				window.gameObject.SetActive(false);
			}
		}

		// === STATE ===
		public T Get<T>() where T : BaseUIElement
		{
			return _windows.TryGetValue(typeof(T), out var window) ? window as T : null;
		}

		public bool IsShowed<T>() where T : BaseUIElement
		{
			return _windows.TryGetValue(typeof(T), out var window) && window.gameObject.activeSelf;
		}

		// === PRELOAD / UNLOAD ===
		public async UniTask PreloadAsync<T>() where T : BaseUIElement
		{
			var type = typeof(T);
			if (_windows.ContainsKey(type))
				return;

			await LoadAsync<T>(_token);
		}

		public void Unload<T>() where T : BaseUIElement
		{
			var type = typeof(T);
			if (!_windows.TryGetValue(type, out var window))
				return;

			window.OnHide();
			Object.Destroy(window.gameObject);
			_windows.Remove(type);
			_logger?.Log($"[UIService] Unloaded window: {type.Name}");
		}

		// === INTERNAL LOADING ===
		private async UniTask<T> LoadAsync<T>(CancellationToken ct) where T : BaseUIElement
		{
			var type = typeof(T);
			var path = $"UI/{type.Name}";
			_logger?.Log($"[UIService] Loading window: {path}");

			var prefab = await _resources.LoadAsync<GameObject>(path);
			if (prefab == null)
			{
				_logger?.LogError($"[UIService] Missing prefab: {path}");
				return null;
			}

			var prefabComponent = prefab.GetComponent<T>();
			if (prefabComponent == null)
			{
				_logger?.LogError($"[UIService] Missing component {type.Name} on prefab");
				return null;
			}

			var target = prefabComponent.CanvasType == UICanvasType.Static ? _staticCanvas : _dynamicCanvas;
			var instance = Object.Instantiate(prefab, target);
			var component = instance.GetComponent<T>();

			if (component == null)
			{
				Object.Destroy(instance);
				_logger?.LogError($"[UIService] Component lost after instantiate: {type.Name}");
				return null;
			}

			component.gameObject.SetActive(false);
			_windows[type] = component;
			_logger?.Log($"[UIService] Loaded {type.Name} to {prefabComponent.CanvasType} Canvas");
			return component;
		}

		// === CLEANUP ===
		public override void Dispose()
		{
			foreach (var w in _windows.Values)
				if (w)
					Object.Destroy(w.gameObject);

			_windows.Clear();
			_cts?.Cancel();
			_cts?.Dispose();
			_logger?.Log("[UIService] Disposed");
		}
	}
}