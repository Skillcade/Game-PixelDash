using FishNet.Object;
using UnityEngine;

namespace Game
{
    public class TestMessagingManager : NetworkBehaviour
    {
        [Server]
        public void SendMessageToClients(string message)
        {
            SendMessageClientRpc(message);
        }
        
        [ObserversRpc]
        private void SendMessageClientRpc(string message)
        {
            Debug.Log($"Received message from server: {message}");
        }
    }
}