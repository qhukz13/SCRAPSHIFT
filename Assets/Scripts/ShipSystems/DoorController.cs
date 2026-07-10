using SpaceMaintenance.Core;
using Unity.Netcode;
using UnityEngine;

namespace SpaceMaintenance.ShipSystems
{
    public class DoorController : NetworkBehaviour, IInteractable, IRepairable
    {
        [SerializeField] private PowerConfig _config;
        [SerializeField] private Animator _doorAnimator;

        public NetworkVariable<bool> IsJammed = new NetworkVariable<bool>(false);
        private bool _isOpen = false;

        // IInteractable properties
        public string InteractionPrompt
        {
            get
            {
                if (IsJammed.Value) return "Jammed!";
                bool hasPower = HasPower();
                if (hasPower) return _isOpen ? "Close Door" : "Open Door";
                return _isOpen ? "Force Close Door" : "Force Open Door";
            }
        }
        public bool RequiresHold => !HasPower();
        public float HoldDuration => 3f;

        // IRepairable properties
        public float RepairTime => 5f;
        
        public NetworkVariable<float> NetworkRepairProgress = new NetworkVariable<float>(0f);
        public float RepairProgress => NetworkRepairProgress.Value;
        
        public bool IsBeingRepaired { get; private set; }
        public bool NeedsRepair => IsJammed.Value;

        private bool HasPower()
        {
            return PowerManager.Instance != null && _config != null && PowerManager.Instance.HasSufficientPower(_config.DoorPowerConsumption);
        }

        public void JamDoor()
        {
            if (!IsServer) return;
            IsJammed.Value = true;
            NetworkRepairProgress.Value = 0f;
            IsBeingRepaired = false;
        }

        // --- INTERACTION ---

        public bool CanInteract(GameObject player) => !IsJammed.Value;

        public void OnInteract(GameObject player)
        {
            if (!CanInteract(player) || RequiresHold) return;
            if (!IsServer) RequestToggleDoorServerRpc();
            else ToggleDoor();
        }

        public void OnInteractHold(GameObject player, float holdTime)
        {
            if (!CanInteract(player) || !RequiresHold) return;
            if (holdTime >= HoldDuration)
            {
                if (!IsServer) RequestToggleDoorServerRpc();
                else ToggleDoor();
            }
        }

        public void OnInteractRelease(GameObject player) { }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void RequestToggleDoorServerRpc()
        {
            if (CanInteract(null)) ToggleDoor();
        }

        private void ToggleDoor()
        {
            _isOpen = !_isOpen;
            ToggleDoorClientRpc(_isOpen);
        }

        [ClientRpc]
        private void ToggleDoorClientRpc(bool isOpen)
        {
            _isOpen = isOpen;
            if (_doorAnimator != null) _doorAnimator.SetBool("IsOpen", _isOpen);
        }

        // --- REPAIR (Unjamming) ---

        public void StartRepair(GameObject repairer)
        {
            if (!IsServer || !NeedsRepair) return;
            IsBeingRepaired = true;
        }

        public void UpdateRepair(float deltaTime)
        {
            if (!IsServer || !IsBeingRepaired || !NeedsRepair) return;
            NetworkRepairProgress.Value += deltaTime / RepairTime;
            if (NetworkRepairProgress.Value >= 1f) CompleteRepair();
        }

        public void CancelRepair()
        {
            if (!IsServer) return;
            IsBeingRepaired = false;
        }

        public void CompleteRepair()
        {
            if (!IsServer) return;
            IsBeingRepaired = false;
            IsJammed.Value = false;
            NetworkRepairProgress.Value = 1f;
            EventBus.Publish(new SpaceMaintenance.Core.Data.SystemRepairedEvent { SystemName = "Door Unjammed" });
        }
    }
}
