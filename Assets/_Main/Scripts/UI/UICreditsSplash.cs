// Assets/_Main/Scripts/UI/UICreditsSplash.cs

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PlatformCore.Services.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UICreditsSplash : BaseUIElement
{
	[Header("Refs")] [SerializeField] private CanvasGroup _group;
	[SerializeField] private TextMeshProUGUI _label;
	[SerializeField] private Button _continueButton;

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
	
	public async UniTask WaitForClickAsync()
	{
		var tcs = new UniTaskCompletionSource();

		void Click()
		{
			tcs.TrySetResult();
		}

		_continueButton.onClick.AddListener(Click);

		await tcs.Task;

		_continueButton.onClick.RemoveListener(Click);
	}
}