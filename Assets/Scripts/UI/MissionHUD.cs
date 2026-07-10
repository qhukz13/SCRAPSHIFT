// ============================================================================
// SCRAPSHIFT — MissionHUD.cs
// In-game HUD displaying mission timer, hull integrity bar, and task counter.
// Fully event-driven — subscribes to EventBus, no direct manager references.
// ============================================================================

using SpaceMaintenance.Core;
using SpaceMaintenance.Core.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceMaintenance.UI
{
    public class MissionHUD : MonoBehaviour
    {
        [Header("Timer")]
        [SerializeField] private TextMeshProUGUI _timerText;

        [Header("Hull Integrity")]
        [SerializeField] private Image _hullFillBar;
        [SerializeField] private TextMeshProUGUI _hullText;
        [SerializeField] private Color _hullHealthyColor = Color.green;
        [SerializeField] private Color _hullDangerColor = Color.red;
        [SerializeField] private float _hullDangerThreshold = 0.3f;

        [Header("Task Counter (Tasks mode only)")]
        [SerializeField] private GameObject _taskPanel;
        [SerializeField] private TextMeshProUGUI _taskText;

        [Header("Warnings")]
        [SerializeField] private GameObject _warningPanel;
        [SerializeField] private TextMeshProUGUI _warningText;
        [SerializeField] private float _lowTimeThreshold = 30f;

        // Cached values for warning logic
        private float _lastTimeRemaining;
        private float _lastHullPercent = 1f;
        private bool _isGameOver = false;

        private void OnEnable()
        {
            EventBus.Subscribe<MissionTimerUpdatedEvent>(OnTimerUpdated);
            EventBus.Subscribe<HullIntegrityUpdatedEvent>(OnHullUpdated);
            EventBus.Subscribe<TaskProgressUpdatedEvent>(OnTaskProgressUpdated);
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<MissionTimerUpdatedEvent>(OnTimerUpdated);
            EventBus.Unsubscribe<HullIntegrityUpdatedEvent>(OnHullUpdated);
            EventBus.Unsubscribe<TaskProgressUpdatedEvent>(OnTaskProgressUpdated);
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
        }

        // =====================================================================
        // Timer
        // =====================================================================
        private void OnTimerUpdated(MissionTimerUpdatedEvent evt)
        {
            if (_isGameOver) return;

            _lastTimeRemaining = evt.TimeRemaining;

            if (_timerText != null)
            {
                int minutes = Mathf.FloorToInt(evt.TimeRemaining / 60f);
                int seconds = Mathf.FloorToInt(evt.TimeRemaining % 60f);
                _timerText.text = $"{minutes:00}:{seconds:00}";

                // Flash red when time is critically low
                if (evt.TimeRemaining <= _lowTimeThreshold)
                {
                    _timerText.color = Color.Lerp(Color.red, Color.white, Mathf.PingPong(Time.time * 2f, 1f));
                }
                else
                {
                    _timerText.color = Color.white;
                }
            }

            UpdateWarnings();
        }

        // =====================================================================
        // Hull Integrity
        // =====================================================================
        private void OnHullUpdated(HullIntegrityUpdatedEvent evt)
        {
            if (_isGameOver) return;

            float percent = evt.Max > 0 ? evt.Current / evt.Max : 0f;
            _lastHullPercent = percent;

            if (_hullFillBar != null)
            {
                _hullFillBar.fillAmount = percent;
                _hullFillBar.color = Color.Lerp(_hullDangerColor, _hullHealthyColor, percent);
            }

            if (_hullText != null)
            {
                _hullText.text = $"{Mathf.CeilToInt(evt.Current)} / {Mathf.CeilToInt(evt.Max)}";
            }

            UpdateWarnings();
        }

        // =====================================================================
        // Task Counter
        // =====================================================================
        private void OnTaskProgressUpdated(TaskProgressUpdatedEvent evt)
        {
            if (_isGameOver) return;

            if (_taskPanel != null)
            {
                // Only show task counter if there are required tasks
                _taskPanel.SetActive(evt.Required > 0);
            }

            if (_taskText != null)
            {
                _taskText.text = $"{evt.Completed} / {evt.Required}";
            }
        }

        // =====================================================================
        // Game Over — hide HUD
        // =====================================================================
        private void OnGameOver(GameOverEvent evt)
        {
            _isGameOver = true;

            // Optionally hide the HUD when the result screen appears
            if (_warningPanel != null) _warningPanel.SetActive(false);
        }

        // =====================================================================
        // Warning System
        // =====================================================================
        private void UpdateWarnings()
        {
            if (_warningPanel == null || _warningText == null) return;

            // Hull critical warning takes priority
            if (_lastHullPercent <= _hullDangerThreshold)
            {
                _warningPanel.SetActive(true);
                _warningText.text = "⚠ HULL CRITICAL ⚠";
                _warningText.color = Color.red;
                return;
            }

            // Low time warning
            if (_lastTimeRemaining <= _lowTimeThreshold && _lastTimeRemaining > 0)
            {
                _warningPanel.SetActive(true);
                _warningText.text = "⏱ TIME RUNNING OUT";
                _warningText.color = Color.yellow;
                return;
            }

            _warningPanel.SetActive(false);
        }
    }
}
