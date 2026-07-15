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
            
            // Generate basic Main Path: Spawn -> Corridor -> Reactor -> Corridor -> Bridge
            RoomNode spawnNode = AddNode(RoomType.Spawn, 1);
            RoomNode curr = spawnNode;
            
            // Very simple graph for now to ensure path
            curr = AddNode(RoomType.Corridor, 1, curr);
            curr = AddNode(RoomType.Crossroad, 1, curr);
            
            RoomNode crossroad = curr;
            
            curr = AddNode(RoomType.Corridor, 1, crossroad);
            RoomNode reactorNode = AddNode(RoomType.Reactor, 1, curr);
            curr = reactorNode;
            
            curr = AddNode(RoomType.Corridor, 1, crossroad);
            curr = AddNode(RoomType.Bridge, 1, curr);
            
            // Attach a Generator to the Reactor
            AddNode(RoomType.Generator, 1, reactorNode);
            
            
            // Add Required Rooms
            foreach (var reqType in template.RequiredRooms)
            {
                if (reqType == RoomType.Spawn || reqType == RoomType.Reactor || reqType == RoomType.Bridge)
                    continue; // already added manually above for guaranteed path
                    
                // Attach to crossroad for now
                AddNode(reqType, 1, crossroad);
            }
            
            // Add optional rooms until MinRooms
            int currentRooms = nodes.Count;
            while (currentRooms < template.MinimumRooms && template.OptionalRooms.Count > 0)
            {
                var randomOpt = template.OptionalRooms[UnityEngine.Random.Range(0, template.OptionalRooms.Count)];
                AddNode(randomOpt.RoomType, 1, crossroad);
                currentRooms++;
            }
        }
    }
}
