using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ProceduralGeneration
{
    public class DoorGenerator
    {
        private class SocketData
        {
            public RoomInstance Room;
            public DoorSocket Socket;
            public Vector3 WorldPos;
            public Quaternion WorldRot;
        }

        public void GenerateDoors(List<RoomInstance> placedRooms, GameObject doorPrefab)
        {
            if (doorPrefab == null)
            {
                Debug.LogWarning("[DoorGenerator] DoorPrefab is not assigned.");
                return;
            }

            List<SocketData> allSockets = new List<SocketData>();

            foreach (var room in placedRooms)
            {
                foreach (var socket in room.Definition.DoorSockets)
                {
                    // We only spawn doors on Wall sockets. Floors/Ceilings are for stairs/elevators.
                    if (socket.IsUsed && socket.SocketType == SocketType.Wall)
                    {
                        allSockets.Add(new SocketData
                        {
                            Room = room,
                            Socket = socket,
                            WorldPos = room.transform.TransformPoint(socket.LocalPosition),
                            WorldRot = room.transform.rotation * Quaternion.LookRotation(socket.LocalDirection)
                        });
                    }
                }
            }

            List<SocketData> processed = new List<SocketData>();
            int doorsSpawned = 0;

            foreach (var s1 in allSockets)
            {
                if (processed.Contains(s1)) continue;

                // Find matching socket from another room within a small epsilon distance
                SocketData s2 = allSockets.Find(s => s != s1 && !processed.Contains(s) && Vector3.Distance(s.WorldPos, s1.WorldPos) < 0.1f);

                if (s2 != null)
                {
                    // Spawn ONE door between the two rooms
                    GameObject doorObj = Object.Instantiate(doorPrefab, s1.WorldPos, s1.WorldRot);
                    doorObj.name = $"Door_{s1.Room.name}_to_{s2.Room.name}";
                    doorObj.transform.SetParent(s1.Room.transform);

                    var doorController = doorObj.GetComponent<DoorController>();
                    if (doorController == null) doorController = doorObj.AddComponent<DoorController>();

                    doorController.Initialize(s1.Room, s2.Room);

                    var netObj = doorObj.GetComponent<NetworkObject>();
                    if (netObj != null) netObj.Spawn();

                    processed.Add(s1);
                    processed.Add(s2);
                    doorsSpawned++;
                }
                else
                {
                    // If unmatched but marked used, it's either an error or an outer door (airlock).
                    // We'll spawn a door anyway to seal the ship.
                    GameObject doorObj = Object.Instantiate(doorPrefab, s1.WorldPos, s1.WorldRot);
                    doorObj.name = $"Door_{s1.Room.name}_Airlock";
                    doorObj.transform.SetParent(s1.Room.transform);

                    var doorController = doorObj.GetComponent<DoorController>();
                    if (doorController == null) doorController = doorObj.AddComponent<DoorController>();

                    doorController.Initialize(s1.Room, null);

                    var netObj = doorObj.GetComponent<NetworkObject>();
                    if (netObj != null) netObj.Spawn();

                    processed.Add(s1);
                    doorsSpawned++;
                }
            }
            
            Debug.Log($"[DoorGenerator] Spawned {doorsSpawned} doors.");
        }
    }
}
