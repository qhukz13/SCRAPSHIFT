using SpaceMaintenance.Core;
using UnityEngine;

namespace SpaceMaintenance.Player
{
    [RequireComponent(typeof(PlayerInputHandler))]
    public class InteractionController : MonoBehaviour
    {
        [SerializeField] private InteractionDetector _detector;
        private PlayerInputHandler _input;

        private void Awake()
        {
            _input = GetComponent<PlayerInputHandler>();
        }

        private void Update()
        {
            if (_input.InteractInput && _detector != null && _detector.CurrentInteractable != null)
            {
                _detector.CurrentInteractable.OnInteract(gameObject);
                _input.ConsumeInteractInput();
            }
        }
    }
}
