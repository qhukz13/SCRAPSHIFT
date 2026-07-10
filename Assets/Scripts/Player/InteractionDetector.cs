using SpaceMaintenance.Core;
using UnityEngine;

namespace SpaceMaintenance.Player
{
    public class InteractionDetector : MonoBehaviour
    {
        [SerializeField] private float _interactionRange = 3f;
        [SerializeField] private LayerMask _interactableLayer;
        
        public IInteractable CurrentInteractable { get; private set; }
        private Camera _cam;

        private void Start()
        {
            _cam = GetComponentInChildren<Camera>();
            if (_cam == null) _cam = Camera.main;
        }

        private void Update()
        {
            Transform rayOrigin = _cam != null ? _cam.transform : transform;
            
            // Debug line to see where the ray goes
            Debug.DrawRay(rayOrigin.position, rayOrigin.forward * _interactionRange, Color.red);

            RaycastHit[] hits = Physics.RaycastAll(rayOrigin.position, rayOrigin.forward, _interactionRange, _interactableLayer);
            
            IInteractable foundInteractable = null;

            // Sort hits by distance so we process the closest objects first
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                // Ignore player's own colliders
                if (hit.collider.transform.root == transform.root) continue;

                // This is the FIRST object hit that is not the player.
                // If it's an interactable, we can interact with it.
                // If it's a wall or floor (no IInteractable), we break immediately so we don't pick up things behind it.
                var interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null)
                {
                    foundInteractable = interactable;
                }
                break;
            }

            if (foundInteractable != CurrentInteractable)
            {
                if (foundInteractable != null)
                    Debug.Log($"InteractionDetector: Found new interactable! {((MonoBehaviour)foundInteractable).gameObject.name}");
                else
                    Debug.Log("InteractionDetector: Lost interactable.");
                
                CurrentInteractable = foundInteractable;
            }
        }
    }
}
