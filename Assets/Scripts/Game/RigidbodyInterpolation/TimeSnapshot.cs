namespace Game.RigidbodyInterpolation
{
    public struct TimeSnapshot : IInterpolateSnapshot
    {
        public float RemoteTime { get; set; }
        public float LocalTime { get; set; }

        public TimeSnapshot(float remoteTime, float localTime)
        {
            RemoteTime = remoteTime;
            LocalTime = localTime;
        }
    }
}