using FishNet.Broadcast;

namespace Game.Level
{
    public struct PlayerProgressEntry
    {
        public int PlayerId;
        public float Progress;
    }

    public struct PlayerProgressBroadcast : IBroadcast
    {
        public PlayerProgressEntry[] Entries;
    }
}
