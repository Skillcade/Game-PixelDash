using FishNet.Managing.Object;
using SkillcadeSDK.DI;
using SkillcadeSDK.Replays;
using UnityEngine;
using VContainer;

namespace Game
{
    public class ReplaysInstaller : MonoInstaller
    {
        [SerializeField] private ReplayReadService _replayReadService;
        [SerializeField] private DefaultPrefabObjects _defaultPrefabObjects;
        
        public override void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(_defaultPrefabObjects);
            builder.RegisterInstance(_replayReadService);
            builder.Register<ReplayPrefabRegistry>(Lifetime.Singleton);
        }
    }
}