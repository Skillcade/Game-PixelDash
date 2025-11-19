using Game.StateMachine.States;
using SkillcadeSDK.FishNetAdapter.StateMachine.States;
using TMPro;
using UnityEngine;

namespace Game.GUI
{
    public class FinishedPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text _technicalWinText;
        [SerializeField] private TMP_Text _winnerText;
        [SerializeField] private TMP_Text _userStateText;

        public void SetWinner(string winnerName, FinishReason reason)
        {
            _winnerText.text = winnerName;
            _technicalWinText.gameObject.SetActive(reason == FinishReason.TechnicalWin);
        }

        public void SetUserState(bool state)
        {
            _userStateText.text = state ? "You won!" : "You lost!";
        }
    }
}