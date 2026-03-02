using System;
using UnityEngine;

namespace Game.Player
{
    [Serializable]
    public class PlayerCharacterContainer
    {
        public string CharacterName;
        public AnimatorOverrideController OverrideController;
    }
    
    [CreateAssetMenu(fileName = "PlayerCharactersConfig", menuName = "Configs/Player Characters")]
    public class PlayerCharactersConfig : ScriptableObject
    {
        [SerializeField] public PlayerCharacterContainer[] Characters;
    }
}