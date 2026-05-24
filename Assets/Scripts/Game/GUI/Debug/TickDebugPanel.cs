using FishNet.Managing;
using FishNet.Managing.Timing;
using TMPro;
using UnityEngine;
using VContainer;

namespace Game.GUI.Debug
{
    public class TickDebugPanel : MonoBehaviour
    {
        [SerializeField] private bool _usePreciseTick;
        [SerializeField] private TMP_Text _tickText;
        [SerializeField] private TMP_Text _localTickText;
        [SerializeField] private TMP_Text _lastPacketTickText;

        [Inject] private readonly NetworkManager _networkManager;

        private void Update()
        {
            if (_networkManager.TimeManager == null)
                return;
            
            if (!_networkManager.IsClientStarted && !_networkManager.IsServerStarted)
                return;

            if (_usePreciseTick)
            {
                _tickText.text = _networkManager.TimeManager.GetPreciseTick(TickType.Tick).Tick.ToString();
                _localTickText.text = _networkManager.TimeManager.GetPreciseTick(TickType.LocalTick).Tick.ToString();
                _lastPacketTickText.text = _networkManager.TimeManager.GetPreciseTick(TickType.LastPacketTick).Tick.ToString();
            }
            else
            {
                _tickText.text = _networkManager.TimeManager.Tick.ToString();
                _localTickText.text = _networkManager.TimeManager.LocalTick.ToString();
                _lastPacketTickText.text = _networkManager.TimeManager.LastPacketTick.RemoteTick.ToString();
            }
        }
    }
}