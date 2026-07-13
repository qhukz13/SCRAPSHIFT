// ============================================================================
// SCRAPSHIFT — EconomyManager.cs
// Manages the shared company funds across the entire session (persistent).
// Lives in a persistent GameObject (DontDestroyOnLoad) so it survives scene
// transitions between Hub and Mission.
// ============================================================================

using SpaceMaintenance.Core.Data;
using Unity.Netcode;
using UnityEngine;

namespace SpaceMaintenance.Core
{
    public class EconomyManager : NetworkBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        // ─── Networked State ────────────────────────────────────────────
        
        /// <summary>Shared company funds for the crew.</summary>
        public NetworkVariable<int> CompanyFunds = new NetworkVariable<int>(0);

        // =================================================================
        //  LIFECYCLE
        // =================================================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            CompanyFunds.OnValueChanged += OnFundsChanged;

            // Trigger initial event for late joiners
            if (CompanyFunds.Value > 0)
            {
                OnFundsChanged(0, CompanyFunds.Value);
            }
        }

        public override void OnNetworkDespawn()
        {
            CompanyFunds.OnValueChanged -= OnFundsChanged;
        }

        // =================================================================
        //  PUBLIC API (Server Only)
        // =================================================================

        /// <summary>Add funds to the shared company account. Server only.</summary>
        public void AddFunds(int amount)
        {
            if (!IsServer || amount <= 0) return;
            CompanyFunds.Value += amount;
            Debug.Log($"[Economy] Added ${amount}. Total: ${CompanyFunds.Value}");
        }

        /// <summary>Attempt to spend funds. Returns true if successful. Server only.</summary>
        public bool TrySpendFunds(int amount)
        {
            if (!IsServer || amount <= 0) return false;
            
            if (CompanyFunds.Value >= amount)
            {
                CompanyFunds.Value -= amount;
                Debug.Log($"[Economy] Spent ${amount}. Remaining: ${CompanyFunds.Value}");
                return true;
            }
            
            Debug.Log($"[Economy] Insufficient funds to spend ${amount}. Current: ${CompanyFunds.Value}");
            return false;
        }

        // =================================================================
        //  CLIENT CALLBACK
        // =================================================================

        private void OnFundsChanged(int previousValue, int newValue)
        {
            EventBus.Publish(new FundsChangedEvent
            {
                OldAmount = previousValue,
                NewAmount = newValue
            });
        }
    }
}
