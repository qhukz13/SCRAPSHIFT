using System.Collections.Generic;
using UnityEngine;

namespace ProceduralGeneration
{
    public class DoorGenerator
    {
        public void GenerateDoors(List<RoomDefinition> placedRooms, GameObject doorPrefab)
        {
            if (doorPrefab == null)
            {
                Debug.LogWarning("[DoorGenerator] DoorPrefab is missing. Skipping door generation.");
                return;
            }

            List<Vector3> processedPositions = new List<Vector3>();
            float tolerance = 0.1f;
            int doorsSpawned = 0;

            foreach (var room in placedRooms)
            {
                foreach (var socket in room.DoorSockets)
                {
                    if (socket.IsUsed)
                    {
                        Vector3 worldPos = room.transform.TransformPoint(socket.LocalPosition);
                        
                        // Check if we already spawned a door here (from the other room's socket)
                        bool alreadyProcessed = false;
                        foreach (var pos in processedPositions)
                        {
                            if (Vector3.Distance(pos, worldPos) < tolerance)
                            {
                                alreadyProcessed = true;
                                break;
                            }
                        }

                        if (!alreadyProcessed)
                        {
                            Vector3 worldDir = room.transform.TransformDirection(socket.LocalDirection);
                            Quaternion rotation = Quaternion.LookRotation(worldDir, Vector3.up);

                            GameObject doorInst = Object.Instantiate(doorPrefab, worldPos, rotation);
                            
                            // Netcode requires the object to be spawned on the server
                            var netObj = doorInst.GetComponent<Unity.Netcode.NetworkObject>();
                            if (netObj != null)
                            {
                                netObj.Spawn();
                            }
                            
                            processedPositions.Add(worldPos);
                            doorsSpawned++;
                        }
                    }
                }
            }

            Debug.Log($"[DoorGenerator] Spawned {doorsSpawned} doors across {placedRooms.Count} rooms.");
        }
    }
}
