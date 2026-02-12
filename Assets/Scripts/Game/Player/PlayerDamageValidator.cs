using System.Collections.Generic;
using FishNet.Component.ColliderRollback;
using FishNet.Managing.Timing;
using FishNet.Object;
using FishNet.Utility.Template;
using Game.Level;
using UnityEngine;

namespace Game.Player
{
    public class PlayerDamageValidator : TickNetworkBehaviour
    {
        [SerializeField] private float _checkRadius;
        [SerializeField] private Vector2 _checkOffset;
        [SerializeField] private PlayerMovement _movement;

        private List<Collider2D> _overlapColliders;
        
        protected override void TimeManager_OnTick()
        {
            if (!IsOwner)
                return;

            var tick = TimeManager.GetPreciseTick(TickType.LastPacketTick);
            Debug.Log($"[PlayerDamageValidator] Call validate on tick: {tick}, current tick: {TimeManager.Tick}, local tick: {TimeManager.LocalTick}");
            ValidateObstaclesServerRpc(tick);
        }

        [ServerRpc(RequireOwnership = true)]
        private void ValidateObstaclesServerRpc(PreciseTick tick)
        {
            RollbackManager.Rollback(tick, RollbackPhysicsType.Physics2D, IsOwner);
            
            _overlapColliders ??= new List<Collider2D>();
            Physics2D.OverlapCircle((Vector2)transform.position + _checkOffset, _checkRadius, new ContactFilter2D().NoFilter(), _overlapColliders);
            
            foreach (var overlapCollider in _overlapColliders)
            {
                if (!overlapCollider.TryGetComponent(out ObstacleController obstacleController))
                    continue;

                _movement.TakeDamage(obstacleController);
            }
            
            _overlapColliders.Clear();
            
            RollbackManager.Return();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position + (Vector3)_checkOffset, _checkRadius);
        }
#endif
    }
}