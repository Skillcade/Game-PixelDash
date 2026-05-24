using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.GUI
{
    /// <summary>
    /// Visual marker on the race progress bar.
    /// Holds three colour states (neutral, close, behind) so PlayerProgressLine can
    /// drive the "drama" of the bar without owning UI details.
    /// </summary>
    public class PlayerProgressMarker : MonoBehaviour
    {
        public enum Role
        {
            Neutral,
            Local,
            Opponent
        }

        public enum State
        {
            Neutral,
            Close,
            Behind,
            Ahead
        }

        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameText;

        [Header("State colours")]
        [SerializeField] private Color _neutralColor = Color.white;
        [SerializeField] private Color _closeColor = new Color(1f, 0.85f, 0.3f);
        [SerializeField] private Color _behindColor = new Color(1f, 0.4f, 0.4f);
        [SerializeField] private Color _aheadColor = new Color(1f, 0.85f, 0.3f);

        [Header("Pulse")]
        [SerializeField] private float _pulseAmount = 0.12f;
        [SerializeField] private float _pulseFrequency = 4.5f;
        [SerializeField] private float _colorLerpSpeed = 8f;

        private RectTransform _rectTransform;
        private Role _role = Role.Neutral;
        private State _state = State.Neutral;
        private bool _pulse;
        private Color _targetColor;
        private Color _currentColor;

        private void Awake()
        {
            EnsureRect();
            _targetColor = _neutralColor;
            _currentColor = _neutralColor;
            ApplyColor(_currentColor);
        }

        private void EnsureRect()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();
        }

        public void Initialize(string nickname, Sprite icon)
        {
            EnsureRect();
            _nameText.text = nickname;
            if (icon != null)
            {
                _icon.sprite = icon;
            }
        }

        public void SetProgress(float t)
        {
            EnsureRect();
            _rectTransform.anchorMin = new Vector2(t, 0.5f);
            _rectTransform.anchorMax = new Vector2(t, 0.5f);
            _rectTransform.anchoredPosition = Vector2.zero;
        }

        public void SetRole(Role role)
        {
            _role = role;
            // Opponent marker pulses by default — even when no contest is happening it
            // signals "another runner is in the race" without any extra UI element.
            _pulse = role == Role.Opponent;
            RefreshTargetColor();
        }

        public void SetState(State state)
        {
            if (_state == state)
                return;
            _state = state;
            RefreshTargetColor();
        }

        private void RefreshTargetColor()
        {
            switch (_state)
            {
                case State.Close:
                    _targetColor = _closeColor;
                    break;
                case State.Behind:
                    _targetColor = _behindColor;
                    break;
                case State.Ahead:
                    _targetColor = _aheadColor;
                    break;
                default:
                    _targetColor = _neutralColor;
                    break;
            }

            // Local marker keeps the warning colours intense; opponent stays a softer
            // accent so the player's own state always reads first.
            if (_role == Role.Opponent && _state != State.Neutral)
            {
                _targetColor = Color.Lerp(_neutralColor, _targetColor, 0.6f);
            }
        }

        private void Update()
        {
            _currentColor = Color.Lerp(_currentColor, _targetColor, Time.unscaledDeltaTime * _colorLerpSpeed);
            ApplyColor(_currentColor);

            float scale = 1f;
            if (_pulse || _state == State.Close)
            {
                scale += Mathf.Sin(Time.unscaledTime * _pulseFrequency) * _pulseAmount * 0.5f;
            }
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void ApplyColor(Color c)
        {
            if (_icon != null)
                _icon.color = c;
        }
    }
}
