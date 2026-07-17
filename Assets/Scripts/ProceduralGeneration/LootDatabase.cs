using UnityEngine;
using System.Collections.Generic;

namespace ProceduralGeneration
{
    [System.Serializable]
    public class LootEntry
    {
        public GameObject ItemPrefab;
        public float Weight = 1f;
        
        [Tooltip("The tags of rooms where this item is allowed to spawn. Set to None to allow anywhere.")]
        public RoomTags AllowedRooms = RoomTags.Storage | RoomTags.Maintenance | RoomTags.Industrial;
    }

    [CreateAssetMenu(fileName = "NewLootDatabase", menuName = "Scrapshift/Procedural Generation/Loot Database")]
    public class LootDatabase : ScriptableObject
    {
        public List<LootEntry> Items = new List<LootEntry>();
        
        [Tooltip("Base number of items to spawn per ship.")]
        public int BaseItemsPerShip = 15;
    }
}
