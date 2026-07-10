using SpaceMaintenance.Core;
using Unity.Netcode;
using UnityEngine;

namespace SpaceMaintenance.ShipSystems
{
    public class ReactorController : NetworkBehaviour, IRepairable
    {
        [SerializeField] private PowerConfig _config;
        
        public float RepairTime => 5f;
        
        public NetworkVariable<float> NetworkRepairProgress = new NetworkVariable<float>(0f);
        public float RepairProgress => NetworkRepairProgress.Value;
        
        public bool IsBeingRepaired { get; private set; }
        // Can be repaired if hot, and stays repairable until fully cooled if already being repaired
        public bool NeedsRepair => (HeatLevel.Value > 0.4f || (HeatLevel.Value > 0f && IsBeingRepaired)) && !_isMeltdown;

        public NetworkVariable<float> HeatLevel = new NetworkVariable<float>(0f); // 0 to 1
        
        private float _heatGenerationRate = 0.015f; // 1.5% per second
        private bool _isMeltdown = false;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                HeatLevel.Value = 0f;
                if (PowerManager.Instance != null && _config != null)
                {
                    PowerManager.Instance.AddPower(_config.MaxReactorPower);
                }
            }
        }

        private void Update()
        {
            if (!IsServer || _isMeltdown) return;

            if (!IsBeingRepaired)
            {
                HeatLevel.Value += _heatGenerationRate * Time.deltaTime;
                
                if (HeatLevel.Value >= 1f)
                {
                    HeatLevel.Value = 1f;
                    TriggerMeltdown();
                }
            }
        }

        private void TriggerMeltdown()
        {
            if (_isMeltdown) return;
            _isMeltdown = true;
            
            if (PowerManager.Instance != null && _config != null)
            {
                PowerManager.Instance.ConsumePower(_config.MaxReactorPower);
            }
            EventBus.Publish(new SpaceMaintenance.Core.Data.ChaosEventTriggered { EventName = "Reactor Meltdown" });
            
            if (SpaceMaintenance.Damage.DamageManager.Instance != null)
            {
                SpaceMaintenance.Damage.DamageManager.Instance.TakeDamage(100f); // Instant game over
            }
        }

        public void SurgeHeat()
        {
            if (!IsServer || _isMeltdown) return;
            HeatLevel.Value = Mathf.Min(1f, HeatLevel.Value + 0.4f);
            if (HeatLevel.Value >= 1f) TriggerMeltdown();
        }

        public void StartRepair(GameObject repairer)
        {
            if (!IsServer || !NeedsRepair || _isMeltdown) return;
            IsBeingRepaired = true;
        }

        public void UpdateRepair(float deltaTime)
        {
            if (!IsServer || !IsBeingRepaired || _isMeltdown) return;
            
            NetworkRepairProgress.Value += deltaTime / RepairTime;
            HeatLevel.Value = Mathf.Max(0f, HeatLevel.Value - (0.2f * deltaTime)); // Cool down

            if (HeatLevel.Value <= 0f)
            {
                CompleteRepair();
            }
        }

        public void CancelRepair()
        {
            if (!IsServer) return;
            IsBeingRepaired = false;
            NetworkRepairProgress.Value = 0f;
        }

        public void CompleteRepair()
        {
            if (!IsServer) return;
            IsBeingRepaired = false;
            NetworkRepairProgress.Value = 0f;
            HeatLevel.Value = 0f;
            EventBus.Publish(new SpaceMaintenance.Core.Data.SystemRepairedEvent { SystemName = "Reactor Cooled" });
        }
    }
}
