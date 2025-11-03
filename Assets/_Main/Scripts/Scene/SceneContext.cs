using System.Collections.Generic;
using UnityEngine;

public class SceneContext : MonoBehaviour
{
	[Header("Homes & Builders")]
	public HomeSlot[] Homes;
	
	[SerializeField] private List<BaseTrigger> triggers;
	[SerializeField] private Transform playerSpawnPoint;
	public IReadOnlyList<BaseTrigger> Triggers => triggers;
	public Vector3 PlayerSpawnPos => playerSpawnPoint.position;
}