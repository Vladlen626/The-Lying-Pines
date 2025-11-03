using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace PlatformCore.Services.GameStates
{
	public interface IGameStateService : IService
	{
		GameState CurrentState { get; }
		UniTask SwitchToState(GameState newState, CancellationToken  ct = default);
		event Action<GameState, GameState> StateChanged;
	}
}