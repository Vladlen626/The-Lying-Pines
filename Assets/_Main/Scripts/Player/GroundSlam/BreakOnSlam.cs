using System;
using System.Collections.Generic;
using _Main.Scripts.Collectibles;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BreakOnSlam : MonoBehaviour, ISlamImpactReceiver
{
	[SerializeField] private GameObject boxView;
	[SerializeField] private GameObject _breakFx;
	[SerializeField] private List<CollectibleView> collectibles;

	[SerializeField] private float jumpPower = 1.5f;
	[SerializeField] private float jumpRadius = 1.2f;
	[SerializeField] private float jumpDuration = 0.6f;
	[SerializeField] private float randomRot = 180f;
	
	private Collider boxCollider;

	private void Start()
	{
		boxCollider = gameObject.GetComponent<Collider>();
		foreach (var collectible in collectibles)
		{
			collectible.canBeCollected = false;
		}
	}

	public void OnSlamImpact(in ImpactCtx ctx)
	{
		Destruct();
	}

	private void Destruct()
	{
		if (_breakFx)
		{
			Instantiate(_breakFx, transform.position, Quaternion.identity);
		}
		boxCollider.enabled = false;
		boxView.SetActive(false);
		

		foreach (var collectible in collectibles)
		{
			if (!collectible) continue;
			
			var startPos = transform.position;
			var rndOffset = UnityEngine.Random.insideUnitSphere * jumpRadius;
			rndOffset.y = Mathf.Abs(rndOffset.y); // чтобы не проваливались

			// создаём красивый прыжок и вращение
			var seq = DOTween.Sequence();

			seq.Append(
				collectible.transform.DOJump(
						startPos + rndOffset,
						jumpPower,
						1,
						jumpDuration
					)
					.SetEase(Ease.OutQuad)
			);

			seq.Join(
				collectible.transform.DORotate(
					new Vector3(
						UnityEngine.Random.Range(-randomRot, randomRot),
						UnityEngine.Random.Range(-randomRot, randomRot),
						UnityEngine.Random.Range(-randomRot, randomRot)
					),
					jumpDuration,
					RotateMode.LocalAxisAdd
				)
			);

			// в конце прыжка включаем коллайдер (чтобы CollectibleController подхватил)
			seq.OnComplete(() => collectible.canBeCollected = true);
		}
	}
}
