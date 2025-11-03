using System;
using System.Collections.Generic;

namespace PlatformCore.Core
{
	public class ServiceLocator
	{
		private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

		public void Register<T>(T service) where T : class, IService
		{
			if (service == null)
			{
				throw new ArgumentNullException(nameof(service));
			}

			var serviceType = typeof(T);

			if (!_services.TryAdd(serviceType, service))
			{
				UnityEngine.Debug.LogWarning($"Service {serviceType.Name} already registered. Skipping.");
			}
		}

		public T Get<T>() where T : class, IService
		{
			var serviceType = typeof(T);

			if (!_services.TryGetValue(serviceType, out var service))
			{
				throw new InvalidOperationException(
					$"Service {serviceType.Name} not registered. " +
					$"Call Register<{serviceType.Name}>() in GameRoot first.");
			}

			return service as T;
		}

		public T TryGet<T>() where T : class, IService
		{
			var serviceType = typeof(T);
			return _services.TryGetValue(serviceType, out var service) ? service as T : null;
		}

		public bool Has<T>() where T : class, IService
		{
			return _services.ContainsKey(typeof(T));
		}

		public IReadOnlyDictionary<Type, object> GetAll()
		{
			return _services;
		}
		
		public void DisposeAll()
		{
			foreach (var service in _services.Values)
			{
				if (service is IDisposable disposable)
				{
					disposable.Dispose();
				}
			}
			_services.Clear();
		}
	}
}