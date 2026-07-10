using System.Collections.Generic;
using UnityEngine;
using SpaceMaintenance.Core.Data;

namespace SpaceMaintenance.Player.Inventory
{
    public class PlayerInventory : MonoBehaviour
    {
        [SerializeField] private int _capacity = 4;
        
        public List<InventoryItem> Items { get; private set; } = new List<InventoryItem>();

        public bool AddItem(ItemData itemData, int amount = 1)
        {
            if (itemData.IsStackable)
            {
                var existingItem = Items.Find(i => i.Data.ItemID == itemData.ItemID && i.CurrentAmount < i.Data.MaxStack);
                if (existingItem != null)
                {
                    int spaceLeft = existingItem.Data.MaxStack - existingItem.CurrentAmount;
                    if (amount <= spaceLeft)
                    {
                        existingItem.CurrentAmount += amount;
                        return true;
                    }
                    else
                    {
                        existingItem.CurrentAmount += spaceLeft;
                        amount -= spaceLeft;
                        // Continue to add as a new slot below
                    }
                }
            }

            if (Items.Count < _capacity)
            {
                Items.Add(new InventoryItem(itemData, amount));
                return true;
            }

            Debug.LogWarning("Inventory is full!");
            return false;
        }

        public void RemoveItem(ItemData itemData, int amount = 1)
        {
            var item = Items.Find(i => i.Data.ItemID == itemData.ItemID);
            if (item != null)
            {
                item.CurrentAmount -= amount;
                if (item.CurrentAmount <= 0)
                {
                    Items.Remove(item);
                }
            }
        }
    }
}
