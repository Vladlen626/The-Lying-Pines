using UnityEngine;

public class BreakOnSlam : MonoBehaviour, ISlamImpactReceiver
{
	[SerializeField] private GameObject _breakFx;

	public void OnSlamImpact(in ImpactCtx ctx)
	{
		// джем: просто включим FX и отключим объект
		if (_breakFx) Instantiate(_breakFx, transform.position, Quaternion.identity);
		gameObject.SetActive(false);
	}
}