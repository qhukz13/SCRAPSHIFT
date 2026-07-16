using SpaceMaintenance.Core;
using SpaceMaintenance.Core.Data;
using Unity.Netcode;
using UnityEngine;

namespace SpaceMaintenance.Damage
{
    public class DamageManager : NetworkBehaviour
    {
        public static DamageManager Instance { get; private set; }

        public NetworkVariable<float> HullIntegrity = new NetworkVariable<float>(100f);
        public NetworkVariable<float> MaxHullIntegrity = new NetworkVariable<float>(100f);

        public bool IsInvincible { get; set; } = false;

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                HullIntegrity.Value = MaxHullIntegrity.Value;
                PublishHullUpdate();
            }
        }

        public void TakeDamage(float amount)
        {
            if (!IsServer) return;
            if (IsInvincible) return;
            
            HullIntegrity.Value = Mathf.Max(0, HullIntegrity.Value - amount);
            PublishHullUpdate();
            
            if (HullIntegrity.Value <= 0)
            {
                EventBus.Publish(new ChaosEventTriggered { EventName = "Hull Breach - Game Over" });
            }
        }

        public void RepairHull(float amount)
        {
            if (!IsServer) return;
            HullIntegrity.Value = Mathf.Min(MaxHullIntegrity.Value, HullIntegrity.Value + amount);
            PublishHullUpdate();
        }

        private void PublishHullUpdate()
        {
            EventBus.Publish(new HullIntegrityUpdatedEvent
            {
                Current = HullIntegrity.Value,
                Max = MaxHullIntegrity.Value
            });
        }
    }
}
