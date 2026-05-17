using System.Collections;
using TMPro;
using UnityEngine;

namespace Game.GUI
{
    public class CountdownPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text _timerText;

        [Header("Punch animation")]
        [SerializeField] private float _punchScale = 1.35f;
        [SerializeField] private float _punchDuration = 0.28f;

        [Header("GO! display")]
        [SerializeField] private string _goText = "GO!";
        [SerializeField] private float _goVisibleDuration = 0.6f;

        private int _lastShownSeconds = -1;
        private Coroutine _punchRoutine;
        private Coroutine _goRoutine;

        private void OnEnable()
        {
            _lastShownSeconds = -1;
        }

        private void OnDisable()
        {
            if (_punchRoutine != null)
            {
                StopCoroutine(_punchRoutine);
                _punchRoutine = null;
            }
            if (_goRoutine != null)
            {
                StopCoroutine(_goRoutine);
                _goRoutine = null;
            }
            _timerText.transform.localScale = Vector3.one;
        }

        public void SetTime(float remainingSeconds)
        {
            int seconds = Mathf.CeilToInt(remainingSeconds);
            seconds = Mathf.Clamp(seconds, 0, int.MaxValue);
            if (seconds == _lastShownSeconds)
                return;

            _lastShownSeconds = seconds;

            // 0 reads as "GO!" but the canonical GO! is triggered explicitly via ShowGo() on RunningStartEvent
            // so the start-of-match flash and shake stay in sync with the actual state transition.
            _timerText.text = seconds > 0 ? seconds.ToString() : _goText;
            Punch();
        }

        public void ShowGo()
        {
            _timerText.text = _goText;
            Punch();

            if (_goRoutine != null)
                StopCoroutine(_goRoutine);
            _goRoutine = StartCoroutine(HideAfter(_goVisibleDuration));
        }

        private IEnumerator HideAfter(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            _goRoutine = null;
            gameObject.SetActive(false);
        }

        private void Punch()
        {
            if (_punchRoutine != null)
                StopCoroutine(_punchRoutine);
            _punchRoutine = StartCoroutine(PunchRoutine());
        }

        private IEnumerator PunchRoutine()
        {
            Transform t = _timerText.transform;
            float half = _punchDuration * 0.5f;
            float elapsed = 0f;

            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(elapsed / half);
                float s = Mathf.Lerp(1f, _punchScale, k);
                t.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(elapsed / half);
                float s = Mathf.Lerp(_punchScale, 1f, k);
                t.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            t.localScale = Vector3.one;
            _punchRoutine = null;
        }
    }
}
