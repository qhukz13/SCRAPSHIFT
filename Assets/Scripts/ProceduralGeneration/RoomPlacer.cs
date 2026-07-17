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
        
        private List<RoomInstance> placed = new List<RoomInstance>();

        private int currentIterations = 0;
        private const int MaxIterations = 200000;

        public RoomPlacer(Transform root, RoomDatabase db, GenerationSettings genSettings)
        {
            shipRoot = root;
            roomDatabase = db;
            settings = genSettings;
        }

        public bool PlaceRooms(RoomGraph graph, out List<RoomInstance> placedRooms)
        {
            currentIterations = 0;
            placedRooms = new List<RoomInstance>();
            placed.Clear();
            
            var nodes = graph.GetNodes();
            if (nodes.Count == 0) return false;

            var sortedNodes = nodes.OrderBy(n => n.Depth).ToList();
            Dictionary<RoomNode, RoomInstance> nodeToRoom = new Dictionary<RoomNode, RoomInstance>();

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

                RoomDefinition roomDef = Object.Instantiate(prefabEntry.Prefab, shipRoot);
                roomDef.name = $"{node.RoomType}_{node.NodeID}";
                
                RoomInstance roomInstance = roomDef.gameObject.AddComponent<RoomInstance>();
                roomInstance.Initialize(roomDef, node.Floor);

                nodeToRoom[node] = roomInstance;
            }

            // 2. Perform Backtracking DFS to find a valid layout
            if (!DFS(0, sortedNodes, nodeToRoom))
            {
                Debug.LogError("[RoomPlacer] Mathematical layout failed. No valid non-overlapping configuration found.");
                Cleanup(nodeToRoom);
                return false;
            }

            CreateDynamicCycles();
            ProcessWallPlugs();
            placedRooms.AddRange(placed);
            return true;
        }

        private void CreateDynamicCycles()
        {
            // O(N^2) search for adjacent unused sockets that can be linked to form cycles (loops) in the graph.
            for (int i = 0; i < placed.Count; i++)
            {
                for (int j = i + 1; j < placed.Count; j++)
                {
                    var roomA = placed[i];
                    var roomB = placed[j];
                    
                    foreach (var sockA in roomA.Definition.DoorSockets)
                    {
                        if (sockA.IsUsed) continue;
                        Vector3 posA = roomA.transform.TransformPoint(sockA.LocalPosition);
                        Vector3 dirA = roomA.transform.TransformDirection(sockA.LocalDirection);

                        foreach (var sockB in roomB.Definition.DoorSockets)
                        {
                            if (sockB.IsUsed) continue;
                            Vector3 posB = roomB.transform.TransformPoint(sockB.LocalPosition);
                            Vector3 dirB = roomB.transform.TransformDirection(sockB.LocalDirection);

                            // Sockets must be touching (distance < 0.1) and facing opposite directions (dot < -0.9)
                            if (Vector3.Distance(posA, posB) < 0.1f && Vector3.Dot(dirA, dirB) < -0.9f)
                            {
                                sockA.IsUsed = true;
                                sockB.IsUsed = true;
                                // Break outer loop to move to the next unused socket in Room A
                                // so we don't accidentally link one socket to multiple others if they happen to overlap
                                goto NextSockA;
                            }
                        }
                        NextSockA:;
                    }
                }
            }
        }

        private void ProcessWallPlugs()
        {
            foreach (var room in placed)
            {
                Transform visuals = room.transform.Find("Visuals");
                if (visuals == null) continue;

                foreach (var socket in room.Definition.DoorSockets)
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

        private void Cleanup(Dictionary<RoomNode, RoomInstance> nodeToRoom)
        {
            foreach (var kvp in nodeToRoom)
            {
                if (kvp.Value != null) Object.Destroy(kvp.Value.gameObject);
            }
            nodeToRoom.Clear();
            placed.Clear();
        }

        private bool DFS(int index, List<RoomNode> sortedNodes, Dictionary<RoomNode, RoomInstance> nodeToRoom)
        {
            currentIterations++;
            if (currentIterations > MaxIterations)
            {
                return false; // Fail fast if we're caught in a combinatorial explosion
            }

            if (index >= sortedNodes.Count) return true; // All placed!

            var node = sortedNodes[index];
            var newRoom = nodeToRoom[node];

            if (node.ParentNode == null)
            {
                newRoom.transform.localPosition = new Vector3(0, (node.Floor - 1) * settings.FloorHeight, 0);
                newRoom.transform.localRotation = Quaternion.identity;
                placed.Add(newRoom);

                if (DFS(index + 1, sortedNodes, nodeToRoom)) return true;

                placed.Remove(newRoom);
                return false;
            }

            var parentRoom = nodeToRoom[node.ParentNode];
            
            // Favor outward growth but with some randomness to keep it organic and avoid straight lines.
            var parentSockets = parentRoom.Definition.DoorSockets.OrderBy(s => 
            {
                Vector3 socketWorldPos = parentRoom.transform.TransformPoint(s.LocalPosition);
                return -socketWorldPos.magnitude + (UnityEngine.Random.value * 15f);
            }).ToList();

            var childSockets = newRoom.Definition.DoorSockets.OrderBy(s => 
            {
                // We want the room's Z-axis to align with the placement direction
                return -Mathf.Abs(s.LocalDirection.z) + (UnityEngine.Random.value * 0.1f);
            }).ToList();

            foreach (var parentSocket in parentSockets)
            {
                if (currentIterations > MaxIterations) return false; // Abort instantly if limit reached
                if (parentSocket.IsUsed) continue;

                foreach (var childSocket in childSockets)
                {
                    if (currentIterations > MaxIterations) return false; // Abort instantly if limit reached
                    if (childSocket.IsUsed) continue;

                    Vector3 parentSocketWorldDir = parentRoom.transform.TransformDirection(parentSocket.LocalDirection);
                    Vector3 targetChildDir = -parentSocketWorldDir;
                    
                    float angle = Vector3.SignedAngle(childSocket.LocalDirection, targetChildDir, Vector3.up);
                    newRoom.transform.rotation = Quaternion.Euler(0, angle, 0);

                    Vector3 parentSocketWorldPos = parentRoom.transform.TransformPoint(parentSocket.LocalPosition);
                    Vector3 childSocketWorldPos = newRoom.transform.TransformPoint(childSocket.LocalPosition);
                    
                    Vector3 offset = parentSocketWorldPos - childSocketWorldPos;
                    newRoom.transform.position += offset;

                    // Calculate expected Y based on floor level
                    float expectedChildY = (node.Floor - 1) * settings.FloorHeight;
                    
                    // Reject this socket pair if their vertical alignment contradicts the floor layout
                    // i.e., childRoom's Y position after socket alignment is fundamentally different from its designated floor Y
                    if (Mathf.Abs(newRoom.transform.position.y - expectedChildY) > 0.5f)
                    {
                        continue;
                    }

                    // Enforce strict multi-deck floor height separation
                    Vector3 strictPos = newRoom.transform.position;
                    strictPos.y = expectedChildY;
                    newRoom.transform.position = strictPos;

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

        private bool CheckCollision(RoomInstance newRoom)
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

        private Bounds GetWorldBounds(RoomInstance room, Vector3 shrinkVector)
        {
            // Instead of just passing size, we create bounds with original size and center, 
            // then get the min and max points, transform them, and create a new bounds.
            // But since bounds are AABB, the easiest way is:
            Vector3 center = room.transform.TransformPoint(room.Definition.RoomBounds.center);
            Vector3 extents = room.Definition.RoomBounds.extents;
            
            // Transform all 8 corners of the local bounds to world space to get the true AABB
            Vector3[] corners = new Vector3[8];
            corners[0] = room.transform.TransformPoint(room.Definition.RoomBounds.center + new Vector3(extents.x, extents.y, extents.z));
            corners[1] = room.transform.TransformPoint(room.Definition.RoomBounds.center + new Vector3(extents.x, extents.y, -extents.z));
            corners[2] = room.transform.TransformPoint(room.Definition.RoomBounds.center + new Vector3(extents.x, -extents.y, extents.z));
            corners[3] = room.transform.TransformPoint(room.Definition.RoomBounds.center + new Vector3(extents.x, -extents.y, -extents.z));
            corners[4] = room.transform.TransformPoint(room.Definition.RoomBounds.center + new Vector3(-extents.x, extents.y, extents.z));
            corners[5] = room.transform.TransformPoint(room.Definition.RoomBounds.center + new Vector3(-extents.x, extents.y, -extents.z));
            corners[6] = room.transform.TransformPoint(room.Definition.RoomBounds.center + new Vector3(-extents.x, -extents.y, extents.z));
            corners[7] = room.transform.TransformPoint(room.Definition.RoomBounds.center + new Vector3(-extents.x, -extents.y, -extents.z));

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
