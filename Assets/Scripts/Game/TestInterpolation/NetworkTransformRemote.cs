using System.Collections.Generic;
using UnityEngine;

namespace PGP.Core.Game.Network
{
    internal class NetworkTransformRemote : MonoBehaviour
    {
        internal bool MovePosition;
        
        internal Vector3 Velocity { get; private set; }
        
        [SerializeField] private bool _showDebug;
        [SerializeField] private Color _overlayColor;
        [SerializeField] private int _debugFontSize;
        [SerializeField] private NetworkObjectTimeInterpolator _timeInterpolator;

        private float _lastPositionUpdateTime;
        private Vector3 _lastPosition;
        private SortedList<float, TransformSnapshot> _snapshotsBuffer;

        private void Awake()
        {
            _snapshotsBuffer = new SortedList<float, TransformSnapshot>();
            MovePosition = true;
        }

        private void Update()
        {
	        _timeInterpolator.UpdateTime();
            if (_snapshotsBuffer.Count == 0) return;
            
            InterpolationUtils.StepInterpolation(
                _snapshotsBuffer,
                _timeInterpolator.LocalTimeline,
                out var from,
                out var to,
                out float t);
            
            var interpolated = TransformSnapshot.Interpolate(from, to, t);
            
            transform.position = interpolated.Position;
            transform.rotation = interpolated.Rotation;

            var delta = interpolated.Position - _lastPosition;
            float deltaTime = Time.time - _lastPositionUpdateTime;
            if (deltaTime == 0f)
	            Velocity = Vector3.zero;
            else
	            Velocity = delta / deltaTime;

            _lastPosition = interpolated.Position;
            _lastPositionUpdateTime = Time.time;
        }

        internal void HandleStream(float remoteTime, Vector3 position, Quaternion rotation, bool updateDisabled)
        {
            _timeInterpolator.AddTimeSnapshot(remoteTime);

            var snapshot = new TransformSnapshot
            {
                RemoteTime = remoteTime + InterpolationUtils.SendInterval * _timeInterpolator.InterpolationSettings.RemoteBufferOffset,
                LocalTime = Time.unscaledTime,
                Position = position,
                Rotation = rotation
            };
         
            InterpolationUtils.InsertIfNotExists(_snapshotsBuffer, _timeInterpolator.InterpolationSettings.BufferLimit,
                snapshot);
        }

#if PGP_DEBUG
        protected void OnGUI()
        {
            if (!_showDebug && !UI.DevConsole.ConsoleCommand.ToggleNetworkInterpolationDebugCommand.Activated) return;
            if (!Camera.main) return;

            if (Vector3.Distance(Target.position, Camera.main.transform.position) > 100f)
	            return;
            
            var point = Camera.main.WorldToScreenPoint(Target.position);
            if (point.z < 0 || !InterpolationUtils.IsPointInScreen(point))
                return;
            
            GUI.color = _overlayColor;
            GUILayout.BeginArea(new Rect(point.x, Screen.height - point.y, 500, 300));
            
            var style = new GUIStyle
            {
                fontSize = _debugFontSize
            };
            GUILayout.Label($"Snapshots Buffer:{_snapshotsBuffer.Count}, Time: {Math.Round(_timeInterpolator.LocalTimeline, 2)}", style);

            GUILayout.EndArea();
            GUI.color = Color.white;
        }

        private void OnDrawGizmos()
        {
            if (!_showDebug) return;
            if (!Application.isPlaying) return;
            if (_snapshotsBuffer.Count < 2) return;

            float threshold = Time.unscaledTimeAsfloat - _timeInterpolator.BufferTime;
            var oldEnoughColor = new Color(0, 1, 0, 0.5f);
            var notOldEnoughColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

            for (int i = 0; i < _snapshotsBuffer.Count; ++i)
            {
                var entry = _snapshotsBuffer.Values[i];
                bool oldEnough = entry.LocalTime <= threshold;
                Gizmos.color = oldEnough ? oldEnoughColor : notOldEnoughColor;
                Gizmos.DrawWireCube(entry.Position, Vector3.one);
            }

            Gizmos.color = Color.green;
            Gizmos.DrawLine(_snapshotsBuffer.Values[0].Position, Target.position);
            Gizmos.color = Color.white;
            Gizmos.DrawLine(Target.position, _snapshotsBuffer.Values[1].Position);
        }
#endif
    }
}
