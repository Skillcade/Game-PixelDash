using UnityEngine;

namespace Game.RigidbodyInterpolation
{
    [CreateAssetMenu(fileName = "SnapshotInterpolationSettings", menuName = "Configs/Snapshot Interpolation Settings")]
    public class SnapshotInterpolationSettings : ScriptableObject
    {
        [SerializeField] public float BufferTimeMultiplier;
        [SerializeField] public int BufferLimit;
        [SerializeField] public float CatchupNegativeThreshold;
        [SerializeField] public float CatchupPositiveThreshold;
        [SerializeField] public float CatchupSpeed;
        [SerializeField] public float SlowDownSpeed;
        [SerializeField] public int DriftEmaDuration;
        [SerializeField] public bool DynamicAdjustment;
        [SerializeField] public float DynamicAdjustmentTolerance;
        [SerializeField] public int DeliveryTimeEmaDuration;
        [SerializeField] public int RemoteBufferOffset;
    }
}