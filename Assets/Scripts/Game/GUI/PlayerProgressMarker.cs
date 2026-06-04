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

        private RectTransform _rectTransform;
        private Role _role = Role.Neutral;
        private State _state = State.Neutral;
        private Color _targetColor;

        private void Awake()
        {
            EnsureRect();
            _targetColor  = _neutralColor;
        }

        private void EnsureRect()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();
        }

        public void Initialize(string nickname, Sprite icon)
        {
            EnsureRect();
            if (_nameText != null)
                _nameText.text = nickname;
            if (_icon == null)
            {
                UnityEngine.Debug.LogWarning("[PlayerProgressMarker] _icon is not assigned on this prefab instance.", this);
                return;
            }
            if (icon != null)
                _icon.sprite = icon;
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
    }
}
