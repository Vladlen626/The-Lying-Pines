using PlatformCore.UIFramework.Runtime.Bindables;
using TMPro;
using UnityEngine;

namespace PlatformCore.Services.UI.Bindings
{
	public class IntTextBinding : MonoBehaviour
	{
		[Header("Target")] [SerializeField] private BaseUIElement _targetScreen;
		[SerializeField] private string _propertyName;

		[Header("Settings")] [SerializeField] private TMP_Text _text;
		[SerializeField] private string _format = "";

		private BindableInt _targetProperty;

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

		private BindableInt GetBindableProperty()
		{
			var field = _targetScreen.GetType().GetField(_propertyName,
				System.Reflection.BindingFlags.NonPublic |
				System.Reflection.BindingFlags.Instance);

			return field?.GetValue(_targetScreen) as BindableInt;
		}

		public void OnValueChanged(int value)
		{
			if (_text == null) return;

			_text.text = string.IsNullOrEmpty(_format) ? value.ToString() : value.ToString(_format);
		}
	}
}