using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    public class PlayerInputReader : MonoBehaviour
    {
        private PlayerControls _playerControls;
        public float Movement { get; private set; }
        public bool Jump { get; private set; }

        public void ClearInput()
        {
            Jump = false;
        }

        private void OnEnable()
        {
            _playerControls = new PlayerControls();
            _playerControls.Enable();
            _playerControls.Player.Move.performed += OnMovePerformed;
            _playerControls.Player.Move.canceled += OnMovePerformed;
            _playerControls.Player.Jump.performed += OnJumpPerformed;
        }

        private void OnDisable()
        {
            if (_playerControls != null)
            {
                _playerControls.Player.Move.performed -= OnMovePerformed;
                _playerControls.Player.Move.canceled -= OnMovePerformed;
                _playerControls.Player.Jump.performed -= OnJumpPerformed;
                _playerControls.Disable();
                _playerControls = null;
            }
        }

        private void OnMovePerformed(InputAction.CallbackContext ctx)
        {
            Movement = ctx.ReadValue<Vector2>().x;
        }

        private void OnJumpPerformed(InputAction.CallbackContext ctx)
        {
            Jump = true;
        }
    }
}