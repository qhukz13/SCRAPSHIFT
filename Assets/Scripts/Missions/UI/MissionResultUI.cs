// ============================================================================
// SCRAPSHIFT — MissionResultUI.cs
// End-of-mission result screen. Subscribes to GameOverEvent from EventBus.
// Displays victory/defeat, reason, and mission statistics.
// ============================================================================

using SpaceMaintenance.Core;
using SpaceMaintenance.Core.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceMaintenance.Missions.UI
{
    public class MissionResultUI : MonoBehaviour
    {
        [Header("Result Panel")]
        [SerializeField] private GameObject _resultPanel;
        [SerializeField] private TextMeshProUGUI _resultTitle;
        [SerializeField] private TextMeshProUGUI _resultDescription;
        [SerializeField] private Button _restartButton;

        [Header("Statistics")]
        [SerializeField] private TextMeshProUGUI _statsText;

        // Cached mission data for statistics display
        private float _lastTimeRemaining;
        private float _totalTime;
        private int _tasksCompleted;
        private int _tasksRequired;

        private void Awake()
        {
            if (_resultPanel != null) _resultPanel.SetActive(false);
            if (_restartButton != null) _restartButton.onClick.AddListener(OnRestartClicked);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
            EventBus.Subscribe<MissionTimerUpdatedEvent>(OnTimerUpdated);
            EventBus.Subscribe<TaskProgressUpdatedEvent>(OnTaskProgressUpdated);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
            EventBus.Unsubscribe<MissionTimerUpdatedEvent>(OnTimerUpdated);
            EventBus.Unsubscribe<TaskProgressUpdatedEvent>(OnTaskProgressUpdated);
        }

        private void OnTimerUpdated(MissionTimerUpdatedEvent evt)
        {
            _lastTimeRemaining = evt.TimeRemaining;
            _totalTime = evt.TotalTime;
        }

        private void OnTaskProgressUpdated(TaskProgressUpdatedEvent evt)
        {
            _tasksCompleted = evt.Completed;
            _tasksRequired = evt.Required;
        }

        private void OnGameOver(GameOverEvent evt)
        {
            ShowResult(evt.IsVictory, evt.Reason);
        }

        public void ShowResult(bool isVictory, string message)
        {
            if (_resultPanel != null)
            {
                _resultPanel.SetActive(true);
                
                if (_resultTitle != null)
                {
                    _resultTitle.text = isVictory ? "VICTORY" : "DEFEAT";
                    _resultTitle.color = isVictory ? Color.green : Color.red;
                }

                if (_resultDescription != null)
                {
                    _resultDescription.text = message;
                }

                // Build statistics string
                if (_statsText != null)
                {
                    float timeSurvived = _totalTime - _lastTimeRemaining;
                    int minutes = Mathf.FloorToInt(timeSurvived / 60f);
                    int seconds = Mathf.FloorToInt(timeSurvived % 60f);

                    string stats = $"Time Survived: {minutes:00}:{seconds:00}";

                    if (_tasksRequired > 0)
                    {
                        stats += $"\nTasks Completed: {_tasksCompleted} / {_tasksRequired}";
                    }

                    _statsText.text = stats;
                }
                
                Cursor.lockState = CursorLockMode.None;
            }
        }

        private void OnRestartClicked()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }
}

