using System.Collections.Generic;
using SkillcadeSDK;
using SkillcadeSDK.Common;
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

        // [Inject] private readonly FishNetPlayerDataService _playerDataService;

        private Dictionary<int, PlayerPingItem> _playerPingItems;
        
        private void Start()
        {
            this.InjectToMe();
            _openButton.onClick.AddListener(OpenPressed);
            _closeButton.onClick.AddListener(ClosePressed);
            
            // _playerDataService.OnPlayerAdded += OnPlayerAdded;
            // _playerDataService.OnPlayerUpdated += OnPlayerUpdated;
            // _playerDataService.OnPlayerRemoved += OnPlayerRemoved;
            
            _playerPingItems = new Dictionary<int, PlayerPingItem>();
            InitExistingPings();
        }

        private void OnDestroy()
        {
            // _playerDataService.OnPlayerAdded -= OnPlayerAdded;
            // _playerDataService.OnPlayerUpdated -= OnPlayerUpdated;
            // _playerDataService.OnPlayerRemoved -= OnPlayerRemoved;
        }

        private void InitExistingPings()
        {
            // foreach (var playerData in _playerDataService.PlayerData)
            // {
            //     CreatePingItem(playerData.Key, playerData.Value);
            // }
        }

        private void OpenPressed()
        {
            _panelRoot.SetActive(true);
        }

        private void ClosePressed()
        {
            _panelRoot.SetActive(false);
        }

        private void OnPlayerAdded(int clientId, PlayerData playerData)
        {
            CreatePingItem(clientId, playerData);
        }

        private void OnPlayerUpdated(int clientId, PlayerData playerData)
        {
            if (!_playerPingItems.TryGetValue(clientId, out var playerPingItem))
                playerPingItem = CreatePingItem(clientId, playerData);
            
            playerPingItem.SetPing(playerData.Ping);
            // if (playerData.Nickname != playerPingItem.Username)
            //     playerPingItem.SetUsername(playerData.Nickname, clientId == _playerDataService.LocalPlayerId);
        }

        private void OnPlayerRemoved(int clientId, PlayerData playerData)
        {
            if (!_playerPingItems.TryGetValue(clientId, out var playerPingItem))
                return;
            
            Destroy(playerPingItem.gameObject);
            _playerPingItems.Remove(clientId);
        }

        private PlayerPingItem CreatePingItem(int clientId, PlayerData playerData)
        {
            var item = Instantiate(_pingItemPrefab, _pingsRoot);
            // item.SetUsername(playerData.Nickname, clientId == _playerDataService.LocalPlayerId);
            item.SetPing(playerData.Ping);
            _playerPingItems.Add(clientId, item);
            return item;
        }
    }
}