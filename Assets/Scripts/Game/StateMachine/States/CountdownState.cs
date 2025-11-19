using Game.GUI;
using SkillcadeSDK.FishNetAdapter.StateMachine;
using SkillcadeSDK.FishNetAdapter.StateMachine.States;
using VContainer;

namespace Game.StateMachine.States
{
    public class CountdownState : CountdownStateBase
    {
        [Inject] private readonly GameUi _gameUi;

        protected override void OnEnter(GameStateType prevState, CountdownStateData data)
        {
            base.OnEnter(prevState, data);
            
            if (IsClient)
            {
                _gameUi.CountdownPanel.gameObject.SetActive(true);
                _gameUi.CountdownPanel.SetTime(data.Timer);
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

            if (IsClient)
                _gameUi.CountdownPanel.SetTime(Timer);
        }
    }
}