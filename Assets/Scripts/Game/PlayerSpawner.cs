using System;
using System.Collections.Generic;
using FishNet.Managing;
using FishNet.Object;
using SkillcadeSDK.Common.Players;
using SkillcadeSDK.FishNetAdapter;
using SkillcadeSDK.FishNetAdapter.Players;
using UnityEngine;
using VContainer;

// #if UNITY_SERVER || UNITY_EDITOR
// using Game.Player;
// using SkillcadeSDK.ServerValidation;
// #endif

namespace Game
{
    public class PlayerSpawner : MonoBehaviour, IPlayerSpawner
    {
        [SerializeField] private NetworkObject _prefab;
        [SerializeField] private Transform _spawnPoint;

        [Inject] private readonly IObjectResolver _objectResolver;
        
// #if UNITY_SERVER || UNITY_EDITOR
//         [Inject] private readonly ServerPayloadController _serverPayloadController;
// #endif
        
        private NetworkManager _networkManager;
        private FishNetPlayersController _playersController;

        private Dictionary<int, NetworkObject> _spawnedPlayers;

        private void Start()
        {
            _spawnedPlayers = new Dictionary<int, NetworkObject>();
            if (_objectResolver.TryResolve(out _playersController))
                _playersController.OnPlayerRemoved += OnPlayerRemoved;
            
            _objectResolver.TryResolve(out _networkManager);
        }

        public void EnsurePlayersSpawned()
        {
            foreach (var playerData in _playersController.GetAllPlayersData())
            {
                if (!PlayerInGameData.TryGetFromPlayer(playerData, out var data) || !data.InGame)
                    continue;

                if (!_networkManager.ServerManager.Clients.TryGetValue(playerData.PlayerNetworkId, out var connection))
                {
                    Debug.LogError($"[PlayerSpawner] Can't get InGame player {playerData.PlayerNetworkId} connection");
                    continue;
                }

                if (_spawnedPlayers.ContainsKey(playerData.PlayerNetworkId))
                    continue;
                
// #if UNITY_SERVER || UNITY_EDITOR
//                 PlayerCharacterData characterData = null;
//                 if (_serverPayloadController.Payload != null &&
//                     _serverPayloadController.Payload.CharacterByPlayerIds != null)
//                 {
//                     if (!PlayerCharacterData.TryGetFromPlayer(playerData, out characterData))
//                         continue;
//                 }
// #endif
                
                try
                {
                    var instance = _networkManager.ServerManager.InstantiateAndSpawn(
                        _prefab,
                        _spawnPoint.position,
                        Quaternion.identity,
                        connection);
                    
// #if UNITY_SERVER || UNITY_EDITOR
//                     if (characterData != null && instance.TryGetComponent(out PlayerMovement movement))
//                         movement.SetCharacterName(characterData.CharacterName);
// #endif
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