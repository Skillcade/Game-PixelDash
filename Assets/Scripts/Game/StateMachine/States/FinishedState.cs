using Game.GUI;
using SkillcadeSDK.FishNetAdapter;
using SkillcadeSDK.FishNetAdapter.Players;
using SkillcadeSDK.StateMachine;
using SkillcadeSDK.WebRequests;
using UnityEngine;
using VContainer;

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
        [Inject] private readonly WebRequester _webRequester;
        [Inject] private readonly PlayerSpawner _playerSpawner;
        [Inject] private readonly FishNetPlayersController _playersController;

        private float _timer;

        protected override void OnEnter(GameStateType prevState, FinishedStateData data)
        {
            base.OnEnter(prevState, data);
            
            if (IsClient)
                InitUi(data);
            
            _timer = _gameConfig.StartGameCountdownSeconds;
            
            if (IsServer)
            {
                if (!TryValidateMatchIds())
                    Debug.LogError("MatchId is invalid");
                
                SendWinnerToBackend(data.WinnerId);
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

        private bool TryValidateMatchIds()
        {
            string matchId = null;
            foreach (var playerData in _playersController.GetAllPlayersData())
            {
                if (!PlayerMatchData.TryGetFromPlayer(playerData, out var matchData))
                    continue;
                
                if (string.IsNullOrEmpty(matchData.MatchId))
                {
                    Debug.Log($"Player {playerData.PlayerNetworkId} matchId is null");
                    continue;
                }
                
                if (string.IsNullOrEmpty(matchId))
                {
                    matchId = matchData.MatchId;
                }
                else if (!string.Equals(matchId, matchData.MatchId))
                {
                    Debug.Log($"Players matchId is different: first {matchId}, second: {matchData.MatchId}");
                    return false;
                }
            }
            
            return matchId != null;
        }

        private void SendWinnerToBackend(int winnerId)
        {
            if (winnerId == 0 || !_playersController.TryGetPlayerData(winnerId, out var playerData))
            {
                Debug.Log($"Don't send winner, id: {winnerId}");
                return;
            }
            
            if (PlayerMatchData.TryGetFromPlayer(playerData, out var matchData))
                _webRequester.SendWinner(matchData.MatchId, matchData.PlayerId);
        }
    }
}