using FishNet.Object;
using UnityEngine;

namespace Game.Level
{
    public class ObjectRotator : NetworkBehaviour
    {
        [SerializeField] private float _rotationSpeed;

        private void Update()
        {
            if (NetworkObject == null || !IsServerInitialized)
                return;
            
            transform.rotation *= Quaternion.Euler(0f, 0f, _rotationSpeed * Time.deltaTime);
        }
    }
}