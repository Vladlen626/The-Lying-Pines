using PlatformCore.Services.UI;
using TMPro;
using UnityEngine;

namespace PlatformCore.UIFramework.Runtime.Bindings
{
	public class FloatTextBinding : MonoBehaviour
	{
		[Header("Target")] [SerializeField] private BaseUIElement _targetScreen;
		[SerializeField] private string _propertyName;

		[Header("Text Settings")] [SerializeField]
		private TMP_Text _text;

		[SerializeField] private string _format = "F1";
		[SerializeField] private string _prefix = "";
		[SerializeField] private string _suffix = "";

		private BindableFloat _targetProperty;

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

		private BindableFloat GetBindableProperty()
		{
			var field = _targetScreen.GetType().GetField(_propertyName,
				System.Reflection.BindingFlags.NonPublic |
				System.Reflection.BindingFlags.Instance);

			return field?.GetValue(_targetScreen) as BindableFloat;
		}

		public void OnValueChanged(float value)
		{
			if (_text == null) return;

			var formattedValue = value.ToString(_format);
			_text.text = _prefix + formattedValue + _suffix;
		}
	}
}