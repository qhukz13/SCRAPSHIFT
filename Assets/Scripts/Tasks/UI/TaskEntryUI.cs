// ============================================================================
// SCRAPSHIFT — TaskEntryUI.cs
// Single task entry in the task list panel. Shows priority color-coded icon,
// task name, and a countdown timer for Critical tasks. Animates completion.
// ============================================================================

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SpaceMaintenance.Core;

namespace SpaceMaintenance.Tasks.UI
{
    public class TaskEntryUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image _priorityIcon;
        [SerializeField] private TextMeshProUGUI _taskNameText;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Priority Colors")]
        [SerializeField] private Color _criticalColor = new Color(1f, 0.15f, 0.15f);
        [SerializeField] private Color _highColor     = new Color(1f, 0.6f, 0.1f);
        [SerializeField] private Color _mediumColor   = new Color(1f, 0.9f, 0.2f);
        [SerializeField] private Color _lowColor      = new Color(0.5f, 0.5f, 0.5f);

        [Header("State Colors")]
        [SerializeField] private Color _activeBackground   = new Color(0.1f, 0.1f, 0.15f, 0.8f);
        [SerializeField] private Color _completedBackground = new Color(0.05f, 0.2f, 0.05f, 0.6f);
        [SerializeField] private Color _failedBackground   = new Color(0.25f, 0.05f, 0.05f, 0.6f);

        // ─── State ──────────────────────────────────────────────────────
        private string _taskId;
        private TaskPriority _priority;
        private TaskStatus _status;
        private bool _hasTimer;
        private float _pulseTimer;

        public string TaskId => _taskId;

        // =================================================================
        //  SETUP
        // =================================================================

        private void Awake()
        {
            if (_backgroundImage == null) _backgroundImage = GetComponent<Image>();
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            
            if (_priorityIcon == null) 
            {
                var t = transform.Find("PriorityIcon");
                if (t != null) _priorityIcon = t.GetComponent<Image>();
            }
            if (_taskNameText == null) 
            {
                var t = transform.Find("TaskNameText");
                if (t != null) _taskNameText = t.GetComponent<TextMeshProUGUI>();
            }
            if (_timerText == null) 
            {
                var t = transform.Find("TimerText");
                if (t != null) _timerText = t.GetComponent<TextMeshProUGUI>();
            }
        }

        /// <summary>Initialize this entry with task data.</summary>
        public void Setup(string taskId, string displayName, TaskPriority priority, float timeLimit)
        {
            _taskId = taskId;
            _priority = priority;
            _hasTimer = timeLimit > 0f;
            _status = TaskStatus.Active;

            if (_taskNameText != null)
                _taskNameText.text = displayName;

            if (_timerText != null)
            {
                _timerText.gameObject.SetActive(_hasTimer);
                if (_hasTimer)
                    UpdateTimerDisplay(timeLimit);
            }

            UpdatePriorityVisual();
            UpdateStatusVisual();
        }

        // =================================================================
        //  UPDATES
        // =================================================================

        /// <summary>Update the countdown timer display.</summary>
        public void UpdateTimer(float timeRemaining)
        {
            if (!_hasTimer || _timerText == null) return;
            UpdateTimerDisplay(timeRemaining);

            // Pulse effect when timer is low
            if (timeRemaining <= 30f && timeRemaining > 0f)
            {
                _pulseTimer += Time.deltaTime;
                float pulse = Mathf.PingPong(_pulseTimer * 3f, 1f);
                if (_taskNameText != null)
                    _taskNameText.color = Color.Lerp(Color.white, _criticalColor, pulse);
                if (_timerText != null)
                    _timerText.color = Color.Lerp(Color.white, _criticalColor, pulse);
            }
        }

        /// <summary>Mark this task as completed.</summary>
        public void MarkCompleted()
        {
            _status = TaskStatus.Completed;
            UpdateStatusVisual();

            if (_taskNameText != null)
            {
                _taskNameText.text = $"<s>{_taskNameText.text}</s>";
                _taskNameText.color = new Color(0.4f, 0.7f, 0.4f);
            }

            if (_timerText != null)
                _timerText.gameObject.SetActive(false);

            if (_canvasGroup != null)
                _canvasGroup.alpha = 0.6f;
        }

        /// <summary>Mark this task as failed.</summary>
        public void MarkFailed()
        {
            _status = TaskStatus.Failed;
            UpdateStatusVisual();

            if (_taskNameText != null)
            {
                _taskNameText.text = $"✗ {_taskNameText.text}";
                _taskNameText.color = _criticalColor;
            }

            if (_timerText != null)
            {
                _timerText.text = "FAILED";
                _timerText.color = _criticalColor;
            }
        }

        // =================================================================
        //  VISUAL HELPERS
        // =================================================================

        private void UpdatePriorityVisual()
        {
            if (_priorityIcon == null) return;

            _priorityIcon.color = _priority switch
            {
                TaskPriority.Critical => _criticalColor,
                TaskPriority.High     => _highColor,
                TaskPriority.Medium   => _mediumColor,
                TaskPriority.Low      => _lowColor,
                _ => _lowColor
            };
        }

        private void UpdateStatusVisual()
        {
            if (_backgroundImage == null) return;

            _backgroundImage.color = _status switch
            {
                TaskStatus.Completed => _completedBackground,
                TaskStatus.Failed    => _failedBackground,
                _                    => _activeBackground
            };
        }

        private void UpdateTimerDisplay(float seconds)
        {
            if (_timerText == null) return;
            int min = Mathf.FloorToInt(seconds / 60f);
            int sec = Mathf.FloorToInt(seconds % 60f);
            _timerText.text = $"{min:0}:{sec:00}";
        }
    }
}
