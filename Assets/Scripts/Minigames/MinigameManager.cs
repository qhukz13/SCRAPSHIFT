// ============================================================================
// SCRAPSHIFT — MinigameManager.cs
// Singleton manager for the minigame UI overlay. Opens the correct minigame
// when a player interacts with an IMinigameRepairable, manages the Screen
// Space Overlay canvas, and handles player input lock during minigames.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using SpaceMaintenance.Core;
using SpaceMaintenance.Core.Data;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        private bool _isShowingBlockedMessage;

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
            if (IsMinigameActive || _isShowingBlockedMessage)
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

        /// <summary>Show a temporary blocked message overlay (e.g. missing required item).</summary>
        public void ShowBlockedMessage(string title, string description)
        {
            if (IsMinigameActive || _isShowingBlockedMessage) return;

            StartCoroutine(ShowBlockedMessageCoroutine(title, description));
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
            var rt = instance.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
            }
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

        // =================================================================
        //  BLOCKED MESSAGE OVERLAY
        // =================================================================

        private IEnumerator ShowBlockedMessageCoroutine(string title, string description)
        {
            _isShowingBlockedMessage = true;

            // Show canvas
            if (_minigameCanvas != null)
                _minigameCanvas.gameObject.SetActive(true);

            // Unlock cursor while message is showing
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Create overlay panel
            var overlayGO = new GameObject("BlockedOverlay", typeof(RectTransform), typeof(Image));
            overlayGO.transform.SetParent(_minigameCanvas.transform, false);

            var overlayRT = overlayGO.GetComponent<RectTransform>();
            overlayRT.anchorMin = Vector2.zero;
            overlayRT.anchorMax = Vector2.one;
            overlayRT.offsetMin = Vector2.zero;
            overlayRT.offsetMax = Vector2.zero;

            overlayGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            // Title text
            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(overlayGO.transform, false);

            var titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.15f, 0.52f);
            titleRT.anchorMax = new Vector2(0.85f, 0.68f);
            titleRT.offsetMin = Vector2.zero;
            titleRT.offsetMax = Vector2.zero;

            var titleTMP = titleGO.GetComponent<TextMeshProUGUI>();
            titleTMP.text = title;
            titleTMP.fontSize = 48;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.color = new Color(1f, 0.3f, 0.3f); // Red
            titleTMP.fontStyle = FontStyles.Bold;

            // Description text
            var descGO = new GameObject("Description", typeof(RectTransform), typeof(TextMeshProUGUI));
            descGO.transform.SetParent(overlayGO.transform, false);

            var descRT = descGO.GetComponent<RectTransform>();
            descRT.anchorMin = new Vector2(0.2f, 0.35f);
            descRT.anchorMax = new Vector2(0.8f, 0.52f);
            descRT.offsetMin = Vector2.zero;
            descRT.offsetMax = Vector2.zero;

            var descTMP = descGO.GetComponent<TextMeshProUGUI>();
            descTMP.text = description;
            descTMP.fontSize = 24;
            descTMP.alignment = TextAlignmentOptions.Center;
            descTMP.color = new Color(0.8f, 0.8f, 0.8f);

            // Wait 3 seconds
            yield return new WaitForSecondsRealtime(3f);

            // Cleanup
            Destroy(overlayGO);

            if (_minigameCanvas != null)
                _minigameCanvas.gameObject.SetActive(false);

            // Restore cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            _isShowingBlockedMessage = false;
        }
    }
}
