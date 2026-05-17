using System.Collections.Generic;
using FishNet;
using FishNet.Transporting;
using Game.Level;
using Game.Player;
using Game.RigidbodyInterpolation;
using SkillcadeSDK;
using SkillcadeSDK.Connection;
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

        [Header("Race feedback")]
        // Below this gap (in normalised progress) the race is considered "close" and both markers light up.
        [SerializeField] private float _closeGapThreshold = 0.05f;
        // Hysteresis added to the close threshold when leaving the close state to avoid flicker.
        [SerializeField] private float _closeGapHysteresis = 0.02f;

        [Inject] private readonly IConnectionController _connectionController;
        [Inject] private readonly FishNetPlayersController _playersController;

        public float LastLocalGap { get; private set; }

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
            public bool IsClose;
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
                if (entry.LocalTimeline < 0f || entry.Buffer.Count < 2)
                    continue;

                entry.LocalTimeline += Time.unscaledDeltaTime;

                InterpolationUtils.StepInterpolation(
                    entry.Buffer, entry.LocalTimeline,
                    out var from, out var to, out float t);

                entry.CurrentProgress = Mathf.Lerp(from.Progress, to.Progress, t);
                entry.Marker.SetProgress(entry.CurrentProgress);
            }

            UpdateRaceFeedback();
        }

        private void UpdateRaceFeedback()
        {
            if (_connectionController.ConnectionState is not (ConnectionState.Connected or ConnectionState.SinglePlayer))
                return;
            
            if (!_playersController.TryGetLocalPlayerData(out _))
                return;
            
            int localId = _playersController.LocalPlayerId;
            if (!_entries.TryGetValue(localId, out var local))
                return;

            // Pick the leading opponent (largest gap in either direction, ignoring the local marker).
            float bestAbsGap = 0f;
            float bestSignedGap = 0f;
            MarkerEntry leadingOpponent = null;
            foreach (var kvp in _entries)
            {
                if (kvp.Key == localId)
                    continue;
                float gap = kvp.Value.CurrentProgress - local.CurrentProgress;
                float abs = Mathf.Abs(gap);
                if (abs > bestAbsGap || leadingOpponent == null)
                {
                    bestAbsGap = abs;
                    bestSignedGap = gap;
                    leadingOpponent = kvp.Value;
                }
            }

            if (leadingOpponent == null)
            {
                LastLocalGap = 0f;
                local.Marker.SetState(PlayerProgressMarker.State.Neutral);
                return;
            }

            LastLocalGap = bestSignedGap;

            // Hysteresis: once close, require a slightly bigger gap to drop the close state.
            float threshold = local.IsClose
                ? _closeGapThreshold + _closeGapHysteresis
                : _closeGapThreshold;
            bool close = bestAbsGap < threshold;
            local.IsClose = close;
            leadingOpponent.IsClose = close;

            PlayerProgressMarker.State localState;
            PlayerProgressMarker.State opponentState;
            if (close)
            {
                localState = PlayerProgressMarker.State.Close;
                opponentState = PlayerProgressMarker.State.Close;
            }
            else if (bestSignedGap > 0f)
            {
                // Opponent is ahead → local marker reads as "behind" (red), opponent stays neutral
                // so it does not over-glorify the leader.
                localState = PlayerProgressMarker.State.Behind;
                opponentState = PlayerProgressMarker.State.Neutral;
            }
            else
            {
                // We are ahead. Subtle gold accent on local; opponent stays neutral.
                localState = PlayerProgressMarker.State.Ahead;
                opponentState = PlayerProgressMarker.State.Neutral;
            }

            local.Marker.SetState(localState);
            leadingOpponent.Marker.SetState(opponentState);

            // Any other opponents (>2 players) read as neutral; they still pulse via SetRole(Opponent).
            foreach (var kvp in _entries)
            {
                if (kvp.Key == localId || kvp.Value == leadingOpponent)
                    continue;
                kvp.Value.Marker.SetState(PlayerProgressMarker.State.Neutral);
            }
        }

        private void OnProgressReceived(PlayerProgressBroadcast broadcast, Channel channel)
        {
            foreach (var progressEntry in broadcast.Entries)
            {
                if (!_entries.TryGetValue(progressEntry.PlayerId, out var entry))
                    continue;

                float now = Time.unscaledTime;

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
            
            if (!PlayerInGameData.TryGetFromPlayer(playerData, out var inGameData) || !inGameData.InGame)
                return;

            CreatePlayerMarker(playerId, playerData);
        }

        private MarkerEntry CreatePlayerMarker(int playerId, FishNetPlayerData playerData)
        {
            var marker = Instantiate(_markerPrefab, _markersContainer);
            var nickname = PlayerMatchData.TryGetFromPlayer(playerData, out var matchData)
                ? matchData.Nickname
                : "Player";

            Sprite icon = null;
            UnityEngine.Debug.Log("[PlayerProgressLine] Searching for player character");
            if (PlayerCharacterData.TryGetFromPlayer(playerData, out var charData))
            {
                UnityEngine.Debug.Log($"[PlayerProgressLine] Character name is {charData.CharacterName}");
                icon = FindIcon(charData.CharacterName);
            }

            marker.Initialize(nickname, icon);
            marker.SetProgress(0f);
            marker.SetRole(playerId == _playersController.LocalPlayerId
                ? PlayerProgressMarker.Role.Local
                : PlayerProgressMarker.Role.Opponent);

            var entry = new MarkerEntry
            {
                Marker = marker,
                OwnerId = playerId
            };
            _entries[playerId] = entry;
            return entry;
        }

        private void OnPlayerDataUpdated(int playerId, FishNetPlayerData playerData)
        {
            if (!PlayerInGameData.TryGetFromPlayer(playerData, out var inGameData) || !inGameData.InGame)
                return;
            
            if (!_entries.TryGetValue(playerId, out var entry))
                entry = CreatePlayerMarker(playerId, playerData);

            var nickname = PlayerMatchData.TryGetFromPlayer(playerData, out var matchData)
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
