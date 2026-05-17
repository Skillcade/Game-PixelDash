using System.Collections;
using SkillcadeSDK.StateMachine;
using TMPro;
using UnityEngine;

namespace Game.GUI
{
    public class FinishedPanel : MonoBehaviour
    {
        public enum FinishPanelMode
        {
            Default,
            SinglePlayer,
            SkillcadeHub
        }

        [SerializeField] private GameObject _defaultWinPanel;
        [SerializeField] private GameObject _singlePlayerWinPanel;
        [SerializeField] private GameObject _skillcadeHubWinPanel;

        [SerializeField] private TMP_Text _technicalWinText;
        [SerializeField] private TMP_Text _winnerText;
        [SerializeField] private TMP_Text[] _userStateTexts;

        [Header("Close-match line")]
        [SerializeField] private TMP_Text _closeMatchText;
        [SerializeField] private string _closeMatchMessage = "You were close!";

        [Header("Confetti (optional)")]
        [SerializeField] private ParticleSystem _winConfetti;

        [Header("Pop-in")]
        [SerializeField] private RectTransform _popTarget;
        [SerializeField] private float _popDuration = 0.35f;
        [SerializeField] private float _popOvershoot = 1.15f;

        private Coroutine _popRoutine;

        private void OnEnable()
        {
            if (_closeMatchText != null)
                _closeMatchText.gameObject.SetActive(false);

            PlayPopIn();
        }

        private void OnDisable()
        {
            if (_popRoutine != null)
            {
                StopCoroutine(_popRoutine);
                _popRoutine = null;
            }
            if (_popTarget != null)
                _popTarget.localScale = Vector3.one;
        }

        public void SetMode(FinishPanelMode mode)
        {
            _defaultWinPanel.SetActive(mode == FinishPanelMode.Default);
            _singlePlayerWinPanel.SetActive(mode == FinishPanelMode.SinglePlayer);
            _skillcadeHubWinPanel.SetActive(mode == FinishPanelMode.SkillcadeHub);
        }

        public void SetWinner(string winnerName, FinishReason reason)
        {
            _winnerText.text = winnerName;
            _technicalWinText.gameObject.SetActive(reason == FinishReason.TechnicalWin || reason == FinishReason.Draw);
            if (reason == FinishReason.Draw)
                _technicalWinText.text = "Draw!";
        }

        public void SetUserState(bool won)
        {
            foreach (var userStateText in _userStateTexts)
            {
                userStateText.text = won ? "You won!" : "You lost!";
            }

            if (won && _winConfetti != null)
                _winConfetti.Play(true);
        }

        public void SetDraw()
        {
            foreach (var userStateText in _userStateTexts)
            {
                userStateText.text = "Draw!";
            }
        }

        /// <summary>
        /// Show a "you were close" line when the local player lost by a small margin.
        /// gapAbs is the absolute progress-bar distance between local and the leading opponent (0..1).
        /// </summary>
        public void ShowCloseMatch(float gapAbs, float threshold = 0.1f)
        {
            if (_closeMatchText == null)
                return;
            bool show = gapAbs > 0f && gapAbs < threshold;
            _closeMatchText.gameObject.SetActive(show);
            if (show)
                _closeMatchText.text = _closeMatchMessage;
        }

        private void PlayPopIn()
        {
            if (_popTarget == null)
                return;
            if (_popRoutine != null)
                StopCoroutine(_popRoutine);
            _popRoutine = StartCoroutine(PopInRoutine());
        }

        private IEnumerator PopInRoutine()
        {
            // Ease-out overshoot: 0 → overshoot → 1. Unscaled so the post-game UI reads the same
            // even if some upstream code experiments with timeScale.
            float t = 0f;
            float halfway = _popDuration * 0.7f;

            while (t < halfway)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / halfway);
                float s = Mathf.Lerp(0f, _popOvershoot, EaseOutCubic(k));
                _popTarget.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            float settle = _popDuration - halfway;
            float st = 0f;
            while (st < settle)
            {
                st += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(st / settle);
                float s = Mathf.Lerp(_popOvershoot, 1f, k);
                _popTarget.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            _popTarget.localScale = Vector3.one;
            _popRoutine = null;
        }

        private static float EaseOutCubic(float t)
        {
            float inv = 1f - t;
            return 1f - inv * inv * inv;
        }
    }
}
