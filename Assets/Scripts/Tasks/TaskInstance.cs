// ============================================================================
// SCRAPSHIFT — TaskInstance.cs
// Runtime representation of a task. Network-serializable via
// INetworkSerializable so it can live in a NetworkList on the server.
// ============================================================================

using SpaceMaintenance.Core;
using Unity.Collections;
using Unity.Netcode;

namespace SpaceMaintenance.Tasks
{
    /// <summary>
    /// A single in-flight task instance. Stored in TaskManager's NetworkList.
    /// </summary>
    public struct TaskInstance : INetworkSerializable, System.IEquatable<TaskInstance>
    {
        public FixedString64Bytes TaskId;
        public FixedString128Bytes DisplayName;
        public TaskPriority Priority;
        public TaskStatus Status;
        public float TimeLimit;      // 0 = no limit
        public float TimeRemaining;  // Counts down for Critical tasks

        // =================================================================
        //  FACTORY
        // =================================================================

        public static TaskInstance Create(string taskId, string displayName,
            TaskPriority priority, float timeLimit = 0f)
        {
            return new TaskInstance
            {
                TaskId = new FixedString64Bytes(taskId),
                DisplayName = new FixedString128Bytes(displayName),
                Priority = priority,
                Status = TaskStatus.Active,
                TimeLimit = timeLimit,
                TimeRemaining = timeLimit
            };
        }

        // =================================================================
        //  HELPERS
        // =================================================================

        public bool HasTimer => TimeLimit > 0f;
        public bool IsTimerExpired => HasTimer && TimeRemaining <= 0f;
        public bool IsActive => Status == TaskStatus.Active;

        // =================================================================
        //  INetworkSerializable
        // =================================================================

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref TaskId);
            serializer.SerializeValue(ref DisplayName);
            serializer.SerializeValue(ref Priority);
            serializer.SerializeValue(ref Status);
            serializer.SerializeValue(ref TimeLimit);
            serializer.SerializeValue(ref TimeRemaining);
        }

        // =================================================================
        //  IEquatable
        // =================================================================

        public bool Equals(TaskInstance other)
        {
            return TaskId.Equals(other.TaskId);
        }

        public override int GetHashCode()
        {
            return TaskId.GetHashCode();
        }
    }
}
