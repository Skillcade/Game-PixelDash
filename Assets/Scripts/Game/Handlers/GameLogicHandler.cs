using System;
using Game.Level;
using SkillcadeSDK.Connection;
using SkillcadeSDK.Events;
using SkillcadeSDK.FishNetAdapter;
using SkillcadeSDK.StateMachine;
using VContainer;
using VContainer.Unity;

namespace Game.Handlers
{
    /// <summary>
    /// Handler for game logic events.
    /// Subscribes to RunningStartEvent and FinishLine events to transition to FinishedState.
    /// </summary>
    public class GameLogicHandler : IInitializable, IDisposable
    {
        [Inject] private readonly GameEventBus _eventBus;
        [Inject] private readonly FinishLine _finishLine;
        [Inject] private readonly SkillcadeGameStateMachine _stateMachine;
        [Inject] private readonly IConnectionController _connectionController;

        public void Initialize()
        {
            _eventBus.Subscribe<RunningStartEvent>(OnRunningStart);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<RunningStartEvent>(OnRunningStart);
            _finishLine.OnPlayerReachedFinish -= OnPlayerFinished;
        }

        private void OnRunningStart(RunningStartEvent evt)
        {
            _finishLine.OnPlayerReachedFinish += OnPlayerFinished;
        }

        private void OnPlayerFinished(int winnerId)
        {
            if (_stateMachine.IsServer || _connectionController.ConnectionState == ConnectionState.SinglePlayer)
                _stateMachine.SetStateServer(GameStateType.Finished, new FinishedStateData(winnerId, FinishReason.CompletedGoal));
        }
    }
}
