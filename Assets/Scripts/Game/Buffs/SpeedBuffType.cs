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
        public float StartTime;
        public float Duration;

        public float EndTime => StartTime + Duration;
        
        public SpeedBuffData(SpeedBuffType buffType, float value, float duration, float startTime)
        {
            BuffType = buffType;
            Value = value;
            Duration = duration;
            StartTime = startTime;
        }
        
        public bool IsExpired(float now)
        {
            return Duration > 0f && now >= EndTime;
        }
    }
}