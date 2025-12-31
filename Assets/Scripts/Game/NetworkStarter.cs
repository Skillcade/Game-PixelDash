using FishNet.Managing;
using Game.GUI;
using SkillcadeSDK.Common;
using SkillcadeSDK.Replays;
using UnityEngine;
using VContainer;

namespace Game
{
    public class NetworkStarter : NetworkStarterBase
    {
        [Header("Manual connection settings")]
        [SerializeField] private NetworkManager _networkManager;
        [SerializeField] private GameVersionConfig _gameVersionConfig;

        [Inject] private readonly LobbyUi _lobbyUi;
        [Inject] private readonly GameUi _gameUi;
        
        public override void Initialize()
        {
            Debug.Log($"[{_gameVersionConfig.GameName}] Starting game, version: {_gameVersionConfig.GameVersion}");
            base.Initialize();
        }

        protected override void InitManualConnection()
        {
            _gameUi.gameObject.SetActive(false);
            _lobbyUi.StartServerButton.onClick.AddListener(OnStartServer);
            _lobbyUi.ConnectButton.onClick.AddListener(OnStartClient);
        }

        private void OnStartServer()
        {
            StartServer();
            _lobbyUi.StartServerButton.gameObject.SetActive(false);
            _lobbyUi.ConnectButton.gameObject.SetActive(false);
        }

        private void OnStartClient()
        {
            StartClient();
            _lobbyUi.StartServerButton.gameObject.SetActive(false);
            _lobbyUi.ConnectButton.gameObject.SetActive(false);
            _gameUi.gameObject.SetActive(true);
        }

        protected override void OnConnectionStarted(ConnectionMode mode)
        {
            _lobbyUi.StartServerButton.gameObject.SetActive(false);
            _lobbyUi.ConnectButton.gameObject.SetActive(false);
            _gameUi.gameObject.SetActive(mode == ConnectionMode.Client);
        }
    }
}