using UnityEngine;

namespace SpaceMaintenance.Player
{
    public class PlayerInputHandler : MonoBehaviour
    {
        // ─── Movement ───────────────────────────────────────────────────
        public Vector2 MoveInput   { get; private set; }
        public Vector2 LookInput   { get; private set; }

        // ─── Actions ────────────────────────────────────────────────────
        public bool JumpInput      { get; private set; }
        public bool InteractInput  { get; private set; }
        public bool SprintInput    { get; private set; }
        public bool CrouchInput    { get; private set; }
        public bool CrouchToggle   { get; private set; } // true on the frame crouch was toggled

        private bool _isCrouching;

        private void Update()
        {
            // Movement axes
            MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            LookInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

            // Jump — single press
            JumpInput = Input.GetButtonDown("Jump");

            // Interact — single press
            InteractInput = Input.GetKeyDown(KeyCode.E);

            // Sprint — held
            SprintInput = Input.GetKey(KeyCode.LeftShift);

            // Crouch — toggle on C press (also support LeftControl hold)
            CrouchToggle = false;
            if (Input.GetKeyDown(KeyCode.C))
            {
                _isCrouching = !_isCrouching;
                CrouchToggle = true;
            }
            CrouchInput = _isCrouching || Input.GetKey(KeyCode.LeftControl);

            // If holding LeftControl, override toggle
            if (Input.GetKeyUp(KeyCode.LeftControl) && !_isCrouching)
            {
                CrouchInput = false;
            }
        }

        // ─── Consume helpers ────────────────────────────────────────────
        public void ConsumeJumpInput()     => JumpInput = false;
        public void ConsumeInteractInput() => InteractInput = false;

        /// <summary>Force-cancel crouch (e.g. when starting a sprint).</summary>
        public void CancelCrouch()
        {
            _isCrouching = false;
            CrouchInput = false;
        }
    }
}
