using UnityEngine;
using Unity.Netcode;
using SpaceMaintenance.Core;

namespace SpaceMaintenance.Hub
{
    public class MissionLaunchConsole : NetworkBehaviour, IInteractable
    {
        public NetworkVariable<int> SelectedMode = new NetworkVariable<int>(0);
        public NetworkVariable<int> SettingValue = new NetworkVariable<int>(10);
        
        public NetworkList<ulong> ReadyPlayers = new NetworkList<ulong>();

        public string InteractionPrompt => "Press E to Setup Mission";
        public bool RequiresHold => false;
        public float HoldDuration => 0f;

        public bool CanInteract(GameObject player)
        {
            return true; // Anyone can interact
        }

        public void OnInteract(GameObject player)
        {
            if (IsServer)
            {
                OpenSetupUIClientRpc();
            }
            else
            {
                // Open locally for client
                if (MissionSetupUI.Instance == null)
                {
                    var go = new GameObject("MissionSetupUIManager");
                    go.AddComponent<MissionSetupUI>();
                }
                MissionSetupUI.Instance.OpenUI(this);
            }
        }

        [ClientRpc]
        private void OpenSetupUIClientRpc()
        {
            if (MissionSetupUI.Instance == null)
            {
                var go = new GameObject("MissionSetupUIManager");
                go.AddComponent<MissionSetupUI>();
            }
            MissionSetupUI.Instance.OpenUI(this);
        }

        [Rpc(SendTo.Server)]
        public void ChangeModeServerRpc(int mode)
        {
            if (!IsServer) return;
            SelectedMode.Value = mode;
        }

        [Rpc(SendTo.Server)]
        public void ChangeSettingServerRpc(int setting)
        {
            if (!IsServer) return;
            SettingValue.Value = setting;
        }

        [Rpc(SendTo.Server)]
        public void RequestSyncServerRpc(ulong clientId)
        {
            if (!IsServer) return;
            
            bool found = false;
            for (int i = 0; i < ReadyPlayers.Count; i++)
            {
                if (ReadyPlayers[i] == clientId)
                {
                    ReadyPlayers.RemoveAt(i);
                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                ReadyPlayers.Add(clientId);
            }
        }

        [Rpc(SendTo.Server)]
        public void ToggleReadyServerRpc(ulong clientId)
        {
            if (!IsServer) return;
            
            bool found = false;
            for (int i = 0; i < ReadyPlayers.Count; i++)
            {
                if (ReadyPlayers[i] == clientId)
                {
                    ReadyPlayers.RemoveAt(i);
                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                ReadyPlayers.Add(clientId);
            }
        }

        [Rpc(SendTo.Server)]
        public void LaunchMissionServerRpc()
        {
            if (!IsServer) return;
            
            if (ReadyPlayers.Count < NetworkManager.Singleton.ConnectedClientsList.Count)
            {
                Debug.LogWarning("[MissionLaunchConsole] Cannot launch: Not all players are ready!");
                return;
            }

            SpaceMaintenance.Core.GlobalMissionParameters.HasCustomSettings = true;
            SpaceMaintenance.Core.GlobalMissionParameters.Mode = SelectedMode.Value == 0 ? SpaceMaintenance.Core.GameMode.Survival : SpaceMaintenance.Core.GameMode.Tasks;
            SpaceMaintenance.Core.GlobalMissionParameters.Duration = SettingValue.Value * 60f;
            SpaceMaintenance.Core.GlobalMissionParameters.Quota = SettingValue.Value;

            Debug.Log("[MissionLaunchConsole] Launching mission!");
            NetworkManager.Singleton.SceneManager.LoadScene("main", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }

        public void OnInteractHold(GameObject player, float holdTime) { }
        public void OnInteractRelease(GameObject player) { }
    }
}
