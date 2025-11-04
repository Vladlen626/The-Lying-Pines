using System;
using PlatformCore.Services.UI;
using UnityEngine;
using UnityEngine.UI;

public class UIPauseMenu : BaseUIElement
{
	public event Action OnResumeClicked;

	[SerializeField] private CanvasGroup _canvasGroup;

	[SerializeField] private Slider _masterSlider;
	[SerializeField] private Slider _musicSlider;
	[SerializeField] private Slider _sfxSlider;
	[SerializeField] private Toggle _fullscreenToggle;
	[SerializeField] private Button _quitButton;

	// события — view ничего не знает о логике
	public event Action<float> OnMasterChanged;
	public event Action<float> OnMusicChanged;
	public event Action<float> OnSfxChanged;
	public event Action<bool> OnMuteChanged;
	public event Action<bool> OnFullscreenChanged;
	public event Action OnQuitClicked;

	private void Awake()
	{
		_masterSlider.onValueChanged.AddListener(v => OnMasterChanged?.Invoke(v));
		_musicSlider.onValueChanged.AddListener(v => OnMusicChanged?.Invoke(v));
		_sfxSlider.onValueChanged.AddListener(v => OnSfxChanged?.Invoke(v));
		_fullscreenToggle.onValueChanged.AddListener(v => OnFullscreenChanged?.Invoke(v));
		_quitButton.onClick.AddListener(() => OnQuitClicked?.Invoke());
	}

	public override void OnShow()
	{
		_canvasGroup.alpha = 1;
		_canvasGroup.interactable = true;
		_canvasGroup.blocksRaycasts = true;
	}

	public override void OnHide()
	{
		_canvasGroup.alpha = 0;
		_canvasGroup.interactable = false;
		_canvasGroup.blocksRaycasts = false;
	}

	// контроллер сам может сеттить начальные значения
	public void SetValues(float master, float music, float sfx, bool mute, bool fullscreen)
	{
		_masterSlider.SetValueWithoutNotify(master);
		_musicSlider.SetValueWithoutNotify(music);
		_sfxSlider.SetValueWithoutNotify(sfx);
		_fullscreenToggle.SetIsOnWithoutNotify(fullscreen);
	}
	
	public void ClearListeners()
	{
		OnMasterChanged = null;
		OnMusicChanged = null;
		OnSfxChanged = null;
		OnMuteChanged = null;
		OnFullscreenChanged = null;
		OnQuitClicked = null;
	}


	public void OnResumeClick()
	{
		OnResumeClicked?.Invoke();
	}
}