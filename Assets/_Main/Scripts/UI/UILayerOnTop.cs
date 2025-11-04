using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UILayerOnTop : MonoBehaviour
{
	[SerializeField] private int sortingOrder = 5000; // выше всего остального UI
	[SerializeField] private bool screenSpaceOverlay = true;

	void Awake()
	{
		var c = GetComponent<Canvas>();
		if (!c) c = gameObject.AddComponent<Canvas>();

		c.overrideSorting = true;
		c.sortingOrder = sortingOrder;
		if (screenSpaceOverlay) c.renderMode = RenderMode.ScreenSpaceOverlay;

		if (!GetComponent<GraphicRaycaster>())
			gameObject.AddComponent<GraphicRaycaster>();
	}
}