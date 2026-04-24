using System.Collections.Generic;
using FishNet.Component.ColliderRollback;
using FishNet.Managing.Timing;
using FishNet.Object;
using FishNet.Utility.Template;
using SkillcadeSDK.FishNetAdapter.ColliderRollback;
using UnityEngine;

namespace Game.RollbackDebug
{
    /// <summary>
    /// Attach to the player NetworkObject (same object that has PlayerRollbackSource).
    /// Press the configured key to snapshot client positions and request server rollback positions.
    /// Press again to clear and request a fresh snapshot.
    /// Positions are visualized via OnDrawGizmos (Scene view only).
    ///
    /// Targets that also have a <see cref="TickBasedMoveBehaviour"/> are resolved via
    /// <see cref="TickBasedMoveBehaviour.GetPositionAtTick"/> on the server instead of
    /// FishNet's ColliderRollback history, matching the path used by PlayerRollbackSource.
    /// </summary>
    public class RollbackDebugger : TickNetworkBehaviour
    {
        [SerializeField] private TickType _tickType;
        [SerializeField] private bool _usePostTick;
        [SerializeField] private KeyCode _triggerKey = KeyCode.F9;

        // Client-side recorded positions (captured on keypress)
        private Dictionary<int, Vector3> _clientPositions = new();
        // Server-returned rolled-back positions (received via TargetRpc)
        private Dictionary<int, Vector3> _serverPositions = new();

        private bool _hasPendingRequest;
        private bool _sendRequest;

        private void Update()
        {
            if (!IsOwner) return;
            if (!Input.GetKeyDown(_triggerKey)) return;

            _sendRequest = true;
        }

        protected override void TimeManager_OnTick()
        {
            base.TimeManager_OnTick();
            if (!_usePostTick && _sendRequest)
                RequestPositions();
        }

        protected override void TimeManager_OnPostTick()
        {
            base.TimeManager_OnPostTick();
            if (_usePostTick && _sendRequest)
                RequestPositions();
        }

        private void RequestPositions()
        {
            _sendRequest = false;
            ClearPositions();

            Debug.Log("[RollbackDebugger] Start request position");

            // Capture current world positions of all marked targets
            foreach (var target in RollbackDebugTarget.All)
            {
                // TickBasedMoveBehaviour targets: client already sees the tick-synced
                // transform position (ServerMovableObject.Update writes it from the same
                // tick the rollback uses), so transform.position is the correct snapshot.
                if (target.TryGetComponent(out TickBasedMoveBehaviour _))
                {
                    _clientPositions[target.ObjectId] = target.transform.position;
                    if (target.Debug)
                        Debug.Log($"[RollbackDebugger] tick-based client pos {target.transform.position}");
                    continue;
                }

                if (!target.TryGetComponent(out ColliderRollback cr))
                    continue;

                if (cr.RollingCollidersPositions == null)
                {
                    if (target.Debug)
                        Debug.Log($"[RollbackDebugger] Rolling collider positions for target are null, use transform: {target.transform.position}");
                    _clientPositions.Add(target.ObjectId, target.transform.position);
                    continue;
                }

                foreach (var position in cr.RollingCollidersPositions)
                {
                    _clientPositions[target.ObjectId] = position;
                    if (target.Debug)
                        Debug.Log($"[RollbackDebugger] client target position is {position}");
                }
            }

            var tick = TimeManager.GetPreciseTick(_tickType);
            _hasPendingRequest = true;
            Debug.Log($"[RollbackDebugger] Request positions with tick {tick.Tick}, current: {TimeManager.Tick}");
            RequestRollbackPositionsServerRpc(tick);
        }

        [ServerRpc(RequireOwnership = true)]
        private void RequestRollbackPositionsServerRpc(PreciseTick tick)
        {
            Debug.Log($"[RollbackDebugger] Rollback positions for tick {tick.Tick}, current: {TimeManager.Tick}");

            var serverPositions = new Dictionary<int, Vector3>();

            // Partition targets: tick-based vs ColliderRollback-driven.
            var colliderRollbackTargets = new List<(RollbackDebugTarget target, ColliderRollback cr)>();
            foreach (var target in RollbackDebugTarget.All)
            {
                if (target.TryGetComponent(out TickBasedMoveBehaviour tmb))
                {
                    if (target.Debug)
                    {
                        // 
                    }
                    Vector2 pos = tmb.GetPositionAtTick(tick);
                    serverPositions[target.ObjectId] = pos;
                    if (target.Debug)
                        Debug.Log($"[RollbackDebugger] tick-based server pos {pos} for target at tick {tick.Tick}");
                    continue;
                }

                if (target.TryGetComponent(out ColliderRollback cr))
                    colliderRollbackTargets.Add((target, cr));
            }

            // Fall back to FishNet ColliderRollback for non-tick-based targets.
            if (colliderRollbackTargets.Count > 0)
            {
                var results = new List<(ColliderRollback colliderRollback, Vector3 position)>();
                RollbackManager.QueryRollbackPositions(tick, IsOwner, results);

                foreach (var (target, cr) in colliderRollbackTargets)
                {
                    foreach (var result in results)
                    {
                        if (result.colliderRollback != cr)
                            continue;

                        serverPositions[target.ObjectId] = result.position;
                        if (target.Debug)
                            Debug.Log($"[RollbackDebugger] collider-rollback server pos {result.position}, root pos: {target.transform.position}");
                    }
                }

                RollbackManager.RevertRollbackPositions();
            }

            ReceiveRollbackPositionsTargetRpc(Owner, serverPositions);
        }

        [TargetRpc]
        private void ReceiveRollbackPositionsTargetRpc(FishNet.Connection.NetworkConnection conn,
            Dictionary<int, Vector3> serverPositions)
        {
            Debug.Log($"[RollbackDebugger] Received server response on tick {TimeManager.Tick}");
            _serverPositions = serverPositions;
            _hasPendingRequest = false;
        }

        private void ClearPositions()
        {
            _clientPositions.Clear();
            _serverPositions.Clear();
            _hasPendingRequest = false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Client-recorded positions: blue
            Gizmos.color = Color.blue;
            foreach (var pos in _clientPositions.Values)
                Gizmos.DrawSphere(pos, 0.15f);

            // Server rollback positions: red
            Gizmos.color = Color.red;
            foreach (var pos in _serverPositions.Values)
                Gizmos.DrawSphere(pos, 0.15f);

            // Draw connecting lines between paired positions (same index)
            Gizmos.color = Color.yellow;
            foreach (var clientObj in _clientPositions)
            {
                if (_serverPositions.TryGetValue(clientObj.Key, out var pos))
                    Gizmos.DrawLine(clientObj.Value, pos);
            }
        }
#endif
    }
}
