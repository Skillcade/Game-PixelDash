using FishNet.Managing;
using SkillcadeSDK.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class NetworkStarter : NetworkStarterBase
    {
        [Header("Manual connection settings")]
        [SerializeField] private NetworkManager _networkManager;
        [SerializeField] private Button _startServerButton;
        [SerializeField] private Button _connectButton;
        [SerializeField] private GameObject _inGamePanel;

        protected override void InitManualConnection()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            _startServerButton.gameObject.SetActive(false);
#endif
            _inGamePanel.SetActive(false);
            _startServerButton.onClick.AddListener(OnStartServer);
            _connectButton.onClick.AddListener(OnStartClient);
        }
        
        private void OnStartServer()
        {
            StartServer();
            _startServerButton.gameObject.SetActive(false);
            _connectButton.gameObject.SetActive(false);
        }

        private void OnStartClient()
        {
            StartClient();
            _startServerButton.gameObject.SetActive(false);
            _connectButton.gameObject.SetActive(false);
            _inGamePanel.SetActive(true);
        }

        protected override void OnConnectionStarted(ConnectionMode mode)
        {
            _startServerButton.gameObject.SetActive(false);
            _connectButton.gameObject.SetActive(false);
            _inGamePanel.SetActive(mode == ConnectionMode.Client);
        }
    }
}