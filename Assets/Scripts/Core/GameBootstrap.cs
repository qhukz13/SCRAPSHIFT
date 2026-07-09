using UnityEngine;

namespace SpaceMaintenance.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            // Initialize Core Systems
            InitializeServices();
            DontDestroyOnLoad(gameObject);
        }

        private void InitializeServices()
        {
            // Initialize ServiceLocator if not already
            var serviceLocator = ServiceLocator.Instance;

            // Register global managers/services here
            // Example:
            // serviceLocator.RegisterService<IGameManager>(new GameManager());
            
            Debug.Log("GameBootstrap: Core services initialized.");
        }
    }
}
