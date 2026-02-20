using UnityEngine;

namespace PGP.Core.Game.Network
{
    internal struct TransformSnapshot : IInterpolateSnapshot
    {
        public float RemoteTime { get; set; }
        public float LocalTime { get; set; } // Will be set from interpolation
        public Vector3 Position;
        public Quaternion Rotation;

        internal static TransformSnapshot Interpolate(TransformSnapshot from, TransformSnapshot to, float t)
        {
            return new TransformSnapshot
            {
                RemoteTime = 0,
                LocalTime = 0,
                Position = Vector3.LerpUnclamped(from.Position, to.Position, (float)t),
                Rotation = Quaternion.SlerpUnclamped(from.Rotation, to.Rotation, (float)t),
            };
        }
    }
}
