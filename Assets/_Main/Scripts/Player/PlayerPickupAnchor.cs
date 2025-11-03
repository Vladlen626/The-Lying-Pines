using UnityEngine;

namespace _Main.Scripts.Player
{
	public interface IAnchor
	{
		Vector3 Position { get; }
	}

	public sealed class PlayerPickupAnchor : IAnchor
	{
		private readonly PlayerView _player;
		public PlayerPickupAnchor(PlayerView player) => _player = player;
		public Vector3 Position => _player.PickupAttach ? _player.PickupAttach.position : _player.transform.position;
	}
}
