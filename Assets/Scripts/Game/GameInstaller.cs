using Game.GameFeel;
using Game.GUI;
using Game.Handlers;
using SkillcadeSDK.Common;
using SkillcadeSDK.DI;
using UnityEngine;
using VContainer;

namespace Game
{
    public class GameInstaller : MonoInstaller
    {
        [SerializeField] private LobbyUi _lobbyUi;
        [SerializeField] private GameUi _gameUi;
        [SerializeField] private GameConfig _gameConfig;
        [SerializeField] private GameFeelController _gameFeelController;

        public override void Install(IContainerBuilder builder)
        {
            // Game-specific UI and config
            builder.RegisterInstance(_lobbyUi);
            builder.RegisterInstance(_gameUi);
            builder.RegisterInstance(_gameConfig).As<ISkillcadeConfig>();

            if (_gameFeelController != null)
                builder.RegisterInstance(_gameFeelController).AsSelf().AsImplementedInterfaces();

            // Game-specific handlers
            builder.Register<GameUiHandler>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<GameLogicHandler>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
        }
    }
}