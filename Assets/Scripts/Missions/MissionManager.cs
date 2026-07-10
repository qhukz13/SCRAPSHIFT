using SpaceMaintenance.Core;
using SpaceMaintenance.Core.Data;
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
                EventBus.Subscribe<SystemRepairedEvent>(OnSystemRepaired);
                PublishTaskProgress();
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
            Debug.Log($"Task Completed! Total: {TasksCompleted.Value}");
            PublishTaskProgress();
        }

        private void PublishTaskProgress()
        {
            int required = 0;
            if (RoundManager.Instance != null && RoundManager.Instance.GetConfig() != null)
            {
                required = RoundManager.Instance.GetConfig().TasksRequired;
            }

            EventBus.Publish(new TaskProgressUpdatedEvent
            {
                Completed = TasksCompleted.Value,
                Required = required
            });
        }
    }
}
