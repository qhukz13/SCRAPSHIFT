// ============================================================================
// SCRAPSHIFT — MinigameManager.cs
// Singleton manager for the minigame UI overlay. Opens the correct minigame
// when a player interacts with an IMinigameRepairable, manages the Screen
// Space Overlay canvas, and handles player input lock during minigames.
// ============================================================================

using System.Collections.Generic;
using SpaceMaintenance.Core;
using SpaceMaintenance.Core.Data;
using UnityEngine;

namespace SpaceMaintenance.Minigames
{
    public class MinigameManager : MonoBehaviour
    {
        public static MinigameManager Instance { get; private set; }

        // ─── Inspector ──────────────────────────────────────────────────
        [Header("Minigame Canvas (Screen Space Overlay)")]
        [SerializeField] private Canvas _minigameCanvas;

        [Header("Registered Minigames")]
        [SerializeField] private MinigameBase _wireConnectPrefab;
        [SerializeField] private MinigameBase _pipeAlignPrefab;
        [SerializeField] private MinigameBase _sequenceInputPrefab;
        [SerializeField] private MinigameBase _pressureBalancePrefab;
        [SerializeField] private MinigameBase _circuitTracePrefab;

        [Header("Background Overlay")]
        [SerializeField] private GameObject _backgroundOverlay; // Semi-transparent dark overlay

        // ─── Runtime ────────────────────────────────────────────────────
        private MinigameBase _activeMinigame;
        private IMinigameRepairable _activeTarget;
        private readonly Dictionary<MinigameType, MinigameBase> _instances = new Dictionary<MinigameType, MinigameBase>();

        /// <summary>True while a minigame is active — used to block player input.</summary>
        public bool IsMinigameActive => _activeMinigame != null && _activeMinigame.IsActive;

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

            // Ensure canvas is overlay mode
            if (_minigameCanvas != null)
            {
                _minigameCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _minigameCanvas.sortingOrder = 100; // Above HUD
                _minigameCanvas.gameObject.SetActive(false);
            }

            if (_backgroundOverlay != null)
                _backgroundOverlay.SetActive(false);
        }

        // =================================================================
        //  PUBLIC API
        // =================================================================

        /// <summary>Request a minigame for the given IMinigameRepairable target.</summary>
        public void RequestMinigame(IMinigameRepairable target)
        {
            if (IsMinigameActive)
            {
                Debug.LogWarning("[MinigameManager] Minigame already active, ignoring request.");
                return;
            }

            _activeTarget = target;

            var minigame = GetOrCreateMinigame(target.MinigameType);
            if (minigame == null)
            {
                Debug.LogWarning($"[MinigameManager] No minigame registered for type: {target.MinigameType}");
                return;
            }

            _activeMinigame = minigame;

            // Show canvas and overlay
            if (_minigameCanvas != null)
                _minigameCanvas.gameObject.SetActive(true);
            if (_backgroundOverlay != null)
                _backgroundOverlay.SetActive(true);

            // Subscribe to completion
            _activeMinigame.OnCompleted += OnMinigameCompleted;

            // Start
            _activeMinigame.StartMinigame(target.MinigameDifficulty);

            // Lock cursor for minigame
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            EventBus.Publish(new MinigameStartedEvent
            {
                SystemName = (target as Component)?.gameObject.name ?? "Unknown",
                Target = (target as Component)?.gameObject
            });

            Debug.Log($"[MinigameManager] Opened {target.MinigameType} minigame.");
        }

        /// <summary>Force-close the active minigame.</summary>
        public void CloseMinigame()
        {
            if (_activeMinigame == null) return;

            _activeMinigame.CancelMinigame();
            CleanupMinigame(false);
        }

        // =================================================================
        //  INTERNAL
        // =================================================================

        private void OnMinigameCompleted(bool success)
        {
            if (_activeTarget != null)
            {
                if (success)
                    _activeTarget.OnMinigameCompleted();
                else
                    _activeTarget.OnMinigameFailed();
            }

            CleanupMinigame(success);
        }

        private void CleanupMinigame(bool success)
        {
            if (_activeMinigame != null)
            {
                _activeMinigame.OnCompleted -= OnMinigameCompleted;
                _activeMinigame.gameObject.SetActive(false);
                _activeMinigame = null;
            }

            _activeTarget = null;

            // Hide canvas and overlay
            if (_minigameCanvas != null)
                _minigameCanvas.gameObject.SetActive(false);
            if (_backgroundOverlay != null)
                _backgroundOverlay.SetActive(false);

            // Restore cursor for gameplay
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// <summary>Get or instantiate the minigame for the given type.</summary>
        private MinigameBase GetOrCreateMinigame(MinigameType type)
        {
            if (_instances.TryGetValue(type, out var existing))
                return existing;

            MinigameBase prefab = type switch
            {
                MinigameType.WireConnect     => _wireConnectPrefab,
                MinigameType.PipeAlign       => _pipeAlignPrefab,
                MinigameType.SequenceInput   => _sequenceInputPrefab,
                MinigameType.PressureBalance => _pressureBalancePrefab,
                MinigameType.CircuitTrace    => _circuitTracePrefab,
                _ => null
            };

            if (prefab == null) return null;

            var instance = Instantiate(prefab, _minigameCanvas.transform);
            instance.gameObject.SetActive(false);
            _instances[type] = instance;

            return instance;
        }

        // =================================================================
        //  INPUT (cancel with Escape)
        // =================================================================

        private void Update()
        {
            if (IsMinigameActive && Input.GetKeyDown(KeyCode.Escape))
            {
                CloseMinigame();
            }
        }
    }
}
