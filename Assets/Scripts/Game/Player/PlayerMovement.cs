using FishNet.Object.Prediction;
using FishNet.Transporting;
using FishNet.Utility.Template;
using Game.Level;
using Game.StateMachine;
using GameKit.Dependencies.Utilities;
using SkillcadeSDK;
using UnityEngine;
using VContainer;

namespace Game.Player
{
    public struct PlayerInput : IReplicateData
    {
        private uint _tick;
        public float Movement;
        public bool Jump;
            
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
        public void Dispose() { }

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
    
    public struct PlayerState : IReconcileData
    {
        private uint _tick;

        public float KnockbackTimer;
        public float Health;
        public float CoyoteTimer;
        public PredictionRigidbody2D Rigidbody;
            
        public RuntimeMoveValues MoveValues;
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
        public void Dispose() { }
    }
    
    public class PlayerMovement : TickNetworkBehaviour
    {
        public event System.Action JumpFx;

        public Vector2 Velocity => _rigidbody.linearVelocity;
        public float Health01 => Mathf.Clamp01(_health / _playerMovementConfig._maxHealth);
        private bool CanMove => _knockbackTimer <= 0 && _health > 0;// && _gameStateMachine.CurrentState == GameStateType.Running;
        
        public PlayerMovementConfig Config => _playerMovementConfig;
        
        public RuntimeMoveValues MoveValues { get; set; }
        public bool IsGrounded { get; private set; }

        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private PlayerInputReader _inputReader;
        [SerializeField] private PlayerMovementConfig _playerMovementConfig;
        [SerializeField] private Collider2D _collider;

        [Inject] private readonly GameStateMachine _gameStateMachine;
        
        // private PredictionRigidbody2D _predictionRigidbody;

        private float _health;
        private float _knockbackTimer;
        private float _coyoteTimer;

        private PlayerInput _lastCreatedInput;

        public override void OnStartNetwork()
        {
            this.InjectToMe();
            // _predictionRigidbody = ObjectCaches<PredictionRigidbody2D>.Retrieve();
            // _predictionRigidbody.Initialize(_rigidbody);

            _knockbackTimer = 0f;
            _health = _playerMovementConfig._maxHealth;
            MoveValues.ResetToConfig(_playerMovementConfig);
        }
        
        public override void OnStopNetwork()
        {
            // ObjectCaches<PredictionRigidbody2D>.StoreAndDefault(ref _predictionRigidbody);
        }
        
        protected override void TimeManager_OnTick() => SimulateInputs(GetInput(), channel: Channel.Reliable);

        private PlayerInput GetInput()
        {
            if (!IsOwner)
                return default;

            var input = new PlayerInput { Movement = _inputReader.Movement, Jump = _inputReader.Jump };
            
            _inputReader.ClearInput();
            return input;
        }
        
        // [Replicate]
        // ReSharper disable once UnusedParameter.Local
        private void SimulateInputs(PlayerInput input, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Reliable)
        {
            if (!IsOwner)
                return;
            
            if (state.IsFuture())
            {
                if (_playerMovementConfig._predictInputs)
                    input = _lastCreatedInput;
            }
            else if (state.IsReplayedCreated())
            {
                _lastCreatedInput = input;
            }
            
            float dt = (float)TimeManager.TickDelta;

            UpdateKnockback(dt);
            Move(input, state, dt);
            
            UpdateGrounded();

            if (IsGrounded)
                _coyoteTimer = _playerMovementConfig._coyoteTime;
            else
                _coyoteTimer -= (float)TimeManager.TickDelta;

            CapFallVelocity();
            
            // _predictionRigidbody.AddForce(Physics2D.gravity);
            // _predictionRigidbody.Simulate();
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
            else if (_knockbackTimer <= 0f && _health <= 0f)
            {
                _rigidbody.linearVelocity = Vector2.zero;
                // _predictionRigidbody.Velocity(Vector2.zero);
                _rigidbody.position = Vector2.zero;
                _health = _playerMovementConfig._maxHealth;
            }
        }

        private void Move(PlayerInput input, ReplicateState state, float dt)
        {
            if (!CanMove)
                input.Reset();
            
            Vector2 currentVelocity = _rigidbody.linearVelocity;
            if (Mathf.Abs(input.Movement) > 0.01f)
            {
                float targetSpeed = input.Movement * MoveValues.Speed;

                float acceleration = IsGrounded ? _playerMovementConfig._groundAcceleration : _playerMovementConfig._airAcceleration;
                float newX = Mathf.MoveTowards(currentVelocity.x, targetSpeed, acceleration * dt);
                // _predictionRigidbody.Velocity(new Vector2(newX, currentVelocity.y));
                _rigidbody.linearVelocity = new Vector2(newX, currentVelocity.y);
            }
            else
            {
                float deceleration = IsGrounded ? _playerMovementConfig._groundDeceleration : _playerMovementConfig._airDeceleration;
                float newX = Mathf.MoveTowards(currentVelocity.x, 0f, deceleration * dt);
                // _predictionRigidbody.Velocity(new Vector2(newX, currentVelocity.y));
                _rigidbody.linearVelocity = new Vector2(newX, currentVelocity.y);
            }

            if (input.Jump && (IsGrounded || _coyoteTimer > 0f))
            {
                Jump(state);
            }
        }

        private void CapFallVelocity()
        {
            Vector2 velocity = _rigidbody.linearVelocity;
            if (velocity.y < -_playerMovementConfig._maxFallSpeed)
            {
                velocity.y = -_playerMovementConfig._maxFallSpeed;
                _rigidbody.linearVelocity = velocity;
                // _predictionRigidbody.Velocity(velocity);
            }
        }

        // protected override void TimeManager_OnPostTick()
        // {
        //     CreateReconcile();
        // }

        // public override void CreateReconcile()
        // {
        //     PlayerState state = new PlayerState
        //     {
        //         Rigidbody = _predictionRigidbody,
        //         KnockbackTimer = _knockbackTimer,
        //         Health = _health,
        //         CoyoteTimer = _coyoteTimer,
        //         MoveValues = MoveValues,
        //     };
        //     ReconcileState(state, Channel.Reliable);
        // }
        //
        // [Reconcile]
        // // ReSharper disable once UnusedParameter.Local
        // private void ReconcileState(PlayerState state, Channel channel = Channel.Reliable)
        // {
        //     _knockbackTimer = state.KnockbackTimer;
        //     _health = state.Health;
        //     _coyoteTimer = state.CoyoteTimer;
        //     MoveValues = state.MoveValues;
        //     _predictionRigidbody.Reconcile(state.Rigidbody);
        // }
        
        private void Jump(ReplicateState state)
        {
            float jumpForce = Mathf.Sqrt(Mathf.Abs(-2.0f * Physics.gravity.y * _playerMovementConfig._jumpHeight * _rigidbody.gravityScale));
            if (_rigidbody.linearVelocity.y < 0f)
            {
                jumpForce -= _rigidbody.linearVelocity.y;
            }

            if (state.ContainsTicked() && !state.ContainsReplayed())
            {
                JumpFx?.Invoke();
            }

            _rigidbody.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            // _predictionRigidbody.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            IsGrounded = false;
        }
        
        public void TakeDamage(ObstacleController obstacleController)
        {
            _health -= obstacleController.Damage;
            if (_health <= 0)
            {
                return;
            }
            Knockback(obstacleController.transform.position);
        }
        
        private void Knockback(Vector3 attackerPosition)
        {
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
    }
}