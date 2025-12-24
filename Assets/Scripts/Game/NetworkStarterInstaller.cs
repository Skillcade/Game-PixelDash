using SkillcadeSDK.DI;
using UnityEngine;
using VContainer;

namespace Game
{
    public class NetworkStarterInstaller : MonoInstaller
    {
        [SerializeField] private NetworkStarter _networkStarter;
        
        public override void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(_networkStarter).AsImplementedInterfaces();
        }
    }
}