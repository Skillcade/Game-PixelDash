using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.GUI
{
    public class WaitForPlayersPanel : MonoBehaviour
    {
        public event Action<bool> OnReadyStateChanged;
        
        [SerializeField] private TMP_Text _othersReadyStateText;
        [SerializeField] private Button _readyButton;
        [SerializeField] private TMP_Text _readyButtonText;

        private bool _isReady;
        
        private void Start()
        {
            _isReady = false;
            _readyButton.onClick.AddListener(OnReadyClick);
        }

        public void SetReadyState(int ready, int notReady, bool selfReady)
        {
            _isReady = selfReady;
            _readyButtonText.text = selfReady ? "Not Ready" : "Ready";
            _othersReadyStateText.text = $"{ready.ToString()} / {notReady.ToString()}";
        }

        private void OnReadyClick()
        {
            _isReady = !_isReady;
            OnReadyStateChanged?.Invoke(_isReady);
        }
    }
}