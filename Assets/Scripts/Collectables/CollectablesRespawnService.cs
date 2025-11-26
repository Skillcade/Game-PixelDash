using System.Collections.Generic;
using FishNet.Object;
using SkillcadeSDK.FishNetAdapter;
using UnityEngine;

namespace DefaultNamespace.Collectables
{
    public class CollectablesRespawnService : NetworkBehaviour, ISpawnService
    {
        private struct SpawnPoint
        {
            public NetworkObject Prefab;
            public Vector3 Position;
            public Quaternion Rotation;
        }

        private readonly List<SpawnPoint> _spawnPoints = new();

        public override void OnStartServer()
        {
            base.OnStartServer();
            CacheSpawnPoints();
        }

        private void CacheSpawnPoints()
        {
            _spawnPoints.Clear();
            var collectables = FindObjectsByType<CollectableBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var c in collectables)
            {
                if (c == null || c.RespawnPrefab == null)
                {
                    continue;
                }

                _spawnPoints.Add(new SpawnPoint
                {
                    Prefab = c.RespawnPrefab,
                    Position = c.transform.position,
                    Rotation = c.transform.rotation
                });
            }
        }

        [Server]
        public void Respawn()
        {
            if (!IsServerInitialized)
            {
                return;
            }

            var existing = FindObjectsByType<CollectableBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var c in existing)
            {
                if (c?.NetworkObject == null)
                {
                    continue;
                }
                
                if (c.NetworkObject.IsSpawned)
                {
                    c.NetworkObject.Despawn();
                }
                else
                {
                    Destroy(c.gameObject);
                }
            }

            foreach (var point in _spawnPoints)
            {
                if (point.Prefab == null)
                {
                    continue;
                }

                var instance = NetworkManager.ServerManager.InstantiateAndSpawn(point.Prefab, point.Position, point.Rotation);
                if (instance.TryGetComponent<Rigidbody2D>(out var rb))
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0;
                }
            }
        }
    }

}