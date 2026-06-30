using FishNet.Object;
using FishNet.Object.Synchronizing;
using Game.Level;
using Game.RigidbodyInterpolation;
using UnityEngine;

namespace Game.Player
{
    public class PlayerPlatformAttacher : NetworkBehaviour
    {
        [SerializeField] private NetworkRigidbody2DInterpolator _rigidbodyInterpolator;
        [SerializeField] private PlayerMovement _playerMovement;
        [SerializeField] private Platform _candidatePlatform;
        [SerializeField] private Platform _attachedToPlatform;
        [SerializeField] private Transform _playerVisuals;

        private readonly SyncVar<int> _attachedToPlatformObjectId = new SyncVar<int>(new SyncTypeSettings(WritePermission.ServerOnly));

        private void Awake()
        {
            ResolvePlayerMovement();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            _attachedToPlatformObjectId.OnChange += OnAttachedToPlatformChanged;
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            _attachedToPlatformObjectId.OnChange -= OnAttachedToPlatformChanged;
        }

        private void OnAttachedToPlatformChanged(int prev, int next, bool asServer)
        {
            if (!IsOwner)
                return;

            if (next <= 0)
            {
                _attachedToPlatform = null;
                return;
            }

            if (!NetworkManager.ClientManager.Objects.Spawned.TryGetValue(next, out var platformNetworkObject))
                return;
            
            if (!platformNetworkObject.TryGetComponent(out Platform platform))
                return;
            
            _attachedToPlatform = platform;
        }

        // private void Update()
        // {
        //     if (!IsOwner)
        //         return;
        //     
        //     var hasPlatform = _attachedToPlatform != null;
        //     _rigidbodyInterpolator.IgnoreY = hasPlatform;
        //
        //     if (!hasPlatform)
        //         return;
        //     
        //     var position = _playerVisuals.position;
        //     position.y = _attachedToPlatform.transform.position.y + _attachedToPlatform.halfOffset;
        //     _playerVisuals.position = position;
        // }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsOwner)
                return;

            var trigger = other.GetComponent<PlatformTrigger>();
            if (trigger == null || trigger.Target == null)
                return;

            var platform = trigger.Target;
            if (_attachedToPlatform != null)
                return;

            _candidatePlatform = platform;
            TryAttachToGroundedCandidate();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsOwner)
                return;

            var trigger = other.GetComponent<PlatformTrigger>();
            if (trigger == null || trigger.Target == null)
                return;

            var platform = trigger.Target;
            if (_candidatePlatform == platform)
                _candidatePlatform = null;

            if (_attachedToPlatform == platform)
                DetachFromPlatform();
        }

        private void LateUpdate()
        {
            if (!IsOwner)
                return;

            TryAttachToGroundedCandidate();

            if (_rigidbodyInterpolator != null && _attachedToPlatform != null)
                _rigidbodyInterpolator.Ignore = true;
        }

        private void TryAttachToGroundedCandidate()
        {
            if (!CanAttachToCandidate())
                return;

            AttachToPlatform(_candidatePlatform);
        }

        private bool CanAttachToCandidate()
        {
            ResolvePlayerMovement();
            return _attachedToPlatform == null &&
                   _candidatePlatform != null &&
                   _playerMovement != null &&
                   _playerMovement.GroundedPlatform == _candidatePlatform;
        }

        private void AttachToPlatform(Platform platform)
        {
            _attachedToPlatform = platform;
            NetworkObject.SetParent(platform);

            SetAttachedToPlatformObjectID(platform.NetworkObject.ObjectId);

            if (_rigidbodyInterpolator != null)
                _rigidbodyInterpolator.Ignore = true;
        }

        private void DetachFromPlatform()
        {
            _attachedToPlatform = null;
            NetworkObject.UnsetParent();

            SetAttachedToPlatformObjectID(-1);

            if (_rigidbodyInterpolator != null)
                _rigidbodyInterpolator.Ignore = false;
        }

        private void ResolvePlayerMovement()
        {
            if (_playerMovement == null)
                _playerMovement = GetComponent<PlayerMovement>();
        }

        [ServerRpc]
        private void SetAttachedToPlatformObjectID(int value)
        {
            _attachedToPlatformObjectId.Value = value;
        }
    }
}
