using System;
using System.Collections.Generic;
using PlatformCore.Services.UI.Bindings;
using UnityEngine;

namespace PlatformCore.UIFramework.Runtime.Bindables
{
	[Serializable]
	public class BindableInt
	{
		[SerializeField] private int _debugValue;
		[SerializeField] private int _value;

		// Read-only список — показывает кто на нас подписался
		[SerializeReference] private List<UnityEngine.Object> _bindings = new();

		public int Value
		{
			get => _value;
			set
			{
				if (_value == value) return;

				_value = value;
				OnValueChanged?.Invoke(value);
				UpdateBindings();
			}
		}

		public event Action<int> OnValueChanged;

		// Метод для регистрации биндинга (вызывается автоматически)
		public void RegisterBinding(UnityEngine.Object binding)
		{
			if (!_bindings.Contains(binding))
				_bindings.Add(binding);
		}

		// Метод для отмены регистрации
		public void UnregisterBinding(UnityEngine.Object binding)
		{
			_bindings.Remove(binding);
		}

		private void UpdateBindings()
		{
			foreach (var b in _bindings)
			{
				if (b is IntTextBinding intTextBinding)
					intTextBinding.OnValueChanged(_value);
			}
		}

		[ContextMenu("Apply Debug Value")]
		public void ApplyDebugValue() => Value = _debugValue;
	}
}