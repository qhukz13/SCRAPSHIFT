using SpaceMaintenance.Core;
using Unity.Netcode;
using UnityEngine;

namespace SpaceMaintenance.Damage
{
    public class DamageManager : NetworkBehaviour
    {
        public static DamageManager Instance { get; private set; }

        public NetworkVariable<float> HullIntegrity = new NetworkVariable<float>(100f);
        public NetworkVariable<float> MaxHullIntegrity = new NetworkVariable<float>(100f);

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
            }
        }

        public void TakeDamage(float amount)
        {
            if (!IsServer) return;
            
            HullIntegrity.Value = Mathf.Max(0, HullIntegrity.Value - amount);
            
            if (HullIntegrity.Value <= 0)
            {
                EventBus.Publish(new SpaceMaintenance.Core.Data.ChaosEventTriggered { EventName = "Hull Breach - Game Over" });
            }
        }

        public void RepairHull(float amount)
        {
            if (!IsServer) return;
            HullIntegrity.Value = Mathf.Min(MaxHullIntegrity.Value, HullIntegrity.Value + amount);
        }
    }
}
