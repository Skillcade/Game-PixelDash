using Game.GUI;
using Game.Player;
using UnityEngine;

namespace Game.Ghost
{
    /// <summary>
    /// Records the local player's position + progress at a fixed sample rate during a run.
    /// StartRecording() / StopAndSave() are called by GameUiHandler via GameUi.
    /// </summary>
    public class GhostRecorder : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerProgressLine _progressLine;

        [Header("Settings")]
        [Tooltip("Snapshots per second (20 = smooth, low file size)")]
        [SerializeField] private float _sampleRate = 20f;

        private bool            _recording;
        private float           _recordTime;
        private float           _nextSampleTime;
        private GhostRecording  _current;
        private PlayerMovement  _localPlayer;

        // ── Public API called by GameUiHandler ────────────────────────────────

        public void StartRecording()
        {
            _localPlayer      = FindLocalPlayer();
            _current          = new GhostRecording();
            _recordTime       = 0f;
            _nextSampleTime   = 0f;
            _recording        = true;
            Debug.Log("[Ghost] Recording started.");
        }

        public void StopAndSave()
        {
            if (!_recording) return;
            _recording = false;

            if (_current.Snapshots.Count > 1)
                GhostRecordingIO.Save(_current);
            else
                Debug.LogWarning("[Ghost] Run too short to record.");
        }

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Update()
        {
            if (!_recording) return;

            if (_localPlayer == null)
                _localPlayer = FindLocalPlayer();
            if (_localPlayer == null) return;

            _recordTime += Time.deltaTime;
            if (_recordTime < _nextSampleTime) return;
            _nextSampleTime = _recordTime + 1f / _sampleRate;

            _current.Snapshots.Add(new GhostSnapshot
            {
                Time     = _recordTime,
                X        = _localPlayer.transform.position.x,
                Y        = _localPlayer.transform.position.y,
                Progress = _progressLine != null ? _progressLine.LocalPlayerProgress : 0f
            });
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static PlayerMovement FindLocalPlayer()
        {
            foreach (var pm in FindObjectsOfType<PlayerMovement>())
            {
                if (pm.IsClientInitialized && pm.IsOwner)
                    return pm;
            }
            return null;
        }
    }
}
