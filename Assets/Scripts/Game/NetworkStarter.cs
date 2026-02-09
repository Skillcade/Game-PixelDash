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
        [SerializeField] private GameVersionConfig _gameVersionConfig;

        [Inject] private readonly LobbyUi _lobbyUi;
        [Inject] private readonly GameUi _gameUi;
        
        public override void Initialize()
        {
            Debug.Log($"[{_gameVersionConfig.GameName}] Starting game, version: {_gameVersionConfig.GameVersion}");
            base.Initialize();
            _gameUi.StopSinglePlayerButton.onClick.AddListener(QuitGame);
        }

        protected override void InitManualConnection()
        {
            _gameUi.gameObject.SetActive(false);
            _lobbyUi.gameObject.SetActive(true);
            _lobbyUi.StartServerButton.onClick.AddListener(OnStartServer);
            _lobbyUi.ConnectButton.onClick.AddListener(OnStartClient);
            _lobbyUi.SinglePlayerButton.onClick.AddListener(OnStartSinglePlayer);
        }

        private void QuitGame()
        {
            _webBridge.TriggerQuitSinglePlayer();
        }

        private void OnStartServer()
        {
            StartServer();
            _lobbyUi.gameObject.SetActive(false);
            _gameUi.StopSinglePlayerButton.gameObject.SetActive(false);
        }

        private void OnStartClient()
        {
            StartClient();
            _lobbyUi.gameObject.SetActive(false);
            _gameUi.gameObject.SetActive(true);
            _gameUi.StopSinglePlayerButton.gameObject.SetActive(false);
        }

        private void OnStartSinglePlayer()
        {
            StartSinglePlayer();
            _lobbyUi.gameObject.SetActive(false);
            _gameUi.gameObject.SetActive(true);
            _gameUi.StopSinglePlayerButton.gameObject.SetActive(true);
        }

        protected override void OnConnectionStarted(ConnectionMode mode)
        {
            _lobbyUi.gameObject.SetActive(false);
            _gameUi.gameObject.SetActive(mode is ConnectionMode.Client or ConnectionMode.SinglePlayer);
            _gameUi.StopSinglePlayerButton.gameObject.SetActive(mode is ConnectionMode.SinglePlayer);
        }
    }
}