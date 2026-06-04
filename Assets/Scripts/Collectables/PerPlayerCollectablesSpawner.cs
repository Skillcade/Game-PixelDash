using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using Game.Buffs;
using SkillcadeSDK.Common.Level;
using SkillcadeSDK.FishNetAdapter;
using SkillcadeSDK.FishNetAdapter.Players;
using UnityEngine;
using VContainer;

namespace Collectables
{
    /// <summary>
    /// Replaces scene SpeedBuff instances with one networked copy per in-game player at each spawn point.
    /// </summary>
    public class PerPlayerCollectablesSpawner : NetworkBehaviour, IRespawnService
    {
        private struct SpawnPoint
        {
            public NetworkObject Prefab;
            public Vector3 Position;
            public Quaternion Rotation;
        }

        private readonly List<SpawnPoint> _spawnPoints = new();
        private readonly List<NetworkObject> _spawnedInstances = new();

        [Inject] private readonly IObjectResolver _objectResolver;
        
        private FishNetPlayersController _playersController;
        private bool _sceneTemplatesRemoved;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _playersController = _objectResolver.Resolve<FishNetPlayersController>();
            CacheSpawnPoints();
            SubscribeToPlayers();
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            UnsubscribeFromPlayers();
        }

        public void Respawn()
        {
            if (!IsServerInitialized)
                return;

            DespawnAllInstances();
            RemoveSceneTemplates();
            SpawnAllForInGamePlayers();
        }
        
        private void SubscribeToPlayers()
        {
            _playersController.OnPlayerAdded += OnPlayerAdded;
            _playersController.OnPlayerDataUpdated += OnPlayerDataUpdated;
            _playersController.OnPlayerRemoved += OnPlayerRemoved;
        }

        private void UnsubscribeFromPlayers()
        {
            _playersController.OnPlayerAdded -= OnPlayerAdded;
            _playersController.OnPlayerDataUpdated -= OnPlayerDataUpdated;
            _playersController.OnPlayerRemoved -= OnPlayerRemoved;
        }

        private void CacheSpawnPoints()
        {
            _spawnPoints.Clear();
            var pickups = FindObjectsByType<SpeedBuffPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var pickup in pickups)
            {
                if (pickup == null || pickup.RespawnPrefab == null)
                    continue;

                _spawnPoints.Add(new SpawnPoint
                {
                    Prefab = pickup.RespawnPrefab,
                    Position = pickup.transform.position,
                    Rotation = pickup.transform.rotation
                });
            }
        }

        private void RemoveSceneTemplates()
        {
            if (_sceneTemplatesRemoved)
                return;

            var pickups = FindObjectsByType<SpeedBuffPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var pickup in pickups)
            {
                if (pickup == null)
                    continue;

                // Only remove scene/template instances, not our spawned copies.
                if (_spawnedInstances.Contains(pickup.NetworkObject))
                    continue;

                if (pickup.NetworkObject == null)
                    continue;

                if (pickup.NetworkObject.IsSpawned)
                    pickup.NetworkObject.Despawn();
                else
                    Destroy(pickup.gameObject);
            }

            _sceneTemplatesRemoved = true;
        }

        private void SpawnAllForInGamePlayers()
        {
            foreach (var playerData in _playersController.GetAllPlayersData())
            {
                TrySpawnForPlayer(playerData);
            }
        }

        private void OnPlayerAdded(int playerId, FishNetPlayerData playerData) => TrySpawnForPlayer(playerData);

        private void OnPlayerDataUpdated(int playerId, FishNetPlayerData playerData) => TrySpawnForPlayer(playerData);

        private void OnPlayerRemoved(int playerId, FishNetPlayerData playerData) =>
            DespawnInstancesForConnection(GetConnectionClientId(playerData));

        private void TrySpawnForPlayer(FishNetPlayerData playerData)
        {
            if (!IsServerInitialized)
                return;

            if (!PlayerInGameData.TryGetFromPlayer(playerData, out var inGameData) || !inGameData.InGame)
                return;

            if (!TryGetConnection(playerData, out var connection))
                return;

            RemoveSceneTemplates();

            int clientId = connection.ClientId;
            if (HasInstancesForClient(clientId))
                return;

            foreach (var point in _spawnPoints)
                SpawnCopy(point, connection);
        }

        private void SpawnCopy(SpawnPoint point, NetworkConnection connection)
        {
            var instance = NetworkManager.ServerManager.InstantiateAndSpawn(
                point.Prefab,
                point.Position,
                point.Rotation,
                connection);

            if (instance.TryGetComponent(out CollectableOwner owner))
                owner.SetOwner(connection.ClientId);

            _spawnedInstances.Add(instance);
        }

        private bool HasInstancesForClient(int clientId)
        {
            foreach (var instance in _spawnedInstances)
            {
                if (instance == null)
                    continue;

                if (instance.TryGetComponent(out CollectableOwner owner) && owner.OwnerClientId == clientId)
                    return true;
            }

            return false;
        }

        private void DespawnInstancesForConnection(int clientId)
        {
            for (int i = _spawnedInstances.Count - 1; i >= 0; i--)
            {
                var instance = _spawnedInstances[i];
                if (instance == null)
                {
                    _spawnedInstances.RemoveAt(i);
                    continue;
                }

                if (!instance.TryGetComponent(out CollectableOwner owner) || owner.OwnerClientId != clientId)
                    continue;

                if (instance.IsSpawned)
                    instance.Despawn();
                else
                    Destroy(instance.gameObject);

                _spawnedInstances.RemoveAt(i);
            }
        }

        private void DespawnAllInstances()
        {
            foreach (var instance in _spawnedInstances)
            {
                if (instance == null)
                    continue;

                if (instance.IsSpawned)
                    instance.Despawn();
                else
                    Destroy(instance.gameObject);
            }

            _spawnedInstances.Clear();
        }

        private static int GetConnectionClientId(FishNetPlayerData playerData)
        {
            return playerData.ServerConnectionClientId >= 0
                ? playerData.ServerConnectionClientId
                : playerData.OwnerId;
        }

        private bool TryGetConnection(FishNetPlayerData playerData, out NetworkConnection connection)
        {
            connection = null;
            var clientId = GetConnectionClientId(playerData);
            return NetworkManager.ServerManager.Clients.TryGetValue(clientId, out connection);
        }
    }
}
