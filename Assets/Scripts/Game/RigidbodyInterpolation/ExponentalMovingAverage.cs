using UnityEngine;

namespace Game.RigidbodyInterpolation
{
    public struct ExponentalMovingAverage
    {
	    public float Value;
        public float Variance;
        public float StandardDeviation;
        
        private readonly float _alpha;
        private bool _initialized;

        public ExponentalMovingAverage(int n)
        {
            _alpha = 2f / (n + 1);
            _initialized = false;
            Value = 0;
            Variance = 0;
            StandardDeviation = 0;
        }

        public void Add(float value)
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

        public void Clear()
        {
            _initialized = false;
            Value = 0;
            Variance = 0;
            StandardDeviation = 0;
        }
    }
}
