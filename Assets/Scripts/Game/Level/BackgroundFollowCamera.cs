using UnityEngine;

namespace Game.Level
{
    public class BackgroundFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private Vector2 _cameraBounds;
        [SerializeField] private Vector2 _selfBounds;

        private void Update()
        {
            var cameraLerpValue = Mathf.InverseLerp(_cameraBounds.x, _cameraBounds.y, _cameraTransform.position.x);
            var selfPositionX = Mathf.Lerp(_selfBounds.x, _selfBounds.y, cameraLerpValue);
            transform.position = new Vector3(selfPositionX, transform.position.y, transform.position.z);
        }
    }
}