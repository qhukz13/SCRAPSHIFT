using Unity.Netcode;
using UnityEngine;

namespace SpaceMaintenance.ShipSystems
{
    public class PowerManager : NetworkBehaviour
    {
        public static PowerManager Instance { get; private set; }

        public NetworkVariable<float> CurrentPower = new NetworkVariable<float>(0f);
        [SerializeField] private PowerConfig _config;

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;
        }

        public void AddPower(float amount)
        {
            if (!IsServer) return;
            CurrentPower.Value += amount;
        }

        public void ConsumePower(float amount)
        {
            if (!IsServer) return;
            CurrentPower.Value = Mathf.Max(0, CurrentPower.Value - amount);
        }

        public bool HasSufficientPower(float amountRequired)
        {
            return CurrentPower.Value >= amountRequired;
        }
    }
}
