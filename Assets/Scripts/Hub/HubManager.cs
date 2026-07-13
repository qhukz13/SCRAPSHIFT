// ============================================================================
// SCRAPSHIFT — HubManager.cs
// Simple manager for the Hub scene. Provides logic to transition players back
// to the mission scene ("main").
// ============================================================================

using Unity.Netcode;
using UnityEngine;

namespace SpaceMaintenance.Hub
{
    public class HubManager : NetworkBehaviour
    {
        public static HubManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // =================================================================
        //  UI / INTERACTION BINDINGS
        // =================================================================

        /// <summary>
        /// Called via a UI button or terminal interaction in the Hub.
        /// </summary>
        public void StartNextShift()
        {
            if (!IsServer)
            {
                Debug.LogWarning("Only the host can start the next shift.");
                return;
            }

            Debug.Log("Host is starting the next shift! Transitioning to 'main' scene...");
            NetworkManager.Singleton.SceneManager.LoadScene("main", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
}
