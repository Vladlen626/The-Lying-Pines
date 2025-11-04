using DG.Tweening;
using PlatformCore.Services.UI;
using TMPro;
using UnityEngine;

public class UIPlayerHud : BaseUIElement
{
	[SerializeField] private TextMeshProUGUI _textMeshProUGUI;
	[SerializeField] private float _animDuration = 0.3f;
	[SerializeField] private Ease _ease = Ease.OutQuad;

	private int _currentValue;
	private Tween _tween;

	public void SetCrumbsCount(int newValue)
	{
		_tween?.Kill();

		_tween = DOVirtual.Int(_currentValue, newValue, _animDuration, value =>
			{
				_currentValue = value;
				_textMeshProUGUI.text = value.ToString();
			})
			.SetEase(_ease);
	}

	private void OnDisable()
	{
		_tween?.Kill();
	}
}