using System;
using PlatformCore.Services.UI;
using UnityEngine;

public class UIPauseMenu : BaseUIElement
{
	public event Action OnResumeClicked;
	public event Action OnSettingsClicked;
	public event Action OnQuitClicked;
	
	[SerializeField]
	private CanvasGroup _canvasGroup;
	public override void OnShow()
	{
		_canvasGroup.alpha = 1;
	}

	public override void OnHide()
	{
		if (_canvasGroup)
		{
			_canvasGroup.alpha = 0;
		}
	}

	public void OnResumeClick()
	{
		OnResumeClicked?.Invoke();
	}

	public void OnSettingsClick()
	{
		OnSettingsClicked?.Invoke();
	}

	public void OnQuitClick()
	{
		OnQuitClicked?.Invoke();
	}
}