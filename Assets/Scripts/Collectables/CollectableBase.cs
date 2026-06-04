using FishNet.Object;
using SkillcadeSDK.FishNetAdapter.ColliderRollback;
using UnityEngine;

namespace Collectables
{
    public class CollectableBase : RollbackTrigger
    {
        public NetworkObject RespawnPrefab => _respawnPrefab;
        public bool Collected { get; private set; }

        [SerializeField] private NetworkObject _respawnPrefab;

        public bool TryCollectServer(PlayerCollector playerCollector)
        {
            if (!IsServerInitialized || Collected)
                return false;

            if (TryGetComponent(out CollectableOwner owner)
                && owner.OwnerClientId != 0
                && (playerCollector.Owner == null || playerCollector.Owner.ClientId != owner.OwnerClientId))
                return false;
            
            Collected = true;
            return true;
        }

        public virtual void OnCollectedServer(PlayerCollector playerCollector)
        {
            if (!IsServerInitialized)
                return;

            if (NetworkObject != null && NetworkObject.IsSpawned)
                NetworkObject.Despawn();
            else
                Destroy(gameObject);
        }

        protected override void HandleTriggerServer_Internal(NetworkObject playerNetworkObject)
        {
            if (playerNetworkObject.TryGetComponent(out PlayerCollector playerCollector))
                playerCollector.CollectServer(this);
        }
    }
}