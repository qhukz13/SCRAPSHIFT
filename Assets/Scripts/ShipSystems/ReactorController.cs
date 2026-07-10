// ============================================================================
// Space Maintenance — ReactorController.cs
// Full reactor state-machine: Offline → Starting → Running → Overheating →
// Critical → Meltdown.  Implements IRepairable (cooling) and IInteractable
// (SCRAM button / restart).
// ============================================================================

using SpaceMaintenance.Core;
using SpaceMaintenance.Core.Data;
using SpaceMaintenance.Audio;
using Unity.Netcode;
using UnityEngine;

namespace SpaceMaintenance.ShipSystems
{
    [RequireComponent(typeof(NetworkObject))]
    public class ReactorController : NetworkBehaviour, IRepairable, IInteractable
    {
        // ─── Inspector ──────────────────────────────────────────────────
        [Header("Config")]
        [SerializeField] private PowerConfig _config;

        [Header("Visual Hooks (optional)")]
        [Tooltip("Emissive renderer whose color reflects reactor state.")]
        [SerializeField] private Renderer _statusRenderer;
        [SerializeField] private Color _offlineColor   = new Color(0.2f, 0.2f, 0.2f);
        [SerializeField] private Color _startingColor  = new Color(1f, 0.9f, 0.3f);
        [SerializeField] private Color _runningColor   = new Color(0f, 1f, 0.4f);
        [SerializeField] private Color _overheatColor  = new Color(1f, 0.5f, 0f);
        [SerializeField] private Color _criticalColor  = new Color(1f, 0f, 0f);
        [SerializeField] private Color _meltdownColor  = new Color(0.6f, 0f, 0f);

        // ─── Networked State ────────────────────────────────────────────
        public NetworkVariable<ReactorState> State =
            new NetworkVariable<ReactorState>(ReactorState.Offline);

        public NetworkVariable<float> HeatLevel =
            new NetworkVariable<float>(0f);

        public NetworkVariable<float> NetworkRepairProgress =
            new NetworkVariable<float>(0f);

        // ─── Server-only ────────────────────────────────────────────────
        private float _startupTimer;
        private float _scramCooldownTimer;
        private bool  _scramCooldownActive;

        // ─── IRepairable ────────────────────────────────────────────────
        public float RepairTime       => _config != null ? 1f / _config.ReactorCooldownRate : 5f;
        public float RepairProgress   => NetworkRepairProgress.Value;
        public bool  IsBeingRepaired  { get; private set; }
        public bool  NeedsRepair      =>
            (State.Value == ReactorState.Overheating ||
             State.Value == ReactorState.Critical    ||
             (IsBeingRepaired && HeatLevel.Value > 0f));

        // ─── IInteractable ──────────────────────────────────────────────
        public string InteractionPrompt
        {
            get
            {
                switch (State.Value)
                {
                    case ReactorState.Offline:
                        return _scramCooldownActive ? "Reactor Cooling Down…" : "[E] Start Reactor";
                    case ReactorState.Starting:
                        return "Starting Up…";
                    case ReactorState.Meltdown:
                        return "MELTDOWN!";
                    default:
                        return "[E] Emergency SCRAM";
                }
            }
        }
        public bool  RequiresHold => false;
        public float HoldDuration => 0f;

        // =================================================================
        //  LIFECYCLE
        // =================================================================

        public override void OnNetworkSpawn()
        {
            // Register visual callback on all clients
            State.OnValueChanged += OnStateChangedCallback;

            if (IsServer)
            {
                // Reactor starts offline — player must start it
                TransitionTo(ReactorState.Offline);
            }

            // Apply initial visual
            UpdateVisuals(State.Value);
        }

        public override void OnNetworkDespawn()
        {
            State.OnValueChanged -= OnStateChangedCallback;
        }

        private void Update()
        {
            if (!IsServer || _config == null) return;

            switch (State.Value)
            {
                case ReactorState.Offline:
                    UpdateOffline();
                    break;
                case ReactorState.Starting:
                    UpdateStarting();
                    break;
                case ReactorState.Running:
                case ReactorState.Overheating:
                case ReactorState.Critical:
                    UpdateActive();
                    break;
                case ReactorState.Meltdown:
                    // Nothing — meltdown is terminal
                    break;
            }
        }

        // =================================================================
        //  STATE UPDATES (Server only)
        // =================================================================

        private void UpdateOffline()
        {
            if (_scramCooldownActive)
            {
                _scramCooldownTimer -= Time.deltaTime;
                if (_scramCooldownTimer <= 0f)
                    _scramCooldownActive = false;
            }
        }

        private void UpdateStarting()
        {
            _startupTimer -= Time.deltaTime;
            if (_startupTimer <= 0f)
            {
                // Add power to the grid
                if (PowerManager.Instance != null)
                    PowerManager.Instance.AddPower(_config.MaxReactorPower);

                TransitionTo(ReactorState.Running);
            }
        }

        private void UpdateActive()
        {
            // Heat rises unless actively being repaired
            if (!IsBeingRepaired)
            {
                float rate = _config.ReactorHeatRate;

                // Overheating and Critical heat up faster
                if (State.Value == ReactorState.Overheating)
                    rate *= 1.5f;
                else if (State.Value == ReactorState.Critical)
                    rate *= 2.5f;

                HeatLevel.Value = Mathf.Min(1f, HeatLevel.Value + rate * Time.deltaTime);
            }

            // Check thresholds
            if (HeatLevel.Value >= 1f)
            {
                TriggerMeltdown();
            }
            else if (HeatLevel.Value >= _config.ReactorHeatCriticalThreshold &&
                     State.Value != ReactorState.Critical)
            {
                TransitionTo(ReactorState.Critical);
            }
            else if (HeatLevel.Value >= _config.ReactorHeatWarningThreshold &&
                     HeatLevel.Value < _config.ReactorHeatCriticalThreshold &&
                     State.Value != ReactorState.Overheating)
            {
                TransitionTo(ReactorState.Overheating);
            }
            else if (HeatLevel.Value < _config.ReactorHeatWarningThreshold &&
                     State.Value != ReactorState.Running)
            {
                TransitionTo(ReactorState.Running);
            }
        }

        // =================================================================
        //  ACTIONS
        // =================================================================

        /// <summary>Chaos event: instant heat spike.</summary>
        public void SurgeHeat()
        {
            if (!IsServer || State.Value == ReactorState.Meltdown || State.Value == ReactorState.Offline)
                return;

            HeatLevel.Value = Mathf.Min(1f, HeatLevel.Value + (_config != null ? _config.ReactorSurgePenalty : 0.3f));

            if (HeatLevel.Value >= 1f)
                TriggerMeltdown();
        }

        /// <summary>Emergency shutdown — kills power, heat lingers.</summary>
        private void PerformScram()
        {
            if (!IsServer) return;

            // Remove power from grid
            if (PowerManager.Instance != null)
                PowerManager.Instance.ConsumePower(_config.MaxReactorPower);

            _scramCooldownActive = true;
            _scramCooldownTimer  = _config.ReactorScramCooldownTime;

            TransitionTo(ReactorState.Offline);
            Debug.Log("[Reactor] Emergency SCRAM executed!");
        }

        /// <summary>Attempt to start the reactor.</summary>
        private void StartReactor()
        {
            if (!IsServer || _scramCooldownActive) return;

            _startupTimer = _config.ReactorStartupTime;
            TransitionTo(ReactorState.Starting);
            Debug.Log("[Reactor] Starting up…");
        }

        private void TriggerMeltdown()
        {
            HeatLevel.Value = 1f;
            TransitionTo(ReactorState.Meltdown);

            // Remove power from grid
            if (PowerManager.Instance != null)
                PowerManager.Instance.ConsumePower(_config.MaxReactorPower);

            EventBus.Publish(new ChaosEventTriggered { EventName = "Reactor Meltdown" });

            if (Damage.DamageManager.Instance != null)
                Damage.DamageManager.Instance.TakeDamage(_config.ReactorMeltdownDamage);

            Debug.Log("[Reactor] *** MELTDOWN ***");
        }

        // =================================================================
        //  STATE TRANSITIONS
        // =================================================================

        private void TransitionTo(ReactorState newState)
        {
            if (!IsServer) return;

            var oldState  = State.Value;
            if (oldState == newState) return;

            State.Value   = newState;

            EventBus.Publish(new ReactorStateChangedEvent
            {
                OldState  = oldState,
                NewState  = newState,
                HeatLevel = HeatLevel.Value
            });

            Debug.Log($"[Reactor] {oldState} → {newState} (heat {HeatLevel.Value:P0})");
        }

        // =================================================================
        //  IInteractable
        // =================================================================

        public bool CanInteract(GameObject player)
        {
            if (State.Value == ReactorState.Meltdown) return false;
            if (State.Value == ReactorState.Starting) return false;
            if (State.Value == ReactorState.Offline)  return !_scramCooldownActive;
            return true; // SCRAM is always available while running
        }

        public void OnInteract(GameObject player)
        {
            if (!IsServer)
            {
                RequestInteractServerRpc();
                return;
            }
            HandleInteraction();
        }

        public void OnInteractHold(GameObject player, float holdTime) { }
        public void OnInteractRelease(GameObject player) { }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void RequestInteractServerRpc()
        {
            HandleInteraction();
        }

        private void HandleInteraction()
        {
            if (State.Value == ReactorState.Offline && !_scramCooldownActive)
            {
                StartReactor();
            }
            else if (State.Value == ReactorState.Running  ||
                     State.Value == ReactorState.Overheating ||
                     State.Value == ReactorState.Critical)
            {
                PerformScram();
            }
        }

        // =================================================================
        //  IRepairable  (Cooling the reactor)
        // =================================================================

        public void StartRepair(GameObject repairer)
        {
            if (!IsServer || !NeedsRepair) return;
            IsBeingRepaired = true;
        }

        public void UpdateRepair(float deltaTime)
        {
            if (!IsServer || !IsBeingRepaired) return;

            float coolRate = _config != null ? _config.ReactorCooldownRate : 0.2f;
            HeatLevel.Value = Mathf.Max(0f, HeatLevel.Value - coolRate * deltaTime);

            // Progress mirrors how much heat has been removed
            NetworkRepairProgress.Value = 1f - HeatLevel.Value;

            if (HeatLevel.Value <= 0f)
                CompleteRepair();
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
            HeatLevel.Value = 0f;
            NetworkRepairProgress.Value = 0f;
            EventBus.Publish(new SystemRepairedEvent { SystemName = "Reactor Cooled" });
        }

        // =================================================================
        //  VISUALS (all clients)
        // =================================================================

        private void OnStateChangedCallback(ReactorState oldState, ReactorState newState)
        {
            UpdateVisuals(newState);
            
            if (AudioManager.Instance != null && AudioManager.Instance.Database != null)
            {
                if (newState == ReactorState.Overheating)
                    AudioManager.Instance.PlaySFX(AudioManager.Instance.Database.ReactorAlarm, transform.position);
                else if (newState == ReactorState.Critical || newState == ReactorState.Meltdown)
                    AudioManager.Instance.PlaySFX(AudioManager.Instance.Database.GlobalCritical, transform.position);
                else if (newState == ReactorState.Offline && oldState != ReactorState.Starting)
                    AudioManager.Instance.PlaySFX(AudioManager.Instance.Database.ReactorScram, transform.position);
            }
        }

        private void UpdateVisuals(ReactorState state)
        {
            if (_statusRenderer == null) return;

            Color target = state switch
            {
                ReactorState.Offline     => _offlineColor,
                ReactorState.Starting    => _startingColor,
                ReactorState.Running     => _runningColor,
                ReactorState.Overheating => _overheatColor,
                ReactorState.Critical    => _criticalColor,
                ReactorState.Meltdown    => _meltdownColor,
                _ => _offlineColor
            };

            _statusRenderer.material.SetColor("_EmissionColor", target * 2f);
            _statusRenderer.material.color = target;
        }
    }
}
