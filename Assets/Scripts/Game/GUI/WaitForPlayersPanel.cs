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
        [SerializeField] private GameObject _readyButtonPanel;
        [SerializeField] private GameObject _waitForOthersPanel;

        private bool _isReady;
        
        private void Awake()
        {
            _isReady = false;
            _readyButton.onClick.AddListener(OnReadyClick);
        }

        public void SetReadyState(int ready, int total, bool selfReady)
        {
            _isReady = selfReady;
            _readyButtonText.text = selfReady ? "Not Ready" : "Ready";
            _othersReadyStateText.text = $"{ready.ToString()} / {total.ToString()}";
        }

        public void SetWaitForOthersState(bool value)
        {
            _readyButtonPanel.SetActive(!value);
            _waitForOthersPanel.SetActive(value);
        }

        private void OnReadyClick()
        {
            UnityEngine.Debug.Log("[WaitForPlayersPanel] Ready click");
            _isReady = !_isReady;
            OnReadyStateChanged?.Invoke(_isReady);
        }
    }
}