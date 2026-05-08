using UnityEngine;
using UnityEngine.UI;

namespace Game.GUI
{
    public class GameUi : MonoBehaviour
    {
        [SerializeField] public WaitForPlayersPanel WaitForPlayersPanel;
        [SerializeField] public CountdownPanel CountdownPanel;
        [SerializeField] public GameObject RunningPanel;
        [SerializeField] public GameTimerPanel GameTimerPanel;
        [SerializeField] public FinishedPanel FinishedPanel;
        [SerializeField] public Button StopSinglePlayerButton;

        private void Awake()
        {
            WaitForPlayersPanel.gameObject.SetActive(false);
            CountdownPanel.gameObject.SetActive(false);
            RunningPanel.gameObject.SetActive(false);
            FinishedPanel.gameObject.SetActive(false);
            StopSinglePlayerButton.gameObject.SetActive(false);
        }
    }
}