using SpaceMaintenance.Core;
using Unity.Netcode;
using UnityEngine;

namespace SpaceMaintenance.Missions
{
    public class MissionManager : NetworkBehaviour
    {
        public static MissionManager Instance { get; private set; }

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
                EventBus.Subscribe<SpaceMaintenance.Core.Data.SystemRepairedEvent>(OnSystemRepaired);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                EventBus.Unsubscribe<SpaceMaintenance.Core.Data.SystemRepairedEvent>(OnSystemRepaired);
            }
        }

        private void OnSystemRepaired(SpaceMaintenance.Core.Data.SystemRepairedEvent evt)
        {
            if (!IsServer) return;
            
            TasksCompleted.Value++;
            Debug.Log($"Task Completed! Total: {TasksCompleted.Value}");
        }
    }
}
