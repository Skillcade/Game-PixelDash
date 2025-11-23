using Game.GUI;
using SkillcadeSDK.Common.Level;
using SkillcadeSDK.FishNetAdapter;
using SkillcadeSDK.FishNetAdapter.Players;
using SkillcadeSDK.StateMachine;
using UnityEngine;
using VContainer;

namespace Game.StateMachine.States
{
    public class WaitForPlayersState : NetworkState<GameStateType>
    {
        public override GameStateType Type => GameStateType.WaitForPlayers;

        [Inject] private readonly GameUi _gameUi;
        [Inject] private readonly GameConfig _gameConfig;
        [Inject] private readonly PlayerSpawner _playerSpawner;
        [Inject] private readonly FishNetPlayersController _playersController;
        [Inject] private readonly RespawnServiceProvider _respawnServiceProvider;

        private bool _skipUpdate;
        private bool _dataSet;
        
        public override void OnEnter(GameStateType prevState)
        {
            base.OnEnter(prevState);
            _playersController.OnPlayerAdded += OnPlayerUpdated;
            _playersController.OnPlayerDataUpdated += OnPlayerUpdated;
            _playersController.OnPlayerRemoved += OnPlayerUpdated;

            if (IsServer)
            {
                ClearReadyStateForPlayers();
                _playerSpawner.DespawnAllPlayers();
                _respawnServiceProvider.TriggerRespawn();
            }

            if (IsClient)
            {
                _gameUi.WaitForPlayersPanel.gameObject.SetActive(true);
                _gameUi.WaitForPlayersPanel.OnReadyStateChanged += OnReadyStateChanged;
                UpdateUi();
            }
        }

        public override void OnExit(GameStateType nextState)
        {
            base.OnExit(nextState);
            _playersController.OnPlayerAdded -= OnPlayerUpdated;
            _playersController.OnPlayerDataUpdated -= OnPlayerUpdated;
            _playersController.OnPlayerRemoved -= OnPlayerUpdated;
            
            if (IsClient)
            {
                _gameUi.WaitForPlayersPanel.gameObject.SetActive(false);
                _gameUi.WaitForPlayersPanel.OnReadyStateChanged -= OnReadyStateChanged;
            }
        }

        private void ClearReadyStateForPlayers()
        {
            if (!IsServer)
                return;
            
            _skipUpdate = true;
            foreach (var playerData in _playersController.GetAllPlayersData())
            {
                if (!PlayerInGameData.TryGetFromPlayer(playerData, out var inGameData))
                    inGameData = new PlayerInGameData();
                
                inGameData.InGame = false;
                inGameData.IsReady = false;
                inGameData.SetToPlayer(playerData);
            }
            _skipUpdate = false;
        }

        private void OnReadyStateChanged(bool isReady)
        {
            if (_playersController.TryGetPlayerData(_playersController.LocalPlayerId, out var playerData))
            {
                if (!PlayerInGameData.TryGetFromPlayer(playerData, out var inGameData))
                    inGameData = new PlayerInGameData();
                
                inGameData.IsReady = isReady;
                inGameData.SetToPlayer(playerData);
            }
        }

        private void OnPlayerUpdated(int clientId, FishNetPlayerData data)
        {
            if (_skipUpdate)
                return;
            
            if (IsServer)
                CheckReadyPlayers();
            
            if (IsClient)
                UpdateUi();
        }

        private void UpdateUi()
        {
            bool localReady = _playersController.TryGetPlayerData(_playersController.LocalPlayerId, out var data) &&
                              PlayerInGameData.TryGetFromPlayer(data, out var inGameData) &&
                              inGameData.IsReady;
            
            int readyPlayers = 0;
            int totalPlayers = 0;
            foreach (var playerData in _playersController.GetAllPlayersData())
            {
                totalPlayers++;
                if (PlayerInGameData.TryGetFromPlayer(playerData, out var playerInGameData) && playerInGameData.IsReady)
                    readyPlayers++;
            }
            
            _gameUi.WaitForPlayersPanel.SetReadyState(readyPlayers, totalPlayers, localReady);
        }

        private void CheckReadyPlayers()
        {
            int readyPlayers = 0;
            int notReadyPlayers = 0;
            foreach (var playerData in _playersController.GetAllPlayersData())
            {
                if (PlayerInGameData.TryGetFromPlayer(playerData, out var playerInGameData) && playerInGameData.IsReady)
                    readyPlayers++;
                else
                    notReadyPlayers++;
            }
            
            bool shouldStartGame = readyPlayers >= 1 && notReadyPlayers == 0;
            if (!shouldStartGame) return;

            SetReadyPlayersInGame();
            _playerSpawner.SpawnAllInGamePlayers();
            StateMachine.SetStateServer(GameStateType.Countdown);
        }
        
        private void SetReadyPlayersInGame()
        {
            _skipUpdate = true;
            foreach (var playerData in _playersController.GetAllPlayersData())
            {
                if (PlayerInGameData.TryGetFromPlayer(playerData, out var playerInGameData) && playerInGameData.IsReady)
                {
                    playerInGameData.InGame = true;
                    playerInGameData.SetToPlayer(playerData);
                }
            }
            _skipUpdate = false;
        }
    }
}