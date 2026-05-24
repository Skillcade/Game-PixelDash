using System;
using System.Collections.Generic;
using FishNet.Managing;
using FishNet.Object;
using SkillcadeSDK.Common.Players;
using SkillcadeSDK.FishNetAdapter;
using SkillcadeSDK.FishNetAdapter.Players;
using UnityEngine;
using VContainer;

#if UNITY_SERVER || UNITY_EDITOR
using Game.Player;
using SkillcadeSDK.ServerValidation;
#endif

namespace Game
{
    public class PlayerSpawner : MonoBehaviour, IPlayerSpawner
    {
        [SerializeField] private NetworkObject _prefab;
        [SerializeField] private Transform _spawnPoint;

        [Inject] private readonly IObjectResolver _objectResolver;
        
#if UNITY_SERVER || UNITY_EDITOR
        private ServerPayloadController _serverPayloadController;
#endif

        private NetworkManager _networkManager;
        private FishNetPlayersController _playersController;

        private Dictionary<int, NetworkObject> _spawnedPlayers;

        private void Start()
        {
            _spawnedPlayers = new Dictionary<int, NetworkObject>();
            if (_objectResolver.TryResolve(out _playersController))
                _playersController.OnPlayerRemoved += OnPlayerRemoved;
            
#if UNITY_SERVER || UNITY_EDITOR
            _objectResolver.TryResolve(out _serverPayloadController);
#endif

            _objectResolver.TryResolve(out _networkManager);
        }

        public void EnsurePlayersSpawned()
        {
            foreach (var playerData in _playersController.GetAllPlayersData())
            {
                if (!PlayerInGameData.TryGetFromPlayer(playerData, out var data) || !data.InGame)
                    continue;

                // PlayerNetworkId is the stable ReplayClientId after a reconnect, so look the
                // connection up by the live FishNet OwnerId (= current connection.ClientId).
                int connectionClientId = playerData.OwnerId;
                if (!_networkManager.ServerManager.Clients.TryGetValue(connectionClientId, out var connection))
                {
                    Debug.LogError($"[PlayerSpawner] Can't get InGame player networkId={playerData.PlayerNetworkId} connection={connectionClientId}");
                    continue;
                }

                if (_spawnedPlayers.ContainsKey(playerData.PlayerNetworkId))
                {
                    Debug.Log($"[PlayerSpawner] [PlayerReconnect] Player networkId={playerData.PlayerNetworkId} already spawned, skipping");
                    continue;
                }
                
#if UNITY_SERVER || UNITY_EDITOR
                PlayerCharacterData characterData = null;
                Debug.Log($"[PlayerSpawner] Searching for character data for player {playerData.PlayerNetworkId}");
                if (_serverPayloadController == null || _serverPayloadController.Payload?.CharacterByPlayerIds != null)
                {
                    if (!PlayerCharacterData.TryGetFromPlayer(playerData, out characterData))
                        continue;
                }
#endif

                try
                {
                    Debug.Log($"[PlayerSpawner] [PlayerReconnect] Spawning player networkId={playerData.PlayerNetworkId} at {_spawnPoint.position} (connection={connectionClientId})");
                    var instance = _networkManager.ServerManager.InstantiateAndSpawn(
                        _prefab,
                        _spawnPoint.position,
                        Quaternion.identity,
                        connection);
                    
#if UNITY_SERVER || UNITY_EDITOR
                    var movement = instance.GetComponent<PlayerMovement>();
                    if (movement == null)
                    {
                        Debug.LogError($"[PlayerSpawner] Player {playerData.PlayerNetworkId} movement is null on player spawn");
                    }
                    else if (characterData != null)
                    {
                        Debug.Log($"[PlayerSpawner] Setting character name {characterData.CharacterName}");
                        movement.SetCharacterName(characterData.CharacterName);
                    }
                    else if (_serverPayloadController != null && _serverPayloadController.Payload?.CharacterByPlayerIds != null)
                    {
                        Debug.LogError($"[PlayerSpawner] Can't get player {playerData.PlayerNetworkId} character data");
                    }
#endif

                    _spawnedPlayers[playerData.PlayerNetworkId] = instance;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PlayerSpawner] Error spawning player {e}");
                }
            }
        }

        public void EnsurePlayersDespawned()
        {
            foreach (var entry in _spawnedPlayers)
            {
                if (entry.Value != null)
                    entry.Value.Despawn();
            }

            _spawnedPlayers.Clear();
        }

        private void OnPlayerRemoved(int playerId, FishNetPlayerData data)
        {
            _spawnedPlayers.Remove(playerId);
        }
    }
}