using Game.Player;
using UnityEngine;

namespace Game.Camera
{
    public class GameCameraTarget : MonoBehaviour
    {
        [Header("Go-ahead distances")]
        [SerializeField] private float _horizontalBaseDistance = 1f;
        [SerializeField] private float _horizontalSpeedFactor = 0.05f;
        [SerializeField] private float _verticalBaseDistance = 0.5f;
        [SerializeField] private float _verticalSpeedFactor = 0.02f;

        [Header("Vertical when airborne")]
        [SerializeField, Range(0f, 1f)] private float _verticalAirMultiplier = 0.2f;

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

            float horizontalDistance = _horizontalBaseDistance + _horizontalSpeedFactor * Mathf.Abs(vel.x);
            float verticalDistanceRaw = _verticalBaseDistance + _verticalSpeedFactor * Mathf.Abs(vel.y);
            float verticalDistance = verticalDistanceRaw * (isGrounded ? 1f : _verticalAirMultiplier);

            float verticalSign = Mathf.Abs(vel.y) > _velocityThreshold ? Mathf.Sign(vel.y) : 0f;

            Vector2 offset = new Vector2(
                facing * horizontalDistance,
                verticalSign * verticalDistance
            );

            Vector3 target = playerPos + (Vector3)offset;
            target.z = transform.position.z;

            transform.position = Vector3.MoveTowards(transform.position, target, _followSpeed * Time.deltaTime);
        }
    }
}
