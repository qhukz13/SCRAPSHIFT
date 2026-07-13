using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceMaintenance.Player
{
    public class PlayerInputHandler : NetworkBehaviour
    {
        // ─── Movement ───────────────────────────────────────────────────
        public Vector2 MoveInput   { get; private set; }
        public Vector2 LookInput   { get; private set; }

        // ─── Actions ────────────────────────────────────────────────────
        public bool JumpInput      { get; private set; }
        public bool InteractInput  { get; private set; }
        public bool SprintInput    { get; private set; }
        public bool CrouchInput    { get; private set; }
        public bool CrouchToggle   { get; private set; }
        public bool FlashlightInput{ get; private set; }

        private bool _isCrouchedState;

        // ─── Input Actions ──────────────────────────────────────────────
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;
        private InputAction _interactAction;
        private InputAction _sprintAction;
        private InputAction _crouchAction;
        private InputAction _crouchToggleAction;
        private InputAction _flashlightAction;

        private void Awake()
        {
            _moveAction = new InputAction("Move", binding: "<Gamepad>/leftStick");
            _moveAction.AddCompositeBinding("Dpad")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            _lookAction = new InputAction("Look", binding: "<Pointer>/delta");
            
            _jumpAction = new InputAction("Jump", binding: "<Keyboard>/space");
            _jumpAction.AddBinding("<Gamepad>/buttonSouth");

            _interactAction = new InputAction("Interact", binding: "<Keyboard>/e");
            _interactAction.AddBinding("<Gamepad>/buttonWest");

            _sprintAction = new InputAction("Sprint", binding: "<Keyboard>/leftShift");
            
            // Left control to hold crouch
            _crouchAction = new InputAction("Crouch", binding: "<Keyboard>/leftCtrl");
            
            // C to toggle crouch
            _crouchToggleAction = new InputAction("CrouchToggle", binding: "<Keyboard>/c");

            // F for flashlight
            _flashlightAction = new InputAction("Flashlight", binding: "<Keyboard>/f");
            _flashlightAction.AddBinding("<Gamepad>/dpad/up");

            _jumpAction.performed += ctx => JumpInput = true;
            _interactAction.performed += ctx => InteractInput = true;
            _flashlightAction.performed += ctx => FlashlightInput = true;
            
            _crouchToggleAction.performed += ctx => 
            {
                _isCrouchedState = !_isCrouchedState;
                CrouchToggle = true;
            };
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                _moveAction.Enable();
                _lookAction.Enable();
                _jumpAction.Enable();
                _interactAction.Enable();
                _sprintAction.Enable();
                _crouchAction.Enable();
                _crouchToggleAction.Enable();
                _flashlightAction.Enable();
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                _moveAction.Disable();
                _lookAction.Disable();
                _jumpAction.Disable();
                _interactAction.Disable();
                _sprintAction.Disable();
                _crouchAction.Disable();
                _crouchToggleAction.Disable();
                _flashlightAction.Disable();
            }
        }

        private void Update()
        {
            if (!IsOwner) return;

            CrouchToggle = false;

            MoveInput = _moveAction.ReadValue<Vector2>();
            LookInput = _lookAction.ReadValue<Vector2>();
            
            SprintInput = _sprintAction.ReadValue<float>() > 0.5f;
            
            bool holdingCrouch = _crouchAction.ReadValue<float>() > 0.5f;
            
            // If holding LeftControl, override toggle
            if (_crouchAction.WasReleasedThisFrame() && !_isCrouchedState)
            {
                CrouchInput = false;
            }

            CrouchInput = _isCrouchedState || holdingCrouch;
        }

        // ─── Consume helpers ────────────────────────────────────────────
        public void ConsumeJumpInput()     => JumpInput = false;
        public void ConsumeInteractInput() => InteractInput = false;
        public void ConsumeFlashlightInput() => FlashlightInput = false;

        /// <summary>Force-cancel crouch (e.g. when starting a sprint).</summary>
        public void CancelCrouch()
        {
            _isCrouchedState = false;
            CrouchInput = false;
        }
    }
}
