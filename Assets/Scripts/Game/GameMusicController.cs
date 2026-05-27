using UnityEngine;

namespace Game
{
    /// <summary>
    /// Plays the background music loop during a race.
    /// Called directly by GameUiHandler — no VContainer injection needed.
    /// </summary>
    public class GameMusicController : MonoBehaviour
    {
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip   _musicClip;

        [Header("Volume")]
        [SerializeField] [Range(0f, 1f)] private float _volume = 0.6f;

        public void Play()
        {
            if (_audioSource == null || _musicClip == null) return;

            _audioSource.clip   = _musicClip;
            _audioSource.loop   = true;
            _audioSource.volume = _volume;
            _audioSource.pitch  = 1f;
            _audioSource.Play();
        }

        public void Stop()
        {
            if (_audioSource == null) return;
            _audioSource.Stop();
        }
    }
}
