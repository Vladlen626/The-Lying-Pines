using TMPro;
using UnityEngine;

public class TooltipWorldView : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI _label;
	private Camera _cam;

	private void Awake()
	{
		_cam = Camera.main;
	}

	private void LateUpdate()
	{
		if (_cam)
		{
			transform.forward = _cam.transform.forward;
		}
	}

	public void SetText(string text)
	{
		if (_label)
		{ 
			_label.text = text;
		}
	}
}