using System;
using System.Collections.Generic;
using PlatformCore.UIFramework.Runtime.Bindings;
using UnityEngine;

namespace PlatformCore.UIFramework.Runtime.Bindings
{
	[Serializable]
	public class BindableString
	{
		[SerializeField] private string _debugValue = "";
		[SerializeField] private string _value = "";

		// Read-only список — показывает кто на нас подписался
		[SerializeReference] private List<UnityEngine.Object> _bindings = new();

		public string Value
		{
			get => _value;
			set
			{
				if (_value == value) return;

				_value = value ?? "";
				OnValueChanged?.Invoke(_value);
				UpdateBindings();
			}
		}

		public event Action<string> OnValueChanged;

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
				if (b is StringTextBinding stringTextBinding)
					stringTextBinding.OnValueChanged(_value);
				// Можно добавлять другие типы string биндингов
			}
		}

		[ContextMenu("Apply Debug Value")]
		public void ApplyDebugValue() => Value = _debugValue;
	}
}