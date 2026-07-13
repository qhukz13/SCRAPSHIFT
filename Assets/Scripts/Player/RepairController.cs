using SpaceMaintenance.Core;
using Unity.Netcode;
using UnityEngine;

namespace SpaceMaintenance.Player
{
    public class RepairController : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _repairRange = 4f;
        [SerializeField] private LayerMask _repairableLayer;
        
        private Camera _mainCamera;
        
        // Client-side tracking
        public IRepairable CurrentRepairTarget { get; private set; }
        private ulong _currentTargetId;

        // Server-side tracking
        private IRepairable _serverRepairTarget;

        private void Awake()
        {
            _mainCamera = GetComponentInChildren<Camera>();
            if (_mainCamera == null) _mainCamera = Camera.main;
        }

        private void Update()
        {
            if (!IsOwner) return;

            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.rKey.isPressed) // Hold R to repair
            {
                if (CurrentRepairTarget == null)
                {
                    if (_mainCamera == null) return;
                    if (Physics.Raycast(_mainCamera.transform.position, _mainCamera.transform.forward, out RaycastHit hit, _repairRange, _repairableLayer))
                    {
                        var targetNetObj = hit.collider.GetComponentInParent<NetworkObject>();
                        if (targetNetObj != null)
                        {
                            var repairable = targetNetObj.GetComponent<IRepairable>();
                            if (repairable != null && repairable.NeedsRepair)
                            {
                                // Skip if this system uses minigames (it handles its own interaction via IInteractable)
                                if (repairable is IMinigameRepairable)
                                {
                                    return;
                                }

                                CurrentRepairTarget = repairable;
                                _currentTargetId = targetNetObj.NetworkObjectId;
                                RequestStartRepairServerRpc(_currentTargetId);
                            }
                        }
                    }
                }

                if (CurrentRepairTarget != null)
                {
                    RequestUpdateRepairServerRpc(_currentTargetId, Time.deltaTime);
                    
                    if (!CurrentRepairTarget.NeedsRepair)
                    {
                        CurrentRepairTarget = null;
                        _currentTargetId = 0;
                    }
                }
            }
            else
            {
                if (CurrentRepairTarget != null)
                {
                    RequestCancelRepairServerRpc(_currentTargetId);
                    CurrentRepairTarget = null;
                    _currentTargetId = 0;
                }
            }
        }

        [ServerRpc]
        private void RequestStartRepairServerRpc(ulong targetId)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out var target))
            {
                _serverRepairTarget = target.GetComponent<IRepairable>();
                if (_serverRepairTarget != null)
                {
                    Debug.Log($"Started repairing {target.name}!");
                    _serverRepairTarget.StartRepair(gameObject);
                }
            }
        }

        [ServerRpc]
        private void RequestUpdateRepairServerRpc(ulong targetId, float deltaTime)
        {
            if (_serverRepairTarget != null)
            {
                _serverRepairTarget.UpdateRepair(deltaTime);
                if (!_serverRepairTarget.NeedsRepair)
                {
                    Debug.Log("Repair completed successfully!");
                    _serverRepairTarget = null;
                }
            }
        }

        [ServerRpc]
        private void RequestCancelRepairServerRpc(ulong targetId)
        {
            if (_serverRepairTarget != null)
            {
                _serverRepairTarget.CancelRepair();
                _serverRepairTarget = null;
            }
        }
    }
}
