using System.Collections.Generic;

namespace ProceduralGeneration
{
    public class RoomNode
    {
        public int NodeID { get; private set; }
        public RoomType RoomType { get; set; }
        public int Floor { get; set; }
        
        public RoomNode ParentNode { get; private set; }
        public int Depth { get; private set; }
        
        public List<RoomNode> ConnectedNodes { get; private set; } = new List<RoomNode>();

        public RoomNode(int id, RoomType type, int floor, RoomNode parent = null)
        {
            NodeID = id;
            RoomType = type;
            Floor = floor;
            ParentNode = parent;
            Depth = parent != null ? parent.Depth + 1 : 0;
            
            if (parent != null)
            {
                parent.ConnectTo(this);
            }
        }

        public void ConnectTo(RoomNode otherNode)
        {
            if (!ConnectedNodes.Contains(otherNode))
            {
                ConnectedNodes.Add(otherNode);
            }
            if (!otherNode.ConnectedNodes.Contains(this))
            {
                otherNode.ConnectedNodes.Add(this);
            }
        }
    }
}
