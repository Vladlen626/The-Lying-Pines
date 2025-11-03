using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Main.Scripts.Core.Services
{
	public class InputBaseService : BaseService, IInputService
	{
		public event Action OnJumpPressed;
		public event Action OnJumpReleased;
		public event Action OnPausePressed;

		public Vector2 Move { get; private set; }
		public Vector2 Look { get; private set; }
		public bool IsJumping { get; private set; }
		public bool IsSprinting { get; private set; }
		public bool IsInteract { get; private set; }

		private InputSystem_Actions _actions;
		private Vector2 _moveVector;
		private Vector2 _lookInput;
		private bool _cancelInput;

		protected override UniTask InitializeServiceAsync()
		{
			_actions = new InputSystem_Actions();

			BindActions();
			EnableAllInputs();
			return UniTask.CompletedTask;
		}

		public void EnableAllInputs()
		{
			EnableUIInputs();
			EnablePlayerInputs();
		}

		public void DisableAllInputs()
		{
			DisableUIInputs();
			DisablePlayerInputs();
		}

		public void EnableUIInputs()
		{
			_actions.UI.Enable();
		}

		public void DisableUIInputs()
		{
			_actions.UI.Disable();
		}

		public void EnablePlayerInputs()
		{
			_actions.Player.Enable();
		}

		public void DisablePlayerInputs()
		{
			_actions.Player.Enable();
		}

		private void BindActions()
		{
			_actions.Player.Move.performed += ctx => { Move = ctx.ReadValue<Vector2>(); };
			_actions.Player.Move.canceled += _ => { Move = Vector2.zero; };

			_actions.Player.Sprint.performed += _ => { IsSprinting = true; };
			_actions.Player.Sprint.canceled += _ => { IsSprinting = false; };

			_actions.Player.Look.performed += ctx => { Look = ctx.ReadValue<Vector2>(); };
			_actions.Player.Look.canceled += _ => { Look = Vector2.zero; };
			
			_actions.Player.Interact.performed += _ => { IsInteract = true; };
			_actions.Player.Interact.canceled += _ => { IsInteract = false; };

			_actions.Player.Jump.started += _ =>
			{
				IsJumping = true;
				OnJumpPressed?.Invoke();
			};
			_actions.Player.Jump.canceled += _ =>
			{
				IsJumping = false;
				OnJumpReleased?.Invoke();
			};

			_actions.UI.Cancel.started += _ => OnPausePressed?.Invoke();
		}

		protected override void DisposeService()
		{
			if (_actions != null)
			{
				_actions.Player.Disable();
				_actions.UI.Disable();
				_actions.Dispose();
				_actions = null;
			}
		}
	}
}