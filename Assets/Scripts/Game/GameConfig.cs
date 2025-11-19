using System;
using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Configs/Game")]
    public class GameConfig : ScriptableObject
    {
        [SerializeField] public float StartGameCountdownSeconds;
        [SerializeField] public float WaitAfterFinishSeconds;
    }
}