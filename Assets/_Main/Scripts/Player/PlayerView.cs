using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerView : MonoBehaviour
{
	[Header("Movement")] [SerializeField] private CharacterController _characterController;
	[SerializeField] private Transform _cameraRoot;
	[SerializeField] private Transform _playerTransform;
	[SerializeField] private float _rotateSpeedDeg = 720f;
	[SerializeField] private Transform _pickupAttach;
	[SerializeField] private Animator _animator; 
	public Transform PickupAttach => _pickupAttach != null ? _pickupAttach : _cameraRoot;
	public CharacterController Controller => _characterController;
	public bool IsGrounded => _characterController.isGrounded;
	public Vector3 Position => transform.position;
	public Transform CameraRoot => _cameraRoot;
	public Transform PlayerTransform => _playerTransform;
	public Vector3 Velocity => _characterController.velocity;
	public Animator Animator => _animator;
	public event Action OnLand;
	private bool _wasGrounded;

	private void Update()
	{
		DetectLanding();
	}

	private void DetectLanding()
	{
		if (!_wasGrounded && IsGrounded)
		{
			OnLand?.Invoke();
		}

		_wasGrounded = IsGrounded;
	}

	public void ApplyMovement(Vector3 velocity)
	{
		_characterController.Move(velocity * Time.deltaTime);

		var horizontal = new Vector3(velocity.x, 0, velocity.z);
		if (horizontal.sqrMagnitude > 0.0001f)
		{
			var target = Quaternion.LookRotation(horizontal, Vector3.up);
			_playerTransform.rotation =
				Quaternion.RotateTowards(_playerTransform.rotation, target, _rotateSpeedDeg * Time.deltaTime);
		}
	}

	public void SetRotateSpeed(float degPerSec)
	{
		_rotateSpeedDeg = degPerSec;
	}
}