using Collectables;
using Game.Level;
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

        public override void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(_finishLine);
            builder.RegisterInstance(_collectablesRespawnService).As<IRespawnService>();
            
            // PlayerSpawner
            builder.RegisterInstance(_playerSpawner).As<IPlayerSpawner>();
        }
    }
}