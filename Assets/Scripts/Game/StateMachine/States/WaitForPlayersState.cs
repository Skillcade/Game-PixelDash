// using Game.GUI;
// using MultiplayerSDK.Common;
// using MultiplayerSDK.FishNetAdapter;
// using MultiplayerSDK.StateMachine;
// using UnityEngine.Pool;
// using VContainer;
//
// namespace Game.StateMachine.States
// {
//     public class WaitForPlayersState : NetworkState<GameStateType>
//     {
//         public override GameStateType Type => GameStateType.WaitForPlayers;
//
//         [Inject] private readonly GameUi _gameUi;
//         [Inject] private readonly GameConfig _gameConfig;
//         [Inject] private readonly PlayerSpawner _playerSpawner;
//         [Inject] private readonly FishNetPlayerDataService _playerDataService;
//
//         private bool _skipUpdate;
//         
//         public override void OnEnter(GameStateType prevState)
//         {
//             base.OnEnter(prevState);
//             _playerDataService.OnPlayerUpdated += OnPlayerUpdated;
//
//             if (IsServer)
//             {
//                 ClearReadyStateForPlayers();
//                 _playerSpawner.DespawnAllPlayers();
//             }
//
//             if (IsClient)
//             {
//                 _gameUi.WaitForPlayersPanel.gameObject.SetActive(true);
//                 _gameUi.WaitForPlayersPanel.OnReadyStateChanged += OnReadyStateChanged;
//                 UpdateUi();
//             }
//         }
//
//         public override void OnExit(GameStateType nextState)
//         {
//             base.OnExit(nextState);
//             _playerDataService.OnPlayerUpdated -= OnPlayerUpdated;
//             
//             if (IsClient)
//             {
//                 _gameUi.WaitForPlayersPanel.gameObject.SetActive(false);
//                 _gameUi.WaitForPlayersPanel.OnReadyStateChanged -= OnReadyStateChanged;
//             }
//         }
//
//         private void OnPlayerUpdated(int clientId, PlayerData playerData)
//         {
//             if (_skipUpdate)
//                 return;
//             
//             if (IsClient)
//                 UpdateUi();
//             
//             if (IsServer)
//             {
//                 int readyPlayers = 0;
//                 int notReadyPlayers = 0;
//                 foreach (var entry in _playerDataService.PlayerData)
//                 {
//                     if (entry.Value.IsReady)
//                         readyPlayers++;
//                     else
//                         notReadyPlayers++;
//                 }
//                 
//                 bool shouldStartGame = readyPlayers >= 1 && notReadyPlayers == 0;
//                 if (!shouldStartGame) return;
//
//                 SetReadyPlayersInGame();
//                 _playerSpawner.SpawnAllInGamePlayers();
//                 StateMachine.SetStateServer(GameStateType.Countdown);
//             }
//         }
//
//         private void ClearReadyStateForPlayers()
//         {
//             using var playersPooled = ListPool<(int, PlayerData)>.Get(out var players);
//             foreach (var entry in _playerDataService.PlayerData)
//             {
//                 players.Add((entry.Key, entry.Value));
//             }
//             
//             _skipUpdate = true;
//             foreach (var (playerId, playerData) in players)
//             {
//                 _playerDataService.SetDataOnServer(playerData.WithInGame(false).WithIsReady(false), playerId);
//             }
//             _skipUpdate = false;
//         }
//
//         private void SetReadyPlayersInGame()
//         {
//             using var playersPooled = ListPool<(int, PlayerData)>.Get(out var players);
//             foreach (var entry in _playerDataService.PlayerData)
//             {
//                 players.Add((entry.Key, entry.Value));
//             }
//
//             _skipUpdate = true;
//             foreach (var (playerId, playerData) in players)
//             {
//                 _playerDataService.SetDataOnServer(playerData.WithInGame(playerData.IsReady), playerId);
//             }
//             _skipUpdate = false;
//         }
//
//         private void UpdateUi()
//         {
//             bool localReady = _playerDataService.TryGetLocalClientData(out var data) && data.IsReady;
//             int readyPlayers = 0;
//             foreach (var entry in _playerDataService.PlayerData)
//             {
//                 if (entry.Value.IsReady)
//                     readyPlayers++;
//             }
//             
//             _gameUi.WaitForPlayersPanel.SetReadyState(readyPlayers, _playerDataService.PlayerData.Count, localReady);
//         }
//
//         private void OnReadyStateChanged(bool isReady)
//         {
//             if (!_playerDataService.TryGetLocalClientData(out var data))
//                 return;
//             
//             _playerDataService.SetDataOnLocalClient(data.WithIsReady(isReady));
//         }
//     }
// }