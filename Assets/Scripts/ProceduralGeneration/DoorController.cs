using UnityEngine;
using Unity.Netcode;

namespace ProceduralGeneration
{
    public class DoorController : NetworkBehaviour
    {
        public RoomInstance RoomA { get; private set; }
        public RoomInstance RoomB { get; private set; }

        public NetworkVariable<bool> IsOpen = new NetworkVariable<bool>(false);
        public NetworkVariable<bool> IsLocked = new NetworkVariable<bool>(false);

        public void Initialize(RoomInstance a, RoomInstance b)
        {
            RoomA = a;
            RoomB = b;
        }

        [ServerRpc(RequireOwnership = false)]
        public void ToggleDoorServerRpc()
        {
            if (!IsLocked.Value)
            {
                IsOpen.Value = !IsOpen.Value;
            }
        }
        
        // This method can be called by interactions or global events like power outages
        public void SetLocked(bool locked)
        {
            if (!IsServer) return;
            IsLocked.Value = locked;
            
            // Auto close if locked
            if (locked && IsOpen.Value)
            {
                IsOpen.Value = false;
            }
        }
    }
}
