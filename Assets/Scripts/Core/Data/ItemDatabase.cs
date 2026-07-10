using System.Collections.Generic;
using UnityEngine;

namespace SpaceMaintenance.Core.Data
{
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "SpaceMaintenance/Inventory/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        public List<ItemData> Items = new List<ItemData>();

        private Dictionary<string, ItemData> _itemDict;

        public void Initialize()
        {
            _itemDict = new Dictionary<string, ItemData>();
            foreach (var item in Items)
            {
                if (item != null && !string.IsNullOrEmpty(item.ItemID))
                {
                    _itemDict[item.ItemID] = item;
                }
            }
        }

        public ItemData GetItem(string itemID)
        {
            if (_itemDict == null) Initialize();
            
            if (_itemDict.TryGetValue(itemID, out var item))
            {
                return item;
            }
            return null;
        }
    }
}
