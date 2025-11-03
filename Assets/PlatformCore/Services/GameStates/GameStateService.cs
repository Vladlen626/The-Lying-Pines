using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PlatformCore.Services.UI;
using UnityEngine;

namespace PlatformCore.Services.GameStates
{
	public class GameStateService : IGameStateService
	{
		public event Action<GameState, GameState> StateChanged;

		public GameState CurrentState => _currentState;

		private readonly ILoggerService _loggerService;

		private GameState _currentState = GameState.Initialization;

		public GameStateService(ILoggerService loggerService, IUIService uiService)
		{
			_loggerService = loggerService;
		}

		public UniTask InitializeAsync(CancellationToken cancellationToken)
		{
			_loggerService?.Log("[GameStateService] Initialized");
			return UniTask.CompletedTask;
		}

		public UniTask SwitchToState(GameState newState, CancellationToken ct = default)
		{
			if (newState == _currentState)
			{
				_loggerService?.Log($"[GameStateService] Already in state: {newState}");
				return UniTask.CompletedTask;
			}
			var previousState = _currentState;

			_loggerService?.Log($"[GameStateService] State transition: {previousState} → {newState}");

			try
			{
				_currentState = newState;
				StateChanged?.Invoke(previousState, newState);

				_loggerService?.Log($"[GameStateService] State transition completed: {previousState} → {newState}");
			}
			catch (Exception ex)
			{
				_loggerService?.LogError(
					$"[GameStateService] State transition failed: {previousState} → {newState}. Error: {ex.Message}");

				_currentState = previousState;
				throw;
			}
			
			return UniTask.CompletedTask;
		}

		public void Dispose()
		{
			_loggerService?.Log("[GameStateService] Disposed");
		}
	}
}