using Game.Replays;
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

        [Inject] private readonly ReplayCameraController _replayCameraController;
        
        private CinemachineCamera _targetCamera;
        
        private void Start()
        {
            _item.FollowButton.onClick.AddListener(FollowPlayer);
        }

        private void FollowPlayer()
        {
            this.InjectToMe();
            _replayCameraController.SetTargetPlayerId(_item.PlayerId);
        }
    }
}