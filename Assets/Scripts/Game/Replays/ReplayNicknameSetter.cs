using FishNet.Object;
using SkillcadeSDK;
using SkillcadeSDK.FishNetAdapter.Replays;
using SkillcadeSDK.Replays.Components;
using TMPro;
using UnityEngine;
using VContainer;

namespace Game.Replays
{
    public class ReplayNicknameSetter : MonoBehaviour
    {
        [SerializeField] private ReplayObjectHandler _replayObjectHandler;
        [SerializeField] private NetworkObject _networkObject;
        [SerializeField] private TMP_Text _nicknameText;
        
        [Inject] private readonly IObjectResolver _objectResolver;
        
        private bool _nicknameSet;

        private void Start()
        {
            this.InjectToMe();
        }

        private void Update()
        {
            if (_nicknameSet)
                return;

            if (_networkObject.IsSpawned)
            {
                _nicknameSet = true;
                enabled = false;
                return;
            }
            
            if (!_replayObjectHandler.IsReplaying)
                return;
            
            if (!TryGetPlayerData(out var playerData))
                return;
            
            _nicknameText.text = $"[View_{_replayObjectHandler.WorldId}] {playerData.Nickname}";
            _nicknameSet = true;
            enabled = false;
        }

        private bool TryGetPlayerData(out FishNetReplayPlayerDataService.PlayerData data)
        {
            data = null;
            if (!_objectResolver.TryResolve(out FishNetReplayPlayerDataService playerDataService))
                return false;
            
            foreach (var playerData in playerDataService.PlayersData)
            {
                if (playerData.Value.PlayerObjectId == _replayObjectHandler.ObjectId)
                {
                    data = playerData.Value;
                    return true;
                }
            }
            
            return false;
        }
    }
}