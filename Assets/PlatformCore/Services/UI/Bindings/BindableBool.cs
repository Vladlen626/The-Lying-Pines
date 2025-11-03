using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlatformCore.Services.UI.Bindings
{
	[Serializable]
	public class BindableBool
	{
		public event Action<bool> OnValueChanged;

		[SerializeField] private bool _debugValue;
		private bool _value;

		[SerializeReference] private List<UnityEngine.Object> _bindings = new();

		public bool Value
		{
			get => _value;
			set
			{
				if (_value != value)
				{
					_value = value;
					OnValueChanged?.Invoke(value);
					UpdateBindings();
				}
			}
		}

		public void RegisterBinding(UnityEngine.Object binding)
		{
			if (!_bindings.Contains(binding))
				_bindings.Add(binding);
		}

		public void UnregisterBinding(UnityEngine.Object binding)
		{
			_bindings.Remove(binding);
		}

		private void UpdateBindings()
		{
			foreach (var b in _bindings)
			{
				if (b is BoolActiveBinding boolBinding)
				{
					boolBinding.OnValueChanged(_value);
				}
			}
		}

		[ContextMenu("Apply Debug Value")]
		public void ApplyDebugValue() => Value = _debugValue;
	}
}