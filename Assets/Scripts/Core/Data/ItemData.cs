using UnityEngine;

namespace SpaceMaintenance.Core.Data
{
    [CreateAssetMenu(fileName = "NewItemData", menuName = "SpaceMaintenance/Inventory/Item Data")]
    public class ItemData : ScriptableObject
    {
        public string ItemID;
        public string DisplayName;
        public GameObject Prefab;
        public Sprite Icon;
        public bool IsStackable;
        public int MaxStack = 1;
    }
}
