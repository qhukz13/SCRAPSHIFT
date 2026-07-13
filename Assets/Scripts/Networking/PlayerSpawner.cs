using Unity.Netcode;
using UnityEngine;

namespace SpaceMaintenance.Networking
{
    public class PlayerSpawner : NetworkBehaviour
    {
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private Transform[] _spawnPoints;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                var generator = FindFirstObjectByType<LevelGeneration.ProceduralShipGenerator>();
                if (generator != null)
                {
                    generator.OnGenerationComplete += HandleGenerationComplete;
                }
                else
                {
                    // Fallback if no generator in scene
                    HandleGenerationComplete();
                }
            }
        }

        private void HandleGenerationComplete()
        {
            // Dynamically find spawn points from the generated ship
            // Assume the Crew Quarters room has GameObjects with "SpawnPoint" in their name
            var points = GameObject.FindGameObjectsWithTag("Respawn");
            if (points.Length > 0)
            {
                _spawnPoints = new Transform[points.Length];
                for (int i = 0; i < points.Length; i++) _spawnPoints[i] = points[i].transform;
            }

            // Spawn for all currently connected clients
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                SpawnPlayer(client.ClientId);
            }

            // Listen for future connections
            NetworkManager.Singleton.OnClientConnectedCallback += SpawnPlayer;
        }

        private void SpawnPlayer(ulong clientId)
        {
            if (_spawnPoints == null || _spawnPoints.Length == 0)
            {
                Debug.LogWarning("[PlayerSpawner] No spawn points found! Spawning at origin.");
                GameObject fallbackPlayer = Instantiate(_playerPrefab, Vector3.up * 2f, Quaternion.identity);
                fallbackPlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
                return;
            }

            // Simple round-robin spawn point selection
            Transform spawnPoint = _spawnPoints[clientId % (ulong)_spawnPoints.Length];
            GameObject player = Instantiate(_playerPrefab, spawnPoint.position, spawnPoint.rotation);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= SpawnPlayer;
            }
        }
    }
}
