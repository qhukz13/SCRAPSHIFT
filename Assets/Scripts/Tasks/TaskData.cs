// ============================================================================
// SCRAPSHIFT — TaskData.cs
// ScriptableObject template defining a task type. Used by TaskManager to
// generate runtime TaskInstances during the Active mission phase.
// ============================================================================

using SpaceMaintenance.Core;
using UnityEngine;

namespace SpaceMaintenance.Tasks
{
    [CreateAssetMenu(fileName = "TaskData", menuName = "SpaceMaintenance/Tasks/Task Data")]
    public class TaskData : ScriptableObject
    {
        [Tooltip("Unique identifier for this task type.")]
        public string TaskId;

        [Tooltip("Display name shown in the task list UI.")]
        public string DisplayName;

        [TextArea(2, 4)]
        [Tooltip("Short description of what needs to be done.")]
        public string Description;

        [Tooltip("Priority tier of this task.")]
        public TaskPriority Priority;

        [Tooltip("Which ship system this task targets.")]
        public ShipSystemType TargetSystem;

        [Tooltip("Time limit in seconds. 0 = no limit. Only Critical tasks should have timers.")]
        public float TimeLimit;

        [Tooltip("Optional icon for the task list UI.")]
        public Sprite Icon;
    }
}
