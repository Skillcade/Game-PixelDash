using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.GUI
{
    public class PlayerProgressMarker : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameText;

        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        public void Initialize(string nickname, Sprite icon)
        {
            _rectTransform = GetComponent<RectTransform>();
            _nameText.text = nickname;
            if (icon != null)
            {
                _icon.sprite = icon;
            }
        }

        public void SetProgress(float t)
        {
            _rectTransform.anchorMin = new Vector2(t, 0.5f);
            _rectTransform.anchorMax = new Vector2(t, 0.5f);
            _rectTransform.anchoredPosition = Vector2.zero;
        }
    }
}
