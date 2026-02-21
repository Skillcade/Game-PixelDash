using System.Collections.Generic;
using FishNet.Object;
using FishNet.Utility.Template;
using UnityEngine;

namespace Game.RigidbodyInterpolation
{
    /// <summary>
    /// Wraps Rigidbody2D and provides smooth visual interpolation for physics-driven objects.
    /// Uses timeline adjustment (from TestInterpolation) to handle FixedUpdate/Update rate differences.
    /// FixedUpdate "sends" snapshots, Update advances a local timeline and interpolates.
    /// </summary>
    [DisallowMultipleComponent]
    public class NetworkRigidbody2DInterpolator : TickNetworkBehaviour
    {
        private struct PositionSnapshot : IInterpolateSnapshot
        {
            public float RemoteTime { get; set; }
            public float LocalTime { get; set; }
            public Vector2 Position;
        }

        public Rigidbody2D Rigidbody => _rigidbody;

        [Header("References")]
        [SerializeField] private NetworkObject _networkObject;
        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private Transform _visualTransform;

        [Header("Timeline")]
        [SerializeField] private SnapshotInterpolationSettings _interpolationSettings;

        private float BufferTime => InterpolationUtils.SendInterval * _bufferTimeMultiplier;
        
        private Transform _visualsParent;
        private Vector2 _visualWorldOffset;
        private float _visualZOffset;

        // Buffers
        private SortedList<float, TimeSnapshot> _timeBuffer;
        private SortedList<float, PositionSnapshot> _positionBuffer;

        // Timeline state
        private float _localTimeline;
        private float _localTimescale = 1f;
        private float _bufferTimeMultiplier;
        private ExponentalMovingAverage _driftEma;
        private ExponentalMovingAverage _deliveryTimeEma;

        public void AddForce(Vector2 force, ForceMode2D mode = ForceMode2D.Force)
        {
            _rigidbody.AddForce(force, mode);
        }

        public void AddForceAtPosition(Vector2 force, Vector2 position, ForceMode2D mode = ForceMode2D.Force)
        {
            _rigidbody.AddForceAtPosition(force, position, mode);
        }

        /// <summary>
        /// Teleport to a position. Skips interpolation and snaps visual immediately.
        /// Use for respawns or other instantaneous moves.
        /// </summary>
        public void Teleport(Vector2 position, bool resetVelocity = false)
        {
            if (resetVelocity)
                _rigidbody.linearVelocity = Vector2.zero;

            _rigidbody.position = position;
            ResetBuffers();
            ApplyVisualPosition(position);
        }

        private void Awake()
        {
            InitializeBuffers();
            CacheVisualOffsets();
            ApplyVisualPosition(_rigidbody.position);
        }

        private void OnEnable()
        {
            InitializeBuffers();
            CacheVisualOffsets();
            ApplyVisualPosition(_rigidbody.position);

            _visualsParent = _visualTransform.parent;
            _visualTransform.parent = null;
        }

        private void OnDisable()
        {
            _visualTransform.parent = _visualsParent;
        }

        public void AddSnapshot()
        {
            if (_interpolationSettings.DynamicAdjustment)
            {
                _bufferTimeMultiplier = InterpolationUtils.DynamicAdjustment(
                    InterpolationUtils.SendInterval,
                    _deliveryTimeEma.StandardDeviation,
                    _interpolationSettings.DynamicAdjustmentTolerance);
            }

            var tick = _networkObject.TimeManager.LocalTick;
            float remoteTime = (float)_networkObject.TimeManager.TicksToTime(tick);
            float localTime = _networkObject.TimeManager.ClientUptime;

            var timeSnapshot = new TimeSnapshot(remoteTime, localTime);
            InterpolationUtils.InsertAndAdjust(
                _timeBuffer, _interpolationSettings, timeSnapshot,
                ref _localTimeline, ref _localTimescale, BufferTime,
                ref _driftEma, ref _deliveryTimeEma);

            var posSnapshot = new PositionSnapshot
            {
                RemoteTime = remoteTime,
                LocalTime = localTime,
                Position = _rigidbody.position
            };
            InterpolationUtils.InsertIfNotExists(_positionBuffer, _interpolationSettings.BufferLimit, posSnapshot);
        }

        private void Update()
        {
            if (!IsOwner)
                return;
            
            if (_timeBuffer.Count > 0)
            {
                _localTimeline += Time.unscaledDeltaTime * _localTimescale;
                InterpolationUtils.StepInterpolation(_timeBuffer, _localTimeline, out _, out _, out _);
            }

            if (_positionBuffer.Count == 0)
            {
                ApplyVisualPosition(_rigidbody.position);
                return;
            }

            InterpolationUtils.StepInterpolation(
                _positionBuffer, _localTimeline,
                out var from, out var to, out float t);

            Vector2 interpolated = Vector2.Lerp(from.Position, to.Position, t);
            ApplyVisualPosition(interpolated);
        }

        private void InitializeBuffers()
        {
            _timeBuffer = new SortedList<float, TimeSnapshot>();
            _positionBuffer = new SortedList<float, PositionSnapshot>();
            _bufferTimeMultiplier = _interpolationSettings.BufferTimeMultiplier;
            _localTimeline = 0;
            _localTimescale = 1f;
            _driftEma = new ExponentalMovingAverage(InterpolationUtils.SendRate * _interpolationSettings.DriftEmaDuration);
            _deliveryTimeEma = new ExponentalMovingAverage(InterpolationUtils.SendRate * _interpolationSettings.DeliveryTimeEmaDuration);
        }

        private void ResetBuffers()
        {
            _timeBuffer.Clear();
            _positionBuffer.Clear();
            _localTimeline = 0;
            _localTimescale = 1f;
            _bufferTimeMultiplier = _interpolationSettings.BufferTimeMultiplier;
            _driftEma = new ExponentalMovingAverage(InterpolationUtils.SendRate * _interpolationSettings.DriftEmaDuration);
            _deliveryTimeEma = new ExponentalMovingAverage(InterpolationUtils.SendRate * _interpolationSettings.DeliveryTimeEmaDuration);
        }

        private void CacheVisualOffsets()
        {
            Vector3 rigidbodyWorld = _rigidbody.transform.position;
            Vector3 visualWorld = _visualTransform.position;

            _visualWorldOffset = new Vector2(visualWorld.x - rigidbodyWorld.x, visualWorld.y - rigidbodyWorld.y);
            _visualZOffset = visualWorld.z - rigidbodyWorld.z;
        }

        private void ApplyVisualPosition(Vector2 worldPosition)
        {
            float z = _rigidbody.transform.position.z + _visualZOffset;
            _visualTransform.position = new Vector3(
                worldPosition.x + _visualWorldOffset.x,
                worldPosition.y + _visualWorldOffset.y,
                z);
        }
    }
}
