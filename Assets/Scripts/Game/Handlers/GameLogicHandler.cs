using System;
using Game.Level;
using SkillcadeSDK.Connection;
using SkillcadeSDK.Events;
using SkillcadeSDK.FishNetAdapter;
using SkillcadeSDK.FishNetAdapter.Players;
using SkillcadeSDK.StateMachine;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Handlers
{
    /// <summary>
    /// Handler for game logic events.
    /// Subscribes to RunningStartEvent and FinishLine events to transition to FinishedState.
    /// </summary>
    public class GameLogicHandler : IInitializable, ITickable, IDisposable
    {
        [Inject] private readonly GameEventBus _eventBus;
        [Inject] private readonly FinishLine _finishLine;
        [Inject] private readonly SkillcadeGameStateMachine _stateMachine;
        [Inject] private readonly IConnectionController _connectionController;
        [Inject] private readonly FishNetPlayersController _playersController;
        [Inject] private readonly GameConfig _gameConfig;

        private int? _winnerId;
        private float _timeBeforeFinish;

        public void Initialize()
        {
            _eventBus.Subscribe<RunningStartEvent>(OnRunningStart);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<RunningStartEvent>(OnRunningStart);
            _finishLine.OnPlayerReachedFinish -= OnPlayerFinished;
        }

        public void Tick()
        {
            if (!_winnerId.HasValue)
                return;
            
            if (_stateMachine.CurrentStateType == GameStateType.Finished)
                return;

            _timeBeforeFinish -= Time.deltaTime;
            if (_timeBeforeFinish > 0f)
                return;

            _stateMachine.SetStateServer(GameStateType.Finished, new FinishedStateData(_winnerId.Value, GetWinnerPlayerId(_winnerId.Value), FinishReason.CompletedGoal));
        }

        private void OnRunningStart(RunningStartEvent evt)
        {
            _winnerId = null;
            _finishLine.OnPlayerReachedFinish += OnPlayerFinished;
        }

        private void OnPlayerFinished(int winnerId)
        {
            if (!_stateMachine.IsServer && _connectionController.ConnectionState != ConnectionState.SinglePlayer)
                return;
                
            if (_winnerId.HasValue)
                return;

            _winnerId = winnerId;
            _timeBeforeFinish = _gameConfig.WaitBeforeFinishSeconds;
        }

        private string GetWinnerPlayerId(int winnerId)
        {
            if (_playersController == null || !_playersController.TryGetPlayerData(winnerId, out var playerData))
                return null;

            return PlayerMatchData.TryGetFromPlayer(playerData, out var matchData)
                ? matchData.PlayerId
                : null;
        }
    }
}
