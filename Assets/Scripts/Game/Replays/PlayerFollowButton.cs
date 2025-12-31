using SkillcadeSDK.Replays;
using SkillcadeSDK.Replays.GUI;
using Unity.Cinemachine;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.Replays
{
    public class PlayerFollowButton : MonoBehaviour
    {
        [SerializeField] private ReplayPlayerInfoItem _item;

        [Inject] private readonly ReplayReadService _replayReadService;
        [Inject] private readonly FishNetReplayPlayerDataService _fishNetReplayPlayerDataService;
        
        private CinemachineCamera _targetCamera;
        
        private void Start()
        {
            _item.FollowButton.onClick.AddListener(FollowPlayer);
        }

        private void FollowPlayer()
        {
            this.InjectToMe();
            if (!_fishNetReplayPlayerDataService.PlayersData.TryGetValue(_item.PlayerId, out var playerData))
                return;
            
            if (!_replayReadService.ReplayObjects.TryGetValue(playerData.PlayerObjectId, out var handler))
                return;
            
            if (_targetCamera == null)
                _targetCamera = FindAnyObjectByType<CinemachineCamera>(FindObjectsInactive.Include);
            
            if (_targetCamera != null)
                _targetCamera.Target.TrackingTarget = handler.transform;
        }
    }
}