using _Main.Scripts.Core.Services;
using PlatformCore.Core;
using PlatformCore.Infrastructure.Lifecycle;
using PlatformCore.Services;
using UnityEngine;

namespace _Main.Scripts.Player
{
	public class PlayerMovementController : IBaseController, IUpdatable
	{
		private readonly IInputService _inputService;
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

		private bool _hasPendingVerticalOverride;
		private float _pendingVerticalY;
		private Vector3 _pendingImpulseXZ;


		public PlayerMovementController(IInputService inputService, PlayerModel playerModel, PlayerView playerView,
			Transform cameraTransform)
		{
			_inputService = inputService;
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
			_pendingImpulseXZ += new Vector3(impulse.x, 0f, impulse.z); // прибавить к XZ-скорости
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
			var f = _cameraTransform.forward;
			f.y = 0;
			f.Normalize();
			var r = _cameraTransform.right;
			r.y = 0;
			r.Normalize();
			Vector3 desiredDir = (r * in2.x + f * in2.y);
			if (desiredDir.sqrMagnitude > 1f) desiredDir.Normalize();

			var groundNormal = Vector3.zero;
			if (_isGrounded || NearGround(out groundNormal))
			{
				desiredDir = Vector3.ProjectOnPlane(desiredDir, groundNormal).normalized;
			}

			// === Целевая скорость ===
			float maxSpeed = _inputService.IsSprinting ? _playerModel.sprintSpeed : _playerModel.walkSpeed;
			Vector3 targetXZ = desiredDir * maxSpeed;

			// === АКЦЕЛ/ДЕКЦЕЛ + ТОРМОЖЕНИЕ ПРИ РАЗВОРОТЕ ===
			float baseAccel = _isGrounded
				? (targetXZ.sqrMagnitude > 0.01f ? _playerModel.groundAccel : _playerModel.groundDecel)
				: _playerModel.airAccel;

			// Если резко меняем направление — усилим "тормоз/подхват"
			float align = (_velXZ.sqrMagnitude > 0.001f && targetXZ.sqrMagnitude > 0.001f)
				? Vector3.Dot(_velXZ.normalized, targetXZ.normalized)
				: 1f;
			float accel = align < 0f ? baseAccel * _playerModel.brakingBoost : baseAccel;

			_velXZ = Vector3.Lerp(_velXZ, targetXZ, 1f - Mathf.Exp(-accel * dt));


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
				_velXZ += desiredDir.normalized * 2f;
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
				_verticalY = _pendingVerticalY;           // перебиваем липучку и граву, если надо
				_hasPendingVerticalOverride = false;
			}
			if (_pendingImpulseXZ.sqrMagnitude > 0f)
			{
				_velXZ += _pendingImpulseXZ;
				_pendingImpulseXZ = Vector3.zero;
			}

			// === Сборка вектора и поворот ===
			_velocity = new Vector3(_velXZ.x, _verticalY, _velXZ.z);

			// динамическая скорость поворота: чем быстрее бежим — тем резче крутимся
			float speed01 = Mathf.Clamp01(_velXZ.magnitude / Mathf.Max(0.01f, maxSpeed));
			float rotSpeed = Mathf.Lerp(_playerModel.minRotateSpeed, _playerModel.maxRotateSpeed, speed01);
			_playerView.SetRotateSpeed(rotSpeed);
			_playerView.ApplyMovement(_velocity);
		}

		private bool NearGround(out Vector3 normal)
		{
			normal = Vector3.up;
			var origin = _playerView.Position + Vector3.up * 0.1f; // небольшой отступ
			if (Physics.SphereCast(origin, 0.05f, Vector3.down, out var hit, _playerModel.groundSnapDistance,
				    ~0, QueryTriggerInteraction.Ignore))
			{
				// проверим, что это не вертикальная стена
				float slope = Vector3.Angle(hit.normal, Vector3.up);
				if (slope <= _playerModel.maxSnapSlope)
				{
					normal = hit.normal;
					return true;
				}
			}

			return false;
		}
		
		private float _suppressJumpTimer;
		public void SuppressJumpFor(float duration) {
			if (duration > _suppressJumpTimer) _suppressJumpTimer = duration;
		}
	}
}