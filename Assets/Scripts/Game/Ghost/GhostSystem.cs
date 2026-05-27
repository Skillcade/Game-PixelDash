using Game.GUI;
using Game.Level;
using Game.Player;
using UnityEngine;

namespace Game.Ghost
{
    /// <summary>
    /// Drop this on any persistent GameObject in the scene. No other wiring needed.
    ///
    ///   Ctrl+Shift+R  →  Start recording  (press again to stop + save)
    ///   Ctrl+Shift+G  →  Spawn / dismiss ghost playback
    ///
    /// The recording is saved to Application.persistentDataPath/ghost_recording.json.
    /// </summary>
    public class GhostSystem : MonoBehaviour
    {
        [Header("Ghost visual (optional)")]
        [Tooltip("Sprite to render on the ghost body. Leave blank for a white square fallback.")]
        [SerializeField] private Sprite  _ghostBodySprite;
        [SerializeField] private Color   _ghostColor = new Color(1f, 1f, 1f, 0.4f);
        [Tooltip("Must match the sorting layer your player character uses (e.g. \"Characters\").")]
        [SerializeField] private string  _sortingLayer = "Default";
        [SerializeField] private int     _sortingOrder = 2;

        [Header("Progress bar")]
        [Tooltip("Icon shown on the progress bar for the ghost (e.g. yellow shield).")]
        [SerializeField] private Sprite _ghostProgressIcon;
        [SerializeField] private string _ghostName = "Ghost";

        [Header("Recording")]
        [SerializeField] private float _sampleRate = 20f; // snapshots per second

        // ── Runtime state ─────────────────────────────────────────────────────

        private PlayerMovement          _localPlayer;
        private PlayerProgressLine      _progressLine;
        private ProgressSyncController  _syncController;

        // Recording
        private bool           _recording;
        private float          _recordTime;
        private float          _nextSampleTime;
        private GhostRecording _currentRecording;

        // Playback
        private bool           _playing;
        private float          _playbackTime;
        private GhostRecording _playbackData;
        private GameObject     _ghostGO;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private PlayerProgressLine ProgressLine
        {
            get
            {
                if (_progressLine == null)
                    _progressLine = FindObjectOfType<PlayerProgressLine>();
                return _progressLine;
            }
        }

        private ProgressSyncController SyncController
        {
            get
            {
                if (_syncController == null)
                    _syncController = FindObjectOfType<ProgressSyncController>();
                return _syncController;
            }
        }

        private void Update()
        {
            HandleHotkeys();
            TickRecording();
            TickPlayback();
        }

        private void OnDestroy() => StopPlayback();

        // ── Hotkeys ───────────────────────────────────────────────────────────

        private void HandleHotkeys()
        {
            bool ctrl  = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool shift = Input.GetKey(KeyCode.LeftShift)   || Input.GetKey(KeyCode.RightShift);

            if (ctrl && shift && Input.GetKeyDown(KeyCode.R))
            {
                if (_recording) StopRecording();
                else            StartRecording();
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                if (_playing) StopPlayback();
                else          StartPlayback();
            }
        }

        // ── Recording ─────────────────────────────────────────────────────────

        private void StartRecording()
        {
            _localPlayer      = FindLocalPlayer();
            _currentRecording = new GhostRecording();
            _recordTime       = 0f;
            _nextSampleTime   = 0f;
            _recording        = true;
            Debug.Log("[Ghost] Recording started — Ctrl+Shift+R to stop & save.");
        }

        private void StopRecording()
        {
            _recording = false;

            if (_currentRecording == null || _currentRecording.Snapshots.Count < 2)
            {
                Debug.LogWarning("[Ghost] Recording too short — nothing saved.");
                return;
            }

            GhostRecordingIO.Save(_currentRecording);
        }

        private void TickRecording()
        {
            if (!_recording) return;

            // Re-find local player if lost (e.g. respawn).
            if (_localPlayer == null || !_localPlayer.IsOwner)
                _localPlayer = FindLocalPlayer();
            if (_localPlayer == null) return;

            _recordTime += Time.deltaTime;
            if (_recordTime < _nextSampleTime) return;
            _nextSampleTime = _recordTime + 1f / _sampleRate;

            float worldX   = _localPlayer.transform.position.x;
            float progress = SyncController != null ? SyncController.GetProgress(worldX) : 0f;

            _currentRecording.Snapshots.Add(new GhostSnapshot
            {
                Time     = _recordTime,
                X        = worldX,
                Y        = _localPlayer.transform.position.y,
                Progress = progress
            });
        }

        // ── Playback ──────────────────────────────────────────────────────────

        private void StartPlayback()
        {
            if (!GhostRecordingIO.TryLoad(out _playbackData)) return;

            // Build ghost GameObject at the first recorded position.
            var first = _playbackData.Snapshots[0];
            _ghostGO = new GameObject("GhostRunner");
            _ghostGO.transform.position = new Vector3(first.X, first.Y, 0f);

            var sr = _ghostGO.AddComponent<SpriteRenderer>();
            sr.sprite            = _ghostBodySprite != null ? _ghostBodySprite : MakeFallbackSprite();
            sr.color             = _ghostColor;
            sr.sortingLayerName  = _sortingLayer;
            sr.sortingOrder      = _sortingOrder;

            // Register ghost on the progress bar.
            if (ProgressLine != null)
            {
                ProgressLine.RegisterGhostMarker(_ghostName, _ghostProgressIcon);
                Debug.Log("[Ghost] Registered ghost marker on progress bar.");
            }
            else
            {
                Debug.LogWarning("[Ghost] Could not find PlayerProgressLine — progress bar marker will be missing.");
            }

            _playbackTime = 0f;
            _playing      = true;
            Debug.Log("[Ghost] Playback started — Ctrl+Shift+G to dismiss.");
        }

        private void StopPlayback()
        {
            _playing = false;

            if (_ghostGO != null) { Destroy(_ghostGO); _ghostGO = null; }

            ProgressLine?.RemoveGhostMarker();
        }

        private void TickPlayback()
        {
            if (!_playing || _playbackData == null) return;

            _playbackTime += Time.deltaTime;

            var snaps = _playbackData.Snapshots;
            int count = snaps.Count;

            // Hold at end if playback time exceeded.
            if (_playbackTime >= snaps[count - 1].Time)
            {
                var last = snaps[count - 1];
                ApplyGhostPosition(last.X, last.Y, last.Progress);
                return;
            }

            // Find surrounding snapshots.
            int hi = 1;
            while (hi < count - 1 && snaps[hi].Time < _playbackTime)
                hi++;

            var from = snaps[hi - 1];
            var to   = snaps[hi];
            float t  = Mathf.InverseLerp(from.Time, to.Time, _playbackTime);

            ApplyGhostPosition(
                Mathf.Lerp(from.X,        to.X,        t),
                Mathf.Lerp(from.Y,        to.Y,        t),
                Mathf.Lerp(from.Progress, to.Progress, t));
        }

        private void ApplyGhostPosition(float x, float y, float progress)
        {
            if (_ghostGO != null)
                _ghostGO.transform.position = new Vector3(x, y, 0f);

            ProgressLine?.UpdateGhostProgress(progress);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static PlayerMovement FindLocalPlayer()
        {
            foreach (var pm in FindObjectsOfType<PlayerMovement>())
                if (pm.IsClientInitialized && pm.IsOwner)
                    return pm;
            return null;
        }

        private static Sprite MakeFallbackSprite()
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
