using System;
using FishNet.Object;

namespace Collectables
{
    public class PlayerCollector : NetworkBehaviour
    {
        public event Action<PlayerCollector, CollectableBase> OnCollectedServer;

        public void CollectServer(CollectableBase collectable)
        {
            if (!collectable.TryCollectServer(this))
                return;

            OnCollectedServer?.Invoke(this, collectable);
            collectable.OnCollectedServer(this);
        }
    }
}