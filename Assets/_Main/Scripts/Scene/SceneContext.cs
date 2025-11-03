using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;


public class SceneContext : MonoBehaviour
{
	
	[Header("Homes & Builders")]
	public HomeSlot[] Homes;
	
	[Header("Directors (привяжи в инспекторе)")]
	[SerializeField] private PlayableDirector _gameStartDirector;
	[SerializeField] private PlayableDirector _gameEndDirector;

	[Header("Home build timelines")]
	[SerializeField] private List<NamedTimeline> _homeTimelines = new();
	
	[SerializeField] private List<BaseTrigger> triggers;
	[SerializeField] private Transform playerSpawnPoint;
	public IReadOnlyList<BaseTrigger> Triggers => triggers;
	public Vector3 PlayerSpawnPos => playerSpawnPoint.position;
	
	public PlayableDirector GameStartDirector => _gameStartDirector;
	public PlayableDirector GameEndDirector => _gameEndDirector;
	
	public List<NamedTimeline> HomeTimeLines => _homeTimelines;
}