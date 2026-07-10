using Unity.Netcode;
using UnityEngine;

namespace SpaceMaintenance.Networking
{
    public class NetworkGameManager : NetworkBehaviour
    {
        public static NetworkGameManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                Debug.Log("Server started. Initializing game state...");
                // Initialize game round, chaos events, etc.
            }
        }
    }
}
