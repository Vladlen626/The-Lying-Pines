// Assets/_Main/Scripts/UI/UICreditsSplash.cs

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PlatformCore.Services.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class UICreditsSplash : BaseUIElement, IPointerClickHandler
{
	[Header("Refs")] [SerializeField] private CanvasGroup _group;
	[SerializeField] private TextMeshProUGUI _label;

	[Header("Content")] [TextArea] [SerializeField]
	private string _text = "created by \"the pair\"";

	[Header("Typing")] [SerializeField, Min(0.05f)]
	private float _typeDuration = 0.9f; // сколько печатать весь текст

	[SerializeField, Min(0f)] private float _postHold = 0.2f; // пауза после конца/скипа
	[SerializeField] private bool _autoplayOnShow = true; // играть сразу при показе

	private CancellationTokenSource _cts;
	private bool _skipRequested;
	private bool _playing;

	public override void OnShow()
	{
		if (_label) _label.text = _text;
		if (_group)
		{
			_group.alpha = 1f;
			_group.interactable = true;
			_group.blocksRaycasts = true; // нужно, чтобы ловить клик для скипа
		}

		_cts?.Cancel();
		_cts = new CancellationTokenSource();

		if (_autoplayOnShow)
			PlayAsync(_cts.Token).Forget();
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

	/// <summary> Запустить анимацию вручную и дождаться её завершения (или скипа). </summary>
	public async UniTask PlayAsync(CancellationToken token = default)
	{
		if (_label == null) return;

		_skipRequested = false;
		_playing = true;

		// подготовка печатания
		_label.text = _text;
		_label.maxVisibleCharacters = 0;

		// реальное число видимых символов (без тегов)
		int total = _label.textInfo.characterCount;
		if (total <= 0)
		{
			await UniTask.Delay(TimeSpan.FromSeconds(_postHold), cancellationToken: token);
			_playing = false;
			return;
		}

		float charTime = Mathf.Max(0.001f, _typeDuration / total);
		int i = 0;

		// main loop
		while (i < total && !_skipRequested)
		{
			// можно открывать по 2–3 символа за тик, чтобы казалось быстрее
			int step = (i < total * 0.4f) ? 2 : 3; // чуть ускоряемся к середине
			i = Mathf.Min(total, i + step);
			_label.maxVisibleCharacters = i;

			// короткий интервал
			await UniTask.Delay(TimeSpan.FromSeconds(charTime), cancellationToken: token);
		}

		// если скипнули — показать всё
		if (_skipRequested)
			_label.maxVisibleCharacters = total;

		// финальная короткая пауза
		await UniTask.Delay(TimeSpan.FromSeconds(_postHold), cancellationToken: token);

		_playing = false;
	}

	/// <summary> Скип по клику в любом месте сплэша. </summary>
	public void OnPointerClick(PointerEventData eventData)
	{
		if (_playing)
			_skipRequested = true;
	}

	/// <summary> Позволяет контроллеру менять текст перед проигрыванием. </summary>
	public void SetText(string t)
	{
		_text = t;
		if (_label) _label.text = t;
	}
}