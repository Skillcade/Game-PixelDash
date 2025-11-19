using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;
using VContainer;

namespace DefaultNamespace.Collectables
{
    public class RespawnService
    {
        [Inject] private IReadOnlyList<ISpawnService> _spawnServices;
        
        [Server]
        public void RespawnAll()
        {
            foreach (var spawner in _spawnServices)
            {
                spawner.Respawn();
            }   
        }
    }
}