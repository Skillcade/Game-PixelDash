using System;
using SkillcadeSDK.FishNetAdapter.Replays;
using SkillcadeSDK.Replays;
using Unity.Cinemachine;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Replays
{
    public class ReplayCameraController : IInitializable, IDisposable
    {
        [Inject] private readonly ReplayReadService _replayReadService;
        [Inject] private readonly ReplayCameraTarget _replayCameraTarget;
        [Inject] private readonly FishNetReplayPlayerDataService _fishNetReplayPlayerDataService;
        
        private CinemachineCamera _targetCamera;
        private int _targetPlayerId;
        
        public void Initialize()
        {
            _targetCamera = GameObject.FindAnyObjectByType<CinemachineCamera>(FindObjectsInactive.Include);
            _replayReadService.OnWorldChanged += RetargetCamera;
            SetFreeCamera();
        }

        public void Dispose()
        {
            _replayReadService.OnWorldChanged -= RetargetCamera;
        }

        public void SetFreeCamera()
        {
            if (_targetCamera.Target.TrackingTarget != null)
                _replayCameraTarget.transform.position = _targetCamera.Target.TrackingTarget.transform.position;
                
            _targetCamera.Target.TrackingTarget = _replayCameraTarget.transform;
            _targetPlayerId = -1;
        }

        public void SetTargetPlayerId(int playerId)
        {
            if (!_fishNetReplayPlayerDataService.PlayersData.TryGetValue(playerId, out var playerData))
                return;
            
            if (!_replayReadService.CurrentActiveWorld.ReplayObjects.TryGetValue(playerData.PlayerObjectId, out var handler))
                return;
            
            _targetCamera.Target.TrackingTarget = handler.transform;
            _targetPlayerId = playerId;
        }

        private void RetargetCamera()
        {
            if (_targetPlayerId == -1) // Don't need to retarget free camera
                return;
            
            SetTargetPlayerId(_targetPlayerId);
        }
    }
}