using UnityEngine;

namespace Game.Level
{
    [DefaultExecutionOrder(1000)]
    public class ParallaxLayer : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float _parallaxFactorX = 0.1f;
        [SerializeField, Range(0f, 1f)] private float _parallaxFactorY = 0.05f;
        [SerializeField] private float _smoothSpeedX = 8f;
        [SerializeField] private float _smoothSpeedY = 2f;

        private Transform _cam;
        private Vector3 _camStartPos;
        private Vector3 _startPos;
        private Vector3 _currentPos;

        private void Start()
        {
            _cam = UnityEngine.Camera.main?.transform;
            if (_cam == null) return;

            _camStartPos = _cam.position;
            _startPos = transform.position;
            _currentPos = _startPos;
        }

        private void LateUpdate()
        {
            if (_cam == null) return;

            Vector3 camDelta = _cam.position - _camStartPos;
            Vector3 target = new Vector3(
                _startPos.x + camDelta.x * _parallaxFactorX,
                _startPos.y + camDelta.y * _parallaxFactorY,
                _startPos.z
            );

            _currentPos.x = Mathf.Lerp(_currentPos.x, target.x, _smoothSpeedX * Time.deltaTime);
            _currentPos.y = Mathf.Lerp(_currentPos.y, target.y, _smoothSpeedY * Time.deltaTime);
            _currentPos.z = _startPos.z;
            transform.position = _currentPos;
        }
    }
}
