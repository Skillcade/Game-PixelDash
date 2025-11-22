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

        public Vector2 Velocity => _rigidbody.linearVelocity;
        public float Health01 => Mathf.Clamp01(_health.Value / _playerMovementConfig._maxHealth);
        private bool CanMove => _knockbackTimer <= 0 && _health.Value > 0 && _gameStateMachine.CurrentStateType == GameStateType.Running;
        
        public PlayerMovementConfig Config => _playerMovementConfig;
        
        public RuntimeMoveValues MoveValues { get; set; }
        public bool IsGrounded { get; private set; }

        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private PlayerInputReader _inputReader;
        [SerializeField] private PlayerMovementConfig _playerMovementConfig;
        [SerializeField] private Collider2D _collider;

        [Inject] private readonly GameStateMachine _gameStateMachine;

        private readonly SyncVar<float> _health = new SyncVar<float>(new SyncTypeSettings(WritePermission.ServerOnly));
        
        private float _knockbackTimer;
        private float _coyoteTimer;

        private PlayerInput _lastCreatedInput;

        public override void OnStartNetwork()
        {
            this.InjectToMe();

            _knockbackTimer = 0f;
            _health.SetInitialValues(_playerMovementConfig._maxHealth);
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
            if (!IsOwner)
                return;
            
            float dt = (float)TimeManager.TickDelta;

            UpdateKnockback(dt);
            Move(input, dt);
            
            UpdateGrounded();

            if (IsGrounded)
                _coyoteTimer = _playerMovementConfig._coyoteTime;
            else
                _coyoteTimer -= (float)TimeManager.TickDelta;

            CapFallVelocity();
        }

        private void UpdateGrounded()
        {
            IsGrounded = _collider.IsTouchingLayers(_playerMovementConfig._groundMask);
        }

        private void UpdateKnockback(float dt)
        {
            if (_knockbackTimer > 0f)
            {
                _knockbackTimer -= dt;
            }
            else if (_knockbackTimer <= 0f && _health.Value <= 0f)
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

                float acceleration = IsGrounded ? _playerMovementConfig._groundAcceleration : _playerMovementConfig._airAcceleration;
                float newX = Mathf.MoveTowards(currentVelocity.x, targetSpeed, acceleration * dt);
                _rigidbody.linearVelocity = new Vector2(newX, currentVelocity.y);
            }
            else
            {
                float deceleration = IsGrounded ? _playerMovementConfig._groundDeceleration : _playerMovementConfig._airDeceleration;
                float newX = Mathf.MoveTowards(currentVelocity.x, 0f, deceleration * dt);
                _rigidbody.linearVelocity = new Vector2(newX, currentVelocity.y);
            }

            if (input.Jump && (IsGrounded || _coyoteTimer > 0f))
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

            _rigidbody.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            IsGrounded = false;
        }
        
        public void TakeDamage(ObstacleController obstacleController)
        {
            if (!IsServerInitialized)
                return;
            
            _health.Value -= obstacleController.Damage;
            if (_health.Value <= 0)
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
            _health.Value = _playerMovementConfig._maxHealth;
        }
    }
}