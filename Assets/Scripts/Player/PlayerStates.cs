// ============================================================================
// Space Maintenance — PlayerStates.cs
// All player states for the state machine:
// Idle, Moving, Sprinting, Crouching, Jumping, Falling, Carrying.
// ============================================================================

using SpaceMaintenance.Core;
using UnityEngine;

namespace SpaceMaintenance.Player.States
{
    // =====================================================================
    //  BASE
    // =====================================================================

    public abstract class PlayerState : IState
    {
        protected readonly PlayerController Player;

        protected PlayerState(PlayerController player)
        {
            Player = player;
        }

        public virtual void Enter() { }
        public virtual void Execute() { }
        public virtual void Exit() { }

        // ─── Common transition checks ───────────────────────────────────

        /// <summary>Check if the player should start jumping.</summary>
        protected bool TryJump()
        {
            if (Player.Input.JumpInput && Player.IsGrounded)
            {
                // Uncrouch before jumping
                if (Player.IsCrouching)
                    Player.ExitCrouch();

                Player.ChangeState(Player.JumpingState);
                return true;
            }
            return false;
        }

        /// <summary>Returns true if there is movement input.</summary>
        protected bool HasMoveInput()
        {
            return Player.Input.MoveInput.sqrMagnitude > 0.01f;
        }
    }

    // =====================================================================
    //  IDLE — standing still
    // =====================================================================

    public class PlayerIdleState : PlayerState
    {
        public PlayerIdleState(PlayerController player) : base(player) { }

        public override void Enter()
        {
            // Zero out horizontal velocity when entering idle
            var vel = Player.Rb.linearVelocity;
            Player.Rb.linearVelocity = new Vector3(0f, vel.y, 0f);
        }

        public override void Execute()
        {
            // ── Transitions (priority order) ────────────────────────────
            if (TryJump()) return;

            if (!Player.IsGrounded && Player.Rb.linearVelocity.y < -0.5f)
            {
                Player.ChangeState(Player.FallingState);
                return;
            }

            if (Player.Input.CrouchInput)
            {
                Player.ChangeState(Player.CrouchState);
                return;
            }

            if (HasMoveInput())
            {
                // Sprint?
                if (Player.Input.SprintInput && Player.CanSprint())
                {
                    Player.ChangeState(Player.SprintState);
                }
                else
                {
                    Player.ChangeState(Player.MovingState);
                }
                return;
            }
        }
    }

    // =====================================================================
    //  MOVING — normal walk
    // =====================================================================

    public class PlayerMovingState : PlayerState
    {
        public PlayerMovingState(PlayerController player) : base(player) { }

        public override void Execute()
        {
            // ── Transitions ─────────────────────────────────────────────
            if (TryJump()) return;

            if (!Player.IsGrounded && Player.Rb.linearVelocity.y < -0.5f)
            {
                Player.ChangeState(Player.FallingState);
                return;
            }

            if (!HasMoveInput())
            {
                Player.ChangeState(Player.IdleState);
                return;
            }

            if (Player.Input.CrouchInput)
            {
                Player.ChangeState(Player.CrouchState);
                return;
            }

            if (Player.Input.SprintInput && Player.CanSprint())
            {
                Player.ChangeState(Player.SprintState);
                return;
            }

            // ── Move ────────────────────────────────────────────────────
            Player.Move(Player.Input.MoveInput, 1f);
        }
    }

    // =====================================================================
    //  SPRINT — fast movement, drains stamina
    // =====================================================================

    public class PlayerSprintState : PlayerState
    {
        public PlayerSprintState(PlayerController player) : base(player) { }

        public override void Enter()
        {
            Player.StartSprint();
        }

        public override void Execute()
        {
            // ── Transitions ─────────────────────────────────────────────
            if (TryJump()) return;

            if (!Player.IsGrounded && Player.Rb.linearVelocity.y < -0.5f)
            {
                Player.StopSprint();
                Player.ChangeState(Player.FallingState);
                return;
            }

            // Stop sprinting conditions
            if (!Player.Input.SprintInput || !Player.CanSprint() || !HasMoveInput())
            {
                Player.StopSprint();

                if (!HasMoveInput())
                    Player.ChangeState(Player.IdleState);
                else
                    Player.ChangeState(Player.MovingState);
                return;
            }

            if (Player.Input.CrouchInput)
            {
                Player.StopSprint();
                Player.ChangeState(Player.CrouchState);
                return;
            }

            // ── Move at sprint speed ────────────────────────────────────
            Player.MoveSprint(Player.Input.MoveInput);
        }

        public override void Exit()
        {
            Player.StopSprint();
        }
    }

    // =====================================================================
    //  CROUCH — slow movement, reduced height
    // =====================================================================

    public class PlayerCrouchState : PlayerState
    {
        public PlayerCrouchState(PlayerController player) : base(player) { }

        public override void Enter()
        {
            Player.EnterCrouch();
        }

        public override void Execute()
        {
            // ── Transitions ─────────────────────────────────────────────

            // Uncrouch if input released (and no ceiling above)
            if (!Player.Input.CrouchInput && !Player.IsCeilingAbove())
            {
                Player.ExitCrouch();

                if (HasMoveInput())
                    Player.ChangeState(Player.MovingState);
                else
                    Player.ChangeState(Player.IdleState);
                return;
            }

            // Jump while crouched — stand up + jump
            if (Player.Input.JumpInput && Player.IsGrounded && !Player.IsCeilingAbove())
            {
                Player.ExitCrouch();
                Player.ChangeState(Player.JumpingState);
                return;
            }

            if (!Player.IsGrounded && Player.Rb.linearVelocity.y < -0.5f)
            {
                Player.ChangeState(Player.FallingState);
                return;
            }

            // ── Move at crouch speed ────────────────────────────────────
            if (HasMoveInput())
            {
                Player.MoveCrouch(Player.Input.MoveInput);
            }
            else
            {
                // Slow to stop
                var vel = Player.Rb.linearVelocity;
                Player.Rb.linearVelocity = new Vector3(0f, vel.y, 0f);
            }
        }

        public override void Exit()
        {
            // ExitCrouch is called explicitly before state change
        }
    }

    // =====================================================================
    //  JUMPING — upward arc after pressing jump
    // =====================================================================

    public class PlayerJumpingState : PlayerState
    {
        public PlayerJumpingState(PlayerController player) : base(player) { }

        public override void Enter()
        {
            Player.Jump();
            Player.Input.ConsumeJumpInput();
        }

        public override void Execute()
        {
            // Transition to Falling once we start descending
            if (Player.Rb.linearVelocity.y <= 0.1f)
            {
                Player.ChangeState(Player.FallingState);
                return;
            }

            // Air control
            if (HasMoveInput())
            {
                Player.Move(Player.Input.MoveInput, Player.Config.AirControlMultiplier);
            }
        }
    }

    // =====================================================================
    //  FALLING — descending (from jump peak or walking off edge)
    // =====================================================================

    public class PlayerFallingState : PlayerState
    {
        public PlayerFallingState(PlayerController player) : base(player) { }

        public override void Execute()
        {
            // Land
            if (Player.IsGrounded)
            {
                if (HasMoveInput())
                {
                    if (Player.Input.SprintInput && Player.CanSprint())
                        Player.ChangeState(Player.SprintState);
                    else
                        Player.ChangeState(Player.MovingState);
                }
                else
                {
                    Player.ChangeState(Player.IdleState);
                }
                return;
            }

            // Air control
            if (HasMoveInput())
            {
                Player.Move(Player.Input.MoveInput, Player.Config.AirControlMultiplier);
            }
        }
    }

    // =====================================================================
    //  CARRYING — holding a physics object (reduced speed)
    // =====================================================================

    public class PlayerCarryingState : PlayerState
    {
        public PlayerCarryingState(PlayerController player) : base(player) { }

        public override void Execute()
        {
            float speedMult = Player.Config.CarrySpeedMultiplier;
            bool canJump = true;

            var grabCtrl = Player.GetComponent<PhysicsGrabController>();
            if (grabCtrl != null && grabCtrl.GrabbedObject is SpaceMaintenance.ShipSystems.HeavyFuse fuse)
            {
                if (fuse.GrabberClientIds.Count >= 2)
                {
                    speedMult = 0.8f; // Two players carry it well
                }
                else
                {
                    speedMult = 0.2f; // Single player struggles
                    canJump = false; // Too heavy to jump
                }
            }

            // Cannot sprint or crouch while carrying
            Player.Move(Player.Input.MoveInput, speedMult);

            if (canJump && Player.Input.JumpInput && Player.IsGrounded)
            {
                Player.Jump();
                Player.Input.ConsumeJumpInput();
            }

            // Drop logic handled by PhysicsGrabController
        }
    }

    // =====================================================================
    //  GLUED — forced to follow the leader while carrying Heavy Fuse
    // =====================================================================

    public class PlayerGluedState : PlayerState
    {
        public PlayerController Leader { get; set; }

        public PlayerGluedState(PlayerController player) : base(player) { }

        public override void Enter()
        {
            Player.Rb.useGravity = false;
            Player.Rb.linearVelocity = Vector3.zero;
        }

        public override void Execute()
        {
            if (Leader == null)
            {
                Player.ChangeState(Player.IdleState);
                return;
            }

            // Lock position in front of the leader, facing the leader
            Vector3 targetPos = Leader.transform.position + Leader.transform.forward * 1.5f;
            Vector3 lookPos = Leader.transform.position;
            lookPos.y = Player.transform.position.y;
            
            Player.Rb.MovePosition(targetPos);
            Player.Rb.MoveRotation(Quaternion.LookRotation(lookPos - Player.transform.position));
            
            // Prevent normal movement inputs
            Player.Rb.linearVelocity = Vector3.zero;
        }

        public override void Exit()
        {
            Player.Rb.useGravity = true;
            Leader = null;
        }
    }
}
