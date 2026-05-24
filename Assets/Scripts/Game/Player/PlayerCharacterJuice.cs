using System.Collections;
using Game.Buffs;
using Game.GameFeel;
using SkillcadeSDK;
using SkillcadeSDK.Connection;
using UnityEngine;
using VContainer;

namespace Game.Player
{
    /// <summary>
    /// Visual juice that lives next to PlayerVisuals on the player prefab.
    /// Handles squash/stretch on jump and landing, dust particles, landing screen shake,
    /// and visual hit-stop on death and speed-buff pickup. All effects are local-owner only
    /// and use unscaled time — the FishNet simulation in PlayerMovement is never paused.
    /// </summary>
    public class PlayerCharacterJuice : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerMovement _movement;
        [SerializeField] private PlayerSpeedBuffs _speedBuffs;
        [SerializeField] private Transform _visualsTransform;
        [SerializeField] private Animator _animator;

        [Header("Squash & Stretch")]
        [SerializeField] private Vector3 _jumpStretch = new Vector3(0.8f, 1.25f, 1f);
        [SerializeField] private Vector3 _landSquash = new Vector3(1.25f, 0.8f, 1f);
        [SerializeField] private float _squashDuration = 0.18f;

        [Header("Particles")]
        [SerializeField] private ParticleSystem _jumpDust;
        [SerializeField] private ParticleSystem _landDust;

        [Header("Landing impact")]
        // Vertical fall speed (positive) at which landing counts as "hard" and triggers shake / strong squash.
        [SerializeField] private float _hardLandingSpeed = 12f;
        [SerializeField] private float _landShakeIntensity = 0.15f;
        [SerializeField] private float _landShakeDuration = 0.12f;

        [Header("Hit-stop (visual only)")]
        [SerializeField] private float _deathFlashDuration = 0.2f;
        [SerializeField] private float _buffFlashDuration = 0.1f;
        [SerializeField] private Color _deathFlashColor = new Color(1f, 0.25f, 0.25f, 0.6f);
        [SerializeField] private Color _buffFlashColor = new Color(1f, 0.95f, 0.4f, 0.45f);

        [Inject] private readonly IObjectResolver _objectResolver;

        private GameFeelController _gameFeel;
        private Vector3 _baseScale = Vector3.one;
        private Coroutine _squashRoutine;
        private Coroutine _hitStopRoutine;
        private bool _isLocal;

        private void OnEnable()
        {
            this.InjectToMe();

            if (_visualsTransform != null)
                _baseScale = _visualsTransform.localScale;

            if (_movement != null)
            {
                _movement.JumpFx += OnJump;
                _movement.LandFx += OnLand;
                _movement.DeathFx += OnDeath;
            }

            if (_speedBuffs != null)
                _speedBuffs.BuffAddedFx += OnBuffAdded;
        }

        private void OnDisable()
        {
            if (_movement != null)
            {
                _movement.JumpFx -= OnJump;
                _movement.LandFx -= OnLand;
                _movement.DeathFx -= OnDeath;
            }

            if (_speedBuffs != null)
                _speedBuffs.BuffAddedFx -= OnBuffAdded;

            if (_squashRoutine != null)
            {
                StopCoroutine(_squashRoutine);
                _squashRoutine = null;
            }
            if (_hitStopRoutine != null)
            {
                StopCoroutine(_hitStopRoutine);
                _hitStopRoutine = null;
                if (_animator != null)
                    _animator.speed = 1f;
            }

            if (_visualsTransform != null)
                _visualsTransform.localScale = _baseScale;
        }

        private void Start()
        {
            // Same local-vs-remote split that PlayerVisuals uses. Single-player counts as local.
            bool singlePlayer = _objectResolver.TryResolve(out IConnectionController connection)
                                && connection.ConnectionState == ConnectionState.SinglePlayer;
            _isLocal = singlePlayer
                       || (_movement != null && _movement.NetworkObject != null
                           && _movement.NetworkObject.Owner != null
                           && _movement.NetworkObject.Owner.IsLocalClient);

            _objectResolver.TryResolve(out _gameFeel);
        }

        private void OnJump()
        {
            // Squash & dust feel right for everyone, not just the local owner — they read as
            // animation polish on remote players too. Hit-stop and screen shake stay local-only.
            PlaySquash(_jumpStretch);
            if (_jumpDust != null)
                _jumpDust.Play(true);
        }

        private void OnLand(float impactSpeed)
        {
            PlaySquash(_landSquash);
            if (_landDust != null)
                _landDust.Play(true);

            if (_isLocal && _gameFeel != null && impactSpeed >= _hardLandingSpeed)
            {
                _gameFeel.Shake(_landShakeIntensity, _landShakeDuration);
            }
        }

        private void OnDeath()
        {
            if (!_isLocal)
                return;
            if (_gameFeel != null)
            {
                _gameFeel.Flash(_deathFlashColor, _deathFlashDuration);
                _gameFeel.ShakeMedium();
            }
        }

        private void OnBuffAdded()
        {
            if (_movement == null || !_movement.IsOwner)
                return;
            if (_gameFeel != null)
                _gameFeel.Flash(_buffFlashColor, _buffFlashDuration);
        }

        private void PlaySquash(Vector3 targetScale)
        {
            if (_visualsTransform == null)
                return;

            if (_squashRoutine != null)
                StopCoroutine(_squashRoutine);
            _squashRoutine = StartCoroutine(SquashRoutine(targetScale));
        }

        private IEnumerator SquashRoutine(Vector3 targetScale)
        {
            // Quick snap to the deformed scale, then ease back to base. Total time = _squashDuration.
            // Unscaled so a death-time-stop doesn't freeze the squash mid-animation.
            float half = _squashDuration * 0.5f;
            float elapsed = 0f;

            // Apply the deformation relative to the base scale so character flips (negative X) are preserved.
            Vector3 deformed = new Vector3(
                _baseScale.x * targetScale.x,
                _baseScale.y * targetScale.y,
                _baseScale.z * targetScale.z);

            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(elapsed / half);
                _visualsTransform.localScale = Vector3.Lerp(_baseScale, deformed, k);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(elapsed / half);
                _visualsTransform.localScale = Vector3.Lerp(deformed, _baseScale, k);
                yield return null;
            }

            _visualsTransform.localScale = _baseScale;
            _squashRoutine = null;
        }
    }
}
