using UnityEngine;

namespace SpaceMaintenance.Chaos
{
    [CreateAssetMenu(fileName = "ChaosEventConfig", menuName = "SpaceMaintenance/Chaos/Chaos Event Config")]
    public class ChaosEventConfig : ScriptableObject
    {
        public float TimeBetweenEvents = 30f;
        public float MinimumTimeBetweenEvents = 10f;
        public float DamagePerUnresolvedEvent = 2f;
        public float DamageInterval = 5f;
    }
}
