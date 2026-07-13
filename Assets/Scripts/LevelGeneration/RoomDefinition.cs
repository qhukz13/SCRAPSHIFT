// ============================================================================
// SCRAPSHIFT — RoomDefinition.cs
// Defines the grid bounds and connection points (doorways) for a room prefab.
// Used by ProceduralShipGenerator to piece the ship together.
// ============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace SpaceMaintenance.LevelGeneration
{
    public enum DoorDirection
    {
        North,  // +Z
        East,   // +X
        South,  // -Z
        West    // -X
    }

    [System.Serializable]
    public class Doorway
    {
        [Tooltip("The local grid coordinate of this doorway (relative to room's 0,0)")]
        public Vector2Int LocalGridPosition;
        
        [Tooltip("The direction this doorway faces OUTWARD from the room")]
        public DoorDirection Direction;
        
        [Tooltip("Optional transform for physical door placement or visual guides")]
        public Transform DoorAnchor;

        // Get the direction opposite to this one (used to match doors)
        public DoorDirection GetOppositeDirection()
        {
            return Direction switch
            {
                DoorDirection.North => DoorDirection.South,
                DoorDirection.South => DoorDirection.North,
                DoorDirection.East => DoorDirection.West,
                DoorDirection.West => DoorDirection.East,
                _ => DoorDirection.North
            };
        }

        // Get the grid offset vector for this direction
        public Vector2Int GetDirectionOffset()
        {
            return Direction switch
            {
                DoorDirection.North => new Vector2Int(0, 1),
                DoorDirection.South => new Vector2Int(0, -1),
                DoorDirection.East => new Vector2Int(1, 0),
                DoorDirection.West => new Vector2Int(-1, 0),
                _ => Vector2Int.zero
            };
        }
    }

    public class RoomDefinition : MonoBehaviour
    {
        [Header("Room Identity")]
        public string RoomName = "Generic Room";
        public bool IsUnique = false; // Only one of this room per ship (e.g. Reactor)

        [Header("Grid Size")]
        [Tooltip("Size of the room in grid cells. Example: 2x2 means it takes up 4 cells.")]
        public Vector2Int GridSize = new Vector2Int(1, 1);

        [Header("Connections")]
        public List<Doorway> Doorways = new List<Doorway>();

        [Header("Spawn Points")]
        [Tooltip("Assign empty GameObjects here to act as spawn points for tasks or items")]
        public List<Transform> TaskSpawnPoints = new List<Transform>();
        public List<Transform> ItemSpawnPoints = new List<Transform>();

        private void OnDrawGizmos()
        {
            // Visual grid size guide
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            
            // Assume 1 Grid Cell = 10x10 meters (configurable in generator, hardcoded here for preview)
            float cellSize = 10f; 
            
            // Draw bounds relative to the room's origin (assuming origin is bottom-left grid cell center)
            Vector3 centerOffset = new Vector3((GridSize.x - 1) * cellSize * 0.5f, 0, (GridSize.y - 1) * cellSize * 0.5f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(centerOffset, new Vector3(GridSize.x * cellSize, 2f, GridSize.y * cellSize));
            
            // Draw Doorways
            foreach (var door in Doorways)
            {
                Gizmos.color = Color.red;
                Vector3 doorPos = new Vector3(door.LocalGridPosition.x * cellSize, 1f, door.LocalGridPosition.y * cellSize);
                
                Vector3 dirOffset = Vector3.zero;
                switch (door.Direction)
                {
                    case DoorDirection.North: dirOffset = new Vector3(0, 0, cellSize * 0.5f); break;
                    case DoorDirection.South: dirOffset = new Vector3(0, 0, -cellSize * 0.5f); break;
                    case DoorDirection.East: dirOffset = new Vector3(cellSize * 0.5f, 0, 0); break;
                    case DoorDirection.West: dirOffset = new Vector3(-cellSize * 0.5f, 0, 0); break;
                }
                
                Gizmos.DrawSphere(doorPos + dirOffset, 1f);
                Gizmos.DrawLine(doorPos, doorPos + dirOffset * 1.5f);
            }
        }
    }
}
