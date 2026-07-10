using UnityEngine;

namespace SpaceMaintenance.Player
{
    [CreateAssetMenu(fileName = "PlayerMovementConfig", menuName = "SpaceMaintenance/Player/Movement Config")]
    public class PlayerMovementConfig : ScriptableObject
    {
        [Header("Walking")]
        public float MoveSpeed = 5f;

        [Header("Sprinting")]
        [Tooltip("Speed while holding sprint.")]
        public float SprintSpeed = 8.5f;
        [Tooltip("Maximum sprint stamina in seconds.")]
        public float MaxStamina = 5f;
        [Tooltip("Stamina recovery rate per second (while not sprinting).")]
        public float StaminaRegenRate = 1.5f;
        [Tooltip("Minimum stamina required to start sprinting again after depletion.")]
        public float StaminaCooldownThreshold = 1f;

        [Header("Jumping")]
        public float JumpForce = 7f;
        [Tooltip("Movement control while airborne (0 = none, 1 = full).")]
        [Range(0f, 1f)]
        public float AirControlMultiplier = 0.4f;

        [Header("Crouching")]
        [Tooltip("Speed while crouching.")]
        public float CrouchSpeed = 2.5f;
        [Tooltip("Height of the character collider while crouching.")]
        public float CrouchHeight = 1f;
        [Tooltip("Height of the character collider while standing.")]
        public float StandHeight = 2f;
        [Tooltip("How fast the camera lerps to crouch / stand height.")]
        public float CrouchTransitionSpeed = 10f;

        [Header("Carrying")]
        public float CarrySpeedMultiplier = 0.6f;

        [Header("Camera")]
        public float MouseSensitivity = 2f;
    }
}
