using DefaultNamespace.Collectables;
using Game.GUI;
using Game.Level;
using Game.StateMachine;
using Game.StateMachine.States;
using SkillcadeSDK.DI;
using UnityEngine;
using VContainer;

namespace Game
{
    public class GameInstaller : MonoInstaller
    {
        [SerializeField] private GameUi _gameUi;
        [SerializeField] private GameConfig _gameConfig;
        [SerializeField] private FinishLine _finishLine;
        // [SerializeField] private PlayerSpawner _playerSpawner;
        [SerializeField] private GameStateMachine _stateMachine;
        [SerializeField] private CollectablesRespawnService _collectablesRespawnService;
        
        public override void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(_gameUi);
            builder.RegisterInstance(_gameConfig);
            builder.RegisterInstance(_finishLine);
            builder.RegisterInstance(_stateMachine);
            // builder.RegisterInstance(_playerSpawner);
            builder.RegisterInstance(_collectablesRespawnService).AsImplementedInterfaces();
            // builder.Register<WaitForPlayersState>(Lifetime.Singleton).AsImplementedInterfaces();
            // builder.Register<CountdownState>(Lifetime.Singleton).AsImplementedInterfaces();
            // builder.Register<RunningState>(Lifetime.Singleton).AsImplementedInterfaces();
            // builder.Register<FinishedState>(Lifetime.Singleton).AsImplementedInterfaces();
            // builder.Register<RespawnService>(Lifetime.Singleton);
        }
    }
}