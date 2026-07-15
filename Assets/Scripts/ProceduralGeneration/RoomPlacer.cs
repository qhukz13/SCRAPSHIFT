using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProceduralGeneration
{
    public class RoomPlacer
    {
        private Transform shipRoot;
        private RoomDatabase roomDatabase;
        private GenerationSettings settings;
        
        private List<RoomDefinition> placed = new List<RoomDefinition>();

        public RoomPlacer(Transform root, RoomDatabase db, GenerationSettings genSettings)
        {
            shipRoot = root;
            roomDatabase = db;
            settings = genSettings;
        }

        public bool PlaceRooms(RoomGraph graph, out List<RoomDefinition> placedRooms)
        {
            placedRooms = new List<RoomDefinition>();
            placed.Clear();
            
            var nodes = graph.GetNodes();
            if (nodes.Count == 0) return false;

            // Sort nodes by depth to ensure we build outwards
            var sortedNodes = nodes.OrderBy(n => n.Depth).ToList();

            // Store mapping from Node to instantiated Room
            Dictionary<RoomNode, RoomDefinition> nodeToRoom = new Dictionary<RoomNode, RoomDefinition>();

            foreach (var node in sortedNodes)
            {
                // Find matching prefab
                var prefabEntry = roomDatabase.Rooms.FirstOrDefault(r => r.RoomType == node.RoomType);
                if (prefabEntry == null || prefabEntry.Prefab == null)
                {
                    Debug.LogError($"[RoomPlacer] No prefab found for {node.RoomType}");
                    return false;
                }

                RoomDefinition roomInstance = Object.Instantiate(prefabEntry.Prefab, shipRoot);
                roomInstance.name = $"{node.RoomType}_{node.NodeID}";

                if (node.ParentNode == null)
                {
                    // This is the Spawn / Root node
                    roomInstance.transform.localPosition = Vector3.zero;
                    roomInstance.transform.localRotation = Quaternion.identity;
                }
                else
                {
                    // Find the physical room for the parent node
                    if (!nodeToRoom.TryGetValue(node.ParentNode, out RoomDefinition parentRoom))
                    {
                        Debug.LogError($"[RoomPlacer] Parent room not instantiated yet for node {node.NodeID}");
                        Object.Destroy(roomInstance.gameObject);
                        return false;
                    }

                    bool placedSuccessfully = TryPlaceRoom(roomInstance, parentRoom);
                    
                    if (!placedSuccessfully)
                    {
                        Debug.LogWarning($"[RoomPlacer] Could not fit room {node.RoomType} without overlapping. Backtracking logic needed.");
                        Object.Destroy(roomInstance.gameObject);
                        
                        // We must destroy already placed rooms to avoid leaving ghostly colliders
                        foreach (var r in placed)
                        {
                            if (r != null) Object.Destroy(r.gameObject);
                        }
                        placed.Clear();
                        
                        return false; // Very simple fail state for now
                    }
                }

                placed.Add(roomInstance);
                nodeToRoom[node] = roomInstance;
            }

            ProcessWallPlugs();

            placedRooms.AddRange(placed);
            return true;
        }

        private void ProcessWallPlugs()
        {
            foreach (var room in placed)
            {
                Transform visuals = room.transform.Find("Visuals");
                if (visuals == null) continue;

                foreach (var socket in room.DoorSockets)
                {
                    if (socket.IsUsed)
                    {
                        foreach (Transform child in visuals)
                        {
                            if (child.name.StartsWith("SocketPlug_"))
                            {
                                // Plugs are spawned with Y=2 (doorHeight/2), socket is Y=0.
                                // Distance is exactly 2.0f.
                                if (Vector3.Distance(child.localPosition, socket.LocalPosition) < 2.5f)
                                {
                                    Object.Destroy(child.gameObject);
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool TryPlaceRoom(RoomDefinition newRoom, RoomDefinition parentRoom)
        {
            // Simple placement logic: try all unused sockets on parent and all unused sockets on new room
            foreach (var parentSocket in parentRoom.DoorSockets)
            {
                // Skip if already connected
                if (parentSocket.IsUsed) continue;

                foreach (var childSocket in newRoom.DoorSockets)
                {
                    if (childSocket.IsUsed) continue;

                    // Calculate rotation to make child socket face opposite of parent socket
                    Vector3 parentSocketWorldDir = parentRoom.transform.TransformDirection(parentSocket.LocalDirection);
                    Vector3 targetChildDir = -parentSocketWorldDir;
                    
                    // targetChildDir is already in world space, so we just need a rotation that transforms local childSocket to world targetChildDir.
                    // We must only rotate around the Y axis to keep the room upright.
                    float angle = Vector3.SignedAngle(childSocket.LocalDirection, targetChildDir, Vector3.up);
                    newRoom.transform.rotation = Quaternion.Euler(0, angle, 0);

                    // Calculate position
                    Vector3 parentSocketWorldPos = parentRoom.transform.TransformPoint(parentSocket.LocalPosition);
                    Vector3 childSocketWorldPos = newRoom.transform.TransformPoint(childSocket.LocalPosition);
                    
                    Vector3 offset = parentSocketWorldPos - childSocketWorldPos;
                    newRoom.transform.position += offset;

                    // Check bounds collision mathematically
                    if (!CheckCollision(newRoom))
                    {
                        // Mark both sockets as used
                        parentSocket.IsUsed = true;
                        childSocket.IsUsed = true;
                        return true; // Successfully placed
                    }
                }
            }
            
            return false;
        }

        private bool CheckCollision(RoomDefinition newRoom)
        {
            // Force a minimum padding even if inspector is set to 0
            float padding = Mathf.Max(0.1f, settings.RoomPadding);
            Vector3 shrinkVector = new Vector3(padding, padding, padding);

            // Correctly calculate world bounds taking rotation into account
            Bounds newBounds = GetWorldBounds(newRoom, shrinkVector);

            foreach (var existing in placed)
            {
                Bounds existingBounds = GetWorldBounds(existing, shrinkVector);

                if (newBounds.Intersects(existingBounds))
                {
                    return true; // Collision detected
                }
            }

            return false; // No collision
        }

        private Bounds GetWorldBounds(RoomDefinition room, Vector3 shrinkVector)
        {
            // Instead of just passing size, we create bounds with original size and center, 
            // then get the min and max points, transform them, and create a new bounds.
            // But since bounds are AABB, the easiest way is:
            Vector3 center = room.transform.TransformPoint(room.RoomBounds.center);
            Vector3 extents = room.RoomBounds.extents;
            
            // Transform all 8 corners of the local bounds to world space to get the true AABB
            Vector3[] corners = new Vector3[8];
            corners[0] = room.transform.TransformPoint(room.RoomBounds.center + new Vector3(extents.x, extents.y, extents.z));
            corners[1] = room.transform.TransformPoint(room.RoomBounds.center + new Vector3(extents.x, extents.y, -extents.z));
            corners[2] = room.transform.TransformPoint(room.RoomBounds.center + new Vector3(extents.x, -extents.y, extents.z));
            corners[3] = room.transform.TransformPoint(room.RoomBounds.center + new Vector3(extents.x, -extents.y, -extents.z));
            corners[4] = room.transform.TransformPoint(room.RoomBounds.center + new Vector3(-extents.x, extents.y, extents.z));
            corners[5] = room.transform.TransformPoint(room.RoomBounds.center + new Vector3(-extents.x, extents.y, -extents.z));
            corners[6] = room.transform.TransformPoint(room.RoomBounds.center + new Vector3(-extents.x, -extents.y, extents.z));
            corners[7] = room.transform.TransformPoint(room.RoomBounds.center + new Vector3(-extents.x, -extents.y, -extents.z));

            Bounds worldBounds = new Bounds(corners[0], Vector3.zero);
            for (int i = 1; i < 8; i++)
            {
                worldBounds.Encapsulate(corners[i]);
            }

            // Shrink the final world bounds to prevent "touching" from counting as intersection
            worldBounds.size -= shrinkVector;
            return worldBounds;
        }
    }
}
