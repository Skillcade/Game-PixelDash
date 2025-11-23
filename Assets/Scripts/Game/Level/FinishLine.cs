using System;
using Game.Player;
using UnityEngine;

namespace Game.Level
{
    public class FinishLine : MonoBehaviour
    {
        public event Action<int> OnPlayerReachedFinish;

        private void OnTriggerEnter2D(Collider2D other)
        {
            var movement = other.GetComponent<PlayerMovement>();
            if (movement != null)
                OnPlayerReachedFinish?.Invoke(movement.NetworkObject.OwnerId);
        }
    }
}