using System;
using System.Collections.Generic;
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
        
        public void GenerateFromTemplate(ShipTemplate template)
        {
            Clear();
            
            // 1. Determine Spine Length
            int targetRooms = template.MaximumRooms;
            int spineLength = Mathf.Clamp(targetRooms / 2, 3, 15);
            
            RoomNode currentSpineNode = AddNode(RoomType.Spawn, 1);
            List<RoomNode> spineNodes = new List<RoomNode>();
            spineNodes.Add(currentSpineNode);

            // Generate Spine
            for (int i = 0; i < spineLength; i++)
            {
                // Mostly Corridors, occasionally Crossroads
                RoomType spineType = (i % 3 == 0 && i > 0) ? RoomType.Crossroad : RoomType.Corridor;
                currentSpineNode = AddNode(spineType, 1, currentSpineNode);
                spineNodes.Add(currentSpineNode);
            }

            // Put Bridge at the end
            RoomNode bridgeNode = AddNode(RoomType.Bridge, 1, currentSpineNode);
            spineNodes.Add(bridgeNode);

            // 2. We need Generator and Reactor. We'll attach them to the middle of the spine.
            RoomNode genAttach = spineNodes[Mathf.Max(1, spineNodes.Count / 3)];
            RoomNode reactorAttach = spineNodes[Mathf.Max(1, (spineNodes.Count * 2) / 3)];
            
            AddNode(RoomType.Generator, 1, genAttach);
            AddNode(RoomType.Reactor, 1, reactorAttach);

            // 3. Attach other required rooms to the spine
            List<RoomType> placedReqTypes = new List<RoomType> { RoomType.Spawn, RoomType.Bridge, RoomType.Generator, RoomType.Reactor, RoomType.Corridor, RoomType.Crossroad };
            foreach (var reqType in template.RequiredRooms)
            {
                if (placedReqTypes.Contains(reqType)) continue;
                RoomNode randomSpine = spineNodes[UnityEngine.Random.Range(1, spineNodes.Count - 1)];
                AddNode(reqType, 1, randomSpine);
                placedReqTypes.Add(reqType);
            }

            // 4. Fill remaining capacity with Optional Rooms attached to the spine
            int currentRooms = nodes.Count;
            List<RoomNode> branchPoints = new List<RoomNode>(spineNodes);
            // Don't branch from spawn or bridge
            branchPoints.Remove(spineNodes[0]);
            branchPoints.Remove(bridgeNode);

            while (currentRooms < targetRooms && template.OptionalRooms.Count > 0)
            {
                var randomOpt = template.OptionalRooms[UnityEngine.Random.Range(0, template.OptionalRooms.Count)];
                
                RoomNode branchPoint = branchPoints[UnityEngine.Random.Range(0, branchPoints.Count)];
                RoomNode newNode = AddNode(randomOpt.RoomType, 1, branchPoint);
                
                // Allow branching off new corridors to create depth, but limit it
                if (randomOpt.RoomType == RoomType.Corridor || randomOpt.RoomType == RoomType.Crossroad)
                {
                    branchPoints.Add(newNode);
                }
                
                currentRooms++;
            }
        }
    }
}
