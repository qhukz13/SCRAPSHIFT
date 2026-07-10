using Unity.Netcode;
using SpaceMaintenance.Core;
using UnityEngine;

namespace SpaceMaintenance.Player
{
    [RequireComponent(typeof(PlayerInputHandler))]
    public class InteractionController : NetworkBehaviour
    {
        [SerializeField] private InteractionDetector _detector;
        private PlayerInputHandler _input;

        private void Awake()
        {
            _input = GetComponent<PlayerInputHandler>();
            if (_detector == null)
            {
                _detector = GetComponentInChildren<InteractionDetector>(true);
            }
        }

        private void Update()
        {
            if (!IsOwner) return;

            if (_input.InteractInput && _detector != null && _detector.CurrentInteractable != null)
            {
                // Note: If interaction needs to be server authoritative, we should use ServerRpc here.
                // For now, we call the interaction logic.
                _detector.CurrentInteractable.OnInteract(gameObject);
                _input.ConsumeInteractInput();
            }
        }
    }
}
