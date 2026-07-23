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

        private void Start()
        {
            // If the user starts the 'main' scene directly in the Editor without a NetworkManager,
            // we redirect them to the MainMenu scene to prevent "No cameras rendering" and broken state.
            if (NetworkManager.Singleton == null)
            {
                Debug.LogWarning("[NetworkGameManager] NetworkManager not found. Redirecting to MainMenu...");
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            }
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
