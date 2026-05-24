using FishNet.Managing.Timing;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// Scene-placed NetworkBehaviour singleton. Server stamps StartTick on spawn;
    /// clients read it via SyncVar. All TickBasedMoveBehaviour instances (e.g.,
    /// ServerMovableObject) use this single start tick so their positions are
    /// deterministic from (CurrentTick - StartTick).
    /// </summary>
    public class GlobalTickTimer : NetworkBehaviour
    {
        public static GlobalTickTimer Instance { get; private set; }

        private readonly SyncVar<uint> _startTick = new(new SyncTypeSettings(WritePermission.ServerOnly));

        public uint StartTick => _startTick.Value;
        public bool IsReady { get; private set; }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            Instance = this;

            if (IsServerInitialized)
            {
                _startTick.Value = TimeManager.Tick;
                IsReady = true;
            }
            else if (IsClientInitialized)
            {
                _startTick.OnChange += OnStartTickChanged;
            }
        }

        private void OnStartTickChanged(uint prev, uint next, bool asServer)
        {
            IsReady = true;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            if (Instance == this)
                Instance = null;
            
            if (IsClientInitialized)
                _startTick.OnChange -= OnStartTickChanged;
        }

        /// <summary>
        /// Elapsed seconds from StartTick to the whole tick of <paramref name="pt"/>.
        /// Sub-tick fraction is intentionally discarded so runtime Update and rollback
        /// use identical math (same tick in = same seconds out = same position).
        /// </summary>
        public float GetElapsedSeconds(PreciseTick pt)
        {
            if (!IsReady)
                return 0f;

            long elapsedTicks = (long)pt.Tick - _startTick.Value;
            return (float)(elapsedTicks * TimeManager.TickDelta);
        }
    }
}
