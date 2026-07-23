using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProceduralGeneration
{
    public class RoomGraph
    {
        private List<RoomNode> nodes = new List<RoomNode>();
        private int nextNodeId = 0;

        public RoomNode AddNode(RoomType type, int floor, RoomNode parent = null)
        {
            RoomNode newNode = new RoomNode(nextNodeId++, type, floor, parent);
            nodes.Add(newNode);
            return newNode;
        }

        public void AddConnection(RoomNode nodeA, RoomNode nodeB)
        {
            nodeA.ConnectTo(nodeB);
        }

        public IReadOnlyList<RoomNode> GetNodes()
        {
            return nodes;
        }

        public void Clear()
        {
            nodes.Clear();
            nextNodeId = 0;
        }
        
        private int GetMaxSockets(RoomType type)
        {
            switch (type)
            {
                case RoomType.Reactor: return 8; // Central hub, many doors
                case RoomType.Generator: return 2; // Often a pass-through
                case RoomType.Crossroad: return 4;
                // Stairs only have 2 sockets per floor. 1 is for parent, 1 is for the opposite floor.
                // We must not allow random same-floor rooms to attach to Stairs, or it runs out of sockets.
                case RoomType.Stairs: return 2; 
                case RoomType.Corridor: return 2;
                case RoomType.Spawn: return 1;
                case RoomType.Bridge: return 1;
                default: return 3; // Generic rooms can have up to 3 doors to allow organic branching
            }
        }

        public void GenerateFromTemplate(ShipTemplate template)
        {
            Clear();
            
            int targetRooms = template.MaximumRooms;
            
            // 1. Create Central Hub (Reactor)
            RoomNode reactorNode = AddNode(RoomType.Reactor, 1);

            int f1Length = Mathf.Max(targetRooms / 4, 3);
            
            // 2. Generate Floor 1 Spine (towards Spawn)
            RoomNode currentF1 = reactorNode;
            for (int i = 0; i < f1Length; i++)
            {
                RoomType type = (i % 2 == 0) ? RoomType.Corridor : RoomType.Crossroad;
                currentF1 = AddNode(type, 1, currentF1);
            }
            AddNode(RoomType.Spawn, 1, currentF1);

            // Calculate free sockets for each node initially
            Dictionary<RoomNode, int> freeSockets = new Dictionary<RoomNode, int>();
            foreach (var node in nodes)
            {
                int used = node.ConnectedNodes.Count;
                freeSockets[node] = GetMaxSockets(node.RoomType) - used;
            }

            // 3. Generate Floor 2 Spines (towards Bridge) OR alternative Floor 1 Spine
            int floor2 = template.NumberOfFloors > 1 ? 2 : 1;
            
            if (floor2 == 2)
            {
                // Generate 3-4 stairs on F1 that point up to F2.
                // We will connect them dynamically via A* in RoomPlacer later.
                int numStairs = UnityEngine.Random.Range(3, 5);
                List<RoomNode> placedStairs = new List<RoomNode>();

                for (int s = 0; s < numStairs; s++)
                {
                    // To keep stairs spaced out, pick nodes on F1 with high depth and avoid clustering
                    var availableF1 = nodes.Where(n => n.Floor == 1 && freeSockets[n] > 0 
                        && !n.ConnectedNodes.Any(c => c.RoomType == RoomType.Stairs))
                        .OrderByDescending(n => n.Depth)
                        .ToList();
                    
                    if (availableF1.Count == 0) break;
                    
                    // Pick from the furthest half
                    int maxIndex = Mathf.Max(1, availableF1.Count / 2);
                    var parentNodeF1 = availableF1[UnityEngine.Random.Range(0, maxIndex)];
                    
                    // Assign Floor=1 so RoomPlacer positions the stairs at Y=0, connecting via its bottom socket.
                    var stairNode = AddNode(RoomType.Stairs, 1, parentNodeF1);
                    freeSockets[parentNodeF1]--;
                    freeSockets[stairNode] = GetMaxSockets(stairNode.RoomType) - 1;
                    placedStairs.Add(stairNode);
                }
            }
            else
            {
                RoomNode currentF2 = reactorNode;
                for (int i = 0; i < f1Length; i++)
                {
                    RoomType type = (i % 2 == 0) ? RoomType.Corridor : RoomType.Crossroad;
                    var nextNode = AddNode(type, 1, currentF2);
                    freeSockets[currentF2]--;
                    freeSockets[nextNode] = GetMaxSockets(nextNode.RoomType) - 1;
                    currentF2 = nextNode;
                }
                var bridge = AddNode(RoomType.Bridge, 1, currentF2);
                freeSockets[currentF2]--;
                freeSockets[bridge] = GetMaxSockets(bridge.RoomType) - 1;
            }

            // 4. (Stairs are now explicitly placed in the graph for F2 transitions)

            // 5. Attach other required rooms
            List<RoomType> placedReqTypes = new List<RoomType> { RoomType.Spawn, RoomType.Bridge, RoomType.Reactor, RoomType.Corridor, RoomType.Crossroad, RoomType.Stairs };
            int reqCount = 0;
            foreach (var reqType in template.RequiredRooms)
            {
                if (placedReqTypes.Contains(reqType)) continue;
                
                var availablePoints = nodes.Where(n => freeSockets[n] > 0).ToList();
                if (availablePoints.Count == 0) break;
                
                int targetFloor = (reqCount % 2 == 0) ? 1 : 2;
                if (template.NumberOfFloors == 1) targetFloor = 1;
                
                var floorPoints = availablePoints.Where(n => n.Floor == targetFloor).ToList();
                if (floorPoints.Count == 0) floorPoints = availablePoints; // Fallback
                
                var branchPoint = floorPoints[UnityEngine.Random.Range(0, floorPoints.Count)];
                var newNode = AddNode(reqType, branchPoint.Floor, branchPoint);
                freeSockets[branchPoint]--;
                freeSockets[newNode] = GetMaxSockets(newNode.RoomType) - 1;
                placedReqTypes.Add(reqType);
                reqCount++;
            }

            // Ensure Generator is placed
            if (!placedReqTypes.Contains(RoomType.Generator))
            {
                var availablePoints = nodes.Where(n => freeSockets[n] > 0).ToList();
                if (availablePoints.Count > 0)
                {
                    var branchPoint = availablePoints[UnityEngine.Random.Range(0, availablePoints.Count)];
                    var genNode = AddNode(RoomType.Generator, branchPoint.Floor, branchPoint);
                    freeSockets[branchPoint]--;
                    freeSockets[genNode] = GetMaxSockets(genNode.RoomType) - 1;
                }
            }

            // 6. Fill remaining capacity with Optional Rooms
            int currentRooms = nodes.Count;
            while (currentRooms < targetRooms && template.OptionalRooms.Count > 0)
            {
                var availablePoints = nodes.Where(n => freeSockets[n] > 0).ToList();
                if (availablePoints.Count == 0) break; // Cannot grow anymore
                
                int targetFloor = (currentRooms % 2 == 0) ? 1 : 2;
                if (template.NumberOfFloors == 1) targetFloor = 1;
                
                var floorPoints = availablePoints.Where(n => n.Floor == targetFloor).ToList();
                if (floorPoints.Count == 0) floorPoints = availablePoints; // Fallback
                
                var randomOpt = template.OptionalRooms[UnityEngine.Random.Range(0, template.OptionalRooms.Count)];
                var branchPoint = floorPoints[UnityEngine.Random.Range(0, floorPoints.Count)];
                
                var newNode = AddNode(randomOpt.RoomType, branchPoint.Floor, branchPoint);
                freeSockets[branchPoint]--;
                freeSockets[newNode] = GetMaxSockets(newNode.RoomType) - 1;
                
                currentRooms++;
            }
        }


    }
}
