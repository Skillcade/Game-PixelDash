using Game.GUI;
using UnityEngine;

namespace Game.Ghost
{
    /// <summary>
    /// Ctrl+Shift+L → loads the last ghost recording and plays it back as a moving GameObject.
    /// The ghost is also registered as a marker on PlayerProgressLine so the glow and
    /// shield icons work exactly like a real opponent.
    /// </summary>
    public class GhostPlaybackController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerProgressLine _progressLine;

        [Header("Ghost prefab")]
        [Tooltip("Assign a prefab with a SpriteRenderer. It will be moved each frame.")]
        [SerializeField] private GameObject _ghostPrefab;
        [SerializeField] private Sprite     _ghostIcon;   // shown on the progress bar
        [SerializeField] private string     _ghostName = "Ghost";

        [Header("Visual")]
        [SerializeField] [Range(0f, 1f)] private float _ghostAlpha = 0.45f;

        // Runtime state
        private GhostRecording  _recording;
        private GameObject      _ghostInstance;
        private SpriteRenderer  _ghostRenderer;
        private bool            _playing;
        private float           _playbackTime;

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Update()
        {
            HandleHotkey();
            TickPlayback();
        }

        private void OnDestroy()
        {
            StopGhost();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void StopGhost()
        {
            _playing = false;
            if (_ghostInstance != null)
            {
                Destroy(_ghostInstance);
                _ghostInstance = null;
            }
            if (_progressLine != null)
                _progressLine.RemoveGhostMarker();
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void HandleHotkey()
        {
            bool ctrl  = Input.GetKey(KeyCode.LeftControl)  || Input.GetKey(KeyCode.RightControl);
            bool shift = Input.GetKey(KeyCode.LeftShift)    || Input.GetKey(KeyCode.RightShift);
            bool g     = Input.GetKeyDown(KeyCode.G);

            if (!ctrl || !shift || !g) return;

            // Toggle: if already playing, stop; otherwise start.
            if (_playing)
            {
                StopGhost();
                return;
            }

            if (!GhostRecordingIO.TryLoad(out _recording))
            {
                Debug.LogWarning("[Ghost] No recording to play back. Complete a run first.");
                return;
            }

            SpawnGhost();
        }

        private void SpawnGhost()
        {
            if (_ghostPrefab == null)
            {
                Debug.LogError("[Ghost] _ghostPrefab is not assigned on GhostPlaybackController.");
                return;
            }

            // Place ghost at the first snapshot position.
            var firstSnap = _recording.Snapshots[0];
            _ghostInstance = Instantiate(_ghostPrefab,
                new Vector3(firstSnap.X, firstSnap.Y, 0f), Quaternion.identity);

            // Make it semi-transparent.
            _ghostRenderer = _ghostInstance.GetComponentInChildren<SpriteRenderer>();
            if (_ghostRenderer != null)
            {
                var c = _ghostRenderer.color;
                c.a = _ghostAlpha;
                _ghostRenderer.color = c;
            }

            // Register with progress bar.
            if (_progressLine != null)
                _progressLine.RegisterGhostMarker(_ghostName, _ghostIcon);

            _playbackTime = 0f;
            _playing      = true;
            Debug.Log("[Ghost] Playback started.");
        }

        private void TickPlayback()
        {
            if (!_playing || _recording == null) return;

            _playbackTime += Time.deltaTime;

            var snaps = _recording.Snapshots;
            int count  = snaps.Count;

            // Find the two snapshots to interpolate between.
            if (_playbackTime >= snaps[count - 1].Time)
            {
                // Reached the end — hold at finish position.
                var last = snaps[count - 1];
                MoveGhost(last.X, last.Y, last.Progress);
                return;
            }

            // Binary-ish scan (list is small, linear is fine at 20 Hz).
            int nextIdx = 1;
            while (nextIdx < count - 1 && snaps[nextIdx].Time < _playbackTime)
                nextIdx++;

            var from = snaps[nextIdx - 1];
            var to   = snaps[nextIdx];
            float t  = Mathf.InverseLerp(from.Time, to.Time, _playbackTime);

            MoveGhost(
                Mathf.Lerp(from.X,        to.X,        t),
                Mathf.Lerp(from.Y,        to.Y,        t),
                Mathf.Lerp(from.Progress, to.Progress, t));
        }

        private void MoveGhost(float x, float y, float progress)
        {
            if (_ghostInstance != null)
                _ghostInstance.transform.position = new Vector3(x, y, 0f);

            if (_progressLine != null)
                _progressLine.UpdateGhostProgress(progress);
        }
    }
}
