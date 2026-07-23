using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ProceduralGeneration
{
    public static class AStarF2
    {
        private class Node
        {
            public Vector3 Position;
            public Node Parent;
            public float G; // Cost from start
            public float H; // Heuristic to end
            public float F => G + H;
        }

        public static void ConnectF2(RoomPlacer placer, List<RoomInstance> placed, RoomDatabase db, GenerationSettings settings, Transform shipRoot)
        {
            RoomInstance reactor = placed.FirstOrDefault(r => r.Definition.RoomType == RoomType.Reactor);
            if (reactor == null) return;

            var stairs = placed.Where(r => r.Definition.RoomType == RoomType.Stairs).ToList();
            if (stairs.Count == 0) return;

            var corridorDefEntry = db.Rooms.FirstOrDefault(r => r.RoomType == RoomType.Corridor);
            if (corridorDefEntry == null) return;
            var corridorDef = corridorDefEntry.Prefab;

            // Target positions are the empty spaces directly outside the Reactor's F2 sockets
            List<Vector3> targets = new List<Vector3>();
            foreach (var sock in reactor.Definition.DoorSockets)
            {
                // The socket might not explicitly say Floor=2 if it's just positioned at Y=10.
                if (sock.LocalPosition.y > 5f && !sock.IsUsed)
                {
                    Vector3 worldPos = reactor.transform.TransformPoint(sock.LocalPosition);
                    Vector3 forward = reactor.transform.TransformDirection(sock.LocalDirection);
                    // The center of the 10x10 space outside the socket
                    targets.Add(worldPos + forward * 5f);
                }
            }
            if (targets.Count == 0) return;

            // We consider any already connected F2 space as a target to allow branching
            List<Vector3> connectedNetwork = new List<Vector3>(targets);

            foreach (var stair in stairs)
            {
                // Find the stair's top socket
                DoorSocket topSock = stair.Definition.DoorSockets.FirstOrDefault(s => s.LocalPosition.y > 5f && !s.IsUsed);
                if (topSock == null) continue;

                Vector3 startPos = stair.transform.TransformPoint(topSock.LocalPosition);
                Vector3 startForward = stair.transform.TransformDirection(topSock.LocalDirection);
                Vector3 startCenter = startPos + startForward * 5f;

                // Pathfind from startCenter to ANY point in connectedNetwork
                List<Vector3> path = FindPath(startCenter, connectedNetwork, placer, corridorDef, settings);
                if (path != null && path.Count > 0)
                {
                    // Place corridors
                    Vector3 lastPlacedPos = new Vector3(-9999, -9999, -9999);
                    for (int i = 0; i < path.Count; i++)
                    {
                        var pt = path[i];
                        
                        bool isLast = (i == path.Count - 1);
                        if (Vector3.Distance(pt, lastPlacedPos) >= 9f || isLast)
                        {
                            var newRoomDef = Object.Instantiate(corridorDef, shipRoot);
                            newRoomDef.name = "Corridor_AStar";
                            var newRoom = newRoomDef.gameObject.AddComponent<RoomInstance>();
                            newRoom.Initialize(newRoomDef, 2);
                            newRoom.transform.position = pt;
                            
                            if (i == 0)
                            {
                                newRoom.transform.rotation = Quaternion.LookRotation(startForward.normalized);
                            }
                            else
                            {
                                Vector3 dir = pt - path[i-1];
                                if (dir.sqrMagnitude > 0.1f)
                                {
                                    newRoom.transform.rotation = Quaternion.LookRotation(dir.normalized);
                                }
                            }
                            
                            placed.Add(newRoom);
                            placer.GetPlacedRooms().Add(newRoom);
                            lastPlacedPos = pt;
                        }
                    }

                    // Add placed to network
                    connectedNetwork.AddRange(path);
                }
            }
        }

        private static List<Vector3> FindPath(Vector3 start, List<Vector3> targets, RoomPlacer placer, RoomDefinition corridorDef, GenerationSettings settings)
        {
            var openList = new List<Node>();
            var closedSet = new HashSet<Vector3>();

            openList.Add(new Node { Position = start, G = 0, H = GetMinDistance(start, targets), Parent = null });

            int maxIterations = 10000;
            int iter = 0;

            Vector3[] directions = { new Vector3(5, 0, 0), new Vector3(-5, 0, 0), new Vector3(0, 0, 5), new Vector3(0, 0, -5) };

            while (openList.Count > 0 && iter < maxIterations)
            {
                iter++;
                openList.Sort((a, b) => a.F.CompareTo(b.F));
                var current = openList[0];
                openList.RemoveAt(0);

                // Check if we reached a target
                if (targets.Any(t => Vector3.Distance(current.Position, t) < 1f))
                {
                    var path = new List<Vector3>();
                    var currNode = current;
                    while (currNode != null)
                    {
                        path.Add(currNode.Position);
                        currNode = currNode.Parent;
                    }
                    path.Reverse();
                    return path;
                }

                closedSet.Add(current.Position);

                foreach (var dir in directions)
                {
                    Vector3 neighborPos = current.Position + dir;
                    if (closedSet.Contains(neighborPos)) continue;

                    // Collision check
                    if (CheckCollisionForPos(neighborPos, corridorDef, placer, settings))
                    {
                        continue;
                    }

                    float tentativeG = current.G + 10f;
                    var neighborNode = openList.FirstOrDefault(n => Vector3.Distance(n.Position, neighborPos) < 1f);

                    if (neighborNode == null)
                    {
                        openList.Add(new Node
                        {
                            Position = neighborPos,
                            Parent = current,
                            G = tentativeG,
                            H = GetMinDistance(neighborPos, targets)
                        });
                    }
                    else if (tentativeG < neighborNode.G)
                    {
                        neighborNode.Parent = current;
                        neighborNode.G = tentativeG;
                    }
                }
            }

            return null; // No path found
        }

        private static float GetMinDistance(Vector3 pos, List<Vector3> targets)
        {
            float min = float.MaxValue;
            foreach (var t in targets)
            {
                float d = Vector3.Distance(pos, t);
                if (d < min) min = d;
            }
            return min;
        }

        private static bool CheckCollisionForPos(Vector3 pos, RoomDefinition def, RoomPlacer placer, GenerationSettings settings)
        {
            float padding = Mathf.Max(0.1f, settings.RoomPadding);
            Vector3 shrinkVector = new Vector3(padding, padding, padding);

            Bounds newBounds = placer.GetWorldBoundsForDef(def, shrinkVector, pos, Quaternion.identity);

            foreach (var existing in placer.GetPlacedRooms())
            {
                Bounds existingBounds = placer.GetWorldBoundsForInstance(existing, shrinkVector);
                if (newBounds.Intersects(existingBounds))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
