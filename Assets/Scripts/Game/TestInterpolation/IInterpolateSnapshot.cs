namespace PGP.Core.Game.Network
{
    public interface IInterpolateSnapshot
    {
        public float RemoteTime { get; set; }
        public float LocalTime { get; set; }
    }

    internal interface IInterpolateSnapshot<T> where T : struct, IInterpolateSnapshot<T>
    {
        internal void Interpolate(T from, T to, float t);
    }
}