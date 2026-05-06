using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Utility.Template;
using Game.Level;
using Game.RigidbodyInterpolation;
using SkillcadeSDK;
using SkillcadeSDK.FishNetAdapter;
using SkillcadeSDK.StateMachine;
using UnityEngine;
using VContainer;

namespace Game.Player
{
    public struct PlayerInput
    {
        public float Movement;
        public bool Jump;
        public bool JumpReleased;

        public void Reset()
        {
            Movement = 0f;
            Jump = false;
            JumpReleased = false;
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
        private const float JUMP_CUT_MULTIPLIER = 0.5f; // lower = shorter tap jump, higher = less difference
        
        public event System.Action JumpFx;
        public event System.Action OnCharacterNameChanged;

        public Vector2 VelocityVisual => IsOwner ? _rigidbody.Rigidbody.linearVelocity : _derivedVelocity;
        public bool IsGroundedVisual => IsOwner ? _isGrounded : _derivedIsGrounded;
        public float Health01 => Mathf.Clamp01(_healthSync.Value / _playerMovementConfig._maxHealth);
        private bool CanMove => _knockbackTimer <= 0 && _healthSync.Value > 0 && _gameStateMachine.CurrentStateType == GameStateType.Running;
        public Transform VisualsTransform => _visualsTransform;
        public string CharacterName => _characterName.Value;
        
        public PlayerMovementConfig Config => _playerMovementConfig;
        
        public RuntimeMoveValues MoveValues { get; set; }
        public float LookVerticalInput => IsOwner ? _inputReader.VerticalInput : 0f;

        [SerializeField] private NetworkRigidbody2DInterpolator _rigidbody;
        [SerializeField] private PlayerInputReader _inputReader;
        [SerializeField] private PlayerMovementConfig _playerMovementConfig;
        [SerializeField] private Collider2D _collider;
        [SerializeField] private Transform _visualsTransform;
        [SerializeField] private bool _debugInputLogs = true;

        [Inject] private readonly SkillcadeGameStateMachine _gameStateMachine;

        private readonly SyncVar<float> _healthSync = new(new SyncTypeSettings(WritePermission.ServerOnly));
        private readonly SyncVar<string> _characterName = new(new SyncTypeSettings(WritePermission.ServerOnly));

        private bool _isGrounded;
        private float _knockbackTimer;
        private float _coyoteTimer;
        private float _jumpTimer;

        private Vector2 _spawnPoint;
        private string _characterNameToSet;

        // Derived state for remote clients — computed from NetworkTransform position, no extra network traffic
        private Vector3 _prevPosition;
        private Vector2 _derivedVelocity;
        private bool _derivedIsGrounded;
        
        private PlayerInput _lastCreatedInput;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            this.InjectToMe();

            _spawnPoint = transform.position;
            _prevPosition = transform.position;

            _knockbackTimer = 0f;
            _healthSync.SetInitialValues(_playerMovementConfig._maxHealth);
            MoveValues.ResetToConfig(_playerMovementConfig);
            
            _characterName.OnChange += HandleCharacterNameChanged;

            if (IsServerInitialized)
            {
                if (_characterName.Value != _characterNameToSet)
                    _characterName.Value = _characterNameToSet;
            }
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            _characterName.OnChange -= HandleCharacterNameChanged;
        }

        public void SetCharacterName(string characterName)
        {
            Debug.Log($"[PlayerCharacter] Player {OwnerId} set character name: {characterName}");
            if (!IsServerInitialized)
            {
                _characterNameToSet = characterName;
                return;
            }
            
            _characterName.Value = characterName;
        }

        protected override void TimeManager_OnTick()
        {
            if (IsOwner)
            {
                SimulateInputs(GetInput());
            }
            else
            {
                Vector3 currentPos = transform.position;
                _derivedVelocity = (currentPos - _prevPosition) / (float)TimeManager.TickDelta;
                _prevPosition = currentPos;
                var origin = currentPos + Vector3.up * _playerMovementConfig._groundCheckOffset;
                
                bool hitTriggers = Physics2D.queriesHitTriggers;
                Physics2D.queriesHitTriggers = false;
                _derivedIsGrounded = Physics2D.Raycast(origin, Vector2.down,
                    _playerMovementConfig._groundCheckDistance, _playerMovementConfig._groundMask);
                Physics2D.queriesHitTriggers = hitTriggers;
            }
        }

        private PlayerInput GetInput()
        {
            if (!IsOwner)
                return default;

            var input = new PlayerInput
            {
                Movement = _inputReader.Movement,
                Jump = _inputReader.Jump,
                JumpReleased = _inputReader.JumpReleased
            };
            
            if (_debugInputLogs && (Mathf.Abs(input.Movement) > 0.01f || input.Jump || input.JumpReleased))
            {
                Debug.Log($"[PlayerMovement] Tick input: movement={input.Movement}, jump={input.Jump}, " +
                          $"jumpReleased={input.JumpReleased}, grounded={_isGrounded}, coyote={_coyoteTimer}, jumpTimer={_jumpTimer}");
            }
            
            _inputReader.ClearInput();
            return input;
        }

        private void SimulateInputs(PlayerInput input)
        {
            if (!IsOwner)
                return;
            
            float dt = (float)TimeManager.TickDelta;

            if (_jumpTimer > 0f)
                _jumpTimer -= dt;

            UpdateKnockback(dt);
            Move(input, dt);
            ApplyJumpCut(input);
            
            UpdateGrounded();

            if (_isGrounded)
                _coyoteTimer = _playerMovementConfig._coyoteTime;
            else
                _coyoteTimer -= (float)TimeManager.TickDelta;

            CapFallVelocity();
        }

        private void UpdateGrounded()
        {
            bool hitTriggers = Physics2D.queriesHitTriggers;
            Physics2D.queriesHitTriggers = false;
            var origin = transform.position + Vector3.up * _playerMovementConfig._groundCheckOffset;
            _isGrounded = Physics2D.Raycast(origin, Vector2.down, _playerMovementConfig._groundCheckDistance,
                _playerMovementConfig._groundMask);
            Physics2D.queriesHitTriggers = hitTriggers;
        }

        private void UpdateKnockback(float dt)
        {
            if (_knockbackTimer > 0f)
            {
                _knockbackTimer -= dt;
            }
            else if (_knockbackTimer <= 0f && _healthSync.Value <= 0f)
            {
                _rigidbody.Teleport(_spawnPoint, resetVelocity: true);
                RespawnServerRpc();
            }
        }

        private void Move(PlayerInput input, float dt)
        {
            bool canMove = CanMove;
            bool jumpRequestedBeforeReset = input.Jump;
            
            if (!canMove)
                input.Reset();
            
            Vector2 currentVelocity = _rigidbody.Rigidbody.linearVelocity;
            if (Mathf.Abs(input.Movement) > 0.01f)
            {
                float targetSpeed = input.Movement * MoveValues.Speed;

                float acceleration = _isGrounded ? _playerMovementConfig._groundAcceleration : _playerMovementConfig._airAcceleration;
                float newX = Mathf.MoveTowards(currentVelocity.x, targetSpeed, acceleration * dt);
                _rigidbody.Rigidbody.linearVelocity = new Vector2(newX, currentVelocity.y);
            }
            else
            {
                float deceleration = _isGrounded ? _playerMovementConfig._groundDeceleration : _playerMovementConfig._airDeceleration;
                float newX = Mathf.MoveTowards(currentVelocity.x, 0f, deceleration * dt);
                _rigidbody.Rigidbody.linearVelocity = new Vector2(newX, currentVelocity.y);
            }

            if (input.Jump && (_isGrounded || _coyoteTimer > 0f) && _jumpTimer <= 0f)
            {
                Jump();
                _jumpTimer = _playerMovementConfig._jumpDelay;
            }
            else if (_debugInputLogs && jumpRequestedBeforeReset)
            {
                Debug.Log($"[PlayerMovement] Jump rejected: canMove={canMove}, grounded={_isGrounded}, " +
                          $"coyote={_coyoteTimer}, jumpTimer={_jumpTimer}, movement={input.Movement}");
            }
        }

        private void CapFallVelocity()
        {
            Vector2 velocity = _rigidbody.Rigidbody.linearVelocity;
            if (velocity.y < -_playerMovementConfig._maxFallSpeed)
            {
                velocity.y = -_playerMovementConfig._maxFallSpeed;
                _rigidbody.Rigidbody.linearVelocity = velocity;
            }
        }

        private void Jump()
        {
            float jumpForce = Mathf.Sqrt(Mathf.Abs(-2.0f * Physics.gravity.y * _playerMovementConfig._jumpHeight * _rigidbody.Rigidbody.gravityScale));
            jumpForce -= _rigidbody.Rigidbody.linearVelocity.y;

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

            _rigidbody.Rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.AddForce(direction * _playerMovementConfig._knockbackForce, ForceMode2D.Impulse);
            _knockbackTimer = .5f;
        }

        [ServerRpc(RequireOwnership = true)]
        private void RespawnServerRpc()
        {
            _healthSync.Value = _playerMovementConfig._maxHealth;
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
            if (IsOwner)
                return;
            
            JumpFx?.Invoke();
        }

        private void ApplyJumpCut(PlayerInput input)
        {
            // Only the owning client is simulating physics here already.
            if (!IsOwner)
                return;

            // If player released jump while still traveling upward, cut the upward velocity.
            if (input.JumpReleased && _rigidbody.Rigidbody.linearVelocity.y > 0f)
            {
                Vector2 v = _rigidbody.Rigidbody.linearVelocity;
                v.y *= JUMP_CUT_MULTIPLIER;
                _rigidbody.Rigidbody.linearVelocity = v;
            }
        }

        private void HandleCharacterNameChanged(string prev, string next, bool asServer)
        {
            OnCharacterNameChanged?.Invoke();
        }
    }
}
