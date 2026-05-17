using SkillcadeSDK.Common;
using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Configs/Game")]
    public class GameConfig : ScriptableObject, ISkillcadeConfig
    {
        [field: SerializeField] public float StartGameCountdownSeconds { get; private set; }
        [field: SerializeField] public float WaitAfterFinishSeconds { get; private set; }
        [field: SerializeField] public float WaitBeforeFinishSeconds { get; private set; }
        [field: SerializeField] public bool UseReplaysV1 { get; private set; }
        [field: SerializeField] public float GameTimerSeconds { get; private set; }
    }
}