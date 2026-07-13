using SpaceMaintenance.Core;
using SpaceMaintenance.Audio;
using Unity.Netcode;
using UnityEngine;

namespace SpaceMaintenance.ShipSystems
{
    public class GeneratorController : NetworkBehaviour, IMinigameRepairable, IInteractable
    {
        [SerializeField] private PowerConfig _config;
        
        // IRepairable (Legacy / interface requirement)
        public float RepairTime => 5f;
        
        public NetworkVariable<float> NetworkRepairProgress = new NetworkVariable<float>(0f);
        public float RepairProgress => NetworkRepairProgress.Value;
        
        public bool IsBeingRepaired { get; private set; }
        
        public NetworkVariable<bool> NetworkNeedsRepair = new NetworkVariable<bool>(false);
        public bool NeedsRepair => NetworkNeedsRepair.Value;

        // IMinigameRepairable
        public SpaceMaintenance.Core.Data.MinigameType MinigameType => SpaceMaintenance.Core.Data.MinigameType.WireConnect;
        public int MinigameDifficulty => 1; // Can be scaled later

        // IInteractable
        public string InteractionPrompt => NeedsRepair ? "Press E to Repair (Minigame)" : "Generator Online";
        public bool RequiresHold => false;
        public float HoldDuration => 0f;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                NetworkNeedsRepair.Value = true; // start broken for testing
                NetworkRepairProgress.Value = 0f;
            }
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

        public void Break()
        {
            if (!IsServer || NeedsRepair) return;
            NetworkNeedsRepair.Value = true;
            NetworkRepairProgress.Value = 0f;
            IsBeingRepaired = false;
            
            if (PowerManager.Instance != null && _config != null)
            {
                PowerManager.Instance.ConsumePower(_config.GeneratorPowerOutput);
            }
            
            if (AudioManager.Instance != null && AudioManager.Instance.Database != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.Database.GeneratorBreak, transform.position);
            }
        }

        public void CompleteRepair()
        {
            if (!IsServer || !NeedsRepair) return;
            
            IsBeingRepaired = false;
            NetworkNeedsRepair.Value = false;
            NetworkRepairProgress.Value = 1f;
            
            if (PowerManager.Instance != null && _config != null)
            {
                PowerManager.Instance.AddPower(_config.GeneratorPowerOutput);
            }
            
            if (AudioManager.Instance != null && AudioManager.Instance.Database != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.Database.GeneratorFix, transform.position);
            }
            
            EventBus.Publish(new SpaceMaintenance.Core.Data.SystemRepairedEvent { SystemName = "Backup Generator" });
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
            // The client tells the server to complete the repair
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
            // Do nothing on failure, maybe play a spark sound later
        }
    }
}
