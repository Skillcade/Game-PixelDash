using System;

namespace Game.Buffs
{
    public enum SpeedBuffType
    {
        Add,
        Multiply
    }

    [Serializable]
    public struct SpeedBuffData
    {
        public SpeedBuffType BuffType;
        public float Value;
        public uint StartTick;
        public uint DurationTicks;

        public bool IsExpired(uint currentTick)
        {
            return DurationTicks > 0 && currentTick >= StartTick + DurationTicks;
        }
    }
}