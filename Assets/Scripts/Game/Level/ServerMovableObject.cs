using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace Game.Level
{
    public class ServerMovableObject : NetworkBehaviour
    {
        [SerializeField] private bool _useNetTransformForSync;
        [SerializeField] private float _oneSideMoveTime;
        [SerializeField] private AnimationCurve _oneSideMoveCurve;
        [SerializeField] private Transform _startPoint;
        [SerializeField] private Transform _endPoint;

        private readonly SyncVar<uint> _startTick = new(new SyncTypeSettings(WritePermission.ServerOnly));

        [Header("Debug")]
        [SerializeField] private float _localElapsed;
        [SerializeField] private bool _started;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _startTick.OnChange += OnStartTickChanged;

            if (IsServerInitialized)
            {
                _startTick.Value = TimeManager.Tick;
                _started = true;
            }
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            _startTick.OnChange -= OnStartTickChanged;
        }

        private void OnStartTickChanged(uint prev, uint next, bool asServer)
        {
            _localElapsed = (TimeManager.Tick - next) * (float)TimeManager.TickDelta;
            _started = true;
        }

        private void Update()
        {
            if (!_started || (_useNetTransformForSync && !IsServerInitialized))
                return;

            _localElapsed += Time.deltaTime;

            // Gently correct toward authoritative server tick time to prevent long-term drift
            float authoritative = (TimeManager.Tick - _startTick.Value) * (float)TimeManager.TickDelta;
            _localElapsed = Mathf.MoveTowards(_localElapsed, authoritative, Time.deltaTime * 0.1f);

            float passedTime = _localElapsed;
            int completedTrips = Mathf.FloorToInt(passedTime / _oneSideMoveTime);
            float timeSinceLastTrip = passedTime - completedTrips * _oneSideMoveTime;
            bool isGoingForward = completedTrips % 2 == 0;

            var startPoint = isGoingForward ? _startPoint : _endPoint;
            var endPoint = isGoingForward ? _endPoint : _startPoint;

            float t = timeSinceLastTrip / _oneSideMoveTime;
            t = t > 1 ? t - 1 : t;
            t = _oneSideMoveCurve.Evaluate(t);

            transform.position = Vector3.Lerp(startPoint.position, endPoint.position, t);
        }
    }
}
