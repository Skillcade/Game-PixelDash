using FishNet.Object;
using Game.Level;
using Game.Player;
using UnityEngine;

namespace Game.Enemy
{
    public class EnemyController : NetworkBehaviour
    {
        [SerializeField] private Transform _spriteRoot;
        [SerializeField] private Transform _frontHitbox;
        [SerializeField] private Animator _animator;
        [SerializeField] private ServerMovableObject _platform;
        
        private Vector3 _lastPosition;
        
        private void Start()
        {
            _lastPosition = transform.position;
        }
        
        private void Update()
        {
            float dx = transform.position.x - _lastPosition.x;
            _lastPosition = transform.position;

            if (Mathf.Abs(dx) > 0.001f)
            {
                float direction = Mathf.Sign(dx);
                Vector3 scale = _spriteRoot.localScale;
                scale.x = Mathf.Abs(scale.x) * direction;
                _spriteRoot.localScale = scale;
            }

            Vector3 position = _frontHitbox.localPosition;
            position.x = Mathf.Abs(position.x) * Mathf.Sign(_spriteRoot.localScale.x);
            _frontHitbox.localPosition = position;
        }
        
        public void OnFrontHit(PlayerMovement player)
        {
            if (!IsServer)
            {
                return;
            }
            
            AttackRpc();
        }
        
        [ObserversRpc]
        private void AttackRpc()
        {
            _animator.SetTrigger("Attack");
        }
        
    }
}