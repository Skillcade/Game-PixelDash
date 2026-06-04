using FishNet.Object;
using FishNet.Object.Synchronizing;

namespace Collectables
{
    /// <summary>
    /// Per-player collectable instance: only the owner connection may collect and observe it.
    /// </summary>
    public class CollectableOwner : NetworkBehaviour
    {
        private readonly SyncVar<int> _ownerClientId = new(new SyncTypeSettings(WritePermission.ServerOnly));

        public int OwnerClientId => _ownerClientId.Value;

        public void SetOwner(int clientId)
        {
            if (IsServerInitialized)
                _ownerClientId.Value = clientId;
        }
    }
}
