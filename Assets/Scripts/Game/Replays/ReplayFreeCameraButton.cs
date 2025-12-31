using UnityEngine;
using UnityEngine.UI;

namespace Game.Replays
{
    public class ReplayFreeCameraButton : MonoBehaviour
    {
        [SerializeField] private Button _freeCamButton;
        [SerializeField] private ReplayCameraTarget _replayCameraTarget;

        private void Awake()
        {
            _freeCamButton.onClick.AddListener(EnableFreeCam);
        }

        private void EnableFreeCam()
        {
            _replayCameraTarget.SetupCamera();
        }
    }
}