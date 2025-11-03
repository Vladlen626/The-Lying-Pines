using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PlatformCore.Services.UI
{
	public class UIBaseService : BaseService, IUIService
	{
		private readonly ILoggerService _loggerService;
		private readonly IResourceService _resourceService;

		private readonly Transform _staticCanvas;
		private readonly Transform _dynamicCanvas;

		private readonly Dictionary<Type, BaseUIElement> _loadedWindows = new Dictionary<Type, BaseUIElement>();

		public UIBaseService(ILoggerService loggerService,
			IResourceService resourceService,
			Transform staticCanvas,
			Transform dynamicCanvas)
		{
			_loggerService = loggerService;
			_resourceService = resourceService;
			_staticCanvas = staticCanvas;
			_dynamicCanvas = dynamicCanvas;
		}

		protected override UniTask InitializeServiceAsync()
		{
			_loggerService?.Log("[UIService] Initialized successfully");
			return UniTask.CompletedTask;
		}
		
		public async UniTask<T> ShowAsync<T>(float duration) where T : BaseUIElement
		{
			var type = typeof(T);

			if (_loadedWindows.TryGetValue(type, out var existingWindow))
			{
				existingWindow.gameObject.SetActive(true);
				await existingWindow.OnShowAsync(duration, ServiceToken);

				_loggerService?.Log($"[UIService] Showed cached window: {type.Name}");
				return existingWindow as T;
			}

			var window = await LoadWindowAsync<T>();
			if (window != null)
			{
				window.gameObject.SetActive(true);
				await window.OnShowAsync(duration, ServiceToken);
				_loggerService?.Log($"[UIService] Showed new window: {type.Name}");
			}

			return window;
		}

		public async UniTask HideAsync<T>(float duration) where T : BaseUIElement
		{
			var type = typeof(T);

			if (_loadedWindows.TryGetValue(type, out var window))
			{
				await window.OnHideAsync(duration, ServiceToken);
				window.gameObject.SetActive(false);

				_loggerService?.Log($"[UIService] Hidden window with animation: {type.Name}");
			}
			else
			{
				_loggerService?.LogWarning($"[UIService] Cannot hide, window not loaded: {type.Name}");
			}
		}

		public void Hide<T>() where T : BaseUIElement
		{
			var type = typeof(T);

			if (_loadedWindows.TryGetValue(type, out var window))
			{
				window.OnHide();
				window.gameObject.SetActive(false);

				_loggerService?.Log($"[UIService] Hidden window instantly: {type.Name}");
			}
			else
			{
				_loggerService?.LogWarning($"[UIService] Cannot hide, window not loaded: {type.Name}");
			}
		}

		public T Get<T>() where T : BaseUIElement
		{
			_loadedWindows.TryGetValue(typeof(T), out var window);
			return window as T;
		}

		public bool IsShowed<T>() where T : BaseUIElement
		{
			if (_loadedWindows.TryGetValue(typeof(T), out var window))
			{
				return window.gameObject.activeSelf;
			}

			return false;
		}

		public async UniTask PreloadAsync<T>() where T : BaseUIElement
		{
			var type = typeof(T);

			if (_loadedWindows.ContainsKey(type))
			{
				_loggerService?.Log($"[UIService] Window already preloaded: {type.Name}");
				return;
			}

			var window = await LoadWindowAsync<T>().AttachExternalCancellation(ServiceToken);
			if (window != null)
			{
				window.gameObject.SetActive(false);
				_loggerService?.Log($"[UIService] Window preloaded: {type.Name}");
			}
		}

		public void Unload<T>() where T : BaseUIElement
		{
			var type = typeof(T);

			if (_loadedWindows.TryGetValue(type, out var window))
			{
				window.OnHide();
				Object.Destroy(window.gameObject);
				_loadedWindows.Remove(type);

				_loggerService?.Log($"[UIService] Window unloaded: {type.Name}");
			}
		}

		private async UniTask<T> LoadWindowAsync<T>() where T : BaseUIElement
		{
			var type = typeof(T);

			var resourcePath = $"UI/{type.Name}";
			_loggerService?.Log($"[UIService] Loading window: {resourcePath}");

			var prefab = await _resourceService.LoadAsync<GameObject>(resourcePath);
			if (prefab == null)
			{
				_loggerService?.LogError($"[UIService] Failed to load prefab: {resourcePath}");
				return null;
			}

			var prefabComponent = prefab.GetComponent<T>();
			if (prefabComponent == null)
			{
				_loggerService?.LogError($"[UIService] Component {type.Name} not found on prefab");
				return null;
			}

			var targetCanvas = prefabComponent.CanvasType == UICanvasType.Static
				? _staticCanvas
				: _dynamicCanvas;

			var instance = Object.Instantiate(prefab, targetCanvas.transform);
			var component = instance.GetComponent<T>();

			if (component == null)
			{
				_loggerService?.LogError($"[UIService] Component lost during instantiation: {type.Name}");
				Object.Destroy(instance);
				return null;
			}

			_loadedWindows[type] = component;

			_loggerService?.Log($"[UIService] Window loaded to {prefabComponent.CanvasType} Canvas: {type.Name}");
			return component;
		}
		
		protected override void DisposeService()
		{
			_loggerService?.Log("[UIService] Disposing all windows...");

			foreach (var window in _loadedWindows.Values)
			{
				if (!window)
				{
					window.OnHide();
					Object.Destroy(window);
				}
			}

			_loadedWindows.Clear();
			_loggerService?.Log("[UIService] UIService disposed");
		}
	}
}