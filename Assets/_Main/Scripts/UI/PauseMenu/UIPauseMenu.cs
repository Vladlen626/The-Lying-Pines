using System;
using PlatformCore.Services.UI;
using UnityEngine;
using UnityEngine.UI;

public class UISettingsAudio : BaseUIElement
{
	[SerializeField] private CanvasGroup _group;
	[Header("Controls")] [SerializeField] private Slider _master;
	[SerializeField] private Slider _music;
	[SerializeField] private Slider _sfx;
	[SerializeField] private Button _close;

	public event Action<float> OnMasterChanged;
	public event Action<float> OnMusicChanged;
	public event Action<float> OnSfxChanged;
	public event Action OnCloseClicked;

	private void Awake()
	{
		_master.onValueChanged.AddListener(v => OnMasterChanged?.Invoke(v));
		_music.onValueChanged.AddListener(v => OnMusicChanged?.Invoke(v));
		_sfx.onValueChanged.AddListener(v => OnSfxChanged?.Invoke(v));
		_close.onClick.AddListener(() => OnCloseClicked?.Invoke());
	}

	public override void OnShow()
	{
		if (!_group) return;
		_group.alpha = 1;
		_group.interactable = true;
		_group.blocksRaycasts = true;
	}

	public override void OnHide()
	{
		if (!_group) return;
		_group.alpha = 0;
		_group.interactable = false;
		_group.blocksRaycasts = false;
	}

	public void SetValues(float master, float music, float sfx, bool mute, bool fullscreen)
	{
		_master.SetValueWithoutNotify(master);
		_music.SetValueWithoutNotify(music);
		_sfx.SetValueWithoutNotify(sfx);
	}
}