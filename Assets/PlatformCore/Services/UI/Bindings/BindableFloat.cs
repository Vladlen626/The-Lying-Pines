using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlatformCore.UIFramework.Runtime.Bindings
{
	[Serializable]
	public class BindableFloat
	{
		[SerializeField] private float _debugValue;
		[SerializeField] private float _value;

		// Read-only список — показывает кто на нас подписался
		[SerializeReference] private List<UnityEngine.Object> _bindings = new();

		public float Value
		{
			get => _value;
			set
			{
				if (Mathf.Approximately(_value, value)) return;

				_value = value;
				OnValueChanged?.Invoke(value);
				UpdateBindings();
			}
		}

		public event Action<float> OnValueChanged;

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
				if (b is FloatTextBinding floatTextBinding)
					floatTextBinding.OnValueChanged(_value);
				if (b is FloatSliderBinding floatSliderBinding)
					floatSliderBinding.OnValueChanged(_value);
				// Можно добавлять другие типы float биндингов
			}
		}

		[ContextMenu("Apply Debug Value")]
		public void ApplyDebugValue() => Value = _debugValue;
	}
}