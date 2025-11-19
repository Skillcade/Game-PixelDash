using UnityEngine;

namespace Game.GUI
{
    public class GameUi : MonoBehaviour
    {
        [SerializeField] public WaitForPlayersPanel WaitForPlayersPanel;
        [SerializeField] public CountdownPanel CountdownPanel;
        [SerializeField] public GameObject RunningPanel;
        [SerializeField] public FinishedPanel FinishedPanel;

        private void Start()
        {
            WaitForPlayersPanel.gameObject.SetActive(false);
            CountdownPanel.gameObject.SetActive(false);
            RunningPanel.gameObject.SetActive(false);
            FinishedPanel.gameObject.SetActive(false);
        }
    }
}