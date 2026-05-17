using Collectables;
using Game.Camera;
using Game.GameFeel;
using Game.Level;
using Game.Player;
using SkillcadeSDK.Common.Level;
using SkillcadeSDK.Common.Players;
using SkillcadeSDK.DI;
using UnityEngine;
using VContainer;

namespace Game
{
    public class LevelInstaller : MonoInstaller
    {
        [SerializeField] private PlayerSpawner _playerSpawner;
        [SerializeField] private FinishLine _finishLine;
        [SerializeField] private CollectablesRespawnService _collectablesRespawnService;
        [SerializeField] private GameCameraTarget gameCameraTarget;
        [SerializeField] private PlayerCharactersConfig _charactersConfig;
        [SerializeField] private GameFeelController _gameFeelController;

        public override void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(gameCameraTarget);
            builder.RegisterInstance(_finishLine);
            builder.RegisterInstance(_charactersConfig);
            builder.RegisterInstance(_collectablesRespawnService).As<IRespawnService>();

            if (_gameFeelController != null)
                builder.RegisterInstance(_gameFeelController);

            // PlayerSpawner
            builder.RegisterInstance(_playerSpawner).As<IPlayerSpawner>();
        }
    }
}