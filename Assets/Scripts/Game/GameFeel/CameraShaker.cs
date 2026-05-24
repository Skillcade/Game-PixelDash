using UnityEngine;

namespace Game.GameFeel
{
    /// <summary>
    /// Holds the current camera shake state. Does NOT touch any transform itself —
    /// the active shake offset is exposed via <see cref="CurrentOffset"/> and consumed
    /// by <see cref="Game.Camera.GameCameraTarget"/> at the end of its LateUpdate, so the
    /// offset is applied to the Cinemachine follow target right before the brain reads it.
    ///
    /// Touching the rendered camera's transform directly is unreliable because
    /// CinemachineBrain overwrites it from its own LateUpdate; offsetting the follow
    /// target instead produces a real smoothed camera shake without fighting the brain.
    /// </summary>
    public class CameraShaker : MonoBehaviour
    {
        private float _duration;
        private float _elapsed;
        private float _intensity;
        private Vector2 _currentOffset;

        /// <summary>
        /// Latest shake offset for this frame. Recomputed in Update so any consumer
        /// (regardless of execution order) sees the same value across LateUpdates.
        /// </summary>
        public Vector2 CurrentOffset => _currentOffset;

        public void Shake(float intensity, float duration)
        {
            if (intensity <= 0f || duration <= 0f)
                return;

            // Restart only if the new shake is at least as strong as what is already running,
            // so a tap-stomp during a tiny landing-shake doesn't drown out a fresh hit.
            float remaining = _duration - _elapsed;
            if (remaining > 0f && intensity < _intensity * (remaining / _duration))
                return;

            _intensity = intensity;
            _duration = duration;
            _elapsed = 0f;
        }

        private void Update()
        {
            if (_elapsed >= _duration)
            {
                _currentOffset = Vector2.zero;
                return;
            }

            _elapsed += Time.unscaledDeltaTime;
            float falloff = 1f - Mathf.Clamp01(_elapsed / _duration);
            _currentOffset = Random.insideUnitCircle * (_intensity * falloff);
        }
    }
}
