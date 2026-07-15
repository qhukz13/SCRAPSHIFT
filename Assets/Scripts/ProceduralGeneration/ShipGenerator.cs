using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace ProceduralGeneration
{
    public class ShipGenerator : NetworkBehaviour
    {
        public event Action OnGenerationComplete;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                GenerateShip();
            }
        }

        [Header("Data References")]
        public ShipTemplate Template;
        public RoomDatabase RoomDatabase;
        public GenerationSettings Settings;

        [Header("Gameplay Prefabs")]
        public GameObject ReactorPrefab;
        public GameObject GeneratorPrefab;

        private RoomGraph currentGraph;
        private List<RoomDefinition> placedRooms;

        public void GenerateShip()
        {
            if (!IsServer) return;

            Debug.Log("Starting Ship Generation Pipeline...");
            
            // 1. Generate Seed
            if (Settings.UseRandomSeed)
            {
                Settings.Seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            }
            UnityEngine.Random.InitState(Settings.Seed);
            Debug.Log($"Generation Seed: {Settings.Seed}");

            // Pipeline Execution
            bool success = false;
            int retries = 0;

            while (!success && retries < Settings.MaxGraphGenerationRetries)
            {
                // 3. Build Room Graph
                currentGraph = BuildRoomGraph(Template);
                
                // 4 & 5. Select & Place Rooms
                RoomPlacer placer = new RoomPlacer(transform, RoomDatabase, Settings);
                if (placer.PlaceRooms(currentGraph, out placedRooms))
                {
                    // 6. Generate Doors
                    DoorGenerator doorGen = new DoorGenerator();
                    doorGen.GenerateDoors(placedRooms);

                    // 7. Generate Stairs
                    StairGenerator stairGen = new StairGenerator();
                    stairGen.GenerateVerticalConnections(placedRooms);

                    // 8. Validate Layout
                    LayoutValidator validator = new LayoutValidator();
                    if (validator.Validate(placedRooms, Template))
                    {
                        success = true;
                    }
                }

                if (!success)
                {
                    CleanupFailedGeneration();
                    retries++;
                    Debug.LogWarning($"Generation failed, retrying ({retries}/{Settings.MaxGraphGenerationRetries})");
                }
            }

            if (success)
            {
                // 9-13. Spawn gameplay systems, loot, decorations
                SpawnGameplayElements();
                
                // 14. Spawn Players
                SpawnPlayers();

                OnGenerationComplete?.Invoke();
                Debug.Log("Ship Generation Complete.");
            }
            else
            {
                Debug.LogError("Ship Generation failed after maximum retries.");
            }
        }

        private RoomGraph BuildRoomGraph(ShipTemplate template)
        {
            RoomGraph graph = new RoomGraph();
            graph.GenerateFromTemplate(template);
            return graph;
        }

        private void SpawnGameplayElements()
        {
            Debug.Log("[ShipGenerator] Spawning gameplay elements...");
            if (placedRooms == null) return;
            
            // Spawn Reactor
            var reactorRoom = placedRooms.Find(r => r.RoomType == RoomType.Reactor);
            if (reactorRoom != null && ReactorPrefab != null)
            {
                // Place reactor at room center or a bit above floor
                Vector3 reactorPos = reactorRoom.transform.position + Vector3.up * 1f;
                GameObject reactorObj = Instantiate(ReactorPrefab, reactorPos, Quaternion.identity, reactorRoom.transform);
                var netObj = reactorObj.GetComponent<NetworkObject>();
                if (netObj != null) netObj.Spawn();
            }

            // Spawn Generators
            var generatorRoom = placedRooms.Find(r => r.RoomType == RoomType.Generator);
            if (generatorRoom != null && GeneratorPrefab != null)
            {
                // Calculate number of generators: 2 to 4 based on difficulty
                int numGenerators = Mathf.Clamp(Mathf.RoundToInt(2 + (Template.Difficulty - 1)), 2, 4);
                
                // Simple layout: spread them out slightly
                for (int i = 0; i < numGenerators; i++)
                {
                    Vector3 offset = new Vector3(
                        (i % 2 == 0 ? -2f : 2f),
                        1f,
                        (i < 2 ? -2f : 2f)
                    );
                    
                    // Add slight rotation or positional jitter if desired, but keep it simple
                    Vector3 genPos = generatorRoom.transform.TransformPoint(offset);
                    GameObject genObj = Instantiate(GeneratorPrefab, genPos, Quaternion.identity, generatorRoom.transform);
                    var netObj = genObj.GetComponent<NetworkObject>();
                    if (netObj != null) netObj.Spawn();
                }
            }
        }

        private void SpawnPlayers()
        {
            Debug.Log("[ShipGenerator] Spawning players...");
            
            if (placedRooms == null || placedRooms.Count == 0) return;

            // Find the spawn room
            var spawnRoom = placedRooms.Find(r => r.RoomType == RoomType.Spawn);
            if (spawnRoom == null)
            {
                Debug.LogWarning("[ShipGenerator] No Spawn room found to teleport players to.");
                return;
            }

            Vector3 spawnPos = spawnRoom.transform.position + Vector3.up * 1f;
            if (spawnRoom.SpawnPoints != null && spawnRoom.SpawnPoints.Count > 0 && spawnRoom.SpawnPoints[0] != null)
            {
                spawnPos = spawnRoom.SpawnPoints[0].position;
            }

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                var playerObj = client.PlayerObject;
                if (playerObj != null)
                {
                    // For NGO, disabling the CharacterController or Rigidbody temporarily during teleport can prevent physics issues.
                    var rb = playerObj.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.position = spawnPos;
                        rb.linearVelocity = Vector3.zero;
                    }
                    playerObj.transform.position = spawnPos;
                }
            }
        }

        private void CleanupFailedGeneration()
        {
            if (placedRooms != null)
            {
                foreach (var room in placedRooms)
                {
                    if (room != null)
                    {
                        Destroy(room.gameObject);
                    }
                }
                placedRooms.Clear();
            }
            if (currentGraph != null)
            {
                currentGraph.Clear();
            }
        }
    }
}
