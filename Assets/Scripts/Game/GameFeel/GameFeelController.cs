using System.Collections;
using Game.GUI;
using SkillcadeSDK;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.GameFeel
{
    /// <summary>
    /// Central juice service: fullscreen flash and camera shake.
    /// All effects run on unscaled time so they never touch the FishNet simulation
    /// (no Time.timeScale, no input gating). The hit-stop in the plan is purely visual
    /// and is applied on the character via PlayerCharacterJuice; this controller only
    /// handles screen-wide juice.
    /// </summary>
    public class GameFeelController : MonoBehaviour, IInitializable
    {
        [Header("Defaults")]
        [SerializeField] private float _shakeLight = 0.1f;
        [SerializeField] private float _shakeMedium = 0.25f;
        [SerializeField] private float _shakeStrong = 0.5f;
        [SerializeField] private float _shakeDurationLight = 0.12f;
        [SerializeField] private float _shakeDurationMedium = 0.2f;
        [SerializeField] private float _shakeDurationStrong = 0.35f;

        [Inject] private readonly GameUi _gameUi;
        [Inject] private readonly CameraShaker _cameraShaker;

        private Coroutine _flashRoutine;

        public void Initialize()
        {
            this.InjectToMe();
            if (_gameUi.FlashImage != null)
            {
                Color c = _gameUi.FlashImage.color;
                c.a = 0f;
                _gameUi.FlashImage.color = c;
                _gameUi.FlashImage.raycastTarget = false;
                _gameUi.FlashImage.gameObject.SetActive(false);
            }
        }

        public void Flash(Color color, float duration)
        {
            if (_gameUi.FlashImage == null || duration <= 0f)
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
            if (_cameraShaker != null)
                _cameraShaker.Shake(intensity, duration);
        }

        private IEnumerator FlashRoutine(Color color, float duration)
        {
            _gameUi.FlashImage.gameObject.SetActive(true);
            float t = 0f;
            while (t < duration)
            {
                float a = 1f - (t / duration);
                Color c = color;
                c.a = a;
                _gameUi.FlashImage.color = c;
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            Color end = color;
            end.a = 0f;
            _gameUi.FlashImage.color = end;
            _gameUi.FlashImage.gameObject.SetActive(false);
            _flashRoutine = null;
        }
    }
}
