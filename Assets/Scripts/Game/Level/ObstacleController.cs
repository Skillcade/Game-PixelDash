using FishNet.Object;
using Game.Player;
using SkillcadeSDK.FishNetAdapter.ColliderRollback;
using UnityEngine;

namespace Game.Level
{
    public class ObstacleController : RollbackTrigger
    {
        [SerializeField] public float Damage;

        protected override void HandleTriggerServer_Internal(NetworkObject playerNetworkObject)
        {
            if (playerNetworkObject.TryGetComponent(out PlayerMovement movement))
                movement.TakeDamage(this);
        }
    }
}