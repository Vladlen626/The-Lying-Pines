// Views/NPC/CockroachView.cs
using UnityEngine;
using System;

[RequireComponent(typeof(Collider))]
public class CockroachView : MonoBehaviour
{
	[SerializeField] private TooltipWorldView _tooltip;
	[SerializeField] private Transform _tooltipAnchor;
	private Collider _col;

	public Transform TooltipAnchor => _tooltipAnchor ? _tooltipAnchor : transform;
	public TooltipWorldView Tooltip => _tooltip;
	public event Action PlayerEnter;
	public event Action PlayerExit;

	private void Awake()
	{
		_col = GetComponent<Collider>();
		_col.isTrigger = true;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.GetComponentInParent<PlayerView>())
			PlayerEnter?.Invoke();
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.GetComponentInParent<PlayerView>())
			PlayerExit?.Invoke();
	}
}