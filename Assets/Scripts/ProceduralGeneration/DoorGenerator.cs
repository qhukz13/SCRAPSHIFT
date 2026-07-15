using System.Collections.Generic;
using UnityEngine;

namespace ProceduralGeneration
{
    public class DoorGenerator
    {
        public void GenerateDoors(List<RoomDefinition> placedRooms)
        {
            // Placeholder: we would iterate over placedRooms and find overlapping sockets,
            // then instantiate door prefabs. For now, the layout is enough.
            Debug.Log($"[DoorGenerator] Evaluated doors for {placedRooms.Count} rooms.");
        }
    }
}
