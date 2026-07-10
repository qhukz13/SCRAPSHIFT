using UnityEngine;
using SpaceMaintenance.Core.Data;

namespace SpaceMaintenance.Missions
{
    [CreateAssetMenu(fileName = "MissionConfig", menuName = "SpaceMaintenance/Missions/Mission Config")]
    public class MissionConfig : ScriptableObject
    {
        public SpaceMaintenance.Core.GameMode Mode = SpaceMaintenance.Core.GameMode.Survival;
        
        [Header("Survival Settings")]
        public float SurvivalDuration = 300f; // 5 minutes
        
        [Header("Task Settings")]
        public int TasksRequired = 5;
    }
}
