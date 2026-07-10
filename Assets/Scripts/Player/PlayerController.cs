using UnityEngine;
using SpaceMaintenance.Core;
using SpaceMaintenance.Player.States;

namespace SpaceMaintenance.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(PlayerInputHandler), typeof(PlayerCameraController))]
    public class PlayerController : MonoBehaviour // Revert to MonoBehaviour temporarily if Unity.Netcode is missing
    {
        [field: SerializeField] public PlayerMovementConfig Config { get; private set; }
        
        public PlayerInputHandler Input { get; private set; }
        public Rigidbody Rb { get; private set; }
        public PlayerCameraController CameraController { get; private set; }
        
        // State Machine
        private StateMachine _stateMachine;
        public PlayerIdleState IdleState { get; private set; }
        public PlayerMovingState MovingState { get; private set; }
        public PlayerJumpingState JumpingState { get; private set; }
        public PlayerCarryingState CarryingState { get; private set; }

        public bool IsGrounded { get; private set; }
        [SerializeField] private Transform _groundCheck;
        [SerializeField] private LayerMask _groundLayer;

        private void Awake()
        {
            Input = GetComponent<PlayerInputHandler>();
            Rb = GetComponent<Rigidbody>();
            CameraController = GetComponent<PlayerCameraController>();

            _stateMachine = new StateMachine();
            IdleState = new PlayerIdleState(this);
            MovingState = new PlayerMovingState(this);
            JumpingState = new PlayerJumpingState(this);
            CarryingState = new PlayerCarryingState(this);
        }

        private void Start()
        {
            // Normally called OnNetworkSpawn for NetworkBehaviour
            CameraController.Initialize(Input, Config);
            ChangeState(IdleState);
        }

        private void Update()
        {
            // if (!IsOwner) return; // Add back when using NetworkBehaviour

            CheckGrounded();
            _stateMachine.Update();
            CameraController.HandleCameraRotation();
        }
        
        public void ChangeState(PlayerState newState)
        {
            _stateMachine.ChangeState(newState);
        }

        public void Move(Vector2 input, float speedMultiplier)
        {
            Vector3 moveDirection = transform.right * input.x + transform.forward * input.y;
            Vector3 targetVelocity = moveDirection.normalized * (Config.MoveSpeed * speedMultiplier);
            
            targetVelocity.y = Rb.linearVelocity.y;
            Rb.linearVelocity = targetVelocity;
        }

        public void Jump()
        {
            Rb.AddForce(Vector3.up * Config.JumpForce, ForceMode.Impulse);
        }

        private void CheckGrounded()
        {
            if (_groundCheck == null) return;
            IsGrounded = Physics.CheckSphere(_groundCheck.position, 0.2f, _groundLayer);
        }
    }
}
