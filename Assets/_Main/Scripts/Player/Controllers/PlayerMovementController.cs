using _Main.Scripts.Core;
using _Main.Scripts.Core.Services;
using PlatformCore.Core;
using PlatformCore.Infrastructure.Lifecycle;
using PlatformCore.Services;
using PlatformCore.Services.Audio;
using UnityEngine;

namespace _Main.Scripts.Player
{
	public class PlayerMovementController : IBaseController, IUpdatable
	{
		private readonly IInputService _inputService;
		private readonly IAudioService _audioService;
		private readonly PlayerModel _playerModel;
		private readonly PlayerView _playerView;
		private readonly Transform _cameraTransform;

		private bool _isGrounded;
		private Vector3 _verticalVelocity;
		private Vector3 _velocity;
		private bool _prevJumpHeld;
		private float _coyoteTimer;
		private float _jumpBufferTimer;
		private Vector3 _velXZ;
		private float _verticalY;
		private bool _wasGrounded;
		private Vector3 _groundNormal = Vector3.up;
		private Vector3 _cameraForward;
		private Vector3 _cameraRight;
		private Vector3 _desiredDirection;
		private Vector3 _targetVelXZ;
		private readonly RaycastHit[] _groundHits = new RaycastHit[1];

		private bool _hasPendingVerticalOverride;
		private float _pendingVerticalY;
		private Vector3 _pendingImpulseXZ;


		public PlayerMovementController(IInputService inputService, PlayerModel playerModel, PlayerView playerView,
			Transform cameraTransform, IAudioService audioService)
		{
			_inputService = inputService;
			_audioService = audioService;
			_playerModel = playerModel;
			_playerView = playerView;
			_cameraTransform = cameraTransform;
		}

		public void OnUpdate(float deltaTime)
		{
			HandleMovement(deltaTime);
		}

		public void RequestVerticalOverride(float y)
		{
			_hasPendingVerticalOverride = true;
			_pendingVerticalY = y; // заменить вертикальную скорость в ЭТОМ кадре
		}

		public void AddImpulseXZ(Vector3 impulse)
		{
			_pendingImpulseXZ.x += impulse.x;
			_pendingImpulseXZ.z += impulse.z;
		}

		private void HandleMovement(float dt)
		{
			// === Grounded / Coyote ===
			_isGrounded = _playerView.IsGrounded;
			_coyoteTimer = _isGrounded ? _playerModel.coyoteTime : Mathf.Max(0f, _coyoteTimer - dt);

			// === Jump buffer ===
			bool jumpHeld = _inputService.IsJumping;
			bool jumpPressedThisFrame = jumpHeld && !_prevJumpHeld;
			_prevJumpHeld = jumpHeld;
			if (jumpPressedThisFrame) _jumpBufferTimer = _playerModel.jumpBuffer;
			else _jumpBufferTimer = Mathf.Max(0f, _jumpBufferTimer - dt);

			// === Ввод и базовое желаемое направление (камеро-ориентированное) ===
			Vector2 in2 = _inputService.Move;
			_cameraForward = _cameraTransform.forward;
			_cameraForward.y = 0f;
			float forwardMag = _cameraForward.sqrMagnitude;
			if (forwardMag > Mathf.Epsilon)
			{
				float invMag = 1f / Mathf.Sqrt(forwardMag);
				_cameraForward.x *= invMag;
				_cameraForward.z *= invMag;
			}

			_cameraRight = _cameraTransform.right;
			_cameraRight.y = 0f;
			float rightMag = _cameraRight.sqrMagnitude;
			if (rightMag > Mathf.Epsilon)
			{
				float invMag = 1f / Mathf.Sqrt(rightMag);
				_cameraRight.x *= invMag;
				_cameraRight.z *= invMag;
			}

			_desiredDirection.x = _cameraRight.x * in2.x + _cameraForward.x * in2.y;
			_desiredDirection.y = 0f;
			_desiredDirection.z = _cameraRight.z * in2.x + _cameraForward.z * in2.y;

			float desiredSqr = _desiredDirection.sqrMagnitude;
			if (desiredSqr > 1f)
			{
				float invMag = 1f / Mathf.Sqrt(desiredSqr);
				_desiredDirection.x *= invMag;
				_desiredDirection.z *= invMag;
				desiredSqr = 1f;
			}

			if (_isGrounded || NearGround(out _groundNormal))
			{
				ProjectOnPlaneNormalized(ref _desiredDirection, _groundNormal);
				desiredSqr = _desiredDirection.sqrMagnitude;
			}

			// === Целевая скорость ===
			float maxSpeed = _inputService.IsSprinting ? _playerModel.sprintSpeed : _playerModel.walkSpeed;
			_targetVelXZ.x = _desiredDirection.x * maxSpeed;
			_targetVelXZ.y = 0f;
			_targetVelXZ.z = _desiredDirection.z * maxSpeed;

			// === АКЦЕЛ/ДЕКЦЕЛ + ТОРМОЖЕНИЕ ПРИ РАЗВОРОТЕ ===
			float baseAccel = _isGrounded
				? (_targetVelXZ.sqrMagnitude > 0.01f ? _playerModel.groundAccel : _playerModel.groundDecel)
				: _playerModel.airAccel;

			// Если резко меняем направление — усилим "тормоз/подхват"
			float align = 1f;
			float velMagSq = _velXZ.sqrMagnitude;
			float targetMagSq = _targetVelXZ.sqrMagnitude;
			if (velMagSq > 0.001f && targetMagSq > 0.001f)
			{
				float invVelMag = 1f / Mathf.Sqrt(velMagSq);
				float invTargetMag = 1f / Mathf.Sqrt(targetMagSq);
				align = (_velXZ.x * invVelMag * _targetVelXZ.x * invTargetMag)
				        + (_velXZ.z * invVelMag * _targetVelXZ.z * invTargetMag);
			}

			float accel = align < 0f ? baseAccel * _playerModel.brakingBoost : baseAccel;

			float lerpT = 1f - Mathf.Exp(-accel * dt);
			_velXZ.x = Mathf.Lerp(_velXZ.x, _targetVelXZ.x, lerpT);
			_velXZ.z = Mathf.Lerp(_velXZ.z, _targetVelXZ.z, lerpT);


			// === Прыжок с coyote + buffer ===
			if (_suppressJumpTimer > 0f)
			{
				_suppressJumpTimer -= dt;
			}

			if (_suppressJumpTimer <= 0f && _jumpBufferTimer > 0f && _coyoteTimer > 0f)
			{
				_jumpBufferTimer = 0f;
				_coyoteTimer = 0f;
				_verticalY = Mathf.Sqrt(_playerModel.jumpHeight * -2f * _playerModel.gravity) * 1.1f;
				if (desiredSqr > 0.0001f)
				{
					float invMag = 1f / Mathf.Sqrt(desiredSqr);
					_velXZ.x += _desiredDirection.x * invMag * 2f;
					_velXZ.z += _desiredDirection.z * invMag * 2f;
				}

				_audioService.PlaySound(AudioEvents.Jump);
				_isGrounded = false;
			}

			// === Apex hang: у самой вершины прыжка немного ослабим граву (реактивнее) ===
			bool nearApex = _verticalY > 0f && Mathf.Abs(_verticalY) < _playerModel.apexHangThreshold;

			// === Variable jump (jump cut) + Fall multiplier ===
			bool releasingJump = !jumpHeld && _verticalY > 0f;
			float gravityMul =
				_verticalY > 0f
					? (releasingJump ? _playerModel.jumpCutMultiplier : (nearApex ? _playerModel.apexHangScale : 1f))
					: _playerModel.fallGravityMultiplier;

			_verticalY += _playerModel.gravity * gravityMul * dt;

			// Лёгкое прилипание к земле
			if (_isGrounded && _verticalY < 0f)
				_verticalY = -2f;

			if (_hasPendingVerticalOverride)
			{
				_verticalY = _pendingVerticalY; // перебиваем липучку и граву, если надо
				_hasPendingVerticalOverride = false;
			}

			if (_pendingImpulseXZ.sqrMagnitude > 0f)
			{
				_velXZ += _pendingImpulseXZ;
				_pendingImpulseXZ.x = 0f;
				_pendingImpulseXZ.y = 0f;
				_pendingImpulseXZ.z = 0f;
			}

			// === Сборка вектора и поворот ===
			_velocity.x = _velXZ.x;
			_velocity.y = _verticalY;
			_velocity.z = _velXZ.z;

			// динамическая скорость поворота: чем быстрее бежим — тем резче крутимся
			float speed01 = Mathf.Clamp01(_velXZ.magnitude / Mathf.Max(0.01f, maxSpeed));
			float rotSpeed = Mathf.Lerp(_playerModel.minRotateSpeed, _playerModel.maxRotateSpeed, speed01);
			_playerView.SetRotateSpeed(rotSpeed);
			_playerView.ApplyMovement(_velocity);
		}

		private bool NearGround(out Vector3 normal)
		{
			normal = Vector3.up;
			Vector3 origin = _playerView.Position;
			origin.y += 0.1f;
			int hits = Physics.SphereCastNonAlloc(origin, 0.05f, Vector3.down, _groundHits,
				_playerModel.groundSnapDistance, ~0, QueryTriggerInteraction.Ignore);
			if (hits <= 0)
				return false;

			var hit = _groundHits[0];
			float slope = Vector3.Angle(hit.normal, Vector3.up);
			if (slope > _playerModel.maxSnapSlope)
				return false;

			normal = hit.normal;
			return true;
		}

		private float _suppressJumpTimer;

		public void SuppressJumpFor(float duration)
		{
			if (duration > _suppressJumpTimer) _suppressJumpTimer = duration;
		}

		private static void ProjectOnPlaneNormalized(ref Vector3 vector, Vector3 planeNormal)
		{
			float dot = vector.x * planeNormal.x + vector.y * planeNormal.y + vector.z * planeNormal.z;
			vector.x -= planeNormal.x * dot;
			vector.y -= planeNormal.y * dot;
			vector.z -= planeNormal.z * dot;

			float sqrMag = vector.x * vector.x + vector.y * vector.y + vector.z * vector.z;
			if (sqrMag > 1e-6f)
			{
				float invMag = 1f / Mathf.Sqrt(sqrMag);
				vector.x *= invMag;
				vector.y *= invMag;
				vector.z *= invMag;
			}
			else
			{
				vector.x = 0f;
				vector.y = 0f;
				vector.z = 0f;
			}
		}
	}
}