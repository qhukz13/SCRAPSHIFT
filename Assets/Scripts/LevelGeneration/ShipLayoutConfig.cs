using UnityEngine;

namespace SpaceMaintenance.LevelGeneration
{
    [CreateAssetMenu(fileName = "ShipLayoutConfig", menuName = "SpaceMaintenance/Level/Ship Layout Config")]
    public class ShipLayoutConfig : ScriptableObject
    {
        [Header("Room Sizes")]
        public Vector2 ReactorRoomSize = new Vector2(15f, 15f);
        public Vector2 ControlRoomSize = new Vector2(12f, 10f);
        public Vector2 StorageRoomSize = new Vector2(10f, 12f);
        public Vector2 GeneratorRoomSize = new Vector2(14f, 10f);
        public Vector2 CrewQuartersSize = new Vector2(12f, 12f);
        public Vector2 HubSize = new Vector2(6f, 6f);
        public float CorridorWidth = 4f;
        
        [Header("Architecture")]
        public float WallHeight = 6f;
        public float WallThickness = 0.5f;
        
        [Header("Materials")]
        public Material FloorMaterial;
        public Material WallMaterial;
        public Material HighlightMaterial;
        
        [Header("Prefabs")]
        public GameObject DoorPrefab;
        public GameObject BedPrefab; // For spawn points
    }
}
