// ============================================================================
// SCRAPSHIFT — TaskManager.cs
// Generates, tracks, and ticks mission tasks. Networked via NetworkList so
// all clients see the same task state. Critical tasks have countdown timers;
// expiry triggers CriticalTaskFailedEvent → mission failure.
// ============================================================================

using System.Collections.Generic;
using SpaceMaintenance.Core;
using SpaceMaintenance.Core.Data;
using SpaceMaintenance.Missions;
using Unity.Netcode;
using UnityEngine;

namespace SpaceMaintenance.Tasks
{
    [RequireComponent(typeof(NetworkObject))]
    public class TaskManager : NetworkBehaviour
    {
        public static TaskManager Instance { get; private set; }

        // ─── Inspector ──────────────────────────────────────────────────
        [Header("Task Templates (optional — used when assigned)")]
        [SerializeField] private TaskData[] _taskTemplates;

        // ─── Networked State ────────────────────────────────────────────
        public NetworkList<TaskInstance> ActiveTasks;

        // ─── Config cache ───────────────────────────────────────────────
        private MissionConfig _config;

        // =================================================================
        //  LIFECYCLE
        // =================================================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // NetworkList must be initialized before OnNetworkSpawn
            ActiveTasks = new NetworkList<TaskInstance>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // Cache config from RoundManager
                if (RoundManager.Instance != null)
                    _config = RoundManager.Instance.GetConfig();

                EventBus.Subscribe<SystemRepairedEvent>(OnSystemRepaired);
                EventBus.Subscribe<MinigameCompletedEvent>(OnMinigameCompleted);
            }

            // All clients listen for list changes to update UI
            ActiveTasks.OnListChanged += OnTaskListChanged;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                EventBus.Unsubscribe<SystemRepairedEvent>(OnSystemRepaired);
                EventBus.Unsubscribe<MinigameCompletedEvent>(OnMinigameCompleted);
            }

            ActiveTasks.OnListChanged -= OnTaskListChanged;
        }

        private void Update()
        {
            if (!IsServer) return;
            TickCriticalTimers();
        }

        // =================================================================
        //  TASK GENERATION (called by MissionFlowController)
        // =================================================================

        /// <summary>Generate mission tasks based on config and scene content.</summary>
        public void GenerateTasks()
        {
            if (!IsServer) return;

            ActiveTasks.Clear();

            if (_taskTemplates != null && _taskTemplates.Length > 0)
            {
                GenerateFromTemplates();
            }
            else
            {
                GenerateFromScene();
            }

            Debug.Log($"[TaskManager] Generated {ActiveTasks.Count} tasks.");
        }

        /// <summary>Generate tasks from assigned ScriptableObject templates.</summary>
        private void GenerateFromTemplates()
        {
            foreach (var template in _taskTemplates)
            {
                if (template == null) continue;

                var task = TaskInstance.Create(
                    template.TaskId,
                    template.DisplayName,
                    template.Priority,
                    template.TimeLimit
                );

                ActiveTasks.Add(task);
                PublishTaskCreated(task);
            }
        }

        /// <summary>Auto-generate tasks by scanning the scene for repairable systems.</summary>
        private void GenerateFromScene()
        {
            int critCount = _config != null ? _config.CriticalTaskCount : 1;
            int highCount = _config != null ? _config.HighTaskCount : 2;
            int medCount  = _config != null ? _config.MediumTaskCount : 3;
            int lowCount  = _config != null ? _config.LowTaskCount : 2;
            float critTime = _config != null ? _config.CriticalTaskTimeLimit : 90f;

            // Scan scene for generators
            var generators = FindObjectsByType<ShipSystems.GeneratorController>(FindObjectsSortMode.None);
            int taskIndex = 0;

            foreach (var gen in generators)
            {
                TaskPriority priority;
                float timeLimit = 0f;

                if (taskIndex < critCount)
                {
                    priority = TaskPriority.Critical;
                    timeLimit = critTime;
                }
                else if (taskIndex < critCount + highCount)
                {
                    priority = TaskPriority.High;
                }
                else if (taskIndex < critCount + highCount + medCount)
                {
                    priority = TaskPriority.Medium;
                }
                else if (taskIndex < critCount + highCount + medCount + lowCount)
                {
                    priority = TaskPriority.Low;
                }
                else break;

                var task = TaskInstance.Create(
                    $"repair_gen_{taskIndex}",
                    $"Repair Generator {taskIndex + 1}",
                    priority,
                    timeLimit
                );

                ActiveTasks.Add(task);
                PublishTaskCreated(task);
                taskIndex++;
            }

            // Scan for broken doors
            var doors = FindObjectsByType<ShipSystems.DoorController>(FindObjectsSortMode.None);
            foreach (var door in doors)
            {
                if (door.NeedsRepair && taskIndex < critCount + highCount + medCount + lowCount)
                {
                    TaskPriority priority;
                    float timeLimit = 0f;

                    if (taskIndex < critCount)
                    {
                        priority = TaskPriority.Critical;
                        timeLimit = critTime;
                    }
                    else if (taskIndex < critCount + highCount)
                        priority = TaskPriority.High;
                    else if (taskIndex < critCount + highCount + medCount)
                        priority = TaskPriority.Medium;
                    else
                        priority = TaskPriority.Low;

                    var task = TaskInstance.Create(
                        $"repair_door_{taskIndex}",
                        $"Unjam Door",
                        priority,
                        timeLimit
                    );

                    ActiveTasks.Add(task);
                    PublishTaskCreated(task);
                    taskIndex++;
                }
            }

            // Fill remaining slots with generic system tasks
            string[] genericTasks = { "Restore Auxiliary Power", "Seal Hull Leak", "Calibrate Sensors", "Clear Debris", "Reset Comms Array" };
            int genericIdx = 0;
            while (taskIndex < critCount + highCount + medCount + lowCount && genericIdx < genericTasks.Length)
            {
                TaskPriority priority;
                float timeLimit = 0f;

                if (taskIndex < critCount)
                {
                    priority = TaskPriority.Critical;
                    timeLimit = critTime;
                }
                else if (taskIndex < critCount + highCount)
                    priority = TaskPriority.High;
                else if (taskIndex < critCount + highCount + medCount)
                    priority = TaskPriority.Medium;
                else
                    priority = TaskPriority.Low;

                var task = TaskInstance.Create(
                    $"generic_{taskIndex}",
                    genericTasks[genericIdx],
                    priority,
                    timeLimit
                );

                ActiveTasks.Add(task);
                PublishTaskCreated(task);
                taskIndex++;
                genericIdx++;
            }
        }

        // =================================================================
        //  TASK COMPLETION
        // =================================================================

        /// <summary>Complete a task by ID. Called when a system is repaired.</summary>
        public void CompleteTask(string taskId)
        {
            if (!IsServer) return;

            for (int i = 0; i < ActiveTasks.Count; i++)
            {
                var task = ActiveTasks[i];
                if (task.TaskId.ToString() == taskId && task.IsActive)
                {
                    var oldStatus = task.Status;
                    task.Status = TaskStatus.Completed;
                    ActiveTasks[i] = task;

                    EventBus.Publish(new TaskStatusChangedEvent
                    {
                        TaskId = taskId,
                        OldStatus = oldStatus,
                        NewStatus = TaskStatus.Completed
                    });

                    Debug.Log($"[TaskManager] Task completed: {task.DisplayName}");

                    if (AreAllTasksCompleted())
                    {
                        EventBus.Publish(new AllTasksCompletedEvent());
                    }
                    return;
                }
            }
        }

        /// <summary>Try to complete a task matching the given system repair name.</summary>
        public void CompleteTaskBySystemName(string systemName)
        {
            if (!IsServer) return;

            for (int i = 0; i < ActiveTasks.Count; i++)
            {
                var task = ActiveTasks[i];
                if (task.IsActive && task.DisplayName.ToString().Contains(systemName.Replace(":", " ")))
                {
                    CompleteTask(task.TaskId.ToString());
                    return;
                }
            }

            // Fallback: complete first active task of matching priority
            // (for generic tasks linked to SystemRepairedEvent)
            for (int i = 0; i < ActiveTasks.Count; i++)
            {
                var task = ActiveTasks[i];
                if (task.IsActive)
                {
                    CompleteTask(task.TaskId.ToString());
                    return;
                }
            }
        }

        /// <summary>Check if all tasks are completed or failed.</summary>
        public bool AreAllTasksCompleted()
        {
            for (int i = 0; i < ActiveTasks.Count; i++)
            {
                if (ActiveTasks[i].IsActive) return false;
            }
            return ActiveTasks.Count > 0;
        }

        /// <summary>Count completed tasks.</summary>
        public int GetCompletedCount()
        {
            int count = 0;
            for (int i = 0; i < ActiveTasks.Count; i++)
            {
                if (ActiveTasks[i].Status == TaskStatus.Completed) count++;
            }
            return count;
        }

        // =================================================================
        //  CRITICAL TASK TIMERS (Server only)
        // =================================================================

        private void TickCriticalTimers()
        {
            for (int i = 0; i < ActiveTasks.Count; i++)
            {
                var task = ActiveTasks[i];
                if (!task.IsActive || !task.HasTimer) continue;

                task.TimeRemaining -= Time.deltaTime;

                if (task.TimeRemaining <= 0f)
                {
                    task.TimeRemaining = 0f;
                    task.Status = TaskStatus.Failed;
                    ActiveTasks[i] = task;

                    EventBus.Publish(new TaskStatusChangedEvent
                    {
                        TaskId = task.TaskId.ToString(),
                        OldStatus = TaskStatus.Active,
                        NewStatus = TaskStatus.Failed
                    });

                    EventBus.Publish(new CriticalTaskFailedEvent
                    {
                        TaskId = task.TaskId.ToString()
                    });

                    Debug.Log($"[TaskManager] CRITICAL TASK FAILED: {task.DisplayName}");
                }
                else
                {
                    // Update the list entry with new timer value
                    ActiveTasks[i] = task;
                }
            }
        }

        // =================================================================
        //  EVENT HANDLERS
        // =================================================================

        private void OnSystemRepaired(SystemRepairedEvent evt)
        {
            if (!IsServer) return;
            CompleteTaskBySystemName(evt.SystemName);
        }

        private void OnMinigameCompleted(MinigameCompletedEvent evt)
        {
            if (!IsServer || !evt.Success) return;
            CompleteTaskBySystemName(evt.SystemName);
        }

        // =================================================================
        //  NETWORK LIST SYNC (all clients)
        // =================================================================

        private void OnTaskListChanged(NetworkListEvent<TaskInstance> changeEvent)
        {
            // Re-publish for UI to react
            int completed = 0;
            int total = 0;
            for (int i = 0; i < ActiveTasks.Count; i++)
            {
                total++;
                if (ActiveTasks[i].Status == TaskStatus.Completed) completed++;
            }

            EventBus.Publish(new TaskProgressUpdatedEvent
            {
                Completed = completed,
                Required = total
            });
        }

        // =================================================================
        //  HELPERS
        // =================================================================

        private void PublishTaskCreated(TaskInstance task)
        {
            EventBus.Publish(new TaskCreatedEvent
            {
                TaskId = task.TaskId.ToString(),
                DisplayName = task.DisplayName.ToString(),
                Priority = task.Priority,
                TimeLimit = task.TimeLimit
            });
        }
    }
}
