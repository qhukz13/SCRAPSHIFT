// ============================================================================
// SCRAPSHIFT — MissionConfig.cs
// ScriptableObject defining mission parameters: mode, timing, dark ship
// settings, task generation counts, and chaos system configuration.
// ============================================================================

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
        
        [Header("Task Settings (Legacy)")]
        public int TasksRequired = 5;

        [Header("Dark Ship")]
        [Tooltip("If true, the mission starts with ship unpowered. Player must start the reactor first.")]
        public bool StartDark = true;

        [Header("Task Generation")]
        [Tooltip("Number of Critical tasks (have strict timers).")]
        public int CriticalTaskCount = 1;

        [Tooltip("Number of High-priority tasks.")]
        public int HighTaskCount = 2;

        [Tooltip("Number of Medium-priority tasks.")]
        public int MediumTaskCount = 3;

        [Tooltip("Number of Low-priority (optional) tasks.")]
        public int LowTaskCount = 2;

        [Tooltip("Time limit in seconds for Critical tasks.")]
        public float CriticalTaskTimeLimit = 90f;

        [Header("Chaos")]
        [Tooltip("Delay in seconds after Active phase begins before chaos events start firing.")]
        public float ChaosStartDelay = 30f;
    }
}
