// ============================================================================
// Space Maintenance — PowerManager.cs
// Manages the ship's power grid: tracks total supply, registers IPowered
// consumers, distributes power by priority, and publishes state changes.
// ============================================================================

using System.Collections.Generic;
using SpaceMaintenance.Core;
using SpaceMaintenance.Core.Data;
using Unity.Netcode;
using UnityEngine;

namespace SpaceMaintenance.ShipSystems
{
    [RequireComponent(typeof(NetworkObject))]
    public class PowerManager : NetworkBehaviour
    {
        public static PowerManager Instance { get; private set; }

        // ─── Inspector ──────────────────────────────────────────────────
        [SerializeField] private PowerConfig _config;

        // ─── Networked State ────────────────────────────────────────────
        public NetworkVariable<float> CurrentPower =
            new NetworkVariable<float>(0f);

        public NetworkVariable<float> MaxPower =
            new NetworkVariable<float>(0f);

        public NetworkVariable<float> TotalDemand =
            new NetworkVariable<float>(0f);

        // ─── Server-only ────────────────────────────────────────────────
        private readonly List<IPowered> _consumers = new List<IPowered>();
        private bool _dirty = true; // force initial distribution

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
            if (IsServer)
            {
                MaxPower.Value = _config != null ? _config.MaxReactorPower : 1000f;
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            if (_dirty)
            {
                DistributePower();
                _dirty = false;
            }
        }

        // =================================================================
        //  PUBLIC API
        // =================================================================

        /// <summary>Add power to the grid (reactor online, generator repaired).</summary>
        public void AddPower(float amount)
        {
            if (!IsServer) return;
            CurrentPower.Value += amount;
            _dirty = true;
            PublishPowerUpdate();
        }

        /// <summary>Remove power from the grid (reactor SCRAM, generator break).</summary>
        public void ConsumePower(float amount)
        {
            if (!IsServer) return;
            CurrentPower.Value = Mathf.Max(0f, CurrentPower.Value - amount);
            _dirty = true;
            PublishPowerUpdate();
        }

        /// <summary>Instant drain — used by Chaos events.</summary>
        public void DrainPower(float amount)
        {
            ConsumePower(amount);
            Debug.Log($"[Power] Drained {amount} units — remaining {CurrentPower.Value}");
        }

        /// <summary>Check if the grid can satisfy a given demand right now.</summary>
        public bool HasSufficientPower(float amountRequired)
        {
            return CurrentPower.Value >= amountRequired;
        }

        // ─── Consumer Registration ──────────────────────────────────────

        /// <summary>Register an IPowered consumer for priority-based distribution.</summary>
        public void RegisterConsumer(IPowered consumer)
        {
            if (!IsServer) return;
            if (!_consumers.Contains(consumer))
            {
                _consumers.Add(consumer);
                _dirty = true;
            }
        }

        /// <summary>Unregister an IPowered consumer.</summary>
        public void UnregisterConsumer(IPowered consumer)
        {
            if (!IsServer) return;
            if (_consumers.Remove(consumer))
                _dirty = true;
        }

        // =================================================================
        //  POWER DISTRIBUTION (Server only)
        // =================================================================

        /// <summary>
        /// Distribute available power across all consumers sorted by priority.
        /// Lower priority number = more important = powered first.
        /// Consumers that can't be satisfied get their power cut.
        /// </summary>
        private void DistributePower()
        {
            // Sort by priority (lower = more important)
            _consumers.Sort((a, b) => a.PowerPriority.CompareTo(b.PowerPriority));

            float remaining = CurrentPower.Value;
            float totalDemand = 0f;

            foreach (var consumer in _consumers)
            {
                totalDemand += consumer.PowerConsumption;

                if (remaining >= consumer.PowerConsumption)
                {
                    remaining -= consumer.PowerConsumption;
                    if (!consumer.IsPowered)
                        consumer.OnPowerStateChanged(true);
                }
                else
                {
                    if (consumer.IsPowered)
                        consumer.OnPowerStateChanged(false);
                }
            }

            TotalDemand.Value = totalDemand;
        }

        // =================================================================
        //  EVENTS
        // =================================================================

        private void PublishPowerUpdate()
        {
            EventBus.Publish(new PowerStateChangedEvent
            {
                CurrentPower = CurrentPower.Value,
                MaxPower     = MaxPower.Value,
                Demand       = TotalDemand.Value
            });
        }
    }
}
