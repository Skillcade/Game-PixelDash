using Game.GUI;
using SkillcadeSDK.StateMachine;
using UnityEngine;
using VContainer;
// using SkillcadeSDK.FishNetAdapter.StateMachine.States;

namespace Game.StateMachine.States
{
    public class CountdownState : NetworkState<GameStateType>
    {
        public override GameStateType Type => GameStateType.Countdown;

        [Inject] private readonly GameUi _gameUi;
        [Inject] private readonly GameConfig _gameConfig;

        private float _timer;

        public override void OnEnter(GameStateType prevState)
        {
            base.OnEnter(prevState);

            _timer = _gameConfig.StartGameCountdownSeconds;

            if (IsClient)
            {
                _gameUi.CountdownPanel.gameObject.SetActive(true);
                _gameUi.CountdownPanel.SetTime(_timer);
            }

            if (IsServer)
            {
                // TODO: spawn players
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

            if (IsClient)
                _gameUi.CountdownPanel.SetTime(_timer);
            
            if (IsServer && _timer <= 0f)
                StateMachine.SetStateServer(GameStateType.Running);
        }
    }
}