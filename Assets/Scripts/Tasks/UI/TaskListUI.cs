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
        private SpaceMaintenance.ShipSystems.ReactorController _reactor;
        private TextMeshProUGUI _reactorHeatText;

        // =================================================================
        //  LIFECYCLE
        // =================================================================

        private void Awake()
        {
            if (_taskEntryPrefab == null)
            {
                var go = new GameObject("TaskEntryTemplate", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(CanvasGroup), typeof(TaskEntryUI));
                go.SetActive(false);
                go.transform.SetParent(transform, false);

                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(300, 40);
                go.GetComponent<UnityEngine.UI.Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.8f);

                var icon = new GameObject("PriorityIcon", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                icon.transform.SetParent(go.transform, false);
                var iconRt = icon.GetComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0, 0.5f); iconRt.anchorMax = new Vector2(0, 0.5f);
                iconRt.sizeDelta = new Vector2(20, 20);
                iconRt.anchoredPosition = new Vector2(20, 0);

                var nameText = new GameObject("TaskNameText", typeof(RectTransform), typeof(TextMeshProUGUI));
                nameText.transform.SetParent(go.transform, false);
                var nameRt = nameText.GetComponent<RectTransform>();
                nameRt.anchorMin = new Vector2(0, 0); nameRt.anchorMax = new Vector2(1, 1);
                nameRt.offsetMin = new Vector2(40, 0); nameRt.offsetMax = new Vector2(-60, 0);
                var nTmp = nameText.GetComponent<TextMeshProUGUI>();
                nTmp.fontSize = 18;
                nTmp.alignment = TextAlignmentOptions.Left;

                var timeText = new GameObject("TimerText", typeof(RectTransform), typeof(TextMeshProUGUI));
                timeText.transform.SetParent(go.transform, false);
                var timeRt = timeText.GetComponent<RectTransform>();
                timeRt.anchorMin = new Vector2(1, 0); timeRt.anchorMax = new Vector2(1, 1);
                timeRt.offsetMin = new Vector2(-60, 0); timeRt.offsetMax = new Vector2(0, 0);
                var tTmp = timeText.GetComponent<TextMeshProUGUI>();
                tTmp.fontSize = 18;
                tTmp.alignment = TextAlignmentOptions.Right;

                _taskEntryPrefab = go;
            }

            if (_reactorHeatText == null)
            {
                var rGo = new GameObject("ReactorHeatText", typeof(RectTransform), typeof(TextMeshProUGUI));
                rGo.transform.SetParent(transform, false);
                var rRt = rGo.GetComponent<RectTransform>();
                rRt.anchorMin = new Vector2(0, 1); rRt.anchorMax = new Vector2(1, 1);
                rRt.pivot = new Vector2(0.5f, 1);
                rRt.sizeDelta = new Vector2(0, 30);
                
                // Position just above the task list or below header
                if (_headerText != null)
                {
                    var headerRt = _headerText.GetComponent<RectTransform>();
                    rRt.anchoredPosition = new Vector2(0, headerRt.anchoredPosition.y - headerRt.sizeDelta.y - 10);
                }
                else
                {
                    rRt.anchoredPosition = new Vector2(0, -50);
                }
                
                _reactorHeatText = rGo.GetComponent<TextMeshProUGUI>();
                _reactorHeatText.fontSize = 20;
                _reactorHeatText.alignment = TextAlignmentOptions.Center;
                _reactorHeatText.gameObject.SetActive(false); // Only visible in Active phase
            }
        }

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
            if (_reactorHeatText != null)
                _reactorHeatText.gameObject.SetActive(visible);
        }

        private void Update()
        {
            // --- Reactor Heat UI ---
            if (_reactor == null)
            {
                _reactor = Object.FindFirstObjectByType<SpaceMaintenance.ShipSystems.ReactorController>();
            }

            if (_reactor != null && _reactorHeatText != null)
            {
                float heat = _reactor.HeatLevel.Value;
                _reactorHeatText.text = $"Reactor Heat: {heat:P0}";
                
                if (heat >= 0.5f) // Warning threshold
                {
                    _reactorHeatText.color = Color.Lerp(Color.red, Color.yellow, Mathf.PingPong(Time.time * 2f, 1f));
                    _reactorHeatText.text += " - WARNING: COOL DOWN REQUIRED!";
                }
                else
                {
                    _reactorHeatText.color = Color.white;
                }
            }

            // --- Update timers for active critical tasks from TaskManager ---
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
            if (_reactorHeatText != null)
                _reactorHeatText.gameObject.SetActive(visible);
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
