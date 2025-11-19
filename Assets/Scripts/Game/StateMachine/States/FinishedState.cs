using Game.GUI;
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
        // [Inject] private readonly PlayerSpawner _playerSpawner;

        private float _timer;

        protected override void OnEnter(GameStateType prevState, FinishedStateData data)
        {
            base.OnEnter(prevState, data);
            _timer = _gameConfig.WaitAfterFinishSeconds;

            // if (!TryValidateMatchIds())
            //     Debug.LogError("MatchId is invalid");
            //
            // if (IsClient)
            //     InitUi(data);
            //
            // if (IsServer)
            // {
            //     SendWinnerToBackend(data.WinnerId);
            //     _playerSpawner.DespawnAllPlayers();
            // }
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

            if (IsServer && _timer <= 0)
                StateMachine.SetStateServer(GameStateType.WaitForPlayers);
        }

        // private bool TryValidateMatchIds()
        // {
        //     string matchId = null;
        //     foreach (var playerData in _playerDataService.PlayerData.Values)
        //     {
        //         Debug.Log($"Processing player {playerData.NetworkPlayerId} on finish: {playerData.ToString()}");
        //         if (string.IsNullOrEmpty(playerData.MatchId))
        //         {
        //             Debug.Log($"Player {playerData.NetworkPlayerId}-{playerData.UserId} matchId is null");
        //             continue;
        //         }
        //         
        //         if (string.IsNullOrEmpty(matchId))
        //         {
        //             matchId = playerData.MatchId;
        //         }
        //         else if (!string.Equals(matchId, playerData.MatchId))
        //         {
        //             Debug.Log($"Players matchId is different: first {matchId}, second: {playerData.MatchId}");
        //             return false;
        //         }
        //     }
        //     
        //     return matchId != null;
        // }
        //
        // private void SendWinnerToBackend(int winnerId)
        // {
        //     if (winnerId == 0 || !_playerDataService.TryGetData(winnerId, out var playerData))
        //     {
        //         Debug.Log($"Don't send winner, id: {winnerId}");
        //         return;
        //     }
        //
        //     _webRequester.SendWinner(playerData.MatchId, playerData.UserId);
        // }
        //
        // private void InitUi(FinishedStateData data)
        // {
        //     _gameUi.FinishedPanel.gameObject.SetActive(true);
        //
        //     if (!_playerDataService.TryGetData(data.WinnerId, out var playerData))
        //     {
        //         Debug.LogError($"[FinishedState] Can't get winner player data {data.WinnerId}");
        //         return;
        //     }
        //     
        //     _gameUi.FinishedPanel.SetWinner(playerData.Nickname, data.FinishReason);
        //     _gameUi.FinishedPanel.SetUserState(_playerDataService.LocalPlayerId == data.WinnerId);
        // }
    }
}