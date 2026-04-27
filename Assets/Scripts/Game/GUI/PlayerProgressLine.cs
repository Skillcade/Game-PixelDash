using System.Collections.Generic;
using FishNet;
using FishNet.Transporting;
using Game.Level;
using Game.Player;
using Game.RigidbodyInterpolation;
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
        // Should match ProgressSyncController._syncInterval so the buffer always has two snapshots to interpolate between.
        [SerializeField] private float _bufferTime = 0.35f;
        [SerializeField] private float _snapBackwardThreshold = 0.05f;
        [SerializeField] private PlayerCharactersConfig _charactersConfig;

        [Inject] private readonly FishNetPlayersController _playersController;

        private struct ProgressSnapshot : IInterpolateSnapshot
        {
            public float RemoteTime { get; set; }
            public float LocalTime { get; set; }
            public float Progress;
        }

        private class MarkerEntry
        {
            public PlayerProgressMarker Marker;
            public int OwnerId;
            public float CurrentProgress;
            public readonly SortedList<float, ProgressSnapshot> Buffer = new();
            public float LocalTimeline = -1f;
        }

        private readonly Dictionary<int, MarkerEntry> _entries = new();

        private void Start()
        {
            this.InjectToMe();

            InstanceFinder.ClientManager.RegisterBroadcast<PlayerProgressBroadcast>(OnProgressReceived);

            _playersController.OnPlayerAdded += OnPlayerAdded;
            _playersController.OnPlayerDataUpdated += OnPlayerDataUpdated;
            _playersController.OnPlayerRemoved += OnPlayerRemoved;

            foreach (var playerData in _playersController.GetAllPlayersData())
                OnPlayerAdded(playerData.PlayerNetworkId, playerData);
        }

        private void OnDestroy()
        {
            if (InstanceFinder.ClientManager != null)
                InstanceFinder.ClientManager.UnregisterBroadcast<PlayerProgressBroadcast>(OnProgressReceived);

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
                if (entry.LocalTimeline < 0f || entry.Buffer.Count == 0)
                    continue;

                entry.LocalTimeline += Time.unscaledDeltaTime;

                InterpolationUtils.StepInterpolation(
                    entry.Buffer, entry.LocalTimeline,
                    out var from, out var to, out float t);

                entry.CurrentProgress = Mathf.Lerp(from.Progress, to.Progress, t);
                entry.Marker.SetProgress(entry.CurrentProgress);
            }
        }

        private void OnProgressReceived(PlayerProgressBroadcast broadcast, Channel channel)
        {
            foreach (var progressEntry in broadcast.Entries)
            {
                if (!_entries.TryGetValue(progressEntry.PlayerId, out var entry))
                    continue;

                float now = Time.time;

                if (entry.Buffer.Count > 0)
                {
                    var last = entry.Buffer.Values[entry.Buffer.Count - 1];
                    if (progressEntry.Progress < last.Progress - _snapBackwardThreshold)
                    {
                        entry.Buffer.Clear();
                        entry.CurrentProgress = progressEntry.Progress;
                        entry.LocalTimeline = -1f;
                    }
                }

                InterpolationUtils.InsertIfNotExists(entry.Buffer, 10, new ProgressSnapshot
                {
                    RemoteTime = now,
                    LocalTime = now,
                    Progress = progressEntry.Progress
                });

                if (entry.LocalTimeline < 0f)
                    entry.LocalTimeline = now - _bufferTime;
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
    }
}
