using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProceduralGeneration
{
    public class LayoutValidator
    {
        public bool Validate(List<RoomDefinition> placedRooms, ShipTemplate template)
        {
            if (placedRooms == null || placedRooms.Count == 0) return false;

            bool hasSpawn = placedRooms.Any(r => r.RoomType == RoomType.Spawn);
            bool hasReactor = placedRooms.Any(r => r.RoomType == RoomType.Reactor);
            bool hasBridge = placedRooms.Any(r => r.RoomType == RoomType.Bridge);

            if (!hasSpawn || !hasReactor || !hasBridge)
            {
                Debug.LogWarning("[LayoutValidator] Missing critical rooms.");
                return false;
            }

            // Since our physical placer relies purely on the tree graph we built,
            // connectivity is guaranteed by the RoomPlacer succeeding.
            // If we later implement loops/cycles, we would need actual pathfinding here.

            return true;
        }
    }
}
