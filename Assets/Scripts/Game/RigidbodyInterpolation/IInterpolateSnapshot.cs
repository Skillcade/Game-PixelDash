namespace Game.RigidbodyInterpolation
{
    public interface IInterpolateSnapshot
    {
        public float RemoteTime { get; set; }
        public float LocalTime { get; set; }
    }
}