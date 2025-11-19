// using System;
// using System.Collections.Generic;
// using FishNet.Managing;
// using FishNet.Object;
// using UnityEngine;
// using VContainer;
//
// namespace Game
// {
//     public class PlayerSpawner : MonoBehaviour
//     {
//         [SerializeField] private NetworkObject _prefab;
//
//         [Inject] private readonly NetworkManager _networkManager;
//
//         private Dictionary<int, NetworkObject> _spawnedPlayers;
//
//         private void Start()
//         {
//             _spawnedPlayers = new Dictionary<int, NetworkObject>();
//         }
//
//         public void SpawnAllInGamePlayers()
//         {
//             foreach (var entry in _playerDataService.PlayerData)
//             {
//                 if (!entry.Value.InGame)
//                     continue;
//
//                 if (!_networkManager.ServerManager.Clients.TryGetValue(entry.Key, out var connection))
//                 {
//                     Debug.LogError($"[PlayerSpawner] Can't get InGame player {entry.Key} connection");
//                     continue;
//                 }
//
//                 if (_spawnedPlayers.ContainsKey(entry.Key))
//                     continue;
//                 
//                 try
//                 {
//                     var instance = Instantiate(_prefab);
//                     _networkManager.ServerManager.Spawn(instance, connection);
//                     _spawnedPlayers[entry.Key] = instance;
//                 }
//                 catch (Exception e)
//                 {
//                     Debug.LogError($"[PlayerSpawner] Error spawning player {e}");
//                 }
//             }
//         }
//
//         public void DespawnAllPlayers()
//         {
//             foreach (var entry in _spawnedPlayers)
//             {
//                 if (entry.Value != null)
//                     entry.Value.Despawn();
//             }
//             
//             _spawnedPlayers.Clear();
//         }
//
//         private void OnPlayerRemoved(int clientId, PlayerData playerData)
//         {
//             _spawnedPlayers.Remove(clientId);
//         }
//     }
// }