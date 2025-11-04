// Assets/_Main/Scripts/UI/UICreditsSplash.cs

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PlatformCore.Services.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class UICreditsSplash : BaseUIElement
{
	[Header("Refs")] [SerializeField] private CanvasGroup _group;
	[SerializeField] private TextMeshProUGUI _label;

	private CancellationTokenSource _cts;

	public override void OnShow()
	{
		if (_group)
		{
			_group.alpha = 1f;
			_group.interactable = true;
			_group.blocksRaycasts = true; // нужно, чтобы ловить клик для скипа
		}

		_cts?.Cancel();
		_cts = new CancellationTokenSource();
	}

	public override void OnHide()
	{
		_cts?.Cancel();
		_cts = null;

		if (_group)
		{
			_group.alpha = 0f;
			_group.interactable = false;
			_group.blocksRaycasts = false;
		}
	}
	public void SetText(string t)
	{
		if (_label) _label.text = t;
	}
}