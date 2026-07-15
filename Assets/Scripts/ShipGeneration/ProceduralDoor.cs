using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace ShipGeneration {
    public class ProceduralDoor : NetworkBehaviour {
        public bool IsSpawnDoor = false;
        private bool isPermanentlyClosed = false;
        
        public Animator DoorAnimator;
        
        private HashSet<ulong> playersInside = new HashSet<ulong>();
        private NetworkVariable<bool> isOpen = new NetworkVariable<bool>(false);
        
        public override void OnNetworkSpawn() {
            isOpen.OnValueChanged += (prev, current) => {
                if (DoorAnimator != null) {
                    DoorAnimator.SetBool("IsOpen", current);
                } else {
                    // Fallback visual: disable the first child (the door mesh) when open
                    if (transform.childCount > 0) {
                        transform.GetChild(0).gameObject.SetActive(!current);
                    }
                }
            };
            
            // Initial state
            if (transform.childCount > 0) {
                transform.GetChild(0).gameObject.SetActive(!isOpen.Value);
            }
        }
        
        void OnTriggerEnter(Collider other) {
            if (!IsServer || isPermanentlyClosed) return;
            
            if (other.CompareTag("Player")) {
                var netObj = other.GetComponent<NetworkObject>();
                if (netObj != null) {
                    playersInside.Add(netObj.OwnerClientId);
                    UpdateDoorState();
                }
            }
        }
        
        void OnTriggerExit(Collider other) {
            if (!IsServer || isPermanentlyClosed) return;
            
            if (other.CompareTag("Player")) {
                var netObj = other.GetComponent<NetworkObject>();
                if (netObj != null) {
                    playersInside.Remove(netObj.OwnerClientId);
                    UpdateDoorState();
                    
                    if (IsSpawnDoor && playersInside.Count == 0) {
                        isPermanentlyClosed = true;
                        Debug.Log("Spawn door permanently closed.");
                    }
                }
            }
        }
        
        private void UpdateDoorState() {
            isOpen.Value = playersInside.Count > 0;
        }
    }
}
