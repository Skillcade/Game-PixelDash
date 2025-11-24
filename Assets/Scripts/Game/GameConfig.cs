using System;
using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Configs/Game")]
    public class GameConfig : ScriptableObject
    {
        [SerializeField] public string GameName;
        [SerializeField] public string GameVersion;
        [SerializeField] public float StartGameCountdownSeconds;
        [SerializeField] public float WaitAfterFinishSeconds;
    }
}