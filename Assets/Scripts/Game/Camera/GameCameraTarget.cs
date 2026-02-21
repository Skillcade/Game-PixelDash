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

        [Header("Debug")]
        [SerializeField] private bool _enableDebugLogging;
        [SerializeField] private bool _enableVisualDebug = true;
        [SerializeField, Min(1)] private int _logEveryNFrames = 30;
        [SerializeField] private float _jitterThreshold = 0.05f;

        private PlayerMovement _followTarget;
        private float _lastFacing = 1f;
        private int _frameCount;

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

            Vector3 beforePosition = transform.position;
            transform.position = Vector3.MoveTowards(transform.position, target, _followSpeed * Time.deltaTime);
            Vector3 afterPosition = transform.position;

            if (_enableDebugLogging || _enableVisualDebug)
                UpdateDebug(playerPos, target, beforePosition, afterPosition, vel);
        }

        private void UpdateDebug(Vector3 playerPos, Vector3 target, Vector3 beforePos, Vector3 afterPos, Vector2 vel)
        {
            float moveDelta = (afterPos - beforePos).magnitude;
            bool possibleJitter = moveDelta > _jitterThreshold;

            if (_enableVisualDebug)
            {
                // Debug.DrawLine(playerPos, target, Color.green, 1f);
                // Debug.DrawLine(transform.position, target, Color.yellow, 1f);
                // if (possibleJitter)
                //     Debug.DrawLine(beforePos, afterPos, Color.red, 1f);
            }

            if (_enableDebugLogging && (possibleJitter || _frameCount % _logEveryNFrames == 0))
            {
                Debug.Log($"[GameCameraTarget] frame={Time.frameCount} " +
                    $"playerPos={playerPos} target={target} " +
                    $"posDelta={moveDelta:F4} vel={vel} " +
                    $"{(possibleJitter ? " [JITTER?]" : "")}");
            }

            _frameCount++;
        }
    }
}
