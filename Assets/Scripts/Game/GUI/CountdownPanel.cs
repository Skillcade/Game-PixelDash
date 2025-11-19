using TMPro;
using UnityEngine;

namespace Game.GUI
{
    public class CountdownPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text _timerText;

        private int _lastShownSeconds;
        
        public void SetTime(float remainingSeconds)
        {
            int seconds = Mathf.CeilToInt(remainingSeconds);
            if (seconds == _lastShownSeconds)
                return;

            _lastShownSeconds = seconds;
            _timerText.text = $"{seconds.ToString()}s";
        }
    }
}