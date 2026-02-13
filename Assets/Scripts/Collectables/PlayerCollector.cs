using System;
using FishNet.Object;
using UnityEngine;

namespace Collectables
{
    [RequireComponent(typeof(Collider2D))]
    public class PlayerCollector : NetworkBehaviour
    {
        public event Action<PlayerCollector, CollectableBase, CollectableType> OnCollectedServer;

        public void CollectServer(CollectableBase collectable)
        {
            Debug.Log($"[PlayerCollector] Collect {collectable.gameObject.name}");
            if (!collectable.TryCollectServer(this))
                return;
            
            OnCollectedServer?.Invoke(this, collectable, collectable.Type);
            collectable.OnCollectedServer(this);
        }
    }
}