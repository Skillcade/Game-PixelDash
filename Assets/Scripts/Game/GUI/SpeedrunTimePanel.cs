using FishNet.Managing;
using SkillcadeSDK;
using TMPro;
using UnityEngine;
using VContainer;

namespace Game.GUI
{
    public class SpeedrunTimePanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text _timeText;
        
        [Inject] private readonly NetworkManager _networkManager;
        
        private double Time => _networkManager.TimeManager.TicksToTime(_networkManager.TimeManager.Tick);

        private double _startTime;
        private bool _started;

        private int _lastShownSeconds;

        public void StartSpeedrunTime()
        {
            this.InjectToMe();
            _startTime = Time;
            _started = true;
            SetTime(0);
        }

        public void StopSpeedrunTime()
        {
            _started = false;
        }

        private void Update()
        {
            if (!_started)
                return;
            
            var time = Time;
            var passedTime = time - _startTime;
            var seconds = Mathf.RoundToInt((float)passedTime);
            if (seconds != _lastShownSeconds)
                SetTime(seconds);
        }

        private void SetTime(int seconds)
        {
            _lastShownSeconds = seconds;
            _timeText.text = ((float)seconds).SecondsToTimeString();
        }
    }
}