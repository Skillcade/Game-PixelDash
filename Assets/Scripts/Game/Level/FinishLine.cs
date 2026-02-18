using System;
using FishNet.Object;
using Game.Player;
using SkillcadeSDK.FishNetAdapter.ColliderRollback;

namespace Game.Level
{
    public class FinishLine : RollbackTrigger
    {
        public event Action<int> OnPlayerReachedFinish;

        protected override void HandleTriggerServer_Internal(NetworkObject playerNetworkObject)
        {
            if (playerNetworkObject.TryGetComponent(out PlayerMovement _))
                OnPlayerReachedFinish?.Invoke(playerNetworkObject.OwnerId);
        }
    }
}