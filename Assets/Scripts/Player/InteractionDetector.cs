using SpaceMaintenance.Core;
using UnityEngine;

namespace SpaceMaintenance.Player
{
    public class InteractionDetector : MonoBehaviour
    {
        [SerializeField] private float _interactionRange = 3f;
        [SerializeField] private LayerMask _interactableLayer;
        
        public IInteractable CurrentInteractable { get; private set; }

        private void Update()
        {
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, _interactionRange, _interactableLayer))
            {
                var interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != CurrentInteractable)
                {
                    CurrentInteractable = interactable;
                    // Fire an event or call a UI manager to show interaction prompt here
                }
            }
            else
            {
                if (CurrentInteractable != null)
                {
                    CurrentInteractable = null;
                    // Hide UI prompt
                }
            }
        }
    }
}
