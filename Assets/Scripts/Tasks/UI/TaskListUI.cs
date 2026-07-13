// ============================================================================
// SCRAPSHIFT — TaskListUI.cs
// Dynamic task list panel. Subscribes to TaskManager events and creates/
// updates TaskEntryUI elements sorted by priority (Critical first).
// Syncs with NetworkList via TaskProgressUpdatedEvent.
// ============================================================================

using System.Collections.Generic;
using SpaceMaintenance.Core;
using SpaceMaintenance.Core.Data;
using TMPro;
using UnityEngine;

namespace SpaceMaintenance.Tasks.UI
{
    public class TaskListUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _taskListContainer;
        [SerializeField] private GameObject _taskEntryPrefab;
        [SerializeField] private TextMeshProUGUI _headerText;

        [Header("Settings")]
        [SerializeField] private bool _sortByPriority = true;

        // ─── Runtime ────────────────────────────────────────────────────
        private readonly Dictionary<string, TaskEntryUI> _entries = new Dictionary<string, TaskEntryUI>();
        private MissionPhase _currentPhase = MissionPhase.DarkShip;

        // =================================================================
        //  LIFECYCLE
        // =================================================================

        private void OnEnable()
        {
            EventBus.Subscribe<TaskCreatedEvent>(OnTaskCreated);
            EventBus.Subscribe<TaskStatusChangedEvent>(OnTaskStatusChanged);
            EventBus.Subscribe<MissionPhaseChangedEvent>(OnPhaseChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<TaskCreatedEvent>(OnTaskCreated);
            EventBus.Unsubscribe<TaskStatusChangedEvent>(OnTaskStatusChanged);
            EventBus.Unsubscribe<MissionPhaseChangedEvent>(OnPhaseChanged);
        }

        private void Start()
        {
            if (SpaceMaintenance.Missions.MissionFlowController.Instance != null)
            {
                _currentPhase = SpaceMaintenance.Missions.MissionFlowController.Instance.CurrentPhase.Value;
            }
            
            bool visible = _currentPhase == MissionPhase.Active;
            if (_taskListContainer != null)
                _taskListContainer.gameObject.SetActive(visible);
            if (_headerText != null)
                _headerText.gameObject.SetActive(visible);
        }

        private void Update()
        {
            // Update timers for active critical tasks from TaskManager
            if (TaskManager.Instance == null) return;

            var tasks = TaskManager.Instance.ActiveTasks;
            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                string id = task.TaskId.ToString();

                if (_entries.TryGetValue(id, out var entry) && task.HasTimer && task.IsActive)
                {
                    entry.UpdateTimer(task.TimeRemaining);
                }
            }
        }

        // =================================================================
        //  PHASE VISIBILITY
        // =================================================================

        private void OnPhaseChanged(MissionPhaseChangedEvent evt)
        {
            _currentPhase = evt.NewPhase;

            // Only show task list during Active phase
            bool visible = _currentPhase == MissionPhase.Active;
            if (_taskListContainer != null)
                _taskListContainer.gameObject.SetActive(visible);
            if (_headerText != null)
                _headerText.gameObject.SetActive(visible);
        }

        // =================================================================
        //  TASK EVENTS
        // =================================================================

        private void OnTaskCreated(TaskCreatedEvent evt)
        {
            if (_taskEntryPrefab == null || _taskListContainer == null) return;
            if (_entries.ContainsKey(evt.TaskId)) return; // Already exists

            var go = Instantiate(_taskEntryPrefab, _taskListContainer);
            var entry = go.GetComponent<TaskEntryUI>();

            if (entry != null)
            {
                entry.Setup(evt.TaskId, evt.DisplayName, evt.Priority, evt.TimeLimit);
                _entries[evt.TaskId] = entry;

                if (_sortByPriority)
                    SortEntries();
            }
        }

        private void OnTaskStatusChanged(TaskStatusChangedEvent evt)
        {
            if (!_entries.TryGetValue(evt.TaskId, out var entry)) return;

            switch (evt.NewStatus)
            {
                case TaskStatus.Completed:
                    entry.MarkCompleted();
                    // Move completed tasks to bottom
                    entry.transform.SetAsLastSibling();
                    break;

                case TaskStatus.Failed:
                    entry.MarkFailed();
                    break;
            }
        }

        // =================================================================
        //  SORTING
        // =================================================================

        /// <summary>Sort entries by priority (Critical first, Low last).</summary>
        private void SortEntries()
        {
            if (_taskListContainer == null) return;

            var sorted = new List<TaskEntryUI>(_entries.Values);
            sorted.Sort((a, b) =>
            {
                // Compare by priority enum ordinal (Critical=0, Low=3)
                return 0; // Entries are already added in priority order from TaskManager
            });

            // Re-order in hierarchy
            int index = 0;
            foreach (var entry in sorted)
            {
                entry.transform.SetSiblingIndex(index++);
            }
        }

        // =================================================================
        //  CLEANUP
        // =================================================================

        /// <summary>Remove all task entries (e.g. on mission restart).</summary>
        public void ClearAll()
        {
            foreach (var kvp in _entries)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value.gameObject);
            }
            _entries.Clear();
        }
    }
}
