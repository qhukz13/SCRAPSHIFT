using UnityEngine;
using System.Collections.Generic;

namespace ShipGeneration {
    public class RoomData : MonoBehaviour {
        [Header("Room Setup")]
        public RoomType RoomType;
        public Vector2Int SizeInCells = new Vector2Int(1, 1);
        
        [Header("Spawn Points")]
        public List<Transform> GeneratorSpawns = new List<Transform>();
        public List<Transform> PipeSpawns = new List<Transform>();
        public Transform ReactorSpawn;
        public Transform PlayerSpawnArea;
        
        [Header("Item Spawn Points")]
        [Tooltip("General-purpose spawn points for items (wrench, scrap, etc.)")]
        public List<Transform> ItemSpawns = new List<Transform>();

        [Header("Door Anchors (Optional)")]
        public List<Transform> DoorAnchors = new List<Transform>();
    }
}
