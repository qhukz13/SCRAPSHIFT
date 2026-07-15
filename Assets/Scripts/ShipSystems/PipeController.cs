// ============================================================================
// SCRAPSHIFT — PipeController.cs
// A ship system that can break, leaking steam and obscuring vision.
// Can be repaired by interacting and completing a minigame.
// ============================================================================

using SpaceMaintenance.Core;
using SpaceMaintenance.Core.Data;
using SpaceMaintenance.Player.Inventory;
using Unity.Netcode;
using UnityEngine;

namespace SpaceMaintenance.ShipSystems
{
    public class PipeController : NetworkBehaviour, IMinigameRepairable, IInteractable
    {
        [Header("Pipe Settings")]
        [Tooltip("The visual steam particle system that activates when broken")]
        [SerializeField] private ParticleSystem _steamParticles;
        [SerializeField] private AudioSource _steamAudio;
        
        // Network state
        public NetworkVariable<bool> NetworkNeedsRepair = new NetworkVariable<bool>(false);
        public bool NeedsRepair => NetworkNeedsRepair.Value;

        // IRepairable (Legacy / interface requirement)
        public float RepairTime => 5f;
        public NetworkVariable<float> NetworkRepairProgress = new NetworkVariable<float>(0f);
        public float RepairProgress => NetworkRepairProgress.Value;
        public bool IsBeingRepaired { get; private set; }

        // IMinigameRepairable
        public SpaceMaintenance.Core.MinigameType MinigameType => SpaceMaintenance.Core.MinigameType.PipeAlign;
        public int MinigameDifficulty => 1 + _repairCount + SpaceMaintenance.Core.GlobalMissionParameters.MissionsCompleted; // Gets harder each time AND each mission

        private int _repairCount = 0;

        // IInteractable
        public const string REQUIRED_ITEM_ID = "wrench";
        public string InteractionPrompt => NeedsRepair ? "Press E to Align Pipes" : "Pipes OK";
        public bool RequiresHold => false;
        public float HoldDuration => 0f;

        public override void OnNetworkSpawn()
        {
            NetworkNeedsRepair.OnValueChanged += OnRepairStateChanged;
            
            // Sync initial state locally
            UpdateVisuals(NetworkNeedsRepair.Value);
        }

        public override void OnNetworkDespawn()
        {
            NetworkNeedsRepair.OnValueChanged -= OnRepairStateChanged;
        }

        private void OnRepairStateChanged(bool previous, bool isBroken)
        {
            UpdateVisuals(isBroken);
        }

        private void UpdateVisuals(bool isBroken)
        {
            if (isBroken)
            {
                if (_steamParticles != null) _steamParticles.Play();
                if (_steamAudio != null && !_steamAudio.isPlaying) _steamAudio.Play();
            }
            else
            {
                if (_steamParticles != null) _steamParticles.Stop();
                if (_steamAudio != null) _steamAudio.Stop();
            }
        }

        /// <summary>
        /// Breaks the pipe, starting the steam leak. Can be called by ChaosManager.
        /// </summary>
        public void Break()
        {
            if (!IsServer || NeedsRepair) return;
            NetworkNeedsRepair.Value = true;
            NetworkRepairProgress.Value = 0f;
            IsBeingRepaired = false;
            
            // Here we could notify the event bus about a broken system
            EventBus.Publish(new SpaceMaintenance.Core.Data.SystemBrokenEvent { SystemName = "Coolant Pipe" });
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

        /// <summary>
        /// Completes the repair, stopping the steam leak.
        /// </summary>
        public void CompleteRepair()
        {
            if (!IsServer || !NeedsRepair) return;
            
            IsBeingRepaired = false;
            NetworkNeedsRepair.Value = false;
            NetworkRepairProgress.Value = 1f;
            _repairCount++;
            
            EventBus.Publish(new SpaceMaintenance.Core.Data.SystemRepairedEvent { SystemName = "Coolant Pipe" });
        }

        // =================================================================
        //  IINTERACTABLE
        // =================================================================
        
        public bool CanInteract(GameObject player)
        {
            return NeedsRepair && !SpaceMaintenance.Minigames.MinigameManager.Instance.IsMinigameActive;
        }

        public void OnInteract(GameObject player)
        {
            if (!CanInteract(player)) return;

            // Check if player has the wrench
            var inventory = player.GetComponent<PlayerInventory>();
            if (inventory == null || !inventory.HasItem(REQUIRED_ITEM_ID))
            {
                // Show blocked overlay
                SpaceMaintenance.Minigames.MinigameManager.Instance.ShowBlockedMessage(
                    "WRENCH REQUIRED",
                    "Find and pick up a Wrench\nbefore repairing the pipes."
                );
                return;
            }

            SpaceMaintenance.Minigames.MinigameManager.Instance.RequestMinigame(this);
        }

        public void OnInteractHold(GameObject player, float holdTime) { }
        public void OnInteractRelease(GameObject player) { }

        // =================================================================
        //  IMINIGAMEREPAIRABLE
        // =================================================================

        public void OnMinigameCompleted()
        {
            // Client tells the server the minigame is done
            if (IsServer)
            {
                CompleteRepair();
            }
            else
            {
                CompleteRepairServerRpc();
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void CompleteRepairServerRpc()
        {
            CompleteRepair();
        }

        public void OnMinigameFailed()
        {
            // Could spark or play an error sound here
        }
    }
}
