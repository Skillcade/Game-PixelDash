using System;
using UnityEngine;

namespace Game.Player
{
    [Serializable]
    public class PlayerCharacterContainer
    {
        public string CharacterName;
        public AnimatorOverrideController OverrideController;
        public Sprite Icon;
    }
    
    [CreateAssetMenu(fileName = "PlayerCharactersConfig", menuName = "Configs/Player Characters")]
    public class PlayerCharactersConfig : ScriptableObject
    {
        [SerializeField] public PlayerCharacterContainer[] Characters;
    }
}