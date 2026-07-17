using System.Collections.Generic;
using UnityEngine;

namespace ProceduralGeneration
{
    public class StairGenerator
    {
        public void GenerateVerticalConnections(List<RoomInstance> placedRooms, GameObject stairPrefab)
        {
            if (stairPrefab == null)
            {
                Debug.LogWarning("[StairGenerator] StairPrefab is missing.");
                return;
            }

            foreach (var room in placedRooms)
            {
                foreach (var socket in room.Definition.DoorSockets)
                {
                    if (socket.IsUsed && socket.SocketType == SocketType.Ceiling)
                    {
                        Vector3 socketPos = room.transform.TransformPoint(socket.LocalPosition);
                        
                        // Spawn vertical connection (ladder/stair/elevator shaft)
                        GameObject stair = Object.Instantiate(stairPrefab, socketPos, Quaternion.identity);
                        stair.name = $"VerticalConnection_{room.name}";
                        stair.transform.SetParent(room.transform);
                    }
                }
            }
            Debug.Log($"[StairGenerator] Evaluated vertical connections for {placedRooms.Count} rooms.");
        }
    }
}
