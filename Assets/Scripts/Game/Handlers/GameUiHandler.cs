using System;
using Game.GameFeel;
using Game.GUI;
using SkillcadeSDK.Connection;
using SkillcadeSDK.Events;
using SkillcadeSDK.FishNetAdapter.Players;
using SkillcadeSDK.FishNetAdapter.StateMachine.Events;
using SkillcadeSDK.StateMachine;
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
        [Inject] private readonly WebBridge _webBridge;
        [Inject] private readonly IObjectResolver _objectResolver;

        private GameFeelController _gameFeel;
        
        public void Initialize()
        {
            _objectResolver.TryResolve(out _gameFeel);

            _eventBus.Subscribe<WaitForPlayersEnterEvent>(OnWaitForPlayersEnter);
            _eventBus.Subscribe<AllPlayersReadyEvent>(OnAllPlayersReady);
            _eventBus.Subscribe<CountdownTickEvent>(OnCountdownTick);
            _eventBus.Subscribe<RunningStartEvent>(OnRunningStart);
            _eventBus.Subscribe<RunningTimerTickEvent>(OnRunningTimerTick);
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
            _eventBus.Unsubscribe<RunningTimerTickEvent>(OnRunningTimerTick);
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
            Debug.Log("[GameUiHandler] Update wait for players ui");
            if (_connectionController.ActiveConfig.TargetPlayerCount > 0)
            {
                _gameUi.WaitForPlayersPanel.SetWaitForOthersState(true);
                return;
            }
            
            _gameUi.WaitForPlayersPanel.SetWaitForOthersState(false);
            var localReady = _playersController.TryGetLocalPlayerData(out var data) &&
                             PlayerInGameData.TryGetFromPlayer(data, out var inGameData) &&
                             inGameData.IsReady;
            
            var readyPlayers = 0;
            var totalPlayers = 0;
            foreach (var playerData in _playersController.GetAllPlayersData())
            {
                totalPlayers++;
                if (PlayerInGameData.TryGetFromPlayer(playerData, out var playerInGameData) && playerInGameData.IsReady)
                    readyPlayers++;
            }

            Debug.Log($"[GameUiHandler] Total players: {totalPlayers}, ready: {readyPlayers}, local ready: {localReady}");

            _gameUi.WaitForPlayersPanel.SetReadyState(readyPlayers, totalPlayers, localReady);
        }

        private void UpdatePlayersReadyState(int playerId, FishNetPlayerData data)
        {
            UpdateWaitForPlayersUi();
        }

        private void OnReadyStateChanged(bool isReady)
        {
            if (!_playersController.TryGetLocalPlayerData(out var playerData))
            {
                Debug.LogError($"[GameUiHandler] Can't get local player data");
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
            // Keep the countdown panel up for the GO! flourish; it self-hides after the punch animation.
            _gameUi.CountdownPanel.gameObject.SetActive(true);
            _gameUi.CountdownPanel.ShowGo();
            _gameUi.RunningPanel.gameObject.SetActive(true);
            _gameUi.speedrunTimePanel.StartSpeedrunTime();
            _gameUi.remainingTimePanel.Disable();

            if (_gameFeel != null)
            {
                _gameFeel.Flash(new Color(1f, 1f, 1f, 0.65f), 0.18f);
                _gameFeel.ShakeStrong();
            }
        }

        private void OnRunningTimerTick(RunningTimerTickEvent evt)
        {
            _gameUi.remainingTimePanel.UpdateTimer(evt);
        }

        private void OnGameFinished(GameFinishedEvent evt)
        {
            _gameUi.RunningPanel.gameObject.SetActive(false);
            _gameUi.speedrunTimePanel.StopSpeedrunTime();

            var mode = _connectionController.ConnectionState == ConnectionState.SinglePlayer
                ? FinishedPanel.FinishPanelMode.SinglePlayer
                : _connectionController.ActiveConfig.SkillcadeHubIntegrated
                    ? FinishedPanel.FinishPanelMode.SkillcadeHub
                    : FinishedPanel.FinishPanelMode.Default;

            _gameUi.FinishedPanel.SetMode(mode);
            _gameUi.FinishedPanel.gameObject.SetActive(true);

            if (evt.FinishReason == FinishReason.Draw)
            {
                _gameUi.FinishedPanel.SetWinner("—", evt.FinishReason);
                _gameUi.FinishedPanel.SetDraw();
                return;
            }
            
            string winnerName = evt.WinnerId >= 0 ? $"Player {evt.WinnerId}" : "—";

            if (_playersController.TryGetPlayerData(evt.WinnerId, out var playerData))
            {
                if (PlayerMatchData.TryGetFromPlayer(playerData, out var matchData))
                    winnerName = matchData.Nickname;
            }
            else
            {
                Debug.LogWarning($"[GameUiHandler] Can't get winner player data {evt.WinnerId}; using stable result data");
            }

            _gameUi.FinishedPanel.SetWinner(winnerName, evt.FinishReason);
            bool localWon = IsLocalWinner(evt);
            _gameUi.FinishedPanel.SetUserState(localWon);

            // Soften loss: if the gap was tight, surface a "you were close" line so the loss
            // doesn't read as a blowout. Skip on win — winners don't need consolation.
            if (!localWon && _gameUi.PlayerProgressLine != null)
            {
                _gameUi.FinishedPanel.ShowCloseMatch(Mathf.Abs(_gameUi.PlayerProgressLine.LastLocalGap));
            }
        }

        private bool IsLocalWinner(GameFinishedEvent evt)
        {
            var useStablePlayerId = _connectionController.ActiveConfig.SkillcadeHubIntegrated &&
                                    !string.IsNullOrEmpty(evt.WinnerPlayerId);

            if (!useStablePlayerId)
                return _playersController.IsLocalPlayerId(evt.WinnerId);

            if (_playersController.TryGetLocalPlayerData(out var localPlayerData) &&
                PlayerMatchData.TryGetFromPlayer(localPlayerData, out var localMatchData))
            {
                return localMatchData.PlayerId == evt.WinnerPlayerId;
            }

            return _webBridge.Payload != null && _webBridge.Payload.PlayerId == evt.WinnerPlayerId;
        }
    }
}
