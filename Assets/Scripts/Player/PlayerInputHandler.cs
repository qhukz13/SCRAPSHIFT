using UnityEngine;

namespace SpaceMaintenance.Player
{
    public class PlayerInputHandler : MonoBehaviour
    {
        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool JumpInput { get; private set; }
        public bool InteractInput { get; private set; }

        private void Update()
        {
            // Fallback to legacy input system for prototyping
            MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            LookInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            
            JumpInput = Input.GetButtonDown("Jump");
            InteractInput = Input.GetKeyDown(KeyCode.E);
        }

        public void ConsumeJumpInput() => JumpInput = false;
        public void ConsumeInteractInput() => InteractInput = false;
    }
}
