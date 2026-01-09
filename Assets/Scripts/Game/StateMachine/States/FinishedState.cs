using Game.GUI;
using SkillcadeSDK.FishNetAdapter;
using SkillcadeSDK.FishNetAdapter.Players;
using SkillcadeSDK.StateMachine;
using UnityEngine;
using VContainer;

#if UNITY_SERVER || UNITY_EDITOR
using SkillcadeSDK.WebRequests;
#endif

namespace Game.StateMachine.States
{
    public enum FinishReason
    {
        ReachedFinish,
        TechnicalWin,
    }
    
    public class FinishedStateData
    {
        public readonly int WinnerId;
        public readonly FinishReason FinishReason;

        public FinishedStateData(int winnerId, FinishReason finishReason)
        {
            WinnerId = winnerId;
            FinishReason = finishReason;
        }
    }
    
    public class FinishedState : NetworkState<GameStateType, FinishedStateData>
    {
        public override GameStateType Type => GameStateType.Finished;
        
        [Inject] private readonly GameUi _gameUi;
        [Inject] private readonly GameConfig _gameConfig;
        [Inject] private readonly PlayerSpawner _playerSpawner;
        [Inject] private readonly FishNetPlayersController _playersController;

#if UNITY_SERVER || UNITY_EDITOR
        [Inject] private readonly WebBridge _webBridge;
        [Inject] private readonly WebRequester _webRequester;
#endif

        private float _timer;

        protected override void OnEnter(GameStateType prevState, FinishedStateData data)
        {
            base.OnEnter(prevState, data);
            
            if (IsClient)
                InitUi(data);
            
            _timer = _gameConfig.StartGameCountdownSeconds;
            
            if (IsServer)
            {
#if UNITY_SERVER || UNITY_EDITOR
                SendWinnerToBackend(data.WinnerId);
#endif
                _playerSpawner.DespawnAllPlayers();
            }
        }

        public override void OnExit(GameStateType nextState)
        {
            base.OnExit(nextState);
            if (IsClient)
                _gameUi.FinishedPanel.gameObject.SetActive(false);
        }

        public override void Update()
        {
            base.Update();
            _timer -= Time.deltaTime;
            
            if (_timer <= 0f && IsServer)
            {
                StateMachine.SetStateServer(GameStateType.WaitForPlayers);
            }
        }

        private void InitUi(FinishedStateData data)
        {
            _gameUi.FinishedPanel.gameObject.SetActive(true);
        
            if (!_playersController.TryGetPlayerData(data.WinnerId, out var playerData))
            {
                Debug.LogError($"[FinishedState] Can't get winner player data {data.WinnerId}");
                return;
            }
            
            if (PlayerMatchData.TryGetFromPlayer(playerData, out var matchData))
                _gameUi.FinishedPanel.SetWinner(matchData.Nickname, data.FinishReason);
            
            _gameUi.FinishedPanel.SetUserState(_playersController.LocalPlayerId == data.WinnerId);
        }

#if UNITY_SERVER || UNITY_EDITOR
        private void SendWinnerToBackend(int winnerId)
        {
            if (!_webBridge.UsePayload)
                return;
            
            if (winnerId == 0 || !_playersController.TryGetPlayerData(winnerId, out var playerData))
            {
                Debug.Log($"Don't send winner, id: {winnerId}");
                return;
            }

            Debug.Log($"[FinishedState] Winner is {winnerId}");
            if (PlayerMatchData.TryGetFromPlayer(playerData, out var matchData))
                _webRequester.SendWinner(matchData.PlayerId);
            else
                Debug.Log("[FinishedState] Can't get winner player data");
        }
#endif
    }
}