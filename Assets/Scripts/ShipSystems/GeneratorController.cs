using SpaceMaintenance.Core;
using Unity.Netcode;
using UnityEngine;

namespace SpaceMaintenance.ShipSystems
{
    public class GeneratorController : NetworkBehaviour, IRepairable
    {
        [SerializeField] private PowerConfig _config;
        
        public float RepairTime => 5f;
        
        public NetworkVariable<float> NetworkRepairProgress = new NetworkVariable<float>(0f);
        public float RepairProgress => NetworkRepairProgress.Value;
        
        public bool IsBeingRepaired { get; private set; }
        
        public NetworkVariable<bool> NetworkNeedsRepair = new NetworkVariable<bool>(false);
        public bool NeedsRepair => NetworkNeedsRepair.Value;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                NetworkNeedsRepair.Value = true; // start broken for testing
                NetworkRepairProgress.Value = 0f;
            }
        }

        public void StartRepair(GameObject repairer)
        {
            if (!IsServer || !NeedsRepair) return;
            IsBeingRepaired = true;
        }

        public void UpdateRepair(float deltaTime)
        {
            if (!IsServer || !IsBeingRepaired || !NeedsRepair) return;
            
            NetworkRepairProgress.Value += deltaTime / RepairTime;
            if (NetworkRepairProgress.Value >= 1f)
            {
                CompleteRepair();
            }
        }

        public void CancelRepair()
        {
            if (!IsServer) return;
            IsBeingRepaired = false;
        }

        public void Break()
        {
            if (!IsServer || NeedsRepair) return;
            NetworkNeedsRepair.Value = true;
            NetworkRepairProgress.Value = 0f;
            IsBeingRepaired = false;
            
            if (PowerManager.Instance != null && _config != null)
            {
                PowerManager.Instance.ConsumePower(_config.GeneratorPowerOutput);
            }
        }

        public void CompleteRepair()
        {
            if (!IsServer || !NeedsRepair) return;
            
            IsBeingRepaired = false;
            NetworkNeedsRepair.Value = false;
            NetworkRepairProgress.Value = 1f;
            
            if (PowerManager.Instance != null && _config != null)
            {
                PowerManager.Instance.AddPower(_config.GeneratorPowerOutput);
            }
            EventBus.Publish(new SpaceMaintenance.Core.Data.SystemRepairedEvent { SystemName = "Backup Generator" });
        }
    }
}
