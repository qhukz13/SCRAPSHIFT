// ============================================================================
// Space Maintenance — PlayerController.cs
// Central player controller: manages movement, crouching, sprinting, stamina,
// jumping, and delegates to the state machine.
// ============================================================================

using UnityEngine;
using SpaceMaintenance.Core;
using SpaceMaintenance.Player.States;

using Unity.Netcode;

namespace SpaceMaintenance.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(PlayerInputHandler), typeof(PlayerCameraController))]
    public class PlayerController : NetworkBehaviour
    {
        // ─── Inspector ──────────────────────────────────────────────────
        [field: SerializeField] public PlayerMovementConfig Config { get; private set; }

        [Header("Ground Check")]
        [SerializeField] private Transform _groundCheck;
        [SerializeField] private LayerMask _groundLayer;

        [Header("Crouch")]
        [Tooltip("The CapsuleCollider that will be resized when crouching.")]
        [SerializeField] private CapsuleCollider _capsule;
        [Tooltip("Transform used to check for ceiling when uncrouching.")]
        [SerializeField] private Transform _ceilingCheck;

        // ─── Components ─────────────────────────────────────────────────
        public PlayerInputHandler Input { get; private set; }
        public Rigidbody Rb { get; private set; }
        public PlayerCameraController CameraController { get; private set; }

        // ─── State Machine ──────────────────────────────────────────────
        private StateMachine _stateMachine;
        public PlayerIdleState     IdleState     { get; private set; }
        public PlayerMovingState   MovingState   { get; private set; }
        public PlayerSprintState   SprintState   { get; private set; }
        public PlayerCrouchState   CrouchState   { get; private set; }
        public PlayerJumpingState  JumpingState  { get; private set; }
        public PlayerFallingState  FallingState  { get; private set; }
        public PlayerCarryingState CarryingState { get; private set; }

        // ─── Runtime ────────────────────────────────────────────────────
        public bool  IsGrounded         { get; private set; }
        public bool  IsCrouching        { get; private set; }
        public bool  IsSprinting        { get; private set; }
        public float CurrentStamina     { get; private set; }
        public bool  StaminaDepleted    { get; private set; }

        // Original heights for crouch lerp
        private float _standCameraY;
        private float _crouchCameraY;
        private float _targetCameraY;

        // =================================================================
        //  LIFECYCLE
        // =================================================================

        private void Awake()
        {
            Input           = GetComponent<PlayerInputHandler>();
            Rb              = GetComponent<Rigidbody>();
            CameraController = GetComponent<PlayerCameraController>();

            if (_capsule == null)
                _capsule = GetComponent<CapsuleCollider>();

            _stateMachine = new StateMachine();
            IdleState     = new PlayerIdleState(this);
            MovingState   = new PlayerMovingState(this);
            SprintState   = new PlayerSprintState(this);
            CrouchState   = new PlayerCrouchState(this);
            JumpingState  = new PlayerJumpingState(this);
            FallingState  = new PlayerFallingState(this);
            CarryingState = new PlayerCarryingState(this);
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                CameraController.Initialize(Input, Config);
            }
            CurrentStamina = Config.MaxStamina;

            // Cache camera heights
            _standCameraY  = CameraController.CameraLocalY;
            _crouchCameraY = _standCameraY - (Config.StandHeight - Config.CrouchHeight);
            _targetCameraY = _standCameraY;

            ChangeState(IdleState);
        }

        private float _footstepTimer;

        private void Update()
        {
            if (!IsOwner) return;

            CheckGrounded();
            _stateMachine.Update();
            CameraController.HandleCameraRotation();
            UpdateCrouchVisuals();
            UpdateStamina();
            UpdateFootsteps();
        }

        // =================================================================
        //  STATE
        // =================================================================

        public void ChangeState(PlayerState newState)
        {
            _stateMachine.ChangeState(newState);
        }

        // =================================================================
        //  MOVEMENT
        // =================================================================

        /// <summary>Move the player with a given speed multiplier.</summary>
        public void Move(Vector2 input, float speedMultiplier)
        {
            Vector3 moveDir = transform.right * input.x + transform.forward * input.y;
            Vector3 target  = moveDir.normalized * (Config.MoveSpeed * speedMultiplier);
            target.y = Rb.linearVelocity.y;
            Rb.linearVelocity = target;
        }

        /// <summary>Move at sprint speed.</summary>
        public void MoveSprint(Vector2 input)
        {
            Vector3 moveDir = transform.right * input.x + transform.forward * input.y;
            Vector3 target  = moveDir.normalized * Config.SprintSpeed;
            target.y = Rb.linearVelocity.y;
            Rb.linearVelocity = target;
        }

        /// <summary>Move at crouch speed.</summary>
        public void MoveCrouch(Vector2 input)
        {
            Vector3 moveDir = transform.right * input.x + transform.forward * input.y;
            Vector3 target  = moveDir.normalized * Config.CrouchSpeed;
            target.y = Rb.linearVelocity.y;
            Rb.linearVelocity = target;
        }

        // =================================================================
        //  JUMP
        // =================================================================

        public void Jump()
        {
            Rb.AddForce(Vector3.up * Config.JumpForce, ForceMode.Impulse);
        }

        // =================================================================
        //  CROUCH
        // =================================================================

        public void EnterCrouch()
        {
            if (IsCrouching) return;
            IsCrouching = true;
            IsSprinting = false;

            if (_capsule != null)
            {
                _capsule.height = Config.CrouchHeight;
                // Keep the bottom of the collider at the same level.
                // Assuming original center is 0 and height is StandHeight.
                float heightDiff = Config.StandHeight - Config.CrouchHeight;
                _capsule.center = new Vector3(0f, -heightDiff / 2f, 0f);
            }

            _targetCameraY = _crouchCameraY;
        }

        public void ExitCrouch()
        {
            if (!IsCrouching) return;

            // Check for ceiling before standing up
            if (IsCeilingAbove()) return;

            IsCrouching = false;

            if (_capsule != null)
            {
                _capsule.height = Config.StandHeight;
                _capsule.center = Vector3.zero;
            }

            _targetCameraY = _standCameraY;
        }

        /// <summary>Returns true if something is blocking the player from standing up.</summary>
        public bool IsCeilingAbove()
        {
            if (_ceilingCheck == null) return false;
            float checkDist = Config.StandHeight - Config.CrouchHeight;
            return Physics.Raycast(_ceilingCheck.position, Vector3.up, checkDist, _groundLayer);
        }

        // =================================================================
        //  SPRINT / STAMINA
        // =================================================================

        public void StartSprint()
        {
            if (StaminaDepleted) return;
            if (IsCrouching) ExitCrouch();
            IsSprinting = true;
            Input.CancelCrouch();
        }

        public void StopSprint()
        {
            IsSprinting = false;
        }

        /// <summary>Can the player start sprinting right now?</summary>
        public bool CanSprint()
        {
            if (StaminaDepleted) return false;
            if (CurrentStamina <= 0f) return false;
            return true;
        }

        private void UpdateStamina()
        {
            if (IsSprinting)
            {
                CurrentStamina -= Time.deltaTime;
                if (CurrentStamina <= 0f)
                {
                    CurrentStamina = 0f;
                    StaminaDepleted = true;
                    StopSprint();
                }
            }
            else
            {
                CurrentStamina = Mathf.Min(Config.MaxStamina, CurrentStamina + Config.StaminaRegenRate * Time.deltaTime);
                if (StaminaDepleted && CurrentStamina >= Config.StaminaCooldownThreshold)
                {
                    StaminaDepleted = false;
                }
            }
        }

        // =================================================================
        //  HELPERS
        // =================================================================

        private void CheckGrounded()
        {
            if (_groundCheck == null)
            {
                GameObject gc = new GameObject("GroundCheck");
                gc.transform.SetParent(transform);
                float bottomY = _capsule != null ? (_capsule.center.y - (_capsule.height / 2f)) : -1f;
                gc.transform.localPosition = new Vector3(0, bottomY, 0);
                _groundCheck = gc.transform;
            }

            if (_groundLayer.value == 0)
            {
                _groundLayer = LayerMask.GetMask("Default");
            }

            IsGrounded = false;
            Collider[] hits = Physics.OverlapSphere(_groundCheck.position, 0.2f, _groundLayer);
            foreach (var hit in hits)
            {
                if (hit.gameObject != gameObject && !hit.isTrigger)
                {
                    IsGrounded = true;
                    break;
                }
            }
        }

        private void UpdateCrouchVisuals()
        {
            CameraController.SetTargetLocalY(
                Mathf.Lerp(CameraController.CameraLocalY, _targetCameraY, Config.CrouchTransitionSpeed * Time.deltaTime)
            );
        }
        private void UpdateFootsteps()
        {
            if (!IsGrounded) return;
            
            // Calculate horizontal speed only
            Vector3 horizVel = Rb.linearVelocity;
            horizVel.y = 0;
            float speed = horizVel.magnitude;
            
            if (speed < 0.5f) return;
            
            _footstepTimer -= Time.deltaTime;
            if (_footstepTimer <= 0f)
            {
                float interval = 0.5f;
                if (IsSprinting) interval = 0.35f;
                else if (IsCrouching) interval = 0.7f;
                
                _footstepTimer = interval;
                
                if (SpaceMaintenance.Audio.AudioManager.Instance != null && SpaceMaintenance.Audio.AudioManager.Instance.Database != null)
                {
                    var steps = SpaceMaintenance.Audio.AudioManager.Instance.Database.Footsteps;
                    if (steps != null && steps.Length > 0)
                    {
                        var clip = steps[UnityEngine.Random.Range(0, steps.Length)];
                        SpaceMaintenance.Audio.AudioManager.Instance.PlaySFX(clip, transform.position, 0.5f);
                    }
                }
            }
        }
    }
}
