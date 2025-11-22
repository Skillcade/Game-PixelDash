using Game.GUI;
using SkillcadeSDK.Common.Level;
using SkillcadeSDK.Common.Players;
using SkillcadeSDK.FishNetAdapter;
using SkillcadeSDK.FishNetAdapter.Players;
using SkillcadeSDK.FishNetAdapter.StateMachine;
using SkillcadeSDK.StateMachine;
using VContainer;

namespace Game.StateMachine.States
{
    public class WaitForPlayersState : NetworkState<GameStateType>
    {
        public override GameStateType Type => GameStateType.WaitForPlayers;

        [Inject] private readonly GameUi _gameUi;
        [Inject] private readonly GameConfig _gameConfig;
        [Inject] private readonly PlayerSpawner _playerSpawner;
        [Inject] private readonly IPlayersController _playersController;
        [Inject] private readonly RespawnServiceProvider _respawnServiceProvider;

        private bool _skipUpdate;
        
        public override void OnEnter(GameStateType prevState)
        {
            base.OnEnter(prevState);
            _playersController.OnPlayerDataUpdated += OnPlayerUpdated;

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
            _playersController.OnPlayerDataUpdated -= OnPlayerUpdated;
            
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
                playerData.SetDataOnServer(PlayerDataConst.IsReady, false);
                playerData.SetDataOnServer(PlayerDataConst.InGame, false);
            }
            _skipUpdate = false;
        }

        private void OnReadyStateChanged(bool isReady)
        {
            if (_playersController.TryGetPlayerData(_playersController.LocalPlayerId, out var data))
                data.SetDataOnLocalClient(PlayerDataConst.IsReady, isReady);
        }

        private void OnPlayerUpdated(int clientId, IPlayerData iPlayerData)
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
                              data.IsReady();
            
            int readyPlayers = 0;
            int totalPlayers = 0;
            foreach (var playerData in _playersController.GetAllPlayersData())
            {
                totalPlayers++;
                if (playerData.IsReady())
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
                if (playerData.IsReady())
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
                if (playerData.IsReady())
                    playerData.SetDataOnServer(PlayerDataConst.InGame, true);
            }
            _skipUpdate = false;
        }
    }
}