using System;
using Game.GUI;
using SkillcadeSDK.Connection;
using SkillcadeSDK.Events;
using SkillcadeSDK.FishNetAdapter.Players;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Handlers
{
    /// <summary>
    /// Handler for UI updates based on game state events from Event Bus.
    /// Subscribes to events and updates UI panels accordingly.
    /// </summary>
    public class GameUiHandler : IInitializable, IDisposable
    {
        [Inject] private readonly GameEventBus _eventBus;
        [Inject] private readonly GameUi _gameUi;
        [Inject] private readonly FishNetPlayersController _playersController;
        [Inject] private readonly IConnectionController _connectionController;
        
        public void Initialize()
        {
            _eventBus.Subscribe<WaitForPlayersEnterEvent>(OnWaitForPlayersEnter);
            _eventBus.Subscribe<AllPlayersReadyEvent>(OnAllPlayersReady);
            _eventBus.Subscribe<CountdownTickEvent>(OnCountdownTick);
            _eventBus.Subscribe<RunningStartEvent>(OnRunningStart);
            _eventBus.Subscribe<GameFinishedEvent>(OnGameFinished);

            // Subscribe to UI button click
            _gameUi.WaitForPlayersPanel.OnReadyStateChanged += OnReadyStateChanged;

            _playersController.OnPlayerAdded += UpdatePlayersReadyState;
            _playersController.OnPlayerDataUpdated += UpdatePlayersReadyState;
            _playersController.OnPlayerRemoved += UpdatePlayersReadyState;
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<WaitForPlayersEnterEvent>(OnWaitForPlayersEnter);
            _eventBus.Unsubscribe<AllPlayersReadyEvent>(OnAllPlayersReady);
            _eventBus.Unsubscribe<CountdownTickEvent>(OnCountdownTick);
            _eventBus.Unsubscribe<RunningStartEvent>(OnRunningStart);
            _eventBus.Unsubscribe<GameFinishedEvent>(OnGameFinished);

            _gameUi.WaitForPlayersPanel.OnReadyStateChanged -= OnReadyStateChanged;
        }

        private void OnWaitForPlayersEnter(WaitForPlayersEnterEvent evt)
        {
            _gameUi.WaitForPlayersPanel.gameObject.SetActive(true);
            _gameUi.CountdownPanel.gameObject.SetActive(false);
            _gameUi.RunningPanel.gameObject.SetActive(false);
            _gameUi.FinishedPanel.gameObject.SetActive(false);

            UpdateWaitForPlayersUi();
        }

        private void OnAllPlayersReady(AllPlayersReadyEvent evt)
        {
            _gameUi.WaitForPlayersPanel.gameObject.SetActive(false);
        }

        private void UpdateWaitForPlayersUi()
        {
            if (_connectionController.ActiveConfig.TargetPlayerCount > 0)
            {
                _gameUi.WaitForPlayersPanel.SetWaitForOthersState(true);
                return;
            }

            _gameUi.WaitForPlayersPanel.SetWaitForOthersState(false);
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

        private void UpdatePlayersReadyState(int playerId, FishNetPlayerData data)
        {
            UpdateWaitForPlayersUi();
        }

        private void OnReadyStateChanged(bool isReady)
        {
            if (!_playersController.TryGetPlayerData(_playersController.LocalPlayerId, out var playerData))
            {
                Debug.LogError($"[GameUiHandler] Can't get local player data for id {_playersController.LocalPlayerId}");
                return;
            }
            
            if (!PlayerInGameData.TryGetFromPlayer(playerData, out var inGameData))
                inGameData = new PlayerInGameData();

            inGameData.IsReady = isReady;
            inGameData.SetToPlayer(playerData);
        }

        private void OnCountdownTick(CountdownTickEvent evt)
        {
            _gameUi.CountdownPanel.gameObject.SetActive(true);
            _gameUi.CountdownPanel.SetTime(evt.RemainingSeconds);
        }

        private void OnRunningStart(RunningStartEvent evt)
        {
            _gameUi.CountdownPanel.gameObject.SetActive(false);
            _gameUi.RunningPanel.gameObject.SetActive(true);
        }

        private void OnGameFinished(GameFinishedEvent evt)
        {
            _gameUi.RunningPanel.gameObject.SetActive(false);
            
            var mode = _connectionController.ConnectionState == ConnectionState.SinglePlayer
                ? FinishedPanel.FinishPanelMode.SinglePlayer
                : _connectionController.ActiveConfig.IsSkillcadeHub
                    ? FinishedPanel.FinishPanelMode.SkillcadeHub
                    : FinishedPanel.FinishPanelMode.Default;
            
            _gameUi.FinishedPanel.SetMode(mode);
            _gameUi.FinishedPanel.gameObject.SetActive(true);
            
            if (!_playersController.TryGetPlayerData(evt.WinnerId, out var playerData))
            {
                Debug.LogError($"[GameUiHandler] Can't get winner player data {evt.WinnerId}");
                return;
            }

            if (PlayerMatchData.TryGetFromPlayer(playerData, out var matchData))
                _gameUi.FinishedPanel.SetWinner(matchData.Nickname, evt.FinishReason);

            _gameUi.FinishedPanel.SetUserState(_playersController.LocalPlayerId == evt.WinnerId);
        }
    }
}
