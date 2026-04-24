using FishNet.Managing.Timing;
using SkillcadeSDK.FishNetAdapter.ColliderRollback;
using UnityEngine;

namespace Game.Level
{
    public class ServerMovableObject : TickBasedMoveBehaviour
    {
        [SerializeField] private bool _debug;
        [SerializeField] private float _oneSideMoveTime;
        [SerializeField] private AnimationCurve _oneSideMoveCurve;
        [SerializeField] private Transform _startPoint;
        [SerializeField] private Transform _endPoint;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            TimeManager.OnTick += OnTick;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            TimeManager.OnTick -= OnTick;
        }

        private void OnTick()
        {
            if (GlobalTickTimer.Instance == null || !GlobalTickTimer.Instance.IsReady)
                return;

            // Drive visuals from the same tick the server uses during rollback so
            // the algorithm is identical in both paths.
            var pt = TimeManager.GetPreciseTick(TickType.Tick);
            transform.position = GetPositionAtTick(pt);
        }

        public override Vector2 GetPositionAtTick(PreciseTick pt)
        {
            if (GlobalTickTimer.Instance == null || !GlobalTickTimer.Instance.IsReady)
                return transform.position;

            float elapsed = GlobalTickTimer.Instance.GetElapsedSeconds(pt);
            var position = ComputePosition(elapsed);
            if (_debug)
                Debug.Log($"[ServerMovableObject] Calculated position {position} at tick {pt.Tick}, elapsed: {elapsed}, start tick: {GlobalTickTimer.Instance.StartTick}");
            return position;
        }

        private Vector3 ComputePosition(float elapsedSeconds)
        {
            int completedTrips = Mathf.FloorToInt(elapsedSeconds / _oneSideMoveTime);
            float timeSinceLastTrip = elapsedSeconds - completedTrips * _oneSideMoveTime;
            bool isGoingForward = completedTrips % 2 == 0;

            var startPoint = isGoingForward ? _startPoint : _endPoint;
            var endPoint   = isGoingForward ? _endPoint   : _startPoint;

            float t = timeSinceLastTrip / _oneSideMoveTime;
            if (t > 1f) t -= 1f;
            t = _oneSideMoveCurve.Evaluate(t);

            return Vector3.Lerp(startPoint.position, endPoint.position, t);
        }
    }
}
