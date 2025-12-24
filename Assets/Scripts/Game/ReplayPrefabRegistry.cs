using FishNet.Managing.Object;
using SkillcadeSDK.Replays;
using SkillcadeSDK.Replays.Components;
using VContainer;

namespace Game
{
    public class ReplayPrefabRegistry : IReplayPrefabRegistry
    {
        [Inject] private readonly DefaultPrefabObjects _defaultPrefabObjects;
        
        public bool TryGetPrefab(int prefabId, out ReplayObjectHandler objectHandlerPrefab)
        {
            if (prefabId < 0 || prefabId >= _defaultPrefabObjects.Prefabs.Count)
            {
                objectHandlerPrefab = null;
                return false;
            }
            
            var prefab = _defaultPrefabObjects.Prefabs[prefabId];
            var handlerPrefab = prefab.GetComponent<ReplayObjectHandler>();
            if (handlerPrefab == null)
            {
                objectHandlerPrefab = null;
                return false;
            }
            
            objectHandlerPrefab = handlerPrefab;
            return true;
        }
    }
}