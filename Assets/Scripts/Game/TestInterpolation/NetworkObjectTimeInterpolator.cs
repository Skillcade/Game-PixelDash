using System.Collections.Generic;
using UnityEngine;

namespace PGP.Core.Game.Network
{
    internal class NetworkObjectTimeInterpolator : MonoBehaviour
    {
        internal float LocalTimeline;
        internal SnapshotInterpolationSettings InterpolationSettings => _interpolationSettings;
        internal float BufferTime => InterpolationUtils.SendInterval * _bufferTimeMultiplier;

        [SerializeField] private SnapshotInterpolationSettings _interpolationSettings;

        private float _bufferTimeMultiplier;
        private float _timeScale;
        private ExponentalMovingAverage _driftEma;
        private ExponentalMovingAverage _deliveryTimeEma;
        private SortedList<float, TimeSnapshot> _snapshots;

        private void Awake()
        {
            _bufferTimeMultiplier = _interpolationSettings.BufferTimeMultiplier;
            LocalTimeline = 0;
            _timeScale = 1f;
            _snapshots = new SortedList<float, TimeSnapshot>();

            _driftEma = new ExponentalMovingAverage(InterpolationUtils.SendRate * _interpolationSettings.DriftEmaDuration);
            _deliveryTimeEma =
                new ExponentalMovingAverage(InterpolationUtils.SendRate * _interpolationSettings.DeliveryTimeEmaDuration);
        }

        internal void UpdateTime()
        {
            if (_snapshots.Count == 0) return;
            
            LocalTimeline += Time.unscaledDeltaTime * _timeScale;
            InterpolationUtils.StepInterpolation(_snapshots, LocalTimeline, out _, out _, out _);
        }

        internal void AddTimeSnapshot(float remoteTime)
        {
            var snapshot = new TimeSnapshot(remoteTime, Time.unscaledTime);
            if (_interpolationSettings.DynamicAdjustment)
            {
                _bufferTimeMultiplier = InterpolationUtils.DynamicAdjustment(
                    InterpolationUtils.SendInterval,
                    _deliveryTimeEma.StandardDeviation,
                    _interpolationSettings.DynamicAdjustmentTolerance);
            }
            
            InterpolationUtils.InsertAndAdjust(
                _snapshots,
                _interpolationSettings,
                snapshot,
                ref LocalTimeline,
                ref _timeScale,
                BufferTime,
                ref _driftEma,
                ref _deliveryTimeEma);
        }
    }
}
