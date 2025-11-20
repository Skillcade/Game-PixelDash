using System;
using SkillcadeSDK;
using SkillcadeSDK.Common.Players;
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

        [Inject] private readonly IPlayersController _playersController;

        private string _nickname;

        private void OnEnable()
        {
            this.InjectToMe();
            InitNickname();
            
            _movement.JumpFx += OnJumpFx;
            _playersController.OnPlayerDataUpdated += OnPlayerUpdated;
        }

        private void OnDisable()
        {
            _movement.JumpFx -= OnJumpFx;
            _playersController.OnPlayerDataUpdated -= OnPlayerUpdated;
        }

        private void Start()
        {
            if (_movement.NetworkObject.Owner.IsLocalClient)
            {
                CinemachineCamera targetCamera = FindAnyObjectByType<CinemachineCamera>(FindObjectsInactive.Include);
                if (targetCamera != null)
                {
                    targetCamera.Target.TrackingTarget = transform;
                }
            }
            else
            {
                TryApplyNonLocalVisuals();
            }
        }

        private void InitNickname()
        {
            if (_movement == null || _movement.NetworkObject == null)
                return;
            
            if (!_playersController.TryGetPlayerData(_movement.OwnerId, out var playerData))
                return;

            if (!playerData.TryGetData(PlayerDataConst.Nickname, out string nickname))
                return;
            
            _nickname = nickname;
            _nicknameText.text = _nickname;
        }
        
        private void Update()
        {
            Vector2 vel = _movement.Velocity;
            _animator.SetFloat(SpeedX, GetAbsWithThreshold(vel.x));
            _animator.SetFloat(VelY, GetAbsWithThreshold(vel.y));
            _animator.SetBool(Grounded, _movement.IsGrounded);

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

        private void OnPlayerUpdated(int clientId, IPlayerData playerData)
        {
            if (_movement == null || _movement.OwnerId != clientId)
                return;
            
            if (!playerData.TryGetData(PlayerDataConst.Nickname, out string nickname))
                return;
            
            if (string.Equals(_nickname, nickname, StringComparison.InvariantCulture))
                return;

            _nickname = nickname;
            _nicknameText.text = _nickname;
        }
    }
}