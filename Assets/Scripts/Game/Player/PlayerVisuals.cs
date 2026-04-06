using System;
using Game.Camera;
using SkillcadeSDK;
using SkillcadeSDK.Connection;
using SkillcadeSDK.FishNetAdapter.Players;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.Player
{
    public class PlayerVisuals : MonoBehaviour
    {
        private static readonly int JumpTrigger = Animator.StringToHash("JumpTrigger");
        private static readonly int Grounded = Animator.StringToHash("Grounded");
        private static readonly int VelY = Animator.StringToHash("VelY");
        private static readonly int SpeedX = Animator.StringToHash("SpeedX");
        
        [Header("References")]
        [SerializeField] private PlayerMovement _movement;
        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Image _hpBarImage;
        [SerializeField] private TMP_Text _nicknameText;
        
        [Header("Config")]
        [SerializeField] private float _speedThreshold;
        [SerializeField] private float _remoteDarkenMul = 0.7f;
        [SerializeField] private float _remoteAlpha = 0.9f;

        [Inject] private readonly IObjectResolver _objectResolver;
        [Inject] private readonly PlayerCharactersConfig _playerCharactersConfig;

        private string _nickname;

        private void OnEnable()
        {
            this.InjectToMe();
            InitNickname();
            
            _movement.JumpFx += OnJumpFx;
            _movement.OnCharacterNameChanged += InitializeCharacterName;
            if (_objectResolver.TryResolve(out FishNetPlayersController playersController))
                playersController.OnPlayerDataUpdated += OnPlayerUpdated;
        }

        private void OnDisable()
        {
            _movement.JumpFx -= OnJumpFx;
            _movement.OnCharacterNameChanged -= InitializeCharacterName;
            if (_objectResolver.TryResolve(out FishNetPlayersController playersController))
                playersController.OnPlayerDataUpdated -= OnPlayerUpdated;
        }

        private void Start()
        {
            bool isLocal = _objectResolver.TryResolve(out IConnectionController connectionController)
                && connectionController.ConnectionState == ConnectionState.SinglePlayer
                || _movement.NetworkObject.Owner.IsLocalClient;

            if (isLocal && _objectResolver.TryResolve(out GameCameraTarget cameraTarget))
            {
                SetupLocalCamera(cameraTarget);
            }
            else if (!isLocal)
            {
                TryApplyNonLocalVisuals();
            }
        }

        private void SetupLocalCamera(GameCameraTarget cameraTarget)
        {
            cameraTarget.SetFollowTarget(_movement);

            var targetCamera = FindAnyObjectByType<CinemachineCamera>(FindObjectsInactive.Include);
            if (targetCamera != null)
                targetCamera.Target.TrackingTarget = cameraTarget.transform;
        }

        private void InitNickname()
        {
            if (_movement == null || _movement.NetworkObject == null)
                return;
            
            if (!_objectResolver.TryResolve(out FishNetPlayersController playersController))
                return;
            
            if (!playersController.TryGetPlayerData(_movement.OwnerId, out var playerData))
                return;

            if (!PlayerMatchData.TryGetFromPlayer(playerData, out var matchData))
                return;
            
            SetNickname(matchData.Nickname);
        }

        public void SetNickname(string nickname) // For replays
        {
            _nickname = nickname;
            
            if (_movement.IsClientInitialized && _movement.IsOwner)
                _nicknameText.text = $"[YOU] {_nickname}";
            else
                _nicknameText.text = _nickname;
        }
        
        private void Update()
        {
            Vector2 vel = _movement.VelocityVisual;
            _animator.SetFloat(SpeedX, GetAbsWithThreshold(vel.x));
            _animator.SetFloat(VelY, GetAbsWithThreshold(vel.y));
            _animator.SetBool(Grounded, _movement.IsGroundedVisual);

            if (vel.x > 0.01f)
            {
                _spriteRenderer.flipX = false;
            }
            else if (vel.x < -0.01f)
            {
                _spriteRenderer.flipX = true;
            }
            
            _hpBarImage.fillAmount = _movement.Health01;
        }

        private float GetAbsWithThreshold(float value)
        {
            var speed = Mathf.Abs(value);
            if (speed < _speedThreshold)
                speed = 0;

            return speed;
        }
        
        private void OnJumpFx()
        {
            Debug.Log("[PlayerVisuals] set jump trigger");
            _animator.SetTrigger(JumpTrigger);
        }
        
        private void TryApplyNonLocalVisuals()
        {
            if (_spriteRenderer == null)
            {
                return;
            }
            
            Color c = _spriteRenderer.color;
            c *= _remoteDarkenMul;
            c.a = _remoteAlpha;
            _spriteRenderer.color = c;
        }

        private void InitializeCharacterName()
        {
            var characterName = _movement.CharacterName;
            Debug.Log($"[PlayerVisuals] Initializing character {characterName}");
            foreach (var characterContainer in _playerCharactersConfig.Characters)
            {
                if (string.Equals(characterContainer.CharacterName, characterName))
                {
                    Debug.Log("[PlayerVisuals] Found character");
                    _animator.runtimeAnimatorController = characterContainer.OverrideController;
                    return;
                }
            }

            Debug.Log($"[PlayerVisuals] Didn't find character {characterName}");
            var characterId = _movement.OwnerId % _playerCharactersConfig.Characters.Length;
            _animator.runtimeAnimatorController = _playerCharactersConfig.Characters[characterId].OverrideController;
        }

        private void OnPlayerUpdated(int clientId, FishNetPlayerData data)
        {
            if (_movement == null || _movement.OwnerId != clientId)
                return;
            
            if (!PlayerMatchData.TryGetFromPlayer(data, out var matchData))
                return;
            
            if (string.Equals(_nickname, matchData.Nickname, StringComparison.InvariantCulture))
                return;
            
            _nickname = matchData.Nickname;
            _nicknameText.text = _nickname;
        }
    }
}