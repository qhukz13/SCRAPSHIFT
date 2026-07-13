using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using SpaceMaintenance.Core.Data;

namespace SpaceMaintenance.Player.Inventory
{
    public class PlayerInventory : NetworkBehaviour
    {
        [SerializeField] private int _capacity = 4;
        [SerializeField] private ItemDatabase _itemDatabase;
        
        // This list is synchronized across the network
        public NetworkList<NetworkInventoryItem> NetworkItems;

        private void Awake()
        {
            NetworkItems = new NetworkList<NetworkInventoryItem>();
        }

        // Server-side method to add an item
        public bool AddItem(string itemID, int amount = 1)
        {
            if (!IsServer) return false;

            if (_itemDatabase != null)
            {
                var data = _itemDatabase.GetItem(itemID);
                if (data != null && data.IsStackable)
                {
                    for (int i = 0; i < NetworkItems.Count; i++)
                    {
                        var item = NetworkItems[i];
                        if (item.ItemID.ToString() == itemID && item.Amount < data.MaxStack)
                        {
                            int spaceLeft = data.MaxStack - item.Amount;
                            if (amount <= spaceLeft)
                            {
                                item.Amount += amount;
                                NetworkItems[i] = item; // Trigger NetworkList update
                                return true;
                            }
                            else
                            {
                                item.Amount += spaceLeft;
                                NetworkItems[i] = item;
                                amount -= spaceLeft;
                            }
                        }
                    }
                }
            }

            if (NetworkItems.Count < _capacity)
            {
                NetworkItems.Add(new NetworkInventoryItem(itemID, amount));
                return true;
            }

            Debug.LogWarning("Inventory is full!");
            return false;
        }

        /// <summary>Check if the inventory contains at least one of the given item.</summary>
        public bool HasItem(string itemID)
        {
            for (int i = 0; i < NetworkItems.Count; i++)
            {
                if (NetworkItems[i].ItemID.ToString() == itemID)
                    return true;
            }
            return false;
        }

        // Server-side method to remove an item
        public void RemoveItem(string itemID, int amount = 1)
        {
            if (!IsServer) return;

            for (int i = 0; i < NetworkItems.Count; i++)
            {
                var item = NetworkItems[i];
                if (item.ItemID.ToString() == itemID)
                {
                    item.Amount -= amount;
                    if (item.Amount <= 0)
                    {
                        NetworkItems.RemoveAt(i);
                    }
                    else
                    {
                        NetworkItems[i] = item;
                    }
                    return;
                }
            }
        }

        [ServerRpc]
        public void RequestPickupItemServerRpc(string itemID, NetworkObjectReference worldItemRef)
        {
            if (AddItem(itemID))
            {
                if (worldItemRef.TryGet(out NetworkObject worldItem))
                {
                    // Despawn but don't destroy immediately to prevent Netcode warning for in-scene objects
                    worldItem.Despawn(false); 
                    worldItem.gameObject.SetActive(false); // Hide the item
                    Destroy(worldItem.gameObject); // Destroy it after hiding
                }
            }
        }

        [ServerRpc]
        public void RequestDropItemServerRpc(int slotIndex, Vector3 dropPosition, Vector3 forward)
        {
            if (slotIndex < 0 || slotIndex >= NetworkItems.Count) return;

            var item = NetworkItems[slotIndex];
            string itemID = item.ItemID.ToString();

            if (_itemDatabase != null)
            {
                var data = _itemDatabase.GetItem(itemID);
                if (data != null && data.Prefab != null)
                {
                    GameObject spawnedItem = Instantiate(data.Prefab, dropPosition, Quaternion.LookRotation(forward));
                    Debug.Log($"[DROP] Spawned {spawnedItem.name} at {spawnedItem.transform.position}");
                    NetworkObject netObj = spawnedItem.GetComponent<NetworkObject>();
                    if (netObj != null)
                    {
                        netObj.Spawn();
                        Debug.Log($"[DROP] NetworkObject spawned successfully!");
                    }
                    else
                    {
                        Debug.LogError($"[DROP] No NetworkObject on spawned prefab!");
                    }
                }
                else
                {
                    Debug.LogError($"[DROP] Prefab for {itemID} is null in ItemDatabase!");
                }
            }
            else
            {
                Debug.LogError("[DROP] ItemDatabase is null!");
            }
            
            RemoveItem(itemID, 1);
        }
    }
}
