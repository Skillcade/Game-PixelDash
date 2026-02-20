using UnityEngine;

namespace PGP.Core.Game.Network
{
    [CreateAssetMenu(fileName = "SnapshotInterpolationSettings", menuName = "Configs/Snapshot Interpolation Settings")]
    internal class SnapshotInterpolationSettings : ScriptableObject
    {
        [SerializeField] internal float BufferTimeMultiplier;
        [SerializeField] internal int BufferLimit;
        [SerializeField] internal float CatchupNegativeThreshold;
        [SerializeField] internal float CatchupPositiveThreshold;
        [SerializeField] internal float CatchupSpeed;
        [SerializeField] internal float SlowDownSpeed;
        [SerializeField] internal int DriftEmaDuration;
        [SerializeField] internal bool DynamicAdjustment;
        [SerializeField] internal float DynamicAdjustmentTolerance;
        [SerializeField] internal int DeliveryTimeEmaDuration;
        [SerializeField] internal int RemoteBufferOffset;
    }
}