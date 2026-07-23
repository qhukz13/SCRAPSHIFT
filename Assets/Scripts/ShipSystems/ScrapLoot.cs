using SpaceMaintenance.Core;
using Unity.Netcode;
using UnityEngine;

namespace SpaceMaintenance.ShipSystems
{
    [RequireComponent(typeof(NetworkObject))]
    public class ScrapLoot : NetworkBehaviour, IInteractable
    {
        [SerializeField] private int _minScrap = 10;
        [SerializeField] private int _maxScrap = 30;

        public string InteractionPrompt => "Collect Scrap";
        public bool RequiresHold => false;
        public float HoldDuration => 0f;

        public bool CanInteract(GameObject player)
        {
            return true;
        }

        public void OnInteract(GameObject player)
        {
            if (IsServer)
            {
                CollectLoot();
            }
            else
            {
                RequestCollectLootServerRpc();
            }
        }

        [Rpc(SendTo.Server)]
        private void RequestCollectLootServerRpc(RpcParams rpcParams = default)
        {
            CollectLoot();
        }

        private void CollectLoot()
        {
            if (!IsServer) return;

            int amount = Random.Range(_minScrap, _maxScrap + 1);
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddFunds(amount);
            }
            
            // Destroy the scrap object
            GetComponent<NetworkObject>().Despawn(true);
        }

        public void OnInteractHold(GameObject player, float holdTime) { }
        public void OnInteractRelease(GameObject player) { }
    }
}
