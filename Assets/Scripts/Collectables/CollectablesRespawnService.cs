using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

namespace DefaultNamespace.Collectables
{
    public class CollectablesRespawnService : NetworkBehaviour, ISpawnService
    {
        private struct SpawnPoint
        {
            public NetworkObject Prefab;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
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
                    LocalPosition = c.transform.localPosition,
                    LocalRotation = c.transform.localRotation
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

                var instance = Instantiate(point.Prefab);
                instance.transform.localPosition = point.LocalPosition;
                instance.transform.localRotation = point.LocalRotation;

                if (instance.TryGetComponent<Rigidbody2D>(out var rb))
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0;
                }

                NetworkManager.ServerManager.Spawn(instance);
                if (instance.TryGetComponent<CollectableBase>(out var c))
                {
                    c.ResetForRespawn();
                }
            }
        }
    }

}