using Game.StateMachine.States;
using SkillcadeSDK.Common.Players;
using SkillcadeSDK.FishNetAdapter.Players;
using SkillcadeSDK.FishNetAdapter.StateMachine;
using SkillcadeSDK.FishNetAdapter.StateMachine.States;
using SkillcadeSDK.StateMachine;
using VContainer;

namespace Game.StateMachine
{
    public class GameStateMachine : NetworkStateMachine<GameStateType>
    {
        [Inject] private readonly GameConfig _gameConfig;
        [Inject] private readonly IPlayersController _playersController;
        
        public override void Initialize()
        {
            base.Initialize();
            _playersController.OnPlayerRemoved += OnPlayerRemoved;
        }

        public override void Dispose()
        {
            base.Dispose();
            _playersController.OnPlayerRemoved -= OnPlayerRemoved;
        }

        private void OnPlayerRemoved(int playerId, IPlayerData data)
        {
            if (!IsServer)
                return;
                
            int inGamePlayers = 0;
            int winnerId = -1;
            foreach (var playerData in _playersController.GetAllPlayersData())
            {
                if (playerData.TryGetData(PlayerDataConst.InGame, out bool inGame) && inGame)
                {
                    winnerId = playerData.PlayerNetworkId;
                    inGamePlayers++;
                }
            }
            
            if (inGamePlayers == 0)
                SetStateServer(GameStateType.WaitForPlayers);
            else if (inGamePlayers == 1 && CurrentStateType != GameStateType.Finished)
                SetStateServer(GameStateType.Finished, new FinishedStateData(winnerId, _gameConfig.WaitAfterFinishSeconds, FinishReason.TechnicalWin));
        }
    }
}