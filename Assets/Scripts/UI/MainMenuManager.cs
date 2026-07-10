using SpaceMaintenance.Networking;
using Unity.Netcode;
using UnityEngine;

namespace SpaceMaintenance.UI
{
    public class MainMenuManager : MonoBehaviour
    {
        [SerializeField] private LobbyManager _lobbyManager;
        [SerializeField] private MainMenuUI _ui;

        private void Awake()
        {
            if (_ui == null) _ui = GetComponent<MainMenuUI>();
            if (_lobbyManager == null) _lobbyManager = GetComponent<LobbyManager>();
        }

        private async void Start()
        {
            if (_ui != null) _ui.ShowLoading("Connecting to Unity Services...");
            
            if (_lobbyManager == null)
            {
                _lobbyManager = FindFirstObjectByType<LobbyManager>();
            }

            if (_lobbyManager != null)
            {
                await _lobbyManager.InitializeUnityServicesAsync();
                _ui.ShowMainPanel();
            }
            else
            {
                _ui.ShowLoading("Error: LobbyManager not found in scene.");
            }
        }

        public async void OnHostClicked()
        {
            _ui.ShowLoading("Creating Lobby and starting server...");
            string joinCode = await _lobbyManager.CreateLobbyAsync();
            
            if (!string.IsNullOrEmpty(joinCode))
            {
                _ui.ShowLoading($"Lobby Created! Join Code: {joinCode}\nStarting Game...");
                
                // Wait briefly so the user can read the join code if they want, though it can also be shown in-game.
                await System.Threading.Tasks.Task.Delay(1500);
                
                // Transition to GameScene! All connected clients will automatically follow.
                NetworkManager.Singleton.SceneManager.LoadScene("main", UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
            else
            {
                _ui.ShowMainPanel();
                Debug.LogError("Failed to create lobby");
            }
        }

        public void OnJoinMenuClicked()
        {
            _ui.ShowJoinPanel();
        }

        public void OnJoinBackClicked()
        {
            _ui.ShowMainPanel();
        }

        public async void OnJoinSubmit(string code)
        {
            if (string.IsNullOrEmpty(code)) return;

            _ui.ShowLoading($"Joining Lobby {code}...");
            bool success = await _lobbyManager.JoinLobbyAsync(code);
            
            if (success)
            {
                _ui.ShowLoading("Connected! Waiting for host to load the game...");
            }
            else
            {
                _ui.ShowJoinPanel();
                Debug.LogError("Failed to join lobby");
            }
        }

        public void OnQuitClicked()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
