// ============================================================================
// Space Maintenance — DoorController.cs
// Full door state-machine: Open / Closed / Locked / Broken.
// Implements IPowered (power-dependent operation), IInteractable (open/close),
// and IRepairable (unjam broken doors).
// ============================================================================

using SpaceMaintenance.Core;
using SpaceMaintenance.Core.Data;
using Unity.Netcode;
using UnityEngine;

namespace SpaceMaintenance.ShipSystems
{
    [RequireComponent(typeof(NetworkObject))]
    public class DoorController : NetworkBehaviour, IInteractable, IRepairable, IPowered
    {
        // ─── Inspector ──────────────────────────────────────────────────
        [Header("Config")]
        [SerializeField] private PowerConfig _config;

        [Header("Identity")]
        [Tooltip("Unique name for this door — used in events and debug.")]
        [SerializeField] private string _doorId = "Door";

        [Header("Animation")]
        [SerializeField] private Animator _doorAnimator;

        [Header("Visual Hooks (optional)")]
        [SerializeField] private Renderer _panelRenderer;
        [SerializeField] private Color _openColor   = new Color(0f, 1f, 0.4f);
        [SerializeField] private Color _closedColor = new Color(0.2f, 0.5f, 1f);
        [SerializeField] private Color _lockedColor = new Color(1f, 0.5f, 0f);
        [SerializeField] private Color _brokenColor = new Color(1f, 0f, 0f);
        [SerializeField] private Color _noPowerColor = new Color(0.15f, 0.15f, 0.15f);

        // ─── Networked State ────────────────────────────────────────────
        public NetworkVariable<DoorState> State =
            new NetworkVariable<DoorState>(DoorState.Closed);

        public NetworkVariable<float> NetworkRepairProgress =
            new NetworkVariable<float>(0f);

        // ─── Server-only ────────────────────────────────────────────────
        private bool _isPowered = true;
        private bool _previousPowerState = true;

        // ─── IPowered ───────────────────────────────────────────────────
        public float PowerConsumption => _config != null ? _config.DoorPowerConsumption : 10f;
        public int   PowerPriority   => 5; // Doors are medium priority
        public bool  IsPowered       => _isPowered;

        public void OnPowerStateChanged(bool hasPower)
        {
            if (!IsServer) return;

            bool wasPowered = _isPowered;
            _isPowered = hasPower;

            if (wasPowered && !hasPower)
            {
                OnPowerLost();
            }
            else if (!wasPowered && hasPower)
            {
                OnPowerRestored();
            }
        }

        // ─── IInteractable ──────────────────────────────────────────────
        public string InteractionPrompt
        {
            get
            {
                switch (State.Value)
                {
                    case DoorState.Broken:
                        return "[R] Repair Door";
                    case DoorState.Locked:
                        return _isPowered
                            ? "[HOLD E] Bypass Lock"
                            : "[HOLD E] Force Open (No Power)";
                    case DoorState.Open:
                        return _isPowered ? "[E] Close Door" : "[HOLD E] Force Close";
                    case DoorState.Closed:
                        return _isPowered ? "[E] Open Door"  : "[HOLD E] Force Open";
                    default:
                        return "[E] Door";
                }
            }
        }

        public bool RequiresHold
        {
            get
            {
                if (State.Value == DoorState.Locked) return true;
                if (State.Value == DoorState.Broken) return false; // Broken uses Repair, not interact
                return !_isPowered; // No power → hold to force
            }
        }

        public float HoldDuration
        {
            get
            {
                if (_config == null) return 3f;
                if (State.Value == DoorState.Locked) return _config.DoorLockBypassTime;
                return _config.DoorForceOpenTime;
            }
        }

        // ─── IRepairable ────────────────────────────────────────────────
        public float RepairTime      => _config != null ? _config.DoorUnjamRepairTime : 5f;
        public float RepairProgress  => NetworkRepairProgress.Value;
        public bool  IsBeingRepaired { get; private set; }
        public bool  NeedsRepair     => State.Value == DoorState.Broken;

        // =================================================================
        //  LIFECYCLE
        // =================================================================

        public override void OnNetworkSpawn()
        {
            // Register visual callback on all clients
            State.OnValueChanged += OnStateChangedCallback;

            if (IsServer)
            {
                // Register with PowerManager
                if (PowerManager.Instance != null)
                    PowerManager.Instance.RegisterConsumer(this);
            }

            // Apply initial visual
            UpdateVisuals(State.Value);
        }

        public override void OnNetworkDespawn()
        {
            State.OnValueChanged -= OnStateChangedCallback;

            if (IsServer && PowerManager.Instance != null)
                PowerManager.Instance.UnregisterConsumer(this);
        }

        // =================================================================
        //  POWER EVENTS (Server only)
        // =================================================================

        private void OnPowerLost()
        {
            bool autoOpen = _config != null && _config.DoorsOpenOnPowerLoss;

            if (autoOpen && State.Value == DoorState.Closed)
            {
                TransitionTo(DoorState.Open);
                Debug.Log($"[Door:{_doorId}] Emergency open — power lost!");
            }

            // Locked doors remain locked — power loss doesn't free them
            UpdatePanelVisualsClientRpc(false);
        }

        private void OnPowerRestored()
        {
            UpdatePanelVisualsClientRpc(true);
            Debug.Log($"[Door:{_doorId}] Power restored.");
        }

        [ClientRpc]
        private void UpdatePanelVisualsClientRpc(bool powered)
        {
            _isPowered = powered;
            UpdateVisuals(State.Value);
        }

        // =================================================================
        //  INTERACTION (IInteractable)
        // =================================================================

        public bool CanInteract(GameObject player)
        {
            // Broken doors must be repaired, not interacted with
            return State.Value != DoorState.Broken;
        }

        public void OnInteract(GameObject player)
        {
            if (RequiresHold) return; // Must hold for unpowered / locked
            if (!IsServer)
            {
                RequestToggleDoorServerRpc();
                return;
            }
            ToggleDoor();
        }

        public void OnInteractHold(GameObject player, float holdTime)
        {
            if (!RequiresHold || State.Value == DoorState.Broken) return;

            if (holdTime >= HoldDuration)
            {
                if (!IsServer)
                {
                    RequestHoldCompleteServerRpc();
                    return;
                }
                HandleHoldComplete();
            }
        }

        public void OnInteractRelease(GameObject player) { }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void RequestToggleDoorServerRpc()
        {
            if (CanInteract(null) && !RequiresHold)
                ToggleDoor();
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void RequestHoldCompleteServerRpc()
        {
            HandleHoldComplete();
        }

        private void ToggleDoor()
        {
            if (State.Value == DoorState.Open)
                TransitionTo(DoorState.Closed);
            else if (State.Value == DoorState.Closed)
                TransitionTo(DoorState.Open);
        }

        private void HandleHoldComplete()
        {
            if (State.Value == DoorState.Locked)
            {
                // Bypass lock → open
                TransitionTo(DoorState.Open);
                Debug.Log($"[Door:{_doorId}] Lock bypassed!");
            }
            else if (!_isPowered)
            {
                // Force open / close without power
                ToggleDoor();
                Debug.Log($"[Door:{_doorId}] Manually forced.");
            }
        }

        // =================================================================
        //  EXTERNAL ACTIONS (called by ChaosManager, etc.)
        // =================================================================

        /// <summary>Jam the door — enters Broken state, needs IRepairable.</summary>
        public void JamDoor()
        {
            if (!IsServer) return;
            TransitionTo(DoorState.Broken);
            NetworkRepairProgress.Value = 0f;
            IsBeingRepaired = false;
            Debug.Log($"[Door:{_doorId}] JAMMED!");
        }

        /// <summary>Lock the door — needs hold-interact or key to unlock.</summary>
        public void LockDoor()
        {
            if (!IsServer) return;
            if (State.Value == DoorState.Broken) return; // Can't lock a broken door
            TransitionTo(DoorState.Locked);
            Debug.Log($"[Door:{_doorId}] LOCKED!");
        }

        /// <summary>Unlock a locked door from script (e.g. key card).</summary>
        public void UnlockDoor()
        {
            if (!IsServer) return;
            if (State.Value != DoorState.Locked) return;
            TransitionTo(DoorState.Closed);
            Debug.Log($"[Door:{_doorId}] Unlocked.");
        }

        // =================================================================
        //  REPAIR (IRepairable — unjamming)
        // =================================================================

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
                CompleteRepair();
        }

        public void CancelRepair()
        {
            if (!IsServer) return;
            IsBeingRepaired = false;
            // Don't reset progress — partial repair is preserved
        }

        public void CompleteRepair()
        {
            if (!IsServer) return;
            IsBeingRepaired = false;
            NetworkRepairProgress.Value = 1f;
            TransitionTo(DoorState.Closed);
            EventBus.Publish(new SystemRepairedEvent { SystemName = $"Door:{_doorId} Unjammed" });
        }

        // =================================================================
        //  STATE TRANSITIONS
        // =================================================================

        private void TransitionTo(DoorState newState)
        {
            if (!IsServer) return;

            var oldState = State.Value;
            if (oldState == newState) return;

            State.Value = newState;

            EventBus.Publish(new DoorStateChangedEvent
            {
                DoorId   = _doorId,
                OldState = oldState,
                NewState = newState
            });

            // Sync animation
            SyncAnimationClientRpc(newState == DoorState.Open);

            Debug.Log($"[Door:{_doorId}] {oldState} → {newState}");
        }

        [ClientRpc]
        private void SyncAnimationClientRpc(bool isOpen)
        {
            if (_doorAnimator != null)
                _doorAnimator.SetBool("IsOpen", isOpen);
        }

        // =================================================================
        //  VISUALS (all clients)
        // =================================================================

        private void OnStateChangedCallback(DoorState oldState, DoorState newState)
        {
            UpdateVisuals(newState);
        }

        private void UpdateVisuals(DoorState state)
        {
            if (_panelRenderer == null) return;

            Color target;

            if (!_isPowered)
            {
                target = _noPowerColor;
            }
            else
            {
                target = state switch
                {
                    DoorState.Open   => _openColor,
                    DoorState.Closed => _closedColor,
                    DoorState.Locked => _lockedColor,
                    DoorState.Broken => _brokenColor,
                    _ => _closedColor
                };
            }

            _panelRenderer.material.SetColor("_EmissionColor", target * 1.5f);
            _panelRenderer.material.color = target;
        }
    }
}
