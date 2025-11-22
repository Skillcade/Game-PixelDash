using FishNet.Object;
using Game.Player;
using UnityEngine;

namespace Game.Level
{
    public class ObstacleController : NetworkBehaviour
    {
        [SerializeField] public float Damage;

        private void OnTriggerEnter2D(Collider2D obj)
        {
            if (!IsServerInitialized)
                return;
            
            if (obj.TryGetComponent(out PlayerMovement movement))
                movement.TakeDamage(this);
        }
    }
}