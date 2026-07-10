using UnityEngine;

namespace SpaceMaintenance.Player
{
    [CreateAssetMenu(fileName = "PlayerMovementConfig", menuName = "SpaceMaintenance/Player/Movement Config")]
    public class PlayerMovementConfig : ScriptableObject
    {
        public float MoveSpeed = 5f;
        public float JumpForce = 10f;
        public float CarrySpeedMultiplier = 0.6f;
        public float MouseSensitivity = 2f;
    }
}
