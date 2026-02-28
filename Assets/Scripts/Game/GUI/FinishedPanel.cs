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

        public void SetMode(FinishPanelMode mode)
        {
            _defaultWinPanel.SetActive(mode == FinishPanelMode.Default);
            _singlePlayerWinPanel.SetActive(mode == FinishPanelMode.SinglePlayer);
            _skillcadeHubWinPanel.SetActive(mode == FinishPanelMode.SkillcadeHub);
        }

        public void SetWinner(string winnerName, FinishReason reason)
        {
            _winnerText.text = winnerName;
            _technicalWinText.gameObject.SetActive(reason == FinishReason.TechnicalWin);
        }

        public void SetUserState(bool state)
        {
            foreach (var userStateText in _userStateTexts)
            {
                userStateText.text = state ? "You won!" : "You lost!";
            }
        }
    }
}