using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Utility.Template;
using Game.Level;
using Game.StateMachine;
using SkillcadeSDK;
using UnityEngine;
using VContainer;

namespace Game.Player
{
    public struct PlayerInput
    {
        public float Movement;
        public bool Jump;

        public void Reset()
        {
            Movement = 0f;
            Jump = false;
        }
    }
    
    [System.Serializable]
    public struct RuntimeMoveValues
    {
        public float Speed;

        public void ResetToConfig(PlayerMovementConfig cfg)
        {
            Speed = cfg._speed;
        }
    }
    
    public class PlayerMovement : TickNetworkBehaviour
    {
        public event System.Action JumpFx;

        public Vector2 VelocityVisual => IsOwner ? _rigidbody.linearVelocity : _velocitySync.Value;
        public bool IsGroundedVisual => IsOwner ? _isGrounded : _isGroundedSync.Value;
        public float Health01 => Mathf.Clamp01(_healthSync.Value / _playerMovementConfig._maxHealth);
        private bool CanMove => _knockbackTimer <= 0 && _healthSync.Value > 0 && _gameStateMachine.CurrentStateType == GameStateType.Running;
        
        public PlayerMovementConfig Config => _playerMovementConfig;
        
        public RuntimeMoveValues MoveValues { get; set; }

        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private PlayerInputReader _inputReader;
        [SerializeField] private PlayerMovementConfig _playerMovementConfig;
        [SerializeField] private Collider2D _collider;

        [SerializeField] private bool _groundedDebugSync;
        
        [Inject] private readonly GameStateMachine _gameStateMachine;

        private readonly SyncVar<float> _healthSync = new SyncVar<float>(new SyncTypeSettings(WritePermission.ServerOnly));
        private readonly SyncVar<Vector2> _velocitySync = new SyncVar<Vector2>(new SyncTypeSettings(WritePermission.ServerOnly));
        private readonly SyncVar<bool> _isGroundedSync = new SyncVar<bool>(new SyncTypeSettings(WritePermission.ServerOnly));
        
        private bool _isGrounded;
        private float _knockbackTimer;
        private float _coyoteTimer;

        private PlayerInput _lastCreatedInput;

        public override void OnStartNetwork()
        {
            this.InjectToMe();

            _knockbackTimer = 0f;
            _healthSync.SetInitialValues(_playerMovementConfig._maxHealth);
            MoveValues.ResetToConfig(_playerMovementConfig);
        }
        
        protected override void TimeManager_OnTick() => SimulateInputs(GetInput());

        private PlayerInput GetInput()
        {
            if (!IsOwner)
                return default;

            var input = new PlayerInput { Movement = _inputReader.Movement, Jump = _inputReader.Jump };
            
            _inputReader.ClearInput();
            return input;
        }
        
        private void SimulateInputs(PlayerInput input)
        {
            _groundedDebugSync = _isGroundedSync.Value;
            if (!IsOwner)
                return;
            
            float dt = (float)TimeManager.TickDelta;

            UpdateKnockback(dt);
            Move(input, dt);
            
            UpdateGrounded();

            if (_isGrounded)
                _coyoteTimer = _playerMovementConfig._coyoteTime;
            else
                _coyoteTimer -= (float)TimeManager.TickDelta;

            CapFallVelocity();
            SetMovementValuesServerRpc(_rigidbody.linearVelocity, _isGrounded);
        }

        private void UpdateGrounded()
        {
            _isGrounded = _collider.IsTouchingLayers(_playerMovementConfig._groundMask);
        }

        private void UpdateKnockback(float dt)
        {
            if (_knockbackTimer > 0f)
            {
                _knockbackTimer -= dt;
            }
            else if (_knockbackTimer <= 0f && _healthSync.Value <= 0f)
            {
                Debug.Log("Respawning");
                _rigidbody.linearVelocity = Vector2.zero;
                _rigidbody.position = Vector2.zero;
                RespawnServerRpc();
            }
        }

        private void Move(PlayerInput input, float dt)
        {
            if (!CanMove)
                input.Reset();
            
            Vector2 currentVelocity = _rigidbody.linearVelocity;
            if (Mathf.Abs(input.Movement) > 0.01f)
            {
                float targetSpeed = input.Movement * MoveValues.Speed;

                float acceleration = _isGrounded ? _playerMovementConfig._groundAcceleration : _playerMovementConfig._airAcceleration;
                float newX = Mathf.MoveTowards(currentVelocity.x, targetSpeed, acceleration * dt);
                _rigidbody.linearVelocity = new Vector2(newX, currentVelocity.y);
            }
            else
            {
                float deceleration = _isGrounded ? _playerMovementConfig._groundDeceleration : _playerMovementConfig._airDeceleration;
                float newX = Mathf.MoveTowards(currentVelocity.x, 0f, deceleration * dt);
                _rigidbody.linearVelocity = new Vector2(newX, currentVelocity.y);
            }

            if (input.Jump && (_isGrounded || _coyoteTimer > 0f))
            {
                Jump();
            }
        }

        private void CapFallVelocity()
        {
            Vector2 velocity = _rigidbody.linearVelocity;
            if (velocity.y < -_playerMovementConfig._maxFallSpeed)
            {
                velocity.y = -_playerMovementConfig._maxFallSpeed;
                _rigidbody.linearVelocity = velocity;
            }
        }
        
        private void Jump()
        {
            float jumpForce = Mathf.Sqrt(Mathf.Abs(-2.0f * Physics.gravity.y * _playerMovementConfig._jumpHeight * _rigidbody.gravityScale));
            if (_rigidbody.linearVelocity.y < 0f)
            {
                jumpForce -= _rigidbody.linearVelocity.y;
            }

            JumpFx?.Invoke();
            TriggerJumpServerRpc();

            _rigidbody.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            _isGrounded = false;
        }
        
        public void TakeDamage(ObstacleController obstacleController)
        {
            if (!IsServerInitialized)
                return;
            
            _healthSync.Value -= obstacleController.Damage;
            if (_healthSync.Value <= 0)
                return;
            
            KnockbackClientRpc(obstacleController.transform.position);
        }
        
        [ObserversRpc]
        private void KnockbackClientRpc(Vector3 attackerPosition)
        {
            if (!IsOwner)
                return;
            
            Vector2 direction = (transform.position - attackerPosition).normalized;
            if (Mathf.Abs(direction.x) < 0.1f)
            {
                direction.x = transform.position.x >= attackerPosition.x ? 1f : -1f;
            }

            direction.y = Mathf.Abs(direction.y);
            direction.Normalize();

            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.AddForce(direction * _playerMovementConfig._knockbackForce, ForceMode2D.Impulse);
            _knockbackTimer = .5f;
        }

        [ServerRpc(RequireOwnership = true)]
        private void RespawnServerRpc()
        {
            _healthSync.Value = _playerMovementConfig._maxHealth;
        }

        [ServerRpc(RequireOwnership = true)]
        private void SetMovementValuesServerRpc(Vector2 velocity, bool isGrounded)
        {
            _velocitySync.Value = velocity;
            _isGroundedSync.Value = isGrounded;
        }

        [ServerRpc(RequireOwnership = true)]
        private void TriggerJumpServerRpc()
        {
            JumpFx?.Invoke();
            TriggerJumpObserversRpc();
        }

        [ObserversRpc(ExcludeOwner = true)]
        private void TriggerJumpObserversRpc()
        {
            Debug.Log("[PlayerMovement] Trigger jump");
            JumpFx?.Invoke();
        }
    }
}