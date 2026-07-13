// ============================================================================
// SCRAPSHIFT — PipeController.cs
// A ship system that can break, leaking steam and obscuring vision.
// Can be repaired by interacting and completing a minigame.
// ============================================================================

using SpaceMaintenance.Core;
using SpaceMaintenance.Core.Data;
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

        // IMinigameRepairable
        public SpaceMaintenance.Core.MinigameType MinigameType => SpaceMaintenance.Core.MinigameType.PipeAlign;
        public int MinigameDifficulty => 1; // Can scale based on mission progress

        // IInteractable
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
            
            // Here we could notify the event bus about a broken system
            EventBus.Publish(new SpaceMaintenance.Core.Data.SystemBrokenEvent { SystemName = "Coolant Pipe" });
        }

        /// <summary>
        /// Completes the repair, stopping the steam leak.
        /// </summary>
        public void CompleteRepair()
        {
            if (!IsServer || !NeedsRepair) return;
            NetworkNeedsRepair.Value = false;
            
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
            if (CanInteract(player))
            {
                SpaceMaintenance.Minigames.MinigameManager.Instance.RequestMinigame(this);
            }
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

        [ServerRpc(RequireOwnership = false)]
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
