using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace Game.Replays
{
    public class ReplayCameraTarget : MonoBehaviour, IInitializable
    {
        [SerializeField] private float _speed;
        [SerializeField] private Vector2 _horizontalBounds;
        [SerializeField] private Vector2 _vecticalBounds;
        
        private Vector2 _movement;
        private PlayerControls _playerControls;
        
        public void Initialize()
        {
            var targetCamera = FindAnyObjectByType<CinemachineCamera>(FindObjectsInactive.Include);
            if (targetCamera != null)
                targetCamera.Target.TrackingTarget = transform;
        }

        private void OnEnable()
        {
            _playerControls = new PlayerControls();
            _playerControls.Enable();
            _playerControls.Player.Move.performed += OnMovePerformed;
            _playerControls.Player.Move.canceled += OnMovePerformed;
        }

        private void OnDisable()
        {
            if (_playerControls != null)
            {
                _playerControls.Player.Move.performed -= OnMovePerformed;
                _playerControls.Player.Move.canceled -= OnMovePerformed;
                _playerControls.Disable();
                _playerControls = null;
            }
        }

        private void OnMovePerformed(InputAction.CallbackContext ctx)
        {
            _movement = ctx.ReadValue<Vector2>();
        }

        private void Update()
        {
            if (_movement.sqrMagnitude <= 0.01f)
                return;
            
            var position = transform.position;
            position += (Vector3)_movement * _speed * Time.deltaTime;
            
            position.x = Mathf.Clamp(position.x, _horizontalBounds.x, _horizontalBounds.y);
            position.y = Mathf.Clamp(position.y, _vecticalBounds.x, _vecticalBounds.y);
            
            transform.position = position;
        }
    }
}