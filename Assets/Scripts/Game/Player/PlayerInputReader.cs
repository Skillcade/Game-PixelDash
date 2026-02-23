using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    public class PlayerInputReader : MonoBehaviour
    {
        private PlayerControls _playerControls;

        public float Movement { get; private set; }
        public float VerticalInput { get; private set; }

        // One-tick pulse: jump was pressed
        public bool Jump { get; private set; }

        // One-tick pulse: jump was released
        public bool JumpReleased { get; private set; }

        // Continuous state: is jump currently held
        public bool JumpHeld { get; private set; }

        public void ClearInput()
        {
            Jump = false;
            JumpReleased = false;
            // DO NOT clear JumpHeld here; it represents current hold state.
        }

        private void OnEnable()
        {
            _playerControls = new PlayerControls();
            _playerControls.Enable();

            _playerControls.Player.Move.performed += OnMovePerformed;
            _playerControls.Player.Move.canceled += OnMovePerformed;

            _playerControls.Player.Jump.performed += OnJumpPerformed;
            _playerControls.Player.Jump.canceled += OnJumpCanceled;
        }

        private void OnDisable()
        {
            if (_playerControls != null)
            {
                _playerControls.Player.Move.performed -= OnMovePerformed;
                _playerControls.Player.Move.canceled -= OnMovePerformed;

                _playerControls.Player.Jump.performed -= OnJumpPerformed;
                _playerControls.Player.Jump.canceled -= OnJumpCanceled;

                _playerControls.Disable();
                _playerControls = null;
            }
        }

        private void OnMovePerformed(InputAction.CallbackContext ctx)
        {
            Vector2 value = ctx.ReadValue<Vector2>();
            Movement = value.x;
            VerticalInput = value.y;
        }

        private void OnJumpPerformed(InputAction.CallbackContext ctx)
        {
            Jump = true;        // pulse
            JumpHeld = true;    // held state
        }

        private void OnJumpCanceled(InputAction.CallbackContext ctx)
        {
            JumpHeld = false;       // released
            JumpReleased = true;    // pulse
        }
    }
}