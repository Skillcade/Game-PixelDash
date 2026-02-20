using UnityEngine;

namespace Game.Utils
{
    /// <summary>
    /// Old version of Rigidbody2DInterpolator using ring buffer and Time.time - delay.
    /// Kept for reference/comparison. Use Rigidbody2DInterpolator (with timeline adjustment) instead.
    /// </summary>
    [DisallowMultipleComponent]
    public class Rigidbody2DInterpolatorOld : MonoBehaviour
    {
        private struct Snapshot
        {
            public float Time;
            public Vector2 Position;
        }

        public Rigidbody2D Rigidbody => _rigidbody;

        [Header("References")]
        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private Transform _visualTransform;

        [Header("Interpolation")]
        [SerializeField, Min(2)] private int _bufferSize = 60;
        [SerializeField, Min(0f)] private float _interpolationDelay = 0.05f;
        [SerializeField, Min(0f)] private float _snapshotReplaceThreshold = 0.0001f;

        private Transform _visualsParent;
        private Snapshot[] _snapshots;
        private int _snapshotHead;
        private int _snapshotCount;

        private Vector2 _visualWorldOffset;
        private float _visualZOffset;

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
            ResetBuffer(position, Time.time);
            ApplyVisualPosition(position);
        }

        private void Awake()
        {
            EnsureBufferCapacity();
            CacheVisualOffsets();
            ResetBuffer(_rigidbody.position, Time.time);
            ApplyVisualPosition(_rigidbody.position);
        }

        private void OnEnable()
        {
            EnsureBufferCapacity();
            CacheVisualOffsets();
            ResetBuffer(_rigidbody.position, Time.time);
            ApplyVisualPosition(_rigidbody.position);

            _visualsParent = _visualTransform.parent;
            _visualTransform.parent = null;
        }

        private void OnDisable()
        {
            _visualTransform.parent = _visualsParent;
        }

        private void FixedUpdate()
        {
            AddSnapshot(Time.fixedTime, _rigidbody.position);
        }

        private void Update()
        {
            if (_snapshotCount < 2)
            {
                var pos = _rigidbody.position;
                ApplyVisualPosition(pos);
                return;
            }

            float renderTime = Time.time - _interpolationDelay;
            if (!TryGetInterpolationPair(renderTime, out var from, out var to))
            {
                var latest = GetLatestSnapshot().Position;
                ApplyVisualPosition(latest);
                return;
            }

            float denominator = to.Time - from.Time;
            float t = denominator > float.Epsilon
                ? (renderTime - from.Time) / denominator
                : 1f;

            t = Mathf.Clamp01(t);

            Vector2 interpolated = Vector2.Lerp(from.Position, to.Position, t);
            ApplyVisualPosition(interpolated);
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

        private void AddSnapshot(float snapshotTime, Vector2 position)
        {
            EnsureBufferCapacity();

            var snapshot = new Snapshot
            {
                Time = snapshotTime,
                Position = position
            };

            if (_snapshotCount > 0)
            {
                var last = GetSnapshot(_snapshotCount - 1);
                if (Mathf.Abs(snapshotTime - last.Time) <= _snapshotReplaceThreshold)
                {
                    int index = (_snapshotHead + _snapshotCount - 1) % _snapshots.Length;
                    _snapshots[index] = snapshot;
                    return;
                }
            }

            if (_snapshotCount < _snapshots.Length)
            {
                int index = (_snapshotHead + _snapshotCount) % _snapshots.Length;
                _snapshots[index] = snapshot;
                _snapshotCount++;
            }
            else
            {
                _snapshotHead = (_snapshotHead + 1) % _snapshots.Length;
                int lastIndex = (_snapshotHead + _snapshotCount - 1) % _snapshots.Length;
                _snapshots[lastIndex] = snapshot;
            }
        }

        private void ResetBuffer(Vector2 position, float snapshotTime)
        {
            _snapshotHead = 0;
            _snapshotCount = 0;

            float dt = Time.fixedDeltaTime;
            if (dt <= 0f)
                dt = 0.016f;

            AddSnapshot(snapshotTime - dt, position);
            AddSnapshot(snapshotTime, position);
        }

        private bool TryGetInterpolationPair(float renderTime, out Snapshot from, out Snapshot to)
        {
            from = default;
            to = default;

            if (_snapshotCount < 2)
                return false;

            var first = GetSnapshot(0);
            var last = GetSnapshot(_snapshotCount - 1);

            if (renderTime <= first.Time)
            {
                from = first;
                to = GetSnapshot(1);
                return true;
            }

            if (renderTime >= last.Time)
            {
                from = GetSnapshot(_snapshotCount - 2);
                to = last;
                return true;
            }

            for (int i = 0; i < _snapshotCount - 1; i++)
            {
                var current = GetSnapshot(i);
                var next = GetSnapshot(i + 1);

                if (current.Time <= renderTime && renderTime <= next.Time)
                {
                    from = current;
                    to = next;
                    return true;
                }
            }

            return false;
        }

        private void EnsureBufferCapacity()
        {
            int capacity = Mathf.Max(_bufferSize, 4);
            if (_snapshots != null && _snapshots.Length == capacity)
                return;

            var oldBuffer = _snapshots;
            int oldHead = _snapshotHead;
            int oldCount = _snapshotCount;

            _snapshots = new Snapshot[capacity];
            _snapshotHead = 0;
            _snapshotCount = 0;

            if (oldBuffer != null && oldCount > 0)
            {
                int keepCount = Mathf.Min(oldCount, capacity);
                int logicalStart = oldCount - keepCount;
                for (int i = 0; i < keepCount; i++)
                {
                    int oldIndex = (oldHead + logicalStart + i) % oldBuffer.Length;
                    AddSnapshot(oldBuffer[oldIndex].Time, oldBuffer[oldIndex].Position);
                }
            }
        }

        private Snapshot GetSnapshot(int logicalIndex)
        {
            int index = (_snapshotHead + logicalIndex) % _snapshots.Length;
            return _snapshots[index];
        }

        private Snapshot GetLatestSnapshot()
        {
            return GetSnapshot(_snapshotCount - 1);
        }
    }
}
