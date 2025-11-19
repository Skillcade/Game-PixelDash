using Game.GUI;
using SkillcadeSDK.Common.Players;
using SkillcadeSDK.FishNetAdapter.Players;
using SkillcadeSDK.FishNetAdapter.StateMachine;
using SkillcadeSDK.FishNetAdapter.StateMachine.States;
using SkillcadeSDK.StateMachine;
using SkillcadeSDK.WebRequests;
using UnityEngine;
using VContainer;

namespace Game.StateMachine.States
{
    public class FinishedState : FinishedStateBase
    {
        [Inject] private readonly GameUi _gameUi;
        [Inject] private readonly GameConfig _gameConfig;
        [Inject] private readonly IPlayersController _playersController;

        protected override void OnEnter(GameStateType prevState, FinishedStateData data)
        {
            base.OnEnter(prevState, data);
            
            if (IsClient)
                InitUi(data);
        }

        public override void OnExit(GameStateType nextState)
        {
            base.OnExit(nextState);
            if (IsClient)
                _gameUi.FinishedPanel.gameObject.SetActive(false);
        }
        
        private void InitUi(FinishedStateData data)
        {
            _gameUi.FinishedPanel.gameObject.SetActive(true);
        
            if (!_playersController.TryGetPlayerData(data.WinnerId, out var playerData))
            {
                Debug.LogError($"[FinishedState] Can't get winner player data {data.WinnerId}");
                return;
            }
            
            if (playerData.TryGetData(PlayerDataConst.Nickname, out string nickname))
                _gameUi.FinishedPanel.SetWinner(nickname, data.FinishReason);
            
            _gameUi.FinishedPanel.SetUserState(_playersController.LocalPlayerId == data.WinnerId);
        }
    }
}