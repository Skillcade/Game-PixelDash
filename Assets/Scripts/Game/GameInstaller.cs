using DefaultNamespace.Collectables;
using Game.GUI;
using Game.Level;
using Game.StateMachine;
using Game.StateMachine.States;
using SkillcadeSDK.DI;
using SkillcadeSDK.FishNetAdapter;
using UnityEngine;
using VContainer;

namespace Game
{
    public class GameInstaller : MonoInstaller
    {
        [SerializeField] private GameUi _gameUi;
        [SerializeField] private GameConfig _gameConfig;
        [SerializeField] private FinishLine _finishLine;
        
        [SerializeField] private CollectablesRespawnService _collectablesRespawnService;
        
        public override void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(_gameUi);
            builder.RegisterInstance(_gameConfig);
            builder.RegisterInstance(_finishLine);
            builder.RegisterInstance(_collectablesRespawnService).AsImplementedInterfaces();

            builder.Register<GameStateMachine>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<WaitForPlayersState>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<CountdownState>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<RunningState>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<FinishedState>(Lifetime.Singleton).AsImplementedInterfaces();
        }
    }
}