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
            if (!IsSpawned || !IsOwner) return;

            if (_input.InteractInput)
            {
                try
                {
                    if (_detector != null && _detector.CurrentInteractable != null)
                    {
                        _detector.CurrentInteractable.OnInteract(gameObject);
                    }
                }
                finally
                {
                    // Always consume the input to prevent it from sticking
                    _input.ConsumeInteractInput();
                }
            }
        }
    }
}
