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
                NetworkManager.Singleton.OnClientConnectedCallback += SpawnPlayer;
                
                var generator = FindFirstObjectByType<ShipGeneration.ShipGenerator>();
                if (generator != null)
                {
                    if (generator.IsGenerationComplete) {
                        StartCoroutine(WaitAndHandleGenerationComplete());
                    } else {
                        generator.OnGenerationComplete += () => StartCoroutine(WaitAndHandleGenerationComplete());
                    }
                }
                else
                {
                    // Fallback if no generator in scene
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
            var points = ShipGeneration.ShipSpawnPoint.SpawnPoints;
            if (points != null && points.Count > 0)
            {
                _spawnPoints = new Transform[points.Count];
                for (int i = 0; i < points.Count; i++) _spawnPoints[i] = points[i];
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
                
                Vector3 teleportPos = Vector3.up * 2f;
                Quaternion teleportRot = Quaternion.identity;

                if (ShipGeneration.ShipSpawnPoint.SpawnPoints.Count > 0)
                {
                    var pt = ShipGeneration.ShipSpawnPoint.SpawnPoints[0];
                    teleportPos = pt.position + Vector3.up;
                    teleportRot = pt.rotation;
                }

                Debug.Log($"[PlayerSpawner] Server commanding client {clientId} to teleport to {teleportPos}");
                ClientRpcParams rpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } }
                };
                TeleportClientRpc(teleportPos, teleportRot, rpcParams);
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

            Vector3 targetPos = spawnPoint.position + Vector3.up;
            Debug.Log($"[PlayerSpawner] Spawning new player for client {clientId} at {targetPos}");
            GameObject player = Instantiate(_playerPrefab, targetPos, spawnPoint.rotation);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= SpawnPlayer;
            }
        }

        [ClientRpc]
        private void TeleportClientRpc(Vector3 targetPos, Quaternion targetRot, ClientRpcParams clientRpcParams = default)
        {
            if (NetworkManager.Singleton.LocalClient == null || NetworkManager.Singleton.LocalClient.PlayerObject == null) return;

            var pObj = NetworkManager.Singleton.LocalClient.PlayerObject;
            Debug.Log($"[PlayerSpawner] Client received teleport RPC to {targetPos}");

            var nt = pObj.GetComponent<Unity.Netcode.Components.NetworkTransform>();
            if (nt != null) {
                nt.Teleport(targetPos, targetRot, pObj.transform.localScale);
            } else {
                pObj.transform.position = targetPos;
                pObj.transform.rotation = targetRot;
            }
            
            var rb = pObj.GetComponent<Rigidbody>();
            if (rb != null) {
                rb.position = targetPos;
                rb.rotation = targetRot;
                rb.linearVelocity = Vector3.zero;
            }
        }
    }
}
