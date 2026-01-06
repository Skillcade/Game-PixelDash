using SkillcadeSDK;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.Replays
{
    public class ReplayFreeCameraButton : MonoBehaviour
    {
        [SerializeField] private Button _freeCamButton;

        [Inject] private readonly ReplayCameraController _replayCameraController;

        private void Awake()
        {
            _freeCamButton.onClick.AddListener(EnableFreeCam);
        }

        private void EnableFreeCam()
        {
            this.InjectToMe();
            _replayCameraController.SetFreeCamera();
        }
    }
}