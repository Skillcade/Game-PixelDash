using System.Collections.Generic;
using SkillcadeSDK;
using SkillcadeSDK.Common;
using SkillcadeSDK.Common.Players;
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

        [Inject] private readonly IPlayersController _playersController;

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

        private void OnPlayerAdded(int clientId, IPlayerData playerData)
        {
            CreatePingItem(clientId, playerData);
        }

        private void OnPlayerUpdated(int clientId, IPlayerData playerData)
        {
            if (!_playerPingItems.TryGetValue(clientId, out var playerPingItem))
                playerPingItem = CreatePingItem(clientId, playerData);
            
            if (playerData.TryGetData(PlayerDataConst.Ping, out int ping))
                playerPingItem.SetPing(ping);
            
            if (playerData.TryGetData(PlayerDataConst.Nickname, out string nickname) && nickname != playerPingItem.Username)
                playerPingItem.SetUsername(nickname, clientId == _playersController.LocalPlayerId);
        }

        private void OnPlayerRemoved(int clientId, IPlayerData playerData)
        {
            if (!_playerPingItems.TryGetValue(clientId, out var playerPingItem))
                return;
            
            Destroy(playerPingItem.gameObject);
            _playerPingItems.Remove(clientId);
        }

        private PlayerPingItem CreatePingItem(int clientId, IPlayerData playerData)
        {
            var item = Instantiate(_pingItemPrefab, _pingsRoot);
            
            if (playerData.TryGetData(PlayerDataConst.Nickname, out string nickname))
                item.SetUsername(nickname, clientId == _playersController.LocalPlayerId);
            
            if (playerData.TryGetData(PlayerDataConst.Ping, out int ping))
                item.SetPing(ping);
            
            _playerPingItems.Add(clientId, item);
            return item;
        }
    }
}