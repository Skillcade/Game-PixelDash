using UnityEngine;

namespace PGP.Core.Game.Network
{
    internal struct ExponentalMovingAverage
    {
	    internal float Value;
        internal float Variance;
        internal float StandardDeviation;
        
        private readonly float _alpha;
        private bool _initialized;

        internal ExponentalMovingAverage(int n)
        {
            _alpha = 2f / (n + 1);
            _initialized = false;
            Value = 0;
            Variance = 0;
            StandardDeviation = 0;
        }

        internal void Add(float value)
        {
            if (!_initialized)
            {
                Value = value;
                _initialized = true;
            }

            float delta = value - Value;
            Value += _alpha * delta;
            Variance = (1 - _alpha) * (Variance + _alpha * delta * delta);
            StandardDeviation = Mathf.Sqrt(Variance);
        }

        internal void Clear()
        {
            _initialized = false;
            Value = 0;
            Variance = 0;
            StandardDeviation = 0;
        }
    }
}
