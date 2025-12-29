using SkillcadeSDK.DI;
using UnityEngine;
using VContainer;

namespace Game.Replays
{
    public class ReplayCameraInstaller : MonoInstaller
    {
        [SerializeField] private ReplayCameraTarget _replayCameraTarget;
        
        public override void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(_replayCameraTarget).AsImplementedInterfaces();
        }
    }
}