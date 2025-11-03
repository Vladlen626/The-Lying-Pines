using PlatformCore.Services.UI;
using TMPro;
using UnityEngine;

namespace PlatformCore.UIFramework.Runtime.Bindings
{
	public class StringTextBinding : MonoBehaviour
	{
		[Header("Target")] [SerializeField] private BaseUIElement _targetScreen;
		[SerializeField] private string _propertyName;

		[Header("Text Settings")] [SerializeField]
		private TMP_Text _text;

		[SerializeField] private string _prefix = "";
		[SerializeField] private string _suffix = "";
		[SerializeField] private bool _toUpperCase = false;
		[SerializeField] private bool _toLowerCase = false;

		private BindableString _targetProperty;

		private void Start()
		{
			if (_targetScreen != null && !string.IsNullOrEmpty(_propertyName))
			{
				_targetProperty = GetBindableProperty();
				_targetProperty?.RegisterBinding(this);
			}
		}

		private void OnDestroy()
		{
			_targetProperty?.UnregisterBinding(this);
		}

		private BindableString GetBindableProperty()
		{
			var field = _targetScreen.GetType().GetField(_propertyName,
				System.Reflection.BindingFlags.NonPublic |
				System.Reflection.BindingFlags.Instance);

			return field?.GetValue(_targetScreen) as BindableString;
		}

		public void OnValueChanged(string value)
		{
			if (_text == null) return;

			// Применяем форматирование
			if (_toUpperCase) value = value.ToUpperInvariant();
			if (_toLowerCase) value = value.ToLowerInvariant();

			_text.text = _prefix + value + _suffix;
		}
	}
}