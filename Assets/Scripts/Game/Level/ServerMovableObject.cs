using FishNet.Object;
using UnityEngine;

namespace Game.Level
{
    public class ServerMovableObject : NetworkBehaviour
    {
        [SerializeField] private float _oneSideMoveTime;
        [SerializeField] private AnimationCurve _oneSideMoveCurve;
        [SerializeField] private Transform _startPoint;
        [SerializeField] private Transform _endPoint;
        
        private double _startMoveTime;
        
        public override void OnStartServer()
        {
            base.OnStartServer();
            _startMoveTime = Time.timeAsDouble;
        }

        private void Update()
        {
            if (!IsServerInitialized)
            {
                return;
            }
            
            double now = Time.timeAsDouble;
            float passedTime = (float)(now - _startMoveTime);

            int completedTrips = Mathf.FloorToInt(passedTime / _oneSideMoveTime);
            float timeSinceLastRoundTrip = passedTime - completedTrips * _oneSideMoveTime;
            bool isGoingForward = completedTrips % 2 == 0;

            var startPoint = isGoingForward ? _startPoint : _endPoint;
            var endPoint = isGoingForward ? _endPoint : _startPoint;

            float t = timeSinceLastRoundTrip / _oneSideMoveTime;
            
            t = t > 1 ? t - 1 : t;
            t = _oneSideMoveCurve.Evaluate(t);

            transform.position = Vector3.Lerp(startPoint.position, endPoint.position, t);
        }
    }
}