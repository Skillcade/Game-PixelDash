namespace PGP.Core.Game.Network
{
    internal struct TimeSnapshot : IInterpolateSnapshot
    {
        public float RemoteTime { get; set; }
        public float LocalTime { get; set; }

        internal TimeSnapshot(float remoteTime, float localTime)
        {
            RemoteTime = remoteTime;
            LocalTime = localTime;
        }
    }
}