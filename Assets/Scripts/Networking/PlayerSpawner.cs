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
                    // Wait a moment for clients to finish scene synchronization before attempting to spawn them.
                    StartCoroutine(WaitAndHandleGenerationComplete());
                }
            }
        }

        private System.Collections.IEnumerator WaitAndHandleGenerationComplete()
        {
            // Wait 1 second to ensure the Host and clients have finished loading the scene
            // and have been fully re-added to the ConnectedClientsList.
            yield return new WaitForSeconds(1.0f);
            HandleGenerationComplete();
        }

        private void HandleGenerationComplete()
        {
            // Dynamically find spawn points from the generated ship
            var points = GameObject.FindGameObjectsWithTag("Respawn");
            if (points != null && points.Length > 0)
            {
                _spawnPoints = new Transform[points.Length];
                for (int i = 0; i < points.Length; i++) _spawnPoints[i] = points[i].transform;
            }
            else
            {
                // Force clear the spawn points if none found, so fallback works reliably
                // even if the inspector had a broken array structure saved.
                _spawnPoints = new Transform[0];
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
            // Simple round-robin spawn point selection
            Transform spawnPoint = null;
            if (_spawnPoints != null && _spawnPoints.Length > 0 && _spawnPoints[0] != null)
            {
                spawnPoint = _spawnPoints[clientId % (ulong)_spawnPoints.Length];
            }
            
            // If the client already has a player object (e.g., from the Hub scene), teleport it instead of creating a clone!
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) && client.PlayerObject != null)
            {
                Debug.Log($"[PlayerSpawner] Client {clientId} already has a player object. Teleporting to spawn point.");
                if (spawnPoint != null)
                {
                    // For NetworkTransform to snap correctly, we might just set position, 
                    // but direct transform modification is fine if the player owns it.
                    client.PlayerObject.transform.position = spawnPoint.position;
                    client.PlayerObject.transform.rotation = spawnPoint.rotation;
                }
                else
                {
                    client.PlayerObject.transform.position = Vector3.up * 2f;
                }
                return;
            }

            // Check if array is empty OR if the first element is unassigned (fake null from Unity Inspector)
            if (spawnPoint == null)
            {
                Debug.LogWarning("[PlayerSpawner] No valid spawn points found! Spawning at origin.");
                
                if (_playerPrefab == null)
                {
                    Debug.LogError("[PlayerSpawner] Cannot spawn player: _playerPrefab is missing!");
                    return;
                }

                GameObject fallbackPlayer = Instantiate(_playerPrefab, Vector3.up * 2f, Quaternion.identity);
                fallbackPlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
                return;
            }

            if (spawnPoint == null) spawnPoint = _spawnPoints[0]; // Extra safety

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
