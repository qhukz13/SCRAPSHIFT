using SpaceMaintenance.Core;
using SpaceMaintenance.Core.Data;
using SpaceMaintenance.Minigames;
using Unity.Netcode;
using UnityEngine;

namespace SpaceMaintenance.ShipSystems
{
    [RequireComponent(typeof(NetworkObject))]
    public class TerminalController : NetworkBehaviour, IInteractable, IMinigameRepairable, IPowered
    {
        // ─── Inspector ──────────────────────────────────────────────────
        [Header("Config")]
        [SerializeField] private PowerConfig _config;

        [Header("Identity")]
        [Tooltip("Unique name for this terminal")]
        [SerializeField] private string _terminalId = "SensorTerminal";

        [Header("Visuals")]
        [SerializeField] private Renderer _screenRenderer;
        [SerializeField] private Color _colorNormal = Color.cyan;
        [SerializeField] private Color _colorBroken = Color.red;
        [SerializeField] private Color _colorNoPower = Color.black;

        // ─── Networked State ────────────────────────────────────────────
        public NetworkVariable<bool> IsBroken = new NetworkVariable<bool>(false);

        // ─── Server-only ────────────────────────────────────────────────
        private bool _isPowered = true;

        // ─── IPowered ───────────────────────────────────────────────────
        public float PowerConsumption => 15f;
        public int PowerPriority => 6; 
        public bool IsPowered => _isPowered;

        public void OnPowerStateChanged(bool hasPower)
        {
            if (!IsServer) return;

            bool wasPowered = _isPowered;
            _isPowered = hasPower;

            if (wasPowered != hasPower)
            {
                UpdateVisualsClientRpc(hasPower);
            }
        }

        // ─── IInteractable ──────────────────────────────────────────────
        public string InteractionPrompt
        {
            get
            {
                if (!_isPowered) return "[NO POWER]";
                if (IsBroken.Value) return "[E] Calibrate Terminal";
                return "[Terminal Online]";
            }
        }

        public bool RequiresHold => false;
        public float HoldDuration => 0f;

        public bool CanInteract(GameObject player)
        {
            return _isPowered && IsBroken.Value;
        }

        public void OnInteract(GameObject player)
        {
            if (!CanInteract(player)) return;

            if (MinigameManager.Instance != null)
            {
                MinigameManager.Instance.RequestMinigame(this);
            }
        }

        public void OnInteractHold(GameObject player, float holdTime) { }
        public void OnInteractRelease(GameObject player) { }

        // ─── IMinigameRepairable (and IRepairable) ──────────────────────
        public MinigameType MinigameType => MinigameType.SequenceInput;
        public int MinigameDifficulty => 2; // Medium difficulty
        
        public float RepairTime => 0f;
        public float RepairProgress => 0f;
        public bool IsBeingRepaired { get; private set; }
        public bool NeedsRepair => IsBroken.Value;

        public void StartRepair(GameObject repairer) { IsBeingRepaired = true; }
        public void UpdateRepair(float deltaTime) { }
        public void CancelRepair() { IsBeingRepaired = false; }
        public void CompleteRepair() 
        { 
            if (!IsServer) return;
            HandleRepairComplete(); 
        }

        public void OnMinigameCompleted()
        {
            if (!IsServer)
            {
                CompleteRepairServerRpc();
            }
            else
            {
                HandleRepairComplete();
            }
        }

        public void OnMinigameFailed()
        {
            // Optional: apply penalty
            Debug.Log($"[Terminal:{_terminalId}] Minigame failed.");
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void CompleteRepairServerRpc()
        {
            HandleRepairComplete();
        }

        private void HandleRepairComplete()
        {
            IsBroken.Value = false;
            EventBus.Publish(new SystemRepairedEvent { SystemName = $"Terminal:{_terminalId}" });
            Debug.Log($"[Terminal:{_terminalId}] Terminal calibrated.");
        }

        // =================================================================
        //  EXTERNAL ACTIONS (called by ChaosManager)
        // =================================================================
        public void BreakTerminal()
        {
            if (!IsServer) return;
            IsBroken.Value = true;
            Debug.Log($"[Terminal:{_terminalId}] BROKEN!");
        }

        // =================================================================
        //  LIFECYCLE & VISUALS
        // =================================================================

        public override void OnNetworkSpawn()
        {
            IsBroken.OnValueChanged += OnBrokenStateChanged;

            if (IsServer && PowerManager.Instance != null)
            {
                PowerManager.Instance.RegisterConsumer(this);
            }

            UpdateVisualsLocally();
        }

        public override void OnNetworkDespawn()
        {
            IsBroken.OnValueChanged -= OnBrokenStateChanged;

            if (IsServer && PowerManager.Instance != null)
            {
                PowerManager.Instance.UnregisterConsumer(this);
            }
        }

        private void OnBrokenStateChanged(bool previousValue, bool newValue)
        {
            UpdateVisualsLocally();
        }

        [ClientRpc]
        private void UpdateVisualsClientRpc(bool hasPower)
        {
            _isPowered = hasPower;
            UpdateVisualsLocally();
        }

        private void UpdateVisualsLocally()
        {
            if (_screenRenderer == null) return;

            Color targetColor = _colorNormal;

            if (!_isPowered)
            {
                targetColor = _colorNoPower;
            }
            else if (IsBroken.Value)
            {
                targetColor = _colorBroken;
            }

            _screenRenderer.material.SetColor("_EmissionColor", targetColor * 1.5f);
            _screenRenderer.material.color = targetColor;
        }
    }
}
