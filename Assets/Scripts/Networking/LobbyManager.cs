using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace SpaceMaintenance.Networking
{
    public class LobbyManager : MonoBehaviour
    {
        private const int MaxPlayers = 4;
        private Lobby _currentLobby;

        public async Task InitializeUnityServicesAsync()
        {
            if (UnityServices.State == ServicesInitializationState.Initialized) return;

            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            Debug.Log($"Signed in as {AuthenticationService.Instance.PlayerId}");
        }

        public async Task<string> CreateLobbyAsync()
        {
            int maxRetries = 3;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxPlayers - 1);
                    string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                    var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                    transport.SetHostRelayData(
                        allocation.RelayServer.IpV4,
                        (ushort)allocation.RelayServer.Port,
                        allocation.AllocationIdBytes,
                        allocation.Key,
                        allocation.ConnectionData
                    );

                    CreateLobbyOptions options = new CreateLobbyOptions
                    {
                        IsPrivate = false,
                        Data = new System.Collections.Generic.Dictionary<string, DataObject>
                        {
                            { "JoinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
                        }
                    };

                    _currentLobby = await LobbyService.Instance.CreateLobbyAsync("SpaceMaintenanceLobby", MaxPlayers, options);
                    Debug.Log($"Created Lobby: {_currentLobby.Name} with Join Code: {joinCode}");

                    NetworkManager.Singleton.StartHost();
                    return joinCode;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Relay/Lobby attempt {i + 1} failed: {e.Message}");
                    if (i == maxRetries - 1)
                    {
                        Debug.LogError($"Failed to create server after {maxRetries} attempts: {e}");
                        return null;
                    }
                    await Task.Delay(1500); // wait 1.5s before retry
                }
            }
            return null;
        }

        public async Task<bool> JoinLobbyAsync(string joinCode)
        {
            try
            {
                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                transport.SetClientRelayData(
                    joinAllocation.RelayServer.IpV4,
                    (ushort)joinAllocation.RelayServer.Port,
                    joinAllocation.AllocationIdBytes,
                    joinAllocation.Key,
                    joinAllocation.ConnectionData,
                    joinAllocation.HostConnectionData
                );

                NetworkManager.Singleton.StartClient();
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to join relay: {e}");
                return false;
            }
        }
    }
}
