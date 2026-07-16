// ============================================================================
// SCRAPSHIFT — ItemSpawner.cs
// Distributes item prefabs across rooms after ship generation completes.
// Compatible with both ShipGeneration.ShipGenerator and
// ProceduralGeneration.ShipGenerator. Collects spawn/loot points from
// whichever system is active and randomly places networked items.
// ============================================================================

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace ShipGeneration
{
    [RequireComponent(typeof(NetworkObject))]
    public class ItemSpawner : NetworkBehaviour
    {
        // ─── Inspector ──────────────────────────────────────────────────
        [Header("Item Prefabs")]
        [Tooltip("Prefabs to randomly spawn across the ship (must have NetworkObject)")]
        [SerializeField] private GameObject[] _itemPrefabs;

        [Header("Spawn Config")]
        [Tooltip("How many items to spawn in total (clamped by available spawn points)")]
        [SerializeField] private int _maxItems = 8;

        [Tooltip("Minimum items guaranteed to spawn")]
        [SerializeField] private int _minItems = 3;

        [Header("Fallback")]
        [Tooltip("If rooms have no item points, place items at random offsets inside rooms")]
        [SerializeField] private bool _useFallbackPositions = true;

        [Tooltip("Y offset for fallback spawns so items don't clip into the floor")]
        [SerializeField] private float _fallbackYOffset = 0.5f;

        // ─── Runtime ────────────────────────────────────────────────────
        private ShipGenerator _shipGen;
        private ProceduralGeneration.ShipGenerator _procGen;

        // =================================================================
        //  LIFECYCLE
        // =================================================================

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            // Try to find either generator in the scene
            _shipGen = FindFirstObjectByType<ShipGenerator>();
            _procGen = FindFirstObjectByType<ProceduralGeneration.ShipGenerator>();

            bool anyFound = false;

            if (_shipGen != null)
            {
                if (_shipGen.IsGenerationComplete)
                    SpawnItems();
                else
                    _shipGen.OnGenerationComplete += SpawnItems;
                anyFound = true;
            }

            if (_procGen != null)
            {
                _procGen.OnGenerationComplete += SpawnItems;
                anyFound = true;
            }

            if (!anyFound)
            {
                Debug.LogWarning("[ItemSpawner] No ShipGenerator found in scene.");
            }
        }

        public override void OnNetworkDespawn()
        {
            if (_shipGen != null)
                _shipGen.OnGenerationComplete -= SpawnItems;
            if (_procGen != null)
                _procGen.OnGenerationComplete -= SpawnItems;
        }

        // =================================================================
        //  ITEM SPAWNING
        // =================================================================

        private void SpawnItems()
        {
            if (_itemPrefabs == null || _itemPrefabs.Length == 0)
            {
                Debug.LogWarning("[ItemSpawner] No item prefabs assigned.");
                return;
            }

            // 1. Collect all available spawn points from generated rooms
            List<Vector3> spawnPositions = CollectSpawnPositions();

            // 2. If no dedicated points, try fallback
            if (spawnPositions.Count == 0 && _useFallbackPositions)
            {
                spawnPositions = GenerateFallbackPositions();
            }

            if (spawnPositions.Count == 0)
            {
                Debug.LogWarning("[ItemSpawner] No spawn positions available for items.");
                return;
            }

            // 3. Determine count and spawn
            int itemCount = Mathf.Clamp(
                Random.Range(_minItems, _maxItems + 1),
                _minItems,
                spawnPositions.Count
            );

            if (itemCount <= 0) return;

            ShuffleList(spawnPositions);

            for (int i = 0; i < itemCount; i++)
            {
                var prefab = _itemPrefabs[Random.Range(0, _itemPrefabs.Length)];
                var pos = spawnPositions[i];

                GameObject item = Instantiate(prefab, pos, Quaternion.identity);
                if (item.TryGetComponent<NetworkObject>(out var netObj))
                {
                    netObj.Spawn();
                }
            }

            Debug.Log($"[ItemSpawner] Spawned {itemCount} items across the ship.");
        }

        // =================================================================
        //  COLLECT POINTS FROM EITHER GENERATOR SYSTEM
        // =================================================================

        private List<Vector3> CollectSpawnPositions()
        {
            var positions = new List<Vector3>();

            // --- ShipGeneration system (RoomData.ItemSpawns) ---
            if (_shipGen != null && _shipGen.SpawnedRooms != null)
            {
                foreach (var room in _shipGen.SpawnedRooms)
                {
                    if (room == null || room.ItemSpawns == null) continue;
                    foreach (var point in room.ItemSpawns)
                    {
                        if (point != null) positions.Add(point.position);
                    }
                }
            }

            // --- ProceduralGeneration system (RoomDefinition.LootPoints) ---
            if (_procGen != null)
            {
                var roomDefs = FindObjectsByType<ProceduralGeneration.RoomDefinition>(FindObjectsSortMode.None);
                foreach (var roomDef in roomDefs)
                {
                    if (roomDef == null || roomDef.LootPoints == null) continue;
                    foreach (var point in roomDef.LootPoints)
                    {
                        if (point != null) positions.Add(point.position);
                    }
                }
            }

            return positions;
        }

        // =================================================================
        //  FALLBACK: random positions inside room bounds
        // =================================================================

        private List<Vector3> GenerateFallbackPositions()
        {
            var positions = new List<Vector3>();

            // --- From ShipGeneration rooms ---
            if (_shipGen != null && _shipGen.SpawnedRooms != null)
            {
                float cellSize = _shipGen.CellSize;
                foreach (var room in _shipGen.SpawnedRooms)
                {
                    if (room == null) continue;
                    if (room.RoomType == RoomType.Spawn || room.RoomType == RoomType.Corridor)
                        continue;

                    Vector3 center = room.transform.position
                        + new Vector3(
                            (room.SizeInCells.x - 1) * cellSize * 0.5f,
                            _fallbackYOffset,
                            (room.SizeInCells.y - 1) * cellSize * 0.5f
                        );

                    for (int j = 0; j < 2; j++)
                    {
                        float rx = Random.Range(-cellSize * 0.3f, cellSize * 0.3f);
                        float rz = Random.Range(-cellSize * 0.3f, cellSize * 0.3f);
                        positions.Add(center + new Vector3(rx, 0, rz));
                    }
                }
            }

            // --- From ProceduralGeneration rooms ---
            if (_procGen != null)
            {
                var roomDefs = FindObjectsByType<ProceduralGeneration.RoomDefinition>(FindObjectsSortMode.None);
                foreach (var roomDef in roomDefs)
                {
                    if (roomDef == null) continue;
                    if (roomDef.RoomType == ProceduralGeneration.RoomType.Spawn) continue;

                    Vector3 center = roomDef.transform.position + roomDef.RoomBounds.center;
                    center.y = _fallbackYOffset;

                    Vector3 extents = roomDef.RoomBounds.extents;
                    for (int j = 0; j < 2; j++)
                    {
                        float rx = Random.Range(-extents.x * 0.5f, extents.x * 0.5f);
                        float rz = Random.Range(-extents.z * 0.5f, extents.z * 0.5f);
                        positions.Add(center + new Vector3(rx, 0, rz));
                    }
                }
            }

            return positions;
        }

        // =================================================================
        //  UTILS
        // =================================================================

        private static void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
