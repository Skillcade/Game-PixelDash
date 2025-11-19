using Game.GUI;
using SkillcadeSDK.Common;
using SkillcadeSDK.Common.Players;
using SkillcadeSDK.FishNetAdapter;
using SkillcadeSDK.FishNetAdapter.Players;
using SkillcadeSDK.FishNetAdapter.StateMachine;
using SkillcadeSDK.FishNetAdapter.StateMachine.States;
using VContainer;

namespace Game.StateMachine.States
{
    public class WaitForPlayersState : WaitForPlayersStateBase
    {
        protected override float CountdownTimer => _gameConfig.StartGameCountdownSeconds;

        [Inject] private readonly GameUi _gameUi;
        [Inject] private readonly GameConfig _gameConfig;
        [Inject] private readonly IPlayersController _playersController;
        
        public override void OnEnter(GameStateType prevState)
        {
            base.OnEnter(prevState);

            if (IsClient)
            {
                _gameUi.WaitForPlayersPanel.gameObject.SetActive(true);
                _gameUi.WaitForPlayersPanel.OnReadyStateChanged += OnReadyStateChanged;
                _playersController.OnPlayerDataUpdated += OnPlayerUpdated;
                UpdateUi();
            }
        }

        public override void OnExit(GameStateType nextState)
        {
            base.OnExit(nextState);
            
            if (IsClient)
            {
                _gameUi.WaitForPlayersPanel.gameObject.SetActive(false);
                _gameUi.WaitForPlayersPanel.OnReadyStateChanged -= OnReadyStateChanged;
                _playersController.OnPlayerDataUpdated -= OnPlayerUpdated;
            }
        }

        private void OnReadyStateChanged(bool obj)
        {
            if (_playersController.TryGetPlayerData(_playersController.LocalPlayerId, out var data))
                data.SetDataOnLocalClient(PlayerDataConst.IsReady, true);
        }

        private void OnPlayerUpdated(int clientId, IPlayerData iPlayerData)
        {
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
    }
}