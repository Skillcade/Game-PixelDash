using FishNet.Object;
using UnityEngine;

namespace Game.Level
{
    public class ObjectRotator : NetworkBehaviour
    {
        [SerializeField] private float _rotationSpeed;
        [SerializeField] private Transform _rotateTarget;

        private bool _hasRotateTarget;

        private void Start()
        {
            _hasRotateTarget = _rotateTarget != null;
        }

        private void Update()
        {
            if (NetworkObject == null || !IsServerInitialized)
                return;

            var targetTransform = _hasRotateTarget ? _rotateTarget : transform;
            targetTransform.rotation *= Quaternion.Euler(0f, 0f, _rotationSpeed * Time.deltaTime);
        }
    }
}