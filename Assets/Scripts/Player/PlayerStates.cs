using SpaceMaintenance.Core;
using UnityEngine;

namespace SpaceMaintenance.Player.States
{
    public abstract class PlayerState : IState
    {
        protected readonly PlayerController Player;
        
        protected PlayerState(PlayerController player)
        {
            Player = player;
        }
        
        public virtual void Enter() {}
        public virtual void Execute() {}
        public virtual void Exit() {}
    }

    public class PlayerIdleState : PlayerState
    {
        public PlayerIdleState(PlayerController player) : base(player) { }

        public override void Execute()
        {
            if (Player.Input.MoveInput.sqrMagnitude > 0.1f)
            {
                Player.ChangeState(Player.MovingState);
            }
            else if (Player.Input.JumpInput && Player.IsGrounded)
            {
                Player.ChangeState(Player.JumpingState);
            }
        }
    }

    public class PlayerMovingState : PlayerState
    {
        public PlayerMovingState(PlayerController player) : base(player) { }

        public override void Execute()
        {
            if (Player.Input.MoveInput.sqrMagnitude <= 0.1f)
            {
                Player.ChangeState(Player.IdleState);
            }
            else if (Player.Input.JumpInput && Player.IsGrounded)
            {
                Player.ChangeState(Player.JumpingState);
            }
            
            Player.Move(Player.Input.MoveInput, 1f);
        }
    }

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
            // Simple logic for returning to movement/idle
            if (Player.IsGrounded && Player.Rb.linearVelocity.y <= 0.1f)
            {
                if (Player.Input.MoveInput.sqrMagnitude > 0.1f)
                    Player.ChangeState(Player.MovingState);
                else
                    Player.ChangeState(Player.IdleState);
            }
            
            // Allow air movement
            Player.Move(Player.Input.MoveInput, 0.8f);
        }
    }

    public class PlayerCarryingState : PlayerState
    {
        public PlayerCarryingState(PlayerController player) : base(player) { }

        public override void Execute()
        {
            Player.Move(Player.Input.MoveInput, Player.Config.CarrySpeedMultiplier);
            
            // Interaction drop logic to be added later
        }
    }
}
