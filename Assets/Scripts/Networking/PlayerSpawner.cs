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
                
                // Spawn for host if the host is also a player
                if (NetworkManager.Singleton.IsHost)
                {
                    SpawnPlayer(NetworkManager.Singleton.LocalClientId);
                }
            }
        }

        private void SpawnPlayer(ulong clientId)
        {
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
