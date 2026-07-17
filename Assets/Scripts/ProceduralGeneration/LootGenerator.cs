using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace ProceduralGeneration
{
    public class LootGenerator
    {
        public void GenerateLoot(LootDatabase database, ShipTemplate template)
        {
            if (database == null || database.Items.Count == 0)
            {
                Debug.LogWarning("[LootGenerator] No LootDatabase provided or it is empty.");
                return;
            }

            int itemsToSpawn = Mathf.RoundToInt(database.BaseItemsPerShip * template.Difficulty);
            int spawnedCount = 0;

            for (int i = 0; i < itemsToSpawn; i++)
            {
                // Pick random item based on weights
                LootEntry item = PickRandomItem(database.Items);
                if (item == null || item.ItemPrefab == null) continue;

                // Find valid rooms that have LootPoints and match the allowed tags
                var validRooms = ShipManager.Instance.GetAllRooms().FindAll(r => 
                    (item.AllowedRooms == RoomTags.None || (r.CurrentTags & item.AllowedRooms) != 0) 
                    && r.Definition.LootPoints != null 
                    && r.Definition.LootPoints.Count > 0
                );

                if (validRooms.Count == 0) continue;

                var room = validRooms[Random.Range(0, validRooms.Count)];
                var spawnPoint = room.Definition.LootPoints[Random.Range(0, room.Definition.LootPoints.Count)];

                GameObject lootInst = Object.Instantiate(item.ItemPrefab, spawnPoint.position, spawnPoint.rotation);
                lootInst.name = $"Loot_{item.ItemPrefab.name}";
                lootInst.transform.SetParent(room.transform);

                var netObj = lootInst.GetComponent<NetworkObject>();
                if (netObj != null) netObj.Spawn();

                spawnedCount++;
            }

            Debug.Log($"[LootGenerator] Spawned {spawnedCount} items across the ship.");
        }

        private LootEntry PickRandomItem(List<LootEntry> items)
        {
            float totalWeight = 0;
            foreach (var item in items) totalWeight += item.Weight;

            float r = Random.value * totalWeight;
            foreach (var item in items)
            {
                if (r < item.Weight) return item;
                r -= item.Weight;
            }
            return null;
        }
    }
}
