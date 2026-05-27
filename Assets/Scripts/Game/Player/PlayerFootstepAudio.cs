using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Plays footstep, jump, and land audio for the local player.
    /// Attach to the Player root alongside PlayerMovement.
    /// </summary>
    public class PlayerFootstepAudio : MonoBehaviour
    {
        [SerializeField] private PlayerMovement _movement;
        [SerializeField] private AudioSource _audioSource;

        [Header("Footstep Clips")]
        [SerializeField] private AudioClip _stepClipA;
        [SerializeField] private AudioClip _stepClipB;

        [Header("Jump / Land / Death Clips")]
        [SerializeField] private AudioClip _jumpClip;
        [SerializeField] private AudioClip _landClip;
        [SerializeField] private AudioClip _deathClip;

        [Header("Timing")]
        [Tooltip("Seconds between each footstep at full run speed.")]
        [SerializeField] private float _stepInterval = 0.32f;
        [Tooltip("Horizontal speed (units/s) below which footsteps are silent.")]
        [SerializeField] private float _minSpeedThreshold = 0.5f;

        [Header("Pitch variation")]
        [SerializeField] private float _pitchMin = 0.85f;
        [SerializeField] private float _pitchMax = 1.15f;

        private float _stepTimer;
        private bool _useClipA = true;

        private void OnEnable()
        {
            if (_movement == null) return;
            _movement.JumpFx  += OnJump;
            _movement.LandFx  += OnLand;
            _movement.DeathFx += OnDeath;
        }

        private void OnDisable()
        {
            if (_movement == null) return;
            _movement.JumpFx  -= OnJump;
            _movement.LandFx  -= OnLand;
            _movement.DeathFx -= OnDeath;
        }

        private void Update()
        {
            // Only the owning client plays footstep audio.
            if (_movement == null || !_movement.IsClientInitialized || !_movement.IsOwner)
                return;

            bool grounded = _movement.IsGroundedVisual;
            float speedX  = Mathf.Abs(_movement.VelocityVisual.x);

            if (!grounded || speedX < _minSpeedThreshold)
                return; // Pause timer while airborne or idle — resumes naturally when grounded again.

            _stepTimer -= Time.deltaTime;
            if (_stepTimer <= 0f)
            {
                PlayStep();
                _stepTimer = _stepInterval;
            }
        }

        private void PlayStep()
        {
            if (_audioSource == null) return;

            AudioClip clip = _useClipA ? _stepClipA : _stepClipB;
            _useClipA = !_useClipA;

            if (clip == null) return;

            _audioSource.pitch = Random.Range(_pitchMin, _pitchMax);
            _audioSource.PlayOneShot(clip);
        }

        private void OnJump()
        {
            if (_movement == null || !_movement.IsClientInitialized || !_movement.IsOwner) return;
            if (_audioSource == null || _jumpClip == null) return;

            _audioSource.pitch = Random.Range(_pitchMin, _pitchMax);
            _audioSource.PlayOneShot(_jumpClip);
        }

        private void OnLand(float impact)
        {
            if (_movement == null || !_movement.IsClientInitialized || !_movement.IsOwner) return;
            if (_audioSource == null || _landClip == null) return;

            // Slightly higher pitch on harder landings, still within the overall range.
            float t = Mathf.Clamp01(impact / 20f);
            _audioSource.pitch = Mathf.Lerp(_pitchMin, _pitchMax, t);
            _audioSource.PlayOneShot(_landClip);
        }

        private void OnDeath()
        {
            if (_movement == null || !_movement.IsClientInitialized || !_movement.IsOwner) return;
            if (_audioSource == null || _deathClip == null) return;

            _audioSource.pitch = Random.Range(_pitchMin, _pitchMax);
            _audioSource.PlayOneShot(_deathClip);
        }
    }
}
