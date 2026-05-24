using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

namespace Game.RollbackDebug
{
    /// <summary>
    /// Attach to any GameObject whose world position should be compared
    /// between client-recorded and server-rolled-back values.
    /// </summary>
    public class RollbackDebugTarget : NetworkBehaviour
    {
        [SerializeField] public bool Debug;
        
        public static readonly List<RollbackDebugTarget> All = new();

        private void OnEnable()  => All.Add(this);
        private void OnDisable() => All.Remove(this);
    }
}
