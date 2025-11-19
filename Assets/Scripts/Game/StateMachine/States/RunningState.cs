using Game.GUI;
using Game.Level;
using SkillcadeSDK.FishNetAdapter.StateMachine;
using SkillcadeSDK.FishNetAdapter.StateMachine.States;
using VContainer;

namespace Game.StateMachine.States
{
    public class RunningState : RunningStateBase
    {
        [Inject] private readonly GameUi _gameUi;
        [Inject] private readonly GameConfig _gameConfig;
        [Inject] private readonly FinishLine _finishLine;

        public override void OnEnter(GameStateType prevState)
        {
            base.OnEnter(prevState);
            
            if (IsClient)
                _gameUi.RunningPanel.gameObject.SetActive(true);

            if (IsServer)
            {
                _finishLine.OnPlayerReachedFinish += OnPlayerFinished;
            }
        }

        public override void OnExit(GameStateType nextState)
        {
            base.OnExit(nextState);

            if (IsClient)
                _gameUi.RunningPanel.gameObject.SetActive(false);
            
            if (IsServer)
                _finishLine.OnPlayerReachedFinish += OnPlayerFinished;
        }

        private void OnPlayerFinished(int winnerId)
        {
            if (IsServer)
                StateMachine.SetStateServer(GameStateType.Finished, new FinishedStateData(winnerId, _gameConfig.WaitAfterFinishSeconds, FinishReason.ReachedFinish));
        }
    }
}