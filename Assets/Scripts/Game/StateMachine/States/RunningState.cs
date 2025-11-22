using Game.GUI;
using Game.Level;
using SkillcadeSDK.Common.Level;
using SkillcadeSDK.FishNetAdapter;
using SkillcadeSDK.StateMachine;
using VContainer;

namespace Game.StateMachine.States
{
    public class RunningState : NetworkState<GameStateType>
    {
        public override GameStateType Type => GameStateType.Running;
        
        [Inject] private readonly GameUi _gameUi;
        [Inject] private readonly GameConfig _gameConfig;
        [Inject] private readonly FinishLine _finishLine;
        [Inject] private readonly PlayerSpawner _playerSpawner;
        [Inject] private readonly RespawnServiceProvider _respawnServiceProvider;

        public override void OnEnter(GameStateType prevState)
        {
            base.OnEnter(prevState);
            
            if (IsClient)
                _gameUi.RunningPanel.gameObject.SetActive(true);

            if (IsServer)
            {
                _playerSpawner.SpawnAllInGamePlayers();
                _finishLine.OnPlayerReachedFinish += OnPlayerFinished;
                _respawnServiceProvider.TriggerRespawn();
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
                StateMachine.SetStateServer(GameStateType.Finished, new FinishedStateData(winnerId, FinishReason.ReachedFinish));
        }
    }
}