using DefaultNamespace.Collectables;
using Game.Level;
using SkillcadeSDK.DI;
using UnityEngine;
using VContainer;

namespace Game
{
    public class LevelInstaller : MonoInstaller
    {
        [SerializeField] private FinishLine _finishLine;
        [SerializeField] private CollectablesRespawnService _collectablesRespawnService;
        
        public override void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(_finishLine);
            builder.RegisterInstance(_collectablesRespawnService);
        }
    }
}