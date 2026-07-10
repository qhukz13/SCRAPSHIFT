using SpaceMaintenance.Core.Data;

namespace SpaceMaintenance.Player.Inventory
{
    [System.Serializable]
    public class InventoryItem
    {
        public ItemData Data;
        public int CurrentAmount;

        public InventoryItem(ItemData data, int amount = 1)
        {
            Data = data;
            CurrentAmount = amount;
        }
    }
}
