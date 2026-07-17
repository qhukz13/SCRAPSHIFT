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

            var sortedNodes = nodes.OrderBy(n => n.Depth).ToList();
            Dictionary<RoomNode, RoomDefinition> nodeToRoom = new Dictionary<RoomNode, RoomDefinition>();

            // 1. Instantiate all prefabs at origin
            foreach (var node in sortedNodes)
            {
                var prefabEntry = roomDatabase.Rooms.FirstOrDefault(r => r.RoomType == node.RoomType);
                if (prefabEntry == null || prefabEntry.Prefab == null)
                {
                    Debug.LogError($"[RoomPlacer] No prefab found for {node.RoomType}");
                    Cleanup(nodeToRoom);
                    return false;
                }

                RoomDefinition roomInstance = Object.Instantiate(prefabEntry.Prefab, shipRoot);
                roomInstance.name = $"{node.RoomType}_{node.NodeID}";
                nodeToRoom[node] = roomInstance;
            }

            // 2. Perform Backtracking DFS to find a valid layout
            if (!DFS(0, sortedNodes, nodeToRoom))
            {
                Debug.LogError("[RoomPlacer] Mathematical layout failed. No valid non-overlapping configuration found.");
                Cleanup(nodeToRoom);
                return false;
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

        private void Cleanup(Dictionary<RoomNode, RoomDefinition> nodeToRoom)
        {
            foreach (var kvp in nodeToRoom)
            {
                if (kvp.Value != null) Object.Destroy(kvp.Value.gameObject);
            }
            nodeToRoom.Clear();
            placed.Clear();
        }

        private bool DFS(int index, List<RoomNode> sortedNodes, Dictionary<RoomNode, RoomDefinition> nodeToRoom)
        {
            if (index >= sortedNodes.Count) return true; // All placed!

            var node = sortedNodes[index];
            var newRoom = nodeToRoom[node];

            if (node.ParentNode == null)
            {
                newRoom.transform.localPosition = Vector3.zero;
                newRoom.transform.localRotation = Quaternion.identity;
                placed.Add(newRoom);

                if (DFS(index + 1, sortedNodes, nodeToRoom)) return true;

                placed.Remove(newRoom);
                return false;
            }

            var parentRoom = nodeToRoom[node.ParentNode];
            
            // Prioritize Forward sockets to make the ship linear and logical
            var parentSockets = parentRoom.DoorSockets.OrderBy(s => 
            {
                Vector3 worldDir = parentRoom.transform.TransformDirection(s.LocalDirection);
                float alignment = Vector3.Dot(worldDir, parentRoom.transform.forward);
                return -alignment + (UnityEngine.Random.value * 0.1f);
            }).ToList();

            var childSockets = newRoom.DoorSockets.OrderBy(s => UnityEngine.Random.value).ToList();

            foreach (var parentSocket in parentSockets)
            {
                if (parentSocket.IsUsed) continue;

                foreach (var childSocket in childSockets)
                {
                    if (childSocket.IsUsed) continue;

                    Vector3 parentSocketWorldDir = parentRoom.transform.TransformDirection(parentSocket.LocalDirection);
                    Vector3 targetChildDir = -parentSocketWorldDir;
                    
                    float angle = Vector3.SignedAngle(childSocket.LocalDirection, targetChildDir, Vector3.up);
                    newRoom.transform.rotation = Quaternion.Euler(0, angle, 0);

                    Vector3 parentSocketWorldPos = parentRoom.transform.TransformPoint(parentSocket.LocalPosition);
                    Vector3 childSocketWorldPos = newRoom.transform.TransformPoint(childSocket.LocalPosition);
                    
                    Vector3 offset = parentSocketWorldPos - childSocketWorldPos;
                    newRoom.transform.position += offset;

                    if (!CheckCollision(newRoom))
                    {
                        parentSocket.IsUsed = true;
                        childSocket.IsUsed = true;
                        placed.Add(newRoom);

                        if (DFS(index + 1, sortedNodes, nodeToRoom)) return true;

                        // Backtrack
                        placed.Remove(newRoom);
                        parentSocket.IsUsed = false;
                        childSocket.IsUsed = false;
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
