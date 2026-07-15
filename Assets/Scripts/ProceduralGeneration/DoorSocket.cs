using System;
using UnityEngine;

namespace ProceduralGeneration
{
    public enum DoorType
    {
        Standard,
        Heavy,
        Airlock,
        Elevator
    }

    public enum SocketType
    {
        Wall,
        Floor,
        Ceiling
    }

    [Serializable]
    public class DoorSocket
    {
        public Vector3 LocalPosition;
        public Vector3 LocalDirection;
        public SocketType SocketType = SocketType.Wall;
        public int Floor = 1;
        public DoorType DoorType = DoorType.Standard;
        
        [NonSerialized]
        public bool IsUsed = false;
    }
}
