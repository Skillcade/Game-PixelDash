using UnityEngine;

namespace Game
{
    public class RigidbodyEnabler : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D _rigidbody;

        private void FixedUpdate()
        {
            _rigidbody.simulated = true;
        }
    }
}