using UnityEngine;

namespace _Main.Scripts.Player
{
	public class PlayerModel
	{
		public float walkSpeed = 5f;
		public float sprintSpeed = 9f;
		
		public float groundAccel = 60f;
		public float groundDecel = 50f;
		public float airAccel = 25f;

		public float jumpHeight = 2.5f;
		public float gravity = -9.81f;
		public float fallGravityMultiplier = 2.8f;
		public float jumpCutMultiplier = 3.0f;

		public float coyoteTime = 0.05f;
		public float jumpBuffer = 0.15f;

		public float brakingBoost = 2.0f;
		public float apexHangThreshold = 1.0f;
		public float apexHangScale = 0.9f;
		public float groundSnapDistance = 0.3f;
		public float maxSnapSlope = 55f;
		public float minRotateSpeed = 480f;
		public float maxRotateSpeed = 1080f;
	}
}	