using Game.GUI;
using Game.StateMachine;
using Game.StateMachine.States;
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
        
        public override void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(_lobbyUi);
            builder.RegisterInstance(_gameUi);
            builder.RegisterInstance(_gameConfig);

            builder.Register<GameStateMachine>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<WaitForPlayersState>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<CountdownState>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<RunningState>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<FinishedState>(Lifetime.Singleton).AsImplementedInterfaces();
        }
    }
}