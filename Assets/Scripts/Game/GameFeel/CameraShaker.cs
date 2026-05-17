using UnityEngine;

namespace Game.GameFeel
{
    /// <summary>
    /// Lightweight unscaled-time camera shake.
    /// Place on the rendering Camera (the one with CinemachineBrain). Runs in LateUpdate after
    /// the brain has written the camera transform, so the offset is applied on top each frame.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class CameraShaker : MonoBehaviour
    {
        private float _duration;
        private float _elapsed;
        private float _intensity;
        private Vector3 _baseLocalPosition;
        private bool _baseCaptured;

        public void Shake(float intensity, float duration)
        {
            if (intensity <= 0f || duration <= 0f)
                return;

            // Restart only if the new shake is at least as strong as what is already running,
            // so a stomp during a tiny landing-shake does not get drowned out.
            if (_elapsed < _duration && intensity < _intensity * (1f - _elapsed / _duration))
                return;

            _intensity = intensity;
            _duration = duration;
            _elapsed = 0f;

            if (!_baseCaptured)
            {
                _baseLocalPosition = transform.localPosition;
                _baseCaptured = true;
            }
        }

        private void LateUpdate()
        {
            if (_elapsed >= _duration)
            {
                if (_baseCaptured)
                {
                    transform.localPosition = _baseLocalPosition;
                    _baseCaptured = false;
                }
                return;
            }

            _elapsed += Time.unscaledDeltaTime;
            float falloff = 1f - Mathf.Clamp01(_elapsed / _duration);
            Vector2 jitter = Random.insideUnitCircle * (_intensity * falloff);
            transform.localPosition = _baseLocalPosition + new Vector3(jitter.x, jitter.y, 0f);
        }
    }
}
