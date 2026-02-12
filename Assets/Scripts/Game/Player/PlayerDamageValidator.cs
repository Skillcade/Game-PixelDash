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
            Debug.Log($"[PlayerDamageValidator] Validate obstacles on tick {tick.Tick}, current: {TimeManager.Tick}");
            RollbackManager.Rollback(tick, RollbackPhysicsType.Physics2D, IsOwner);
            
            _overlapColliders ??= new List<Collider2D>();
            int results = Physics2D.OverlapCircle(transform.position, _checkRadius, new ContactFilter2D().NoFilter(), _overlapColliders);

            Debug.Log($"[PlayerDamageValidator] Overlap result: {results}");
            foreach (var overlapCollider in _overlapColliders)
            {
                Debug.Log($"[PlayerDamageValidator] Processing collider {overlapCollider.gameObject.name}");
                if (!overlapCollider.TryGetComponent(out ObstacleController obstacleController))
                    continue;

                Debug.Log($"[PlayerDamageValidator] Found obstacle with damage: {obstacleController.Damage}");
                _movement.TakeDamage(obstacleController);
            }
            
            _overlapColliders.Clear();
            
            RollbackManager.Return();
        }
    }
}