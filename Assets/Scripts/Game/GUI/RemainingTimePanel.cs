using SkillcadeSDK;
using SkillcadeSDK.FishNetAdapter.StateMachine.Events;
using TMPro;
using UnityEngine;

namespace Game.GUI
{
    public class RemainingTimePanel : MonoBehaviour
    {
        [SerializeField] private GameObject _container;
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private float _warningThreshold = 30f;
        [SerializeField] private Color _warningColor = Color.yellow;
        [SerializeField] private float _criticalThreshold = 10f;
        [SerializeField] private Color _criticalColor = Color.red;
        [SerializeField] private float _pulseSpeed = 3f;
        [SerializeField] private float _pulseAmount = 0.2f;

        private Color _normalColor;
        private Vector3 _baseScale;
        private int _lastShownSeconds;

        private void Awake()
        {
            _normalColor = _timerText.color;
            _baseScale = _timerText.transform.localScale;
        }

        private void Update()
        {
            if (_lastShownSeconds <= _criticalThreshold)
            {
                _timerText.color = _criticalColor;
                float pulse = 1f + Mathf.Sin(Time.unscaledTime * _pulseSpeed) * _pulseAmount;
                _timerText.transform.localScale = _baseScale * pulse;
                _container.SetActive(true);
            }
            else if (_lastShownSeconds <= _warningThreshold)
            {
                _timerText.color = _warningColor;
                _timerText.transform.localScale = _baseScale;
                _container.SetActive(true);
            }
            else
            {
                _timerText.color = _normalColor;
                _timerText.transform.localScale = _baseScale;
                _container.SetActive(false);
            }
        }

        public void UpdateTimer(RunningTimerTickEvent evt)
        {
            int remaining = evt.RemainingSeconds;
            if (remaining == _lastShownSeconds)
                return;

            _lastShownSeconds = remaining;
            _timerText.text = ((float)remaining).SecondsToTimeString();
        }

        public void Disable()
        {
            _container.SetActive(false);
        }
    }
}
