using UnityEngine;

namespace PlatformCore.Services.UI.Bindings
{
	public class BoolActiveBinding : MonoBehaviour
	{
		[Header("Target")] [SerializeField] private BaseUIElement _targetScreen;

		[SerializeField] private string _propertyName;

		[Header("Settings")] [SerializeField] private GameObject _target;

		[SerializeField] private bool _inverse;

		private BindableBool _targetProperty;

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

		private BindableBool GetBindableProperty()
		{
			var field = _targetScreen.GetType().GetField(_propertyName,
				System.Reflection.BindingFlags.NonPublic |
				System.Reflection.BindingFlags.Instance);

			return field?.GetValue(_targetScreen) as BindableBool;
		}

		public void OnValueChanged(bool value)
		{
			if (_target == null) return;
			_target.SetActive(_inverse ? !value : value);
		}
	}
}