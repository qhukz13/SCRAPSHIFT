using SpaceMaintenance.Core;
using SpaceMaintenance.Player.Inventory;
using Unity.Netcode;
using UnityEngine;

namespace SpaceMaintenance.ShipSystems
{
    public class WorldItem : NetworkBehaviour, IInteractable
    {
        [SerializeField] private string _itemID;
        [SerializeField] private string _displayName = "Item";

        public string InteractionPrompt => $"Pick up {_displayName}";
        public bool RequiresHold => false;
        public float HoldDuration => 0f;

        public bool CanInteract(GameObject player)
        {
            var inventory = player.GetComponent<PlayerInventory>();
            // We can interact if we have an inventory and it has space (client-side prediction could be complex, so we just check if they have inventory)
            return inventory != null && inventory.NetworkItems.Count < 4; // Hardcoded capacity check for now
        }

        public void OnInteract(GameObject player)
        {
            Debug.Log($"WorldItem {_itemID}: OnInteract called by {player.name}");
            var inventory = player.GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                if (inventory.IsOwner)
                {
                    Debug.Log($"WorldItem: Requesting pickup via ServerRpc...");
                    inventory.RequestPickupItemServerRpc(_itemID, GetComponent<NetworkObject>());
                }
                else
                {
                    Debug.Log("WorldItem: PlayerInventory is not IsOwner!");
                }
            }
            else
            {
                Debug.LogWarning("WorldItem: Player has no PlayerInventory component!");
            }
        }

        public void OnInteractHold(GameObject player, float holdTime) { }
        public void OnInteractRelease(GameObject player) { }
    }
}
