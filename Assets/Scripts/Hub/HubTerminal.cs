using UnityEngine;
using Unity.Netcode;
using SpaceMaintenance.Core;

namespace SpaceMaintenance.Hub
{
    public class HubTerminal : NetworkBehaviour, IInteractable
    {
        private bool _isInteracting = false;

        public string InteractionPrompt => "Press E to Access Terminal";
        public bool RequiresHold => false;
        public float HoldDuration => 0f;

        public bool CanInteract(GameObject player)
        {
            return !_isInteracting;
        }

        public void OnInteract(GameObject player)
        {
            if (!CanInteract(player)) return;
            
            _isInteracting = true;

            if (ShopUI.Instance == null)
            {
                var go = new GameObject("ShopUIManager");
                go.AddComponent<ShopUI>();
            }

            if (ShopUI.Instance != null)
            {
                ShopUI.Instance.OpenShop(this);
            }
        }

        public void OnInteractHold(GameObject player, float holdTime) { }
        public void OnInteractRelease(GameObject player) { }

        public void OnShopClosed()
        {
            _isInteracting = false;
        }

        [Rpc(SendTo.Server)]
        public void RequestPurchaseServerRpc(int price, string itemId, RpcParams rpcParams = default)
        {
            if (EconomyManager.Instance != null && EconomyManager.Instance.TrySpendFunds(price))
            {
                ulong senderId = rpcParams.Receive.SenderClientId;
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(senderId, out var client) && client.PlayerObject != null)
                {
                    var inventory = client.PlayerObject.GetComponent<SpaceMaintenance.Player.Inventory.PlayerInventory>();
                    if (inventory != null)
                    {
                        bool success = inventory.AddItem(itemId);
                        if (!success)
                        {
                            // Refund if inventory is full
                            EconomyManager.Instance.AddFunds(price);
                            Debug.LogWarning($"[HubTerminal] Inventory full for client {senderId}, refunded ${price}.");
                        }
                    }
                }
            }
        }
    }
}
