using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using PlatformCore.Services.UI;

namespace PlatformCore.UIFramework.Runtime.Bindings
{
	public class FloatSliderBinding : MonoBehaviour
	{
		[Header("Target")]
		[SerializeField] private BaseUIElement _targetScreen;
		[SerializeField] private string _propertyName;
        
		[Header("Slider Settings")]
		[SerializeField] private Slider _slider;
		[SerializeField] private float _animationDuration = 0.2f;
		[SerializeField] private bool _useAnimation = true;
        
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
			if (_slider == null) return;
            
			if (_useAnimation)
				_slider.DOValue(value, _animationDuration);
			else
				_slider.value = value;
		}
	}
}