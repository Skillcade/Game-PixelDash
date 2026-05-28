using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game.Ghost
{
    [Serializable]
    public struct GhostSnapshot
    {
        public float Time;
        public float X;
        public float Y;
        public float Progress; // normalised 0-1, same scale as PlayerProgressLine
    }

    [Serializable]
    public class GhostRecording
    {
        public List<GhostSnapshot> Snapshots = new();
    }

    public static class GhostRecordingIO
    {
        private static string SavePath =>
            Path.Combine(Application.persistentDataPath, "ghost_recording.json");

        public static void Save(GhostRecording recording)
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(recording));
            Debug.Log($"[Ghost] Recording saved — {recording.Snapshots.Count} snapshots → {SavePath}");
        }

        public static bool TryLoad(out GhostRecording recording)
        {
            if (!File.Exists(SavePath))
            {
                Debug.LogWarning("[Ghost] No recording found at " + SavePath);
                recording = null;
                return false;
            }

            recording = JsonUtility.FromJson<GhostRecording>(File.ReadAllText(SavePath));
            bool valid = recording?.Snapshots?.Count > 1;
            if (!valid) Debug.LogWarning("[Ghost] Recording file exists but has no usable data.");
            return valid;
        }
    }
}
