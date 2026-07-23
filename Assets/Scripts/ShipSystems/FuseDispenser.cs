// ============================================================================
// Space Maintenance — FuseDispenser.cs
// Spawns a new Heavy Fuse when interacted with.
// Prevents spamming by setting a cooldown.
// ============================================================================

using UnityEngine;
using Unity.Netcode;
using SpaceMaintenance.Core;

namespace SpaceMaintenance.ShipSystems
{
    public class FuseDispenser : NetworkBehaviour, IInteractable
    {
        [SerializeField] private GameObject _heavyFusePrefab;
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private float _cooldown = 3f;

        private float _lastDispenseTime;

        public string InteractionPrompt => Time.time > _lastDispenseTime + _cooldown ? "Press E to Dispense Heavy Fuse" : "Recharging...";
        public bool RequiresHold => false;
        public float HoldDuration => 0f;

        public bool CanInteract(GameObject player)
        {
            return Time.time > _lastDispenseTime + _cooldown;
        }

        public void OnInteract(GameObject player)
        {
            if (CanInteract(player))
            {
                RequestDispenseServerRpc();
                // Set local cooldown for immediate feedback
                _lastDispenseTime = Time.time;
            }
        }

        public void OnInteractHold(GameObject player, float holdTime) { }
        public void OnInteractRelease(GameObject player) { }

        [ServerRpc(RequireOwnership = false)]
        private void RequestDispenseServerRpc()
        {
            if (Time.time > _lastDispenseTime + _cooldown)
            {
                _lastDispenseTime = Time.time;
                UpdateCooldownClientRpc();

                if (_heavyFusePrefab != null)
                {
                    var go = Instantiate(_heavyFusePrefab, _spawnPoint.position, _spawnPoint.rotation);
                    var netObj = go.GetComponent<NetworkObject>();
                    if (netObj != null)
                    {
                        netObj.Spawn();
                    }
                }
            }
        }

        [ClientRpc]
        private void UpdateCooldownClientRpc()
        {
            _lastDispenseTime = Time.time;
        }
    }
}
