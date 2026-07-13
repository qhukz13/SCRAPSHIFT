// ============================================================================
// SCRAPSHIFT — ProceduralShipGenerator.cs
// Handles the seeded grid-based generation of the ship layout.
// Runs locally on all clients using a synchronized seed to ensure identical maps.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace SpaceMaintenance.LevelGeneration
{
    public class ProceduralShipGenerator : NetworkBehaviour
    {
        [Header("Configuration")]
        public float CellSize = 10f;
        public int MaxRooms = 20;

        [Header("Room Pools")]
        public RoomDefinition StartRoomPrefab;
        public RoomDefinition[] StandardRoomPrefabs;
        public RoomDefinition[] UniqueRoomPrefabs; // e.g. Reactor, Generator
        public GameObject WallCapPrefab; // Spawns when a doorway is left open

        // Seed sync
        public NetworkVariable<int> GenerationSeed = new NetworkVariable<int>(0);
        public System.Action OnGenerationComplete;

        // Internal State
        private Dictionary<Vector2Int, RoomDefinition> _occupiedCells = new Dictionary<Vector2Int, RoomDefinition>();
        private List<RoomDefinition> _spawnedRooms = new List<RoomDefinition>();
        
        private class OpenDoor
        {
            public Vector2Int GridPos;
            public DoorDirection Direction;
            public RoomDefinition SourceRoom;
        }
        private List<OpenDoor> _openDoors = new List<OpenDoor>();
        private List<RoomDefinition> _pendingUniqueRooms = new List<RoomDefinition>();

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                GenerationSeed.Value = Random.Range(1000, 999999);
            }

            GenerationSeed.OnValueChanged += (oldVal, newVal) => 
            {
                if (newVal != 0) StartCoroutine(GenerateShipRoutine(newVal));
            };

            // If late join or server already set it
            if (GenerationSeed.Value != 0)
            {
                StartCoroutine(GenerateShipRoutine(GenerationSeed.Value));
            }
        }

        private IEnumerator GenerateShipRoutine(int seed)
        {
            Debug.Log($"[ShipGenerator] Starting generation with seed: {seed}");
            
            // Initialize random state
            Random.InitState(seed);

            // Reset state
            _occupiedCells.Clear();
            _spawnedRooms.Clear();
            _openDoors.Clear();
            _pendingUniqueRooms = new List<RoomDefinition>(UniqueRoomPrefabs);

            // Spawn Start Room at (0,0)
            SpawnRoom(StartRoomPrefab, Vector2Int.zero);

            // Generation Loop
            int safetyNet = 1000;
            while (_openDoors.Count > 0 && safetyNet > 0)
            {
                safetyNet--;

                // Pick a random open door to process
                int doorIndex = Random.Range(0, _openDoors.Count);
                OpenDoor targetDoor = _openDoors[doorIndex];
                _openDoors.RemoveAt(doorIndex);

                // Determine target cell (the cell right outside the door)
                Vector2Int targetCell = targetDoor.GridPos + GetDirectionOffset(targetDoor.Direction);
                DoorDirection requiredDoorDir = GetOppositeDirection(targetDoor.Direction);

                // Is target cell already occupied? 
                if (_occupiedCells.ContainsKey(targetCell))
                {
                    CapDoorway(targetDoor);
                    continue;
                }

                // Should we stop spawning rooms?
                bool limitReached = _spawnedRooms.Count >= MaxRooms;
                
                RoomDefinition roomToSpawn = null;
                Vector2Int spawnOrigin = Vector2Int.zero;
                bool foundFit = false;

                // Try to find a room that fits
                List<RoomDefinition> candidates = GetCandidates(limitReached);
                
                // Shuffle candidates
                for (int i = 0; i < candidates.Count; i++)
                {
                    RoomDefinition temp = candidates[i];
                    int randomIndex = Random.Range(i, candidates.Count);
                    candidates[i] = candidates[randomIndex];
                    candidates[randomIndex] = temp;
                }

                foreach (var candidatePrefab in candidates)
                {
                    // Find a matching doorway in the candidate
                    foreach (var candidateDoor in candidatePrefab.Doorways)
                    {
                        if (candidateDoor.Direction == requiredDoorDir)
                        {
                            // Calculate where the candidate's origin must be so that its door lands on targetCell
                            Vector2Int potentialOrigin = targetCell - candidateDoor.LocalGridPosition;

                            if (CanFitRoom(candidatePrefab, potentialOrigin))
                            {
                                roomToSpawn = candidatePrefab;
                                spawnOrigin = potentialOrigin;
                                foundFit = true;
                                break;
                            }
                        }
                    }
                    if (foundFit) break;
                }

                if (foundFit)
                {
                    SpawnRoom(roomToSpawn, spawnOrigin);
                    
                    if (_pendingUniqueRooms.Contains(roomToSpawn))
                        _pendingUniqueRooms.Remove(roomToSpawn);
                }
                else
                {
                    // No room fits here, cap it
                    CapDoorway(targetDoor);
                }

                // Yield to not freeze main thread on huge ships
                if (safetyNet % 10 == 0) yield return null;
            }

            // Cap remaining doors
            foreach (var door in _openDoors)
            {
                CapDoorway(door);
            }
            _openDoors.Clear();

            Debug.Log($"[ShipGenerator] Generation Complete. Spawned {_spawnedRooms.Count} rooms.");
            OnGenerationComplete?.Invoke();
        }

        private List<RoomDefinition> GetCandidates(bool limitReached)
        {
            var list = new List<RoomDefinition>();
            
            // Prioritize unique rooms if we have them and aren't full
            if (_pendingUniqueRooms.Count > 0 && !limitReached)
            {
                list.Add(_pendingUniqueRooms[0]);
                return list;
            }

            if (!limitReached)
            {
                list.AddRange(StandardRoomPrefabs);
            }

            // If limit reached, ideally return "DeadEnd" small rooms here.
            // For now, if we reach the limit, we return nothing, which forces capping.
            return list;
        }

        private bool CanFitRoom(RoomDefinition prefab, Vector2Int origin)
        {
            for (int x = 0; x < prefab.GridSize.x; x++)
            {
                for (int y = 0; y < prefab.GridSize.y; y++)
                {
                    Vector2Int cell = origin + new Vector2Int(x, y);
                    if (_occupiedCells.ContainsKey(cell))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private void SpawnRoom(RoomDefinition prefab, Vector2Int origin)
        {
            Vector3 worldPos = new Vector3(origin.x * CellSize, 0, origin.y * CellSize);
            
            var roomInstance = Instantiate(prefab, worldPos, Quaternion.identity, transform);
            _spawnedRooms.Add(roomInstance);

            // Mark cells as occupied
            for (int x = 0; x < prefab.GridSize.x; x++)
            {
                for (int y = 0; y < prefab.GridSize.y; y++)
                {
                    _occupiedCells.Add(origin + new Vector2Int(x, y), roomInstance);
                }
            }

            // Register unused doors
            // Note: We don't register the door we just connected to. The way we avoid it is by 
            // checking if the door's outward cell is already occupied.
            foreach (var door in roomInstance.Doorways)
            {
                Vector2Int worldGridPos = origin + door.LocalGridPosition;
                Vector2Int targetCell = worldGridPos + GetDirectionOffset(door.Direction);

                if (!_occupiedCells.ContainsKey(targetCell))
                {
                    _openDoors.Add(new OpenDoor
                    {
                        GridPos = worldGridPos,
                        Direction = door.Direction,
                        SourceRoom = roomInstance
                    });
                }
            }
        }

        private void CapDoorway(OpenDoor door)
        {
            if (WallCapPrefab != null)
            {
                Vector3 worldPos = new Vector3(door.GridPos.x * CellSize, 0, door.GridPos.y * CellSize);
                
                // Offset to the edge of the cell based on direction
                Vector3 offset = Vector3.zero;
                float halfCell = CellSize * 0.5f;
                switch(door.Direction)
                {
                    case DoorDirection.North: offset = new Vector3(0, 0, halfCell); break;
                    case DoorDirection.South: offset = new Vector3(0, 0, -halfCell); break;
                    case DoorDirection.East: offset = new Vector3(halfCell, 0, 0); break;
                    case DoorDirection.West: offset = new Vector3(-halfCell, 0, 0); break;
                }

                Quaternion rot = Quaternion.identity;
                switch(door.Direction)
                {
                    case DoorDirection.North: rot = Quaternion.Euler(0, 0, 0); break;
                    case DoorDirection.East: rot = Quaternion.Euler(0, 90, 0); break;
                    case DoorDirection.South: rot = Quaternion.Euler(0, 180, 0); break;
                    case DoorDirection.West: rot = Quaternion.Euler(0, 270, 0); break;
                }

                Instantiate(WallCapPrefab, worldPos + offset, rot, transform);
            }
        }

        private DoorDirection GetOppositeDirection(DoorDirection dir)
        {
            return dir switch
            {
                DoorDirection.North => DoorDirection.South,
                DoorDirection.South => DoorDirection.North,
                DoorDirection.East => DoorDirection.West,
                DoorDirection.West => DoorDirection.East,
                _ => DoorDirection.North
            };
        }

        private Vector2Int GetDirectionOffset(DoorDirection dir)
        {
            return dir switch
            {
                DoorDirection.North => new Vector2Int(0, 1),
                DoorDirection.South => new Vector2Int(0, -1),
                DoorDirection.East => new Vector2Int(1, 0),
                DoorDirection.West => new Vector2Int(-1, 0),
                _ => Vector2Int.zero
            };
        }
    }
}
