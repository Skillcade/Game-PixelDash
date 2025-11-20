using Game.GUI;
using SkillcadeSDK.FishNetAdapter;
using SkillcadeSDK.FishNetAdapter.StateMachine;
using SkillcadeSDK.StateMachine;
using UnityEngine;
using VContainer;

namespace Game.StateMachine.States
{
    public class CountdownState : NetworkState<GameStateType>
    {
        public override GameStateType Type => GameStateType.Countdown;
        
        [Inject] private readonly GameUi _gameUi;
        [Inject] private readonly GameConfig _gameConfig;
        [Inject] private readonly PlayerSpawner _playerSpawner;
        
        private float _timer;

        public override void OnEnter(GameStateType prevState)
        {
            base.OnEnter(prevState);
            _timer = _gameConfig.StartGameCountdownSeconds;

            if (IsServer)
            {
                _playerSpawner.SpawnAllInGamePlayers();
            }
            
            if (IsClient)
            {
                _gameUi.CountdownPanel.gameObject.SetActive(true);
                _gameUi.CountdownPanel.SetTime(_timer);
            }
        }

        public override void OnExit(GameStateType nextState)
        {
            base.OnExit(nextState);
            
            if (IsClient)
                _gameUi.CountdownPanel.gameObject.SetActive(false);
        }

        public override void Update()
        {
            base.Update();
            _timer -= Time.deltaTime;
            
            if (IsServer && _timer <= 0f)
                StateMachine.SetStateServer(GameStateType.Running);
            
            if (IsClient)
                _gameUi.CountdownPanel.SetTime(_timer);
        }
    }
}