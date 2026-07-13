using UnityEngine;
using Unity.Netcode;
using SpaceMaintenance.Core;

namespace SpaceMaintenance.Hub
{
    public class HubTerminal : NetworkBehaviour, IInteractable
    {
        private bool _isInteracting = false;

        public string InteractionPrompt => "Access Company Terminal";
        public bool RequiresHold => false;
        public float HoldDuration => 0f;

        public bool CanInteract(GameObject player)
        {
            return !_isInteracting;
        }

        public void OnInteract(GameObject player)
        {
            if (_isInteracting) return;
            
            _isInteracting = true;
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

        [Rpc(SendTo.Server, RequireOwnership = false)]
        public void RequestPurchaseServerRpc(int price, string upgradeId, RpcParams rpcParams = default)
        {
            if (EconomyManager.Instance != null && EconomyManager.Instance.TrySpendFunds(price))
            {
                if (ProgressionManager.Instance != null)
                {
                    ProgressionManager.Instance.UnlockUpgrade(upgradeId);
                    // Notify client of success if needed, but the Economy UI will update automatically
                }
            }
        }
    }
}
