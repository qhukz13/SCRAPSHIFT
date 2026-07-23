using SpaceMaintenance.Core;
using SpaceMaintenance.Damage;
using SpaceMaintenance.Player.Inventory;
using Unity.Netcode;
using UnityEngine;

namespace SpaceMaintenance.ShipSystems
{
    [RequireComponent(typeof(NetworkObject))]
    public class FireHazard : NetworkBehaviour, IInteractable
    {
        [SerializeField] private float _damagePerSecond = 5f;
        [SerializeField] private float _damageRadius = 3f;

        private bool _isExtinguished = false;

        public string InteractionPrompt => "Use Extinguisher";
        public bool RequiresHold => true;
        public float HoldDuration => 2f;

        private void Update()
        {
            if (!IsServer || _isExtinguished) return;

            // Damage hull periodically
            if (DamageManager.Instance != null)
            {
                DamageManager.Instance.TakeDamage(_damagePerSecond * Time.deltaTime);
            }
        }

        public bool CanInteract(GameObject player)
        {
            if (_isExtinguished) return false;
            
            var inventory = player.GetComponent<PlayerInventory>();
            return inventory != null && inventory.HasItem("Extinguisher");
        }

        public void OnInteract(GameObject player) { }

        public void OnInteractHold(GameObject player, float holdTime) { }

        public void OnInteractRelease(GameObject player)
        {
            if (_isExtinguished) return;
            
            if (IsServer)
            {
                Extinguish();
            }
            else
            {
                RequestExtinguishServerRpc();
            }
        }

        [Rpc(SendTo.Server)]
        private void RequestExtinguishServerRpc(RpcParams rpcParams = default)
        {
            // Verify player has extinguisher
            ulong senderId = rpcParams.Receive.SenderClientId;
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(senderId, out var client) && client.PlayerObject != null)
            {
                var inventory = client.PlayerObject.GetComponent<PlayerInventory>();
                if (inventory != null && inventory.HasItem("Extinguisher"))
                {
                    Extinguish();
                }
            }
        }

        private void Extinguish()
        {
            _isExtinguished = true;
            GetComponent<NetworkObject>().Despawn(true);
        }
    }
}
