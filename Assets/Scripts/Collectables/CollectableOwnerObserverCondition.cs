using FishNet.Connection;
using FishNet.Observing;
using UnityEngine;

namespace Collectables
{
    [CreateAssetMenu(fileName = "CollectableOwnerObserverCondition", menuName = "FishNet/Observer Conditions/Collectable Owner")]
    public class CollectableOwnerObserverCondition : ObserverCondition
    {
        private CollectableOwner _owner;

        public override ObserverConditionType GetConditionType() => ObserverConditionType.Normal;

        public override void Initialize(FishNet.Object.NetworkObject networkObject)
        {
            base.Initialize(networkObject);
            _owner = networkObject.GetComponent<CollectableOwner>();
        }

        public override void Deinitialize(bool destroyed)
        {
            _owner = null;
            base.Deinitialize(destroyed);
        }

        public override bool ConditionMet(NetworkConnection connection, bool currentlyAdded, out bool notProcessed)
        {
            notProcessed = false;
            if (_owner == null || connection == null)
                return false;

            return connection.ClientId == _owner.OwnerClientId;
        }
    }
}
