using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(fileName = "PlayerMovementConfig", menuName = "Configs/Player Movement Config", order = 1)]
    public class PlayerMovementConfig : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField] public float _speed;
        [SerializeField] public float _groundAcceleration;
        [SerializeField] public float _groundDeceleration;
        [SerializeField] public float _airAcceleration;
        [SerializeField] public float _airDeceleration;
        
        [Header("Jump")]
        [SerializeField] public float _maxFallSpeed;
        [SerializeField] public float _jumpHeight;
        [SerializeField] public float _coyoteTime;
        
        [Header("Ground check")]
        [SerializeField] public float _groundCheckOffset;
        [SerializeField] public float _groundCheckDistance;
        [SerializeField] public LayerMask _groundMask;
        
        [Header("Common")]
        [SerializeField] public float _maxHealth;
        [SerializeField] public float _knockbackForce;
    }
}