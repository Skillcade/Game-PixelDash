using System;
using System.Collections.Generic;
using Game.Player;
using SkillcadeSDK;
using SkillcadeSDK.FishNetAdapter.Players;
using UnityEngine;
using VContainer;

namespace Game.GUI
{
    public class PlayerProgressLine : MonoBehaviour
    {
        [SerializeField] private PlayerProgressMarker _markerPrefab;
        [SerializeField] private RectTransform _markersContainer;
        [SerializeField] private float _minX;
        [SerializeField] private float _maxX;
        [SerializeField] private PlayerCharactersConfig _charactersConfig;

        [Inject] private readonly FishNetPlayersController _playersController;

        private class MarkerEntry
        {
            public PlayerProgressMarker Marker;
            public PlayerMovement Movement;
            public int OwnerId;
        }

        private readonly Dictionary<int, MarkerEntry> _entries = new();

        private void Start()
        {
            this.InjectToMe();

            _playersController.OnPlayerAdded += OnPlayerAdded;
            _playersController.OnPlayerDataUpdated += OnPlayerDataUpdated;
            _playersController.OnPlayerRemoved += OnPlayerRemoved;

            foreach (var playerData in _playersController.GetAllPlayersData())
                OnPlayerAdded(playerData.PlayerNetworkId, playerData);
        }

        private void OnDestroy()
        {
            if (_playersController == null)
                return;

            _playersController.OnPlayerAdded -= OnPlayerAdded;
            _playersController.OnPlayerDataUpdated -= OnPlayerDataUpdated;
            _playersController.OnPlayerRemoved -= OnPlayerRemoved;
        }

        private void Update()
        {
            foreach (var entry in _entries.Values)
            {
                if (entry.Movement == null)
                    entry.Movement = FindMovementForOwner(entry.OwnerId);

                if (entry.Movement == null)
                    continue;

                float t = Mathf.InverseLerp(_minX, _maxX, entry.Movement.transform.position.x);
                entry.Marker.SetProgress(t);
            }
        }

        private void OnPlayerAdded(int playerId, FishNetPlayerData playerData)
        {
            if (_entries.ContainsKey(playerId))
                return;

            var marker = Instantiate(_markerPrefab, _markersContainer);

            string nickname = PlayerMatchData.TryGetFromPlayer(playerData, out var matchData)
                ? matchData.Nickname
                : "Player";

            Sprite icon = null;
            if (PlayerCharacterData.TryGetFromPlayer(playerData, out var charData))
                icon = FindIcon(charData.CharacterName);

            marker.Initialize(nickname, icon);

            _entries[playerId] = new MarkerEntry
            {
                Marker = marker,
                OwnerId = playerId
            };
        }

        private void OnPlayerDataUpdated(int playerId, FishNetPlayerData playerData)
        {
            if (!_entries.TryGetValue(playerId, out var entry))
                return;

            string nickname = PlayerMatchData.TryGetFromPlayer(playerData, out var matchData)
                ? matchData.Nickname
                : "Player";

            Sprite icon = null;
            if (PlayerCharacterData.TryGetFromPlayer(playerData, out var charData))
                icon = FindIcon(charData.CharacterName);

            entry.Marker.Initialize(nickname, icon);
        }

        private void OnPlayerRemoved(int playerId, FishNetPlayerData playerData)
        {
            if (!_entries.TryGetValue(playerId, out var entry))
                return;

            Destroy(entry.Marker.gameObject);
            _entries.Remove(playerId);
        }

        private Sprite FindIcon(string characterName)
        {
            foreach (var c in _charactersConfig.Characters)
            {
                if (c.CharacterName == characterName)
                    return c.Icon;
            }

            return null;
        }

        private PlayerMovement FindMovementForOwner(int ownerId)
        {
            foreach (var movement in FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None))
            {
                if (movement.OwnerId == ownerId)
                    return movement;
            }

            return null;
        }
    }
}