using System.Collections.Generic;
using SkillcadeSDK;
using SkillcadeSDK.FishNetAdapter.PingService;
using SkillcadeSDK.FishNetAdapter.Players;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.GUI.Debug
{
    public class DebugPanel : MonoBehaviour
    {
        [SerializeField] private Button _openButton;
        [SerializeField] private Button _closeButton;

        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Transform _pingsRoot;
        [SerializeField] private PlayerPingItem _pingItemPrefab;

        [Inject] private readonly FishNetPlayersController _playersController;

        private Dictionary<int, PlayerPingItem> _playerPingItems;
        
        private void Start()
        {
            this.InjectToMe();
            _openButton.onClick.AddListener(OpenPressed);
            _closeButton.onClick.AddListener(ClosePressed);
            
            _playersController.OnPlayerAdded += OnPlayerAdded;
            _playersController.OnPlayerDataUpdated += OnPlayerUpdated;
            _playersController.OnPlayerRemoved += OnPlayerRemoved;
            
            _playerPingItems = new Dictionary<int, PlayerPingItem>();
            InitExistingPings();
        }

        private void OnDestroy()
        {
            _playersController.OnPlayerAdded -= OnPlayerAdded;
            _playersController.OnPlayerDataUpdated -= OnPlayerUpdated;
            _playersController.OnPlayerRemoved -= OnPlayerRemoved;
        }

        private void InitExistingPings()
        {
            foreach (var playerData in _playersController.GetAllPlayersData())
            {
                CreatePingItem(playerData.PlayerNetworkId, playerData);
            }
        }

        private void OpenPressed()
        {
            _panelRoot.SetActive(true);
        }

        private void ClosePressed()
        {
            _panelRoot.SetActive(false);
        }

        private void OnPlayerAdded(int clientId, FishNetPlayerData data)
        {
            CreatePingItem(clientId, data);
        }

        private void OnPlayerUpdated(int clientId, FishNetPlayerData data)
        {
            if (!_playerPingItems.TryGetValue(clientId, out var playerPingItem))
                playerPingItem = CreatePingItem(clientId, data);
            
            if (PlayerPingData.TryGetFromPlayer(data, out var pingData))
                playerPingItem.SetPing(pingData.PingInMs);
            
            if (PlayerMatchData.TryGetFromPlayer(data, out var matchData))
                playerPingItem.SetUsername(matchData.Nickname, clientId == _playersController.LocalPlayerId);
        }

        private void OnPlayerRemoved(int clientId, FishNetPlayerData data)
        {
            if (!_playerPingItems.TryGetValue(clientId, out var playerPingItem))
                return;
            
            Destroy(playerPingItem.gameObject);
            _playerPingItems.Remove(clientId);
        }

        private PlayerPingItem CreatePingItem(int clientId, FishNetPlayerData playerData)
        {
            var item = Instantiate(_pingItemPrefab, _pingsRoot);
            
            if (PlayerPingData.TryGetFromPlayer(playerData, out var pingData))
                item.SetPing(pingData.PingInMs);
            
            if (PlayerMatchData.TryGetFromPlayer(playerData, out var matchData))
                item.SetUsername(matchData.Nickname, clientId == _playersController.LocalPlayerId);
            
            _playerPingItems.Add(clientId, item);
            return item;
        }
    }
}