using Game.GameFeel;
using Game.Player;
using UnityEngine;

namespace Game.Camera
{
    public class GameCameraTarget : MonoBehaviour
    {
        [Header("Horizontal go-ahead")]
        [SerializeField] private float _rightBaseDistance = 1f;
        [SerializeField] private float _rightSpeedFactor = 0.05f;
        [SerializeField] private float _leftBaseDistance = 1f;
        [SerializeField] private float _leftSpeedFactor = 0.05f;

        [Header("Shake (optional)")]
        [SerializeField] private CameraShaker _cameraShaker;

        [Header("Vertical go-ahead")]
        [SerializeField] private float _upBaseDistance = 0.5f;
        [SerializeField] private float _upSpeedFactor = 0.02f;
        [SerializeField] private float _downBaseDistance = 0.5f;
        [SerializeField] private float _downSpeedFactor = 0.02f;

        [Header("Vertical when airborne")]
        [SerializeField, Range(0f, 1f)] private float _verticalAirMultiplier = 0.2f;

        [Header("Look input")]
        [SerializeField] private float _lookUpDistance = 2f;
        [SerializeField] private float _lookDownDistance = 2f;

        [Header("Orientation")]
        [SerializeField] private float _velocityThreshold = 0.01f;

        [Header("Movement")]
        [SerializeField] private float _followSpeed = 15f;

        private PlayerMovement _followTarget;
        private float _lastFacing = 1f;

        public void SetFollowTarget(PlayerMovement movement)
        {
            _followTarget = movement;
            if (movement != null)
            {
                transform.position = GetPlayerPosition(movement);
                _lastFacing = 1f;
            }
        }

        public void ClearFollowTarget()
        {
            _followTarget = null;
        }

        private static Vector3 GetPlayerPosition(PlayerMovement movement)
        {
            if (movement?.VisualsTransform != null)
                return movement.VisualsTransform.position;
            return movement != null ? movement.transform.position : Vector3.zero;
        }

        private void LateUpdate()
        {
            if (_followTarget == null)
                return;

            Vector2 vel = _followTarget.VelocityVisual;
            bool isGrounded = _followTarget.IsGroundedVisual;
            Vector3 playerPos = GetPlayerPosition(_followTarget);

            float facing = Mathf.Abs(vel.x) > _velocityThreshold ? Mathf.Sign(vel.x) : _lastFacing;
            _lastFacing = facing;

            // Horizontal: pick right or left distances based on facing
            float hBase = facing > 0f ? _rightBaseDistance : _leftBaseDistance;
            float hFactor = facing > 0f ? _rightSpeedFactor : _leftSpeedFactor;
            float horizontalDistance = hBase + hFactor * Mathf.Abs(vel.x);

            // Vertical velocity-based offset
            float verticalSign = Mathf.Abs(vel.y) > _velocityThreshold ? Mathf.Sign(vel.y) : 0f;
            float vBase = verticalSign > 0f ? _upBaseDistance : _downBaseDistance;
            float vFactor = verticalSign > 0f ? _upSpeedFactor : _downSpeedFactor;
            float verticalDistanceRaw = vBase + vFactor * Mathf.Abs(vel.y);
            float verticalDistance = verticalDistanceRaw * (isGrounded ? 1f : _verticalAirMultiplier);

            // Look input offset (only on ground)
            float lookOffset = 0f;
            if (isGrounded)
            {
                float lookInput = _followTarget.LookVerticalInput;
                if (lookInput > 0f)
                    lookOffset = lookInput * _lookUpDistance;
                else if (lookInput < 0f)
                    lookOffset = lookInput * _lookDownDistance;
            }

            Vector2 offset = new Vector2(
                facing * horizontalDistance,
                verticalSign * verticalDistance + lookOffset
            );

            Vector3 target = playerPos + (Vector3)offset;
            target.z = transform.position.z;

            transform.position = Vector3.MoveTowards(transform.position, target, _followSpeed * Time.deltaTime);

            // Add the shake offset on top of the followed position. Cinemachine then
            // damps toward this jittered target each frame and produces a real shake,
            // without any execution-order tricks against the brain.
            if (_cameraShaker != null)
            {
                Vector2 shake = _cameraShaker.CurrentOffset;
                if (shake.sqrMagnitude > 0f)
                    transform.position += new Vector3(shake.x, shake.y, 0f);
            }
        }
    }
}
