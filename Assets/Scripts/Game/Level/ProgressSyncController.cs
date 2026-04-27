using System.Collections.Generic;
using FishNet.Object;
using FishNet.Transporting;
using Game.Player;
using SkillcadeSDK.FishNetAdapter.Players;
using UnityEngine;
using VContainer;

namespace Game.Level
{
    public class ProgressSyncController : NetworkBehaviour
    {
        [SerializeField] private float _syncInterval = 0.35f;
        [SerializeField] private float _minX;
        [SerializeField] private float _maxX;

        [Inject] private readonly FishNetPlayersController _playersController;

        private float _syncTimer;
        private readonly List<PlayerProgressEntry> _entries = new();

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();

            if (IsServerInitialized)
                TimeManager.OnTick += OnTick;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();

            if (IsServerInitialized)
                TimeManager.OnTick -= OnTick;
        }

        private void OnTick()
        {
            _syncTimer -= (float)TimeManager.TickDelta;
            if (_syncTimer > 0f)
                return;

            _syncTimer = _syncInterval;
            SendProgress();
        }

        private void SendProgress()
        {
            _entries.Clear();

            foreach (var playerData in _playersController.GetAllPlayersData())
            {
                int id = playerData.PlayerNetworkId;
                if (!NetworkManager.ServerManager.Clients.TryGetValue(id, out var conn))
                    continue;

                PlayerMovement movement = null;
                foreach (var obj in conn.Objects)
                {
                    if (obj.TryGetComponent(out movement))
                        break;
                }

                if (movement == null)
                    continue;

                float t = Mathf.InverseLerp(_minX, _maxX, movement.transform.position.x);
                var entry = new PlayerProgressEntry
                {
                    PlayerId = id,
                    Progress = t
                };
                _entries.Add(entry);
            }

            var broadcast = new PlayerProgressBroadcast
            {
                Entries = _entries.ToArray()
            };

            NetworkManager.ServerManager.Broadcast(broadcast, channel: Channel.Unreliable);
        }
    }
}
