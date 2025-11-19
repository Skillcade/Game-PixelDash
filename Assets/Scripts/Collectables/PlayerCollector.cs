using System;
using FishNet.Object;
using UnityEngine;

namespace DefaultNamespace.Collectables
{
    [RequireComponent(typeof(Collider2D))]
    public class PlayerCollector : NetworkBehaviour
    {
        public event Action<PlayerCollector, CollectableBase, CollectableType> OnCollectedServer;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServerInitialized) return;
            if (!other || !other.TryGetComponent(out CollectableBase collectable)) return;
            if (!collectable.TryCollectServer(this)) return;
            
            OnCollectedServer?.Invoke(this, collectable, collectable.Type);
            collectable.OnCollectedServer(this);
        }
    }
}