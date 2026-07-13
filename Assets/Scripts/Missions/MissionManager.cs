// ============================================================================
// SCRAPSHIFT — MissionManager.cs
// Lightweight mission coordinator. Delegates task tracking to TaskManager.
// Retained for backward compatibility and as a central reference point.
// ============================================================================

using SpaceMaintenance.Core;
using SpaceMaintenance.Core.Data;
using Unity.Netcode;
using UnityEngine;

namespace SpaceMaintenance.Missions
{
    public class MissionManager : NetworkBehaviour
    {
        public static MissionManager Instance { get; private set; }

        /// <summary>Legacy field — now mirrors TaskManager's completed count.</summary>
        public NetworkVariable<int> TasksCompleted = new NetworkVariable<int>(0);

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                TasksCompleted.Value = 0;
                EventBus.Subscribe<SystemRepairedEvent>(OnSystemRepaired);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                EventBus.Unsubscribe<SystemRepairedEvent>(OnSystemRepaired);
            }
        }

        private void OnSystemRepaired(SystemRepairedEvent evt)
        {
            if (!IsServer) return;
            
            TasksCompleted.Value++;
            Debug.Log($"[MissionManager] System repaired: {evt.SystemName} (Total: {TasksCompleted.Value})");
        }
    }
}
