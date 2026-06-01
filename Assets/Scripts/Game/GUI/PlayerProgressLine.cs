using System.Collections.Generic;
using FishNet;
using FishNet.Transporting;
using Game.Level;
using Game.RigidbodyInterpolation;
using SkillcadeSDK;
using SkillcadeSDK.Connection;
using SkillcadeSDK.FishNetAdapter.Players;
using UnityEngine;
using UnityEngine.UI;
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

        [Header("Player icons")]
        [SerializeField] private Sprite _localPlayerIcon;
        [SerializeField] private Sprite _opponentIcon;

        [Header("Race feedback")]
        // Below this gap (in normalised progress) the race is considered "close" and both markers light up.
        [SerializeField] private float _closeGapThreshold = 0.05f;
        // Hysteresis added to the close threshold when leaving the close state to avoid flicker.
        [SerializeField] private float _closeGapHysteresis = 0.02f;

        [Header("Bar glow")]
        [SerializeField] private Image _barBackground;
        [SerializeField] private Color _barNeutralColor = new Color(0.12f, 0.12f, 0.14f, 1f);
        [SerializeField] private Color _barAheadColor   = new Color(0.05f, 0.10f, 0.35f, 1f);
        [SerializeField] private Color _barBehindColor  = new Color(0.35f, 0.04f, 0.04f, 1f);
        // Gradient overlay colours — RGB is the hue, alpha controls peak brightness of the soft haze.
        [SerializeField] private Color _glowAheadColor  = new Color(0.10f, 0.45f, 1.00f, 0.40f);
        [SerializeField] private Color _glowBehindColor = new Color(1.00f, 0.15f, 0.05f, 0.40f);
        [SerializeField] private float _barGlowLerpSpeed = 2f;

        private Color _barCurrentColor;
        private Color _barTargetColor;
        // Gradient glow overlay — spawned at runtime, sits between image-bg and the markers.
        private Image _barGlowImage;
        private Texture2D _glowTexture;
        private Color _glowCurrentColor;
        private Color _glowTargetColor;

        [Inject] private readonly IConnectionController _connectionController;
        [Inject] private readonly FishNetPlayersController _playersController;

        public float LastLocalGap       { get; private set; }
        /// <summary>Current normalised progress (0-1) of the local player. Used by GhostRecorder.</summary>
        public float LocalPlayerProgress { get; private set; }

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

        // ── Ghost marker (GhostPlaybackController drives these) ───────────────
        private PlayerProgressMarker _ghostMarker;
        private float                _ghostProgress;
        private bool                 _ghostIsClose;

        private void Start()
        {
            this.InjectToMe();

#if UNITY_SERVER && !UNITY_EDITOR
            return;
#endif

            CreateGlowOverlay();

            InstanceFinder.ClientManager.RegisterBroadcast<PlayerProgressBroadcast>(OnProgressReceived);

            _playersController.OnPlayerAdded += OnPlayerAdded;
            _playersController.OnPlayerDataUpdated += OnPlayerDataUpdated;
            _playersController.OnPlayerRemoved += OnPlayerRemoved;

            foreach (var playerData in _playersController.GetAllPlayersData())
                OnPlayerAdded(playerData.PlayerNetworkId, playerData);

            _barCurrentColor = _barNeutralColor;
            _barTargetColor  = _barNeutralColor;
            if (_barBackground != null)
                _barBackground.color = _barCurrentColor;
        }

        private void OnDestroy()
        {
#if UNITY_SERVER && !UNITY_EDITOR
            return;
#endif
            
            if (InstanceFinder.ClientManager != null)
                InstanceFinder.ClientManager.UnregisterBroadcast<PlayerProgressBroadcast>(OnProgressReceived);

            if (_playersController == null)
                return;

            _playersController.OnPlayerAdded -= OnPlayerAdded;
            _playersController.OnPlayerDataUpdated -= OnPlayerDataUpdated;
            _playersController.OnPlayerRemoved -= OnPlayerRemoved;

            if (_glowTexture != null)
                Destroy(_glowTexture);
        }

        private void Update()
        {
#if UNITY_SERVER && !UNITY_EDITOR
            return;
#endif
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

            if (_barBackground != null)
            {
                _barCurrentColor = Color.Lerp(_barCurrentColor, _barTargetColor, Time.unscaledDeltaTime * _barGlowLerpSpeed);
                _barBackground.color = _barCurrentColor;
            }

            if (_barGlowImage != null)
            {
                _glowCurrentColor = Color.Lerp(_glowCurrentColor, _glowTargetColor, Time.unscaledDeltaTime * _barGlowLerpSpeed);
                _barGlowImage.color = _glowCurrentColor;
            }
        }

        private void UpdateRaceFeedback()
        {
            if (_connectionController.ConnectionState is not (ConnectionState.Connected or ConnectionState.SinglePlayer))
                return;

            if (!_playersController.TryGetLocalPlayerId(out var localPlayerId))
                return;

            if (!_entries.TryGetValue(localPlayerId, out var local))
                return;

            // Expose local progress for GhostRecorder.
            LocalPlayerProgress = local.CurrentProgress;

            // Pick the leading opponent — real players first, then fall back to ghost.
            float bestAbsGap = 0f;
            float bestSignedGap = 0f;
            MarkerEntry leadingOpponent = null;
            foreach (var kvp in _entries)
            {
                if (kvp.Key == localPlayerId)
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

            // If no real opponent, use ghost (if active).
            if (leadingOpponent == null && _ghostMarker != null)
            {
                float ghostGap = _ghostProgress - local.CurrentProgress;
                bestAbsGap    = Mathf.Abs(ghostGap);
                bestSignedGap = ghostGap;
            }

            bool ghostIsLeading = leadingOpponent == null && _ghostMarker != null;

            if (leadingOpponent == null && _ghostMarker == null)
            {
                LastLocalGap = 0f;
                local.Marker.SetState(PlayerProgressMarker.State.Neutral);
                _barTargetColor  = _barNeutralColor;
                _glowTargetColor = Color.clear;
                return;
            }

            LastLocalGap = bestSignedGap;

            float threshold = local.IsClose
                ? _closeGapThreshold + _closeGapHysteresis
                : _closeGapThreshold;
            bool close = bestAbsGap < threshold;
            local.IsClose    = close;
            _ghostIsClose    = close;
            if (leadingOpponent != null) leadingOpponent.IsClose = close;

            PlayerProgressMarker.State localState;
            PlayerProgressMarker.State opponentState;
            if (close)
            {
                localState    = PlayerProgressMarker.State.Close;
                opponentState = PlayerProgressMarker.State.Close;
                _barTargetColor  = _barNeutralColor;
                _glowTargetColor = Color.clear;
            }
            else if (bestSignedGap > 0f)
            {
                localState    = PlayerProgressMarker.State.Behind;
                opponentState = PlayerProgressMarker.State.Neutral;
                _barTargetColor  = _barBehindColor;
                _glowTargetColor = _glowBehindColor;
            }
            else
            {
                localState    = PlayerProgressMarker.State.Ahead;
                opponentState = PlayerProgressMarker.State.Neutral;
                _barTargetColor  = _barAheadColor;
                _glowTargetColor = _glowAheadColor;
            }

            local.Marker.SetState(localState);

            if (ghostIsLeading)
                _ghostMarker.SetState(opponentState);
            else if (leadingOpponent != null)
                leadingOpponent.Marker.SetState(opponentState);

            // Ghost stays neutral when a real leading opponent exists.
            if (!ghostIsLeading && _ghostMarker != null)
                _ghostMarker.SetState(PlayerProgressMarker.State.Neutral);

            foreach (var kvp in _entries)
            {
                if (kvp.Key == localPlayerId || kvp.Value == leadingOpponent)
                    continue;
                kvp.Value.Marker.SetState(PlayerProgressMarker.State.Neutral);
            }
        }

        // ── Ghost marker public API ───────────────────────────────────────────

        /// <summary>Spawns a ghost marker on the bar using the opponent icon style.</summary>
        public void RegisterGhostMarker(string name, Sprite icon)
        {
            if (_ghostMarker != null)
                RemoveGhostMarker();

            _ghostMarker = Instantiate(_markerPrefab, _markersContainer);
            _ghostMarker.Initialize(name, icon);
            _ghostMarker.SetProgress(0f);
            _ghostMarker.SetRole(PlayerProgressMarker.Role.Opponent);
            _ghostProgress = 0f;
            _ghostIsClose  = false;
        }

        /// <summary>Updates the ghost marker's bar position.</summary>
        public void UpdateGhostProgress(float progress)
        {
            _ghostProgress = progress;
            _ghostMarker?.SetProgress(progress);
        }

        /// <summary>Destroys the ghost marker and resets ghost state.</summary>
        public void RemoveGhostMarker()
        {
            if (_ghostMarker != null)
            {
                Destroy(_ghostMarker.gameObject);
                _ghostMarker = null;
            }
            _ghostProgress = 0f;
            _ghostIsClose  = false;
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

            RemoveDuplicateMarkerForPlayer(playerData);
            
            if (!PlayerInGameData.TryGetFromPlayer(playerData, out var inGameData) || !inGameData.InGame)
                return;

            CreatePlayerMarker(playerId, playerData);
        }

        private void RemoveDuplicateMarkerForPlayer(FishNetPlayerData playerData)
        {
            if (!PlayerMatchData.TryGetFromPlayer(playerData, out var matchData)
                || string.IsNullOrEmpty(matchData.PlayerId))
                return;

            var staleKeys = new List<int>();
            foreach (var kvp in _entries)
            {
                if (kvp.Key == playerData.PlayerNetworkId)
                    continue;

                if (!_playersController.TryGetPlayerData(kvp.Key, out var existingData))
                    continue;

                if (!PlayerMatchData.TryGetFromPlayer(existingData, out var existingMatch))
                    continue;

                if (existingMatch.PlayerId == matchData.PlayerId)
                    staleKeys.Add(kvp.Key);
            }

            for (int i = 0; i < staleKeys.Count; i++)
                RemoveMarkerEntry(staleKeys[i]);
        }

        private void RemoveMarkerEntry(int playerId)
        {
            if (!_entries.TryGetValue(playerId, out var entry))
                return;

            Destroy(entry.Marker.gameObject);
            _entries.Remove(playerId);
        }

        private MarkerEntry CreatePlayerMarker(int playerId, FishNetPlayerData playerData)
        {
            var marker = Instantiate(_markerPrefab, _markersContainer);
            var nickname = PlayerMatchData.TryGetFromPlayer(playerData, out var matchData)
                ? matchData.Nickname
                : "Player";

            bool isLocal = _playersController.IsLocalPlayerId(playerId);
            Sprite icon = isLocal ? _localPlayerIcon : _opponentIcon;

            marker.Initialize(nickname, icon);
            marker.SetProgress(0f);
            marker.SetRole(_playersController.IsLocalPlayerId(playerId)
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

            RemoveDuplicateMarkerForPlayer(playerData);
            
            if (!_entries.TryGetValue(playerId, out var entry))
                entry = CreatePlayerMarker(playerId, playerData);

            var nickname = PlayerMatchData.TryGetFromPlayer(playerData, out var matchData)
                ? matchData.Nickname
                : "Player";

            bool isLocal = _playersController.IsLocalPlayerId(playerId);
            Sprite icon = isLocal ? _localPlayerIcon : _opponentIcon;

            entry.Marker.Initialize(nickname, icon);
        }

        private void OnPlayerRemoved(int playerId, FishNetPlayerData playerData)
        {
            RemoveMarkerEntry(playerId);
        }

        // ── Glow overlay ─────────────────────────────────────────────────────────

        /// <summary>
        /// Spawns a full-stretch Image between image-bg (index 0) and the markers
        /// (added at runtime). Its colour is driven by race state each frame.
        /// </summary>
        private void CreateGlowOverlay()
        {
            var go = new GameObject("image-glow",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(_markersContainer, false);

            // Stretch to fill the entire bar.
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Sit at index 1 — above image-bg (0) but below markers added later.
            go.transform.SetSiblingIndex(1);

            _barGlowImage = go.GetComponent<Image>();
            _barGlowImage.sprite = CreateGradientSprite();
            _barGlowImage.type   = Image.Type.Simple;
            _barGlowImage.color  = Color.clear;

            _glowCurrentColor = Color.clear;
            _glowTargetColor  = Color.clear;
        }

        /// <summary>
        /// Generates a 1×64 white texture whose alpha follows a bell curve
        /// (transparent at top/bottom edges, opaque in the centre).
        /// Unity bilinear-stretches this across the bar for a soft inner glow.
        /// </summary>
        private Sprite CreateGradientSprite()
        {
            const int H = 64;
            _glowTexture = new Texture2D(1, H, TextureFormat.RGBA32, false)
            {
                wrapMode   = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            for (int y = 0; y < H; y++)
            {
                float t     = (float)y / (H - 1);
                float alpha = Mathf.Sin(t * Mathf.PI); // 0 → 1 → 0
                _glowTexture.SetPixel(0, y, new Color(1f, 1f, 1f, alpha));
            }
            _glowTexture.Apply();

            return Sprite.Create(
                _glowTexture,
                new Rect(0, 0, 1, H),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: 1f);
        }

    }
}
