using FishNet.Object;
using Game.Level;
using UnityEngine;

namespace Game.Player
{
    public class PlayerPlatformAttacher : NetworkBehaviour
    {
        private Platform _attachedToPlatform;
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsOwner)
                return;

            if (!other.TryGetComponent(out Platform platform))
                return;

            if (_attachedToPlatform != null)
                return;

            _attachedToPlatform = platform;
            NetworkObject.SetParent(platform);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsOwner)
                return;

            if (!other.TryGetComponent(out Platform platform))
                return;

            if (_attachedToPlatform != platform)
                return;

            _attachedToPlatform = null;
            NetworkObject.UnsetParent();
        }
    }
}