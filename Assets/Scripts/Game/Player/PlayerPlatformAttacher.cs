using FishNet.Object;
using Game.Level;
using Game.RigidbodyInterpolation;
using UnityEngine;

namespace Game.Player
{
    public class PlayerPlatformAttacher : NetworkBehaviour
    {
        [SerializeField] private NetworkRigidbody2DInterpolator _rigidbodyInterpolator;
        [SerializeField] private Platform _attachedToPlatform;
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsOwner)
                return;

            var platform = other.GetComponentInParent<Platform>();
            if (platform == null)
                return;

            if (_attachedToPlatform != null)
                return;

            _attachedToPlatform = platform;
            NetworkObject.SetParent(platform);
            
            if (_rigidbodyInterpolator != null)
                _rigidbodyInterpolator.Ignore = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsOwner)
                return;

            var platform = other.GetComponentInParent<Platform>();
            if (platform == null)
                return;

            if (_attachedToPlatform != platform)
                return;

            _attachedToPlatform = null;
            NetworkObject.UnsetParent();
            
            if (_rigidbodyInterpolator != null)
                _rigidbodyInterpolator.Ignore = false;
        }
    }
}