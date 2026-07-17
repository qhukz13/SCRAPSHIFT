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

            CreateDynamicStairs();
            CreateDynamicCycles();
            ProcessWallPlugs();
            placedRooms.AddRange(placed);
            return true;
        }

        private void CreateDynamicStairs()
        {
            var stairsPrefabEntry = roomDatabase.Rooms.FirstOrDefault(r => r.RoomType == RoomType.Stairs);
            if (stairsPrefabEntry == null || stairsPrefabEntry.Prefab == null) return;
            var stairsPrefab = stairsPrefabEntry.Prefab;

            int targetStairs = UnityEngine.Random.Range(4, 7);
            int placedStairs = 0;
            List<Vector3> placedStairPositions = new List<Vector3>();
            float minStairDistance = 15f; // Minimum distance between any two stairs (about 1.5 rooms apart)

            var f1Sockets = new List<(RoomInstance room, DoorSocket sock, Vector3 pos, Vector3 dir)>();
            var f2Sockets = new List<(RoomInstance room, DoorSocket sock, Vector3 pos, Vector3 dir)>();

            foreach (var room in placed)
            {
                foreach (var sock in room.Definition.DoorSockets)
                {
                    if (sock.IsUsed) continue;
                    Vector3 pos = room.transform.TransformPoint(sock.LocalPosition);
                    Vector3 dir = room.transform.TransformDirection(sock.LocalDirection);
                    
                    if (room.Floor == 1) f1Sockets.Add((room, sock, pos, dir));
                    else if (room.Floor == 2) f2Sockets.Add((room, sock, pos, dir));
                }
            }

            // Shuffle sockets for organic distribution
            System.Random rng = new System.Random();
            f1Sockets = f1Sockets.OrderBy(a => rng.Next()).ToList();
            f2Sockets = f2Sockets.OrderBy(a => rng.Next()).ToList();

            foreach (var f1 in f1Sockets)
            {
                if (placedStairs >= targetStairs) break;
                if (f1.sock.IsUsed) continue;

                foreach (var f2 in f2Sockets)
                {
                    if (f2.sock.IsUsed) continue;

                    // Condition 1: Must face opposite directions
                    if (Vector3.Dot(f1.dir, f2.dir) < -0.99f)
                    {
                        // Condition 2: F2 must be directly above and 10 units forward relative to F1 socket
                        Vector3 expectedF2Pos = f1.pos + new Vector3(0, settings.FloorHeight, 0) + (f1.dir * 10f);

                        if (Vector3.Distance(f2.pos, expectedF2Pos) < 0.1f)
                        {
                            Vector3 stairPos = f1.pos + f1.dir * 5f;
                            
                            // Check distance from already placed stairs
                            bool tooClose = false;
                            foreach (var p in placedStairPositions)
                            {
                                if (Vector3.Distance(stairPos, p) < minStairDistance)
                                {
                                    tooClose = true;
                                    break;
                                }
                            }
                            if (tooClose) continue;

                            Quaternion stairRot = Quaternion.LookRotation(f1.dir);

                            // Verify collision before placing
                            RoomInstance dummyInst = stairsPrefab.gameObject.GetComponent<RoomInstance>();
                            if (dummyInst == null) dummyInst = stairsPrefab.gameObject.AddComponent<RoomInstance>();
                            
                            // Let's just instantiate it, test collision, and destroy if failed.
                            RoomDefinition stairDef = Object.Instantiate(stairsPrefab, shipRoot);
                            stairDef.name = $"Stairs_Dynamic_{placedStairs}";
                            RoomInstance stairInst = stairDef.gameObject.AddComponent<RoomInstance>();
                            stairInst.Initialize(stairDef, 1);
                            
                            if (!CheckCollision(stairInst, stairPos, stairRot))
                            {
                                stairInst.transform.position = stairPos;
                                stairInst.transform.rotation = stairRot;

                                f1.sock.IsUsed = true;
                                f2.sock.IsUsed = true;
                                foreach (var s in stairDef.DoorSockets) s.IsUsed = true;

                                placed.Add(stairInst);
                                placedStairPositions.Add(stairPos);
                                placedStairs++;
                                break;
                            }
                            else
                            {
                                Object.DestroyImmediate(stairDef.gameObject);
                            }
                        }
                    }
                }
            }
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

        private RoomInstance GetConnectedRoom(RoomInstance myRoom, DoorSocket mySocket)
        {
            Vector3 myWorldPos = myRoom.transform.TransformPoint(mySocket.LocalPosition);
            foreach (var otherRoom in placed)
            {
                if (otherRoom == myRoom) continue;
                foreach (var otherSocket in otherRoom.Definition.DoorSockets)
                {
                    if (!otherSocket.IsUsed) continue;
                    Vector3 otherWorldPos = otherRoom.transform.TransformPoint(otherSocket.LocalPosition);
                    if (Vector3.Distance(myWorldPos, otherWorldPos) < 0.1f)
                    {
                        return otherRoom;
                    }
                }
            }
            return null;
        }

        private void ProcessWallPlugs()
        {
            // First pass: remove standard plugs
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

            // Second pass: remove pillars between wide connections to merge doorways
            foreach (var room in placed)
            {
                Transform visuals = room.transform.Find("Visuals");
                if (visuals == null) continue;

                var usedSockets = room.Definition.DoorSockets.Where(s => s.IsUsed).ToList();
                for (int i = 0; i < usedSockets.Count; i++)
                {
                    for (int j = i + 1; j < usedSockets.Count; j++)
                    {
                        var sockA = usedSockets[i];
                        var sockB = usedSockets[j];

                        // Must be on the same floor (Y should be similar)
                        if (Mathf.Abs(sockA.LocalPosition.y - sockB.LocalPosition.y) > 0.1f) continue;

                        // Distance must be exactly 10 units (adjacent sockets on the same wall)
                        if (Mathf.Abs(Vector3.Distance(sockA.LocalPosition, sockB.LocalPosition) - 10f) < 0.1f)
                        {
                            // Must face the same direction (on the same wall)
                            if (Vector3.Dot(sockA.LocalDirection, sockB.LocalDirection) > 0.99f)
                            {
                                // Must connect to the same other room
                                RoomInstance connectedA = GetConnectedRoom(room, sockA);
                                RoomInstance connectedB = GetConnectedRoom(room, sockB);

                                if (connectedA != null && connectedA == connectedB)
                                {
                                    // They connect to the same room!
                                    // Find and replace the wall segment (pillar) between them.
                                    Vector3 midpoint = (sockA.LocalPosition + sockB.LocalPosition) / 2f;
                                    Vector3 tangent = (sockB.LocalPosition - sockA.LocalPosition).normalized;
                                    Vector3 tangentAbs = new Vector3(Mathf.Abs(tangent.x), Mathf.Abs(tangent.y), Mathf.Abs(tangent.z));
                                    Vector3 normalAbs = Vector3.one - tangentAbs;
                                    
                                    List<GameObject> toDestroy = new List<GameObject>();
                                    foreach (Transform child in visuals)
                                    {
                                        if (child.name.StartsWith("WallSegment"))
                                        {
                                            Vector2 childXZ = new Vector2(child.localPosition.x, child.localPosition.z);
                                            Vector2 midXZ = new Vector2(midpoint.x, midpoint.z);
                                            
                                            // If this wall segment is located exactly at the midpoint
                                            if (Vector2.Distance(childXZ, midXZ) < 0.5f)
                                            {
                                                toDestroy.Add(child.gameObject);
                                            }
                                        }
                                    }

                                    foreach (var obj in toDestroy)
                                    {
                                        // If it's a full-height pillar (height roughly 10)
                                        if (Mathf.Abs(obj.transform.localScale.y - 10f) < 0.5f)
                                        {
                                            // 1. Create a top cover to maintain the doorway arch over the center
                                            GameObject topCover = GameObject.Instantiate(obj, visuals, false);
                                            topCover.name = obj.name + "_MergedCover";
                                            // Shift up by 2 units, shrink height by 4 units
                                            topCover.transform.localPosition = obj.transform.localPosition + new Vector3(0, 2f, 0);
                                            topCover.transform.localScale = obj.transform.localScale - new Vector3(0, 4f, 0);
                                            
                                            // 2. Create left side wall to shrink the gap from 14 units to 8 units
                                            GameObject leftWall = GameObject.Instantiate(obj, visuals, false);
                                            leftWall.name = obj.name + "_MergedLeft";
                                            Vector3 leftPos = midpoint - tangent * 5.5f;
                                            leftPos.y = obj.transform.localPosition.y; // Keep original pillar Y height!
                                            leftWall.transform.localPosition = leftPos;
                                            leftWall.transform.localScale = Vector3.Scale(obj.transform.localScale, normalAbs) + Vector3.Scale(obj.transform.localScale, tangentAbs) * 0.5f;
                                            
                                            // 3. Create right side wall
                                            GameObject rightWall = GameObject.Instantiate(obj, visuals, false);
                                            rightWall.name = obj.name + "_MergedRight";
                                            Vector3 rightPos = midpoint + tangent * 5.5f;
                                            rightPos.y = obj.transform.localPosition.y; // Keep original pillar Y height!
                                            rightWall.transform.localPosition = rightPos;
                                            rightWall.transform.localScale = leftWall.transform.localScale;
                                        }
                                        
                                        Object.Destroy(obj);
                                    }
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
                    Quaternion testRotation = Quaternion.Euler(0, angle, 0);

                    Vector3 parentSocketWorldPos = parentRoom.transform.TransformPoint(parentSocket.LocalPosition);
                    
                    // We need to know where child socket would be if newRoom was at Vector3.zero with testRotation
                    Vector3 rotatedChildSocketPos = testRotation * childSocket.LocalPosition;
                    
                    Vector3 testPosition = parentSocketWorldPos - rotatedChildSocketPos;

                    // Calculate expected Y based on floor level
                    float expectedChildY = (node.Floor - 1) * settings.FloorHeight;
                    
                    // Reject this socket pair if their vertical alignment contradicts the floor layout
                    // i.e., childRoom's Y position after socket alignment is fundamentally different from its designated floor Y
                    if (Mathf.Abs(testPosition.y - expectedChildY) > 0.5f)
                    {
                        continue;
                    }

                    // Enforce strict multi-deck floor height separation
                    testPosition.y = expectedChildY;

                    if (!CheckCollision(newRoom, testPosition, testRotation))
                    {
                        newRoom.transform.position = testPosition;
                        newRoom.transform.rotation = testRotation;
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

        private bool CheckCollision(RoomInstance newRoom, Vector3 testPos, Quaternion testRot)
        {
            // Force a minimum padding even if inspector is set to 0
            float padding = Mathf.Max(0.1f, settings.RoomPadding);
            Vector3 shrinkVector = new Vector3(padding, padding, padding);

            // Correctly calculate world bounds taking rotation into account
            Bounds newBounds = GetWorldBounds(newRoom, shrinkVector, testPos, testRot);

            foreach (var existing in placed)
            {
                Bounds existingBounds = GetWorldBounds(existing, shrinkVector, existing.transform.position, existing.transform.rotation);

                if (newBounds.Intersects(existingBounds))
                {
                    return true; // Collision detected
                }
            }

            return false; // No collision
        }

        private Bounds GetWorldBounds(RoomInstance room, Vector3 shrinkVector, Vector3 pos, Quaternion rot)
        {
            // Instead of just passing size, we create bounds with original size and center, 
            // then get the min and max points, transform them, and create a new bounds.
            Vector3 center = pos + (rot * room.Definition.RoomBounds.center);
            Vector3 extents = room.Definition.RoomBounds.extents;
            
            // Transform all 8 corners of the local bounds to world space to get the true AABB
            Vector3[] corners = new Vector3[8];
            corners[0] = pos + (rot * (room.Definition.RoomBounds.center + new Vector3(extents.x, extents.y, extents.z)));
            corners[1] = pos + (rot * (room.Definition.RoomBounds.center + new Vector3(extents.x, extents.y, -extents.z)));
            corners[2] = pos + (rot * (room.Definition.RoomBounds.center + new Vector3(extents.x, -extents.y, extents.z)));
            corners[3] = pos + (rot * (room.Definition.RoomBounds.center + new Vector3(extents.x, -extents.y, -extents.z)));
            corners[4] = pos + (rot * (room.Definition.RoomBounds.center + new Vector3(-extents.x, extents.y, extents.z)));
            corners[5] = pos + (rot * (room.Definition.RoomBounds.center + new Vector3(-extents.x, extents.y, -extents.z)));
            corners[6] = pos + (rot * (room.Definition.RoomBounds.center + new Vector3(-extents.x, -extents.y, extents.z)));
            corners[7] = pos + (rot * (room.Definition.RoomBounds.center + new Vector3(-extents.x, -extents.y, -extents.z)));

            Vector3 min = corners[0];
            Vector3 max = corners[0];
            for (int i = 1; i < 8; i++)
            {
                min = Vector3.Min(min, corners[i]);
                max = Vector3.Max(max, corners[i]);
            }

            Bounds bounds = new Bounds((min + max) / 2f, max - min);
            
            // Apply shrink for padding
            bounds.Expand(-shrinkVector);
            
            return bounds;
        }
    }
}
