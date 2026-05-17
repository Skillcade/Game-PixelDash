using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Game.GameFeel
{
    /// <summary>
    /// Central juice service: fullscreen flash and camera shake.
    /// All effects run on unscaled time so they never touch the FishNet simulation
    /// (no Time.timeScale, no input gating). The hit-stop in the plan is purely visual
    /// and is applied on the character via PlayerCharacterJuice; this controller only
    /// handles screen-wide juice.
    /// </summary>
    public class GameFeelController : MonoBehaviour
    {
        [Header("Flash")]
        [SerializeField] private Image _flashImage;

        [Header("Shake")]
        [SerializeField] private CameraShaker _cameraShaker;

        [Header("Defaults")]
        [SerializeField] private float _shakeLight = 0.1f;
        [SerializeField] private float _shakeMedium = 0.25f;
        [SerializeField] private float _shakeStrong = 0.5f;
        [SerializeField] private float _shakeDurationLight = 0.12f;
        [SerializeField] private float _shakeDurationMedium = 0.2f;
        [SerializeField] private float _shakeDurationStrong = 0.35f;

        private Coroutine _flashRoutine;

        private void Awake()
        {
            if (_flashImage != null)
            {
                Color c = _flashImage.color;
                c.a = 0f;
                _flashImage.color = c;
                _flashImage.raycastTarget = false;
                _flashImage.gameObject.SetActive(false);
            }
        }

        public void Flash(Color color, float duration)
        {
            if (_flashImage == null || duration <= 0f)
                return;

            if (_flashRoutine != null)
                StopCoroutine(_flashRoutine);

            _flashRoutine = StartCoroutine(FlashRoutine(color, duration));
        }

        public void ShakeLight() => Shake(_shakeLight, _shakeDurationLight);
        public void ShakeMedium() => Shake(_shakeMedium, _shakeDurationMedium);
        public void ShakeStrong() => Shake(_shakeStrong, _shakeDurationStrong);

        public void Shake(float intensity, float duration)
        {
            if (_cameraShaker == null)
                return;
            _cameraShaker.Shake(intensity, duration);
        }

        private IEnumerator FlashRoutine(Color color, float duration)
        {
            _flashImage.gameObject.SetActive(true);
            float t = 0f;
            while (t < duration)
            {
                float a = 1f - (t / duration);
                Color c = color;
                c.a = a;
                _flashImage.color = c;
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            Color end = color;
            end.a = 0f;
            _flashImage.color = end;
            _flashImage.gameObject.SetActive(false);
            _flashRoutine = null;
        }
    }
}
