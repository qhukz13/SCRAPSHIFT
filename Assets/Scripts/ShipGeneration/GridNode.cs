using UnityEngine;

namespace ShipGeneration {
    public class GridNode {
        public int X;
        public int Y;
        public bool IsOccupied;
        public RoomType RoomType;
        public RoomData RoomInstance;
        
        // A* Pathfinding variables
        public int gCost;
        public int hCost;
        public int fCost { get { return gCost + hCost; } }
        public GridNode cameFromNode;
        
        public GridNode(int x, int y) {
            X = x;
            Y = y;
            IsOccupied = false;
            RoomType = RoomType.None;
        }
    }
}
