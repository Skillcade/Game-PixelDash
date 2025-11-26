using Game.Player;
using UnityEngine;

namespace Game.Enemy
{
    public class FrontHitboxTrigger : MonoBehaviour
    {
        [SerializeField] private EnemyController _controller;

        private void OnTriggerEnter2D(Collider2D collider)
        {
            if (collider.TryGetComponent(out PlayerMovement player))
            {
                _controller.OnFrontHit(player);
            }
        }
    }
}