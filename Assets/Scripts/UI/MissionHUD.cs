// ============================================================================
// SCRAPSHIFT — MissionHUD.cs
// In-game HUD displaying mission timer, hull integrity bar, and task counter.
// Phase-aware: hides during DarkShip, shows prompt during ReactorStartup,
// fully visible during Active phase. Event-driven via EventBus.
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

        [Header("Dark Ship Phase")]
        [SerializeField] private GameObject _darkShipPrompt;   // "FIND THE REACTOR" text/panel
        [SerializeField] private GameObject _startupPrompt;    // "REACTOR STARTING..." text/panel

        [Header("Main HUD Container")]
        [SerializeField] private GameObject _mainHudPanel;     // Parent panel for timer/hull/tasks

        // Cached values for warning logic
        private float _lastTimeRemaining;
        private float _lastHullPercent = 1f;
        private bool _isGameOver = false;
        private MissionPhase _currentPhase = MissionPhase.DarkShip;

        private void Awake()
        {
            if (_darkShipPrompt == null || _startupPrompt == null)
            {
                Debug.LogWarning("[MissionHUD] _darkShipPrompt or _startupPrompt is missing! Please assign them in the Inspector.");
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<MissionTimerUpdatedEvent>(OnTimerUpdated);
            EventBus.Subscribe<HullIntegrityUpdatedEvent>(OnHullUpdated);
            EventBus.Subscribe<TaskProgressUpdatedEvent>(OnTaskProgressUpdated);
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
            EventBus.Subscribe<MissionPhaseChangedEvent>(OnPhaseChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<MissionTimerUpdatedEvent>(OnTimerUpdated);
            EventBus.Unsubscribe<HullIntegrityUpdatedEvent>(OnHullUpdated);
            EventBus.Unsubscribe<TaskProgressUpdatedEvent>(OnTaskProgressUpdated);
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
            EventBus.Unsubscribe<MissionPhaseChangedEvent>(OnPhaseChanged);
        }

        private void Start()
        {
            if (Missions.MissionFlowController.Instance != null)
            {
                _currentPhase = Missions.MissionFlowController.Instance.CurrentPhase.Value;
            }
            UpdatePhaseVisibility();
        }

        // =====================================================================
        // Mission Phase
        // =====================================================================
        private void OnPhaseChanged(MissionPhaseChangedEvent evt)
        {
            _currentPhase = evt.NewPhase;
            UpdatePhaseVisibility();
        }

        private void UpdatePhaseVisibility()
        {
            switch (_currentPhase)
            {
                case MissionPhase.DarkShip:
                    // Hide everything except the "Find the Reactor" prompt
                    SetMainHudVisible(false);
                    SetDarkShipPromptVisible(true);
                    SetStartupPromptVisible(false);
                    break;

                case MissionPhase.ReactorStartup:
                    // Show "Reactor Starting..." prompt
                    SetMainHudVisible(false);
                    SetDarkShipPromptVisible(false);
                    SetStartupPromptVisible(true);
                    break;

                case MissionPhase.Active:
                    // Full HUD
                    SetMainHudVisible(true);
                    SetDarkShipPromptVisible(false);
                    SetStartupPromptVisible(false);
                    break;

                case MissionPhase.Completed:
                case MissionPhase.Failed:
                    // Hide prompts, main HUD stays for result screen to overlay
                    SetDarkShipPromptVisible(false);
                    SetStartupPromptVisible(false);
                    break;
            }
        }

        private void SetMainHudVisible(bool visible)
        {
            if (_mainHudPanel != null) _mainHudPanel.SetActive(visible);
            // Fallback: toggle individual elements if no parent panel
            else
            {
                if (_timerText != null) _timerText.gameObject.SetActive(visible);
                if (_hullFillBar != null) _hullFillBar.gameObject.SetActive(visible);
                if (_taskPanel != null) _taskPanel.SetActive(visible);
            }
        }

        private void SetDarkShipPromptVisible(bool visible)
        {
            if (_darkShipPrompt != null) _darkShipPrompt.SetActive(visible);
        }

        private void SetStartupPromptVisible(bool visible)
        {
            if (_startupPrompt != null) _startupPrompt.SetActive(visible);
        }

        // =====================================================================
        // Timer
        // =====================================================================
        private void OnTimerUpdated(MissionTimerUpdatedEvent evt)
        {
            if (_isGameOver || _currentPhase != MissionPhase.Active) return;

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
            if (_isGameOver || _currentPhase != MissionPhase.Active) return;

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
            if (_currentPhase != MissionPhase.Active) return;

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
