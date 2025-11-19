// using DefaultNamespace.Collectables;
// using Game.GUI;
// using Game.Level;
// using MultiplayerSDK.StateMachine;
// using UnityEngine;
// using VContainer;
//
// namespace Game.StateMachine.States
// {
//     public class RunningState : NetworkState<GameStateType>
//     {
//         public override GameStateType Type => GameStateType.Running;
//
//         [Inject] private readonly GameUi _gameUi;
//         [Inject] private readonly FinishLine _finishLine;
//         [Inject] private readonly PlayerSpawner _playerSpawner;
//         [Inject] private readonly RespawnService _respawnService;
//
//
//         public override void OnEnter(GameStateType prevState)
//         {
//             base.OnEnter(prevState);
//             
//             if (IsClient)
//                 _gameUi.RunningPanel.gameObject.SetActive(true);
//
//             if (IsServer)
//             {
//                 _playerSpawner.SpawnAllInGamePlayers();
//                 _finishLine.OnPlayerReachedFinish += OnPlayerFinished;
//                 Debug.Log("RespawnAll called");
//                 _respawnService.RespawnAll();
//             }
//         }
//
//         public override void OnExit(GameStateType nextState)
//         {
//             base.OnExit(nextState);
//
//             if (IsClient)
//                 _gameUi.RunningPanel.gameObject.SetActive(false);
//             
//             if (IsServer)
//                 _finishLine.OnPlayerReachedFinish += OnPlayerFinished;
//         }
//
//         private void OnPlayerFinished(int winnerId)
//         {
//             if (IsServer)
//                 StateMachine.SetStateServer(GameStateType.Finished, new FinishedStateData(winnerId, FinishReason.ReachedFinish));
//         }
//     }
// }