// ============================================================================
// SCRAPSHIFT — WireConnectMinigame.cs
// First repair minigame: connect colored wires on the left to their matching
// sockets on the right by clicking and dragging. Difficulty scales wire count
// and adds decoy sockets. Time limit: 10–15 seconds.
//
// UI is built programmatically — no prefab setup needed beyond the base panel.
// ============================================================================

using System.Collections.Generic;
using SpaceMaintenance.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SpaceMaintenance.Minigames.Games
{
    public class WireConnectMinigame : MinigameBase
    {
        // ─── Config ─────────────────────────────────────────────────────
        [Header("Wire Connect Settings")]
        [SerializeField] private int _baseWireCount = 4;
        [SerializeField] private int _extraWiresPerDifficulty = 1;
        [SerializeField] private int _maxWires = 8;

        [Header("Visual")]
        [SerializeField] private RectTransform _wirePanel;  // Container for wire UI

        // ─── Wire Colors ────────────────────────────────────────────────
        private static readonly Color[] WireColors = new Color[]
        {
            new Color(1f, 0.2f, 0.2f),    // Red
            new Color(0.2f, 0.6f, 1f),    // Blue
            new Color(0.2f, 1f, 0.3f),    // Green
            new Color(1f, 0.9f, 0.2f),    // Yellow
            new Color(1f, 0.5f, 0f),      // Orange
            new Color(0.7f, 0.3f, 1f),    // Purple
            new Color(1f, 1f, 1f),        // White
            new Color(0.6f, 0.6f, 0.6f)   // Grey
        };

        // ─── Runtime ────────────────────────────────────────────────────
        private int _wireCount;
        private int _connectedCount;
        private int _dragSourceIndex = -1;
        private bool _isDragging;

        // UI elements created at runtime
        private readonly List<Image> _leftPorts = new List<Image>();
        private readonly List<Image> _rightPorts = new List<Image>();
        private readonly List<Image> _connectionLines = new List<Image>();
        private int[] _correctMapping;  // leftIndex → rightIndex
        private int[] _shuffledRight;   // Shuffled order for right-side ports
        private bool[] _connected;      // Which wires are connected

        // Drag visual
        private RectTransform _dragLine;
        private Image _dragLineImage;

        // Timer display
        private TextMeshProUGUI _timerDisplay;
        private TextMeshProUGUI _titleText;

        // =================================================================
        //  MINIGAME LIFECYCLE
        // =================================================================

        private void Awake()
        {
            Type = MinigameType.WireConnect;
        }

        protected override void OnStart()
        {
            // Calculate wire count based on difficulty
            _wireCount = Mathf.Clamp(
                _baseWireCount + (Difficulty - 1) * _extraWiresPerDifficulty,
                3, Mathf.Min(_maxWires, WireColors.Length));

            _connectedCount = 0;
            _isDragging = false;
            _dragSourceIndex = -1;

            // Set time limit based on difficulty (shrinks down to 5 seconds max)
            _maxTime = Mathf.Max(5f, 15f - (Difficulty - 1) * 2f);

            // Setup mapping
            SetupMapping();

            // Build UI
            ClearUI();
            BuildUI();
        }

        protected override void OnCancel()
        {
            ClearUI();
        }

        protected override void OnTick(float deltaTime)
        {
            // Update timer display
            if (_timerDisplay != null)
            {
                float remaining = _maxTime - _elapsedTime;
                _timerDisplay.text = $"{remaining:F1}s";

                if (remaining <= 5f)
                    _timerDisplay.color = Color.Lerp(Color.red, Color.white, Mathf.PingPong(Time.unscaledTime * 3f, 1f));
            }

            // Handle drag visual
            if (_isDragging && _dragLine != null)
            {
                UpdateDragLine(Input.mousePosition);
            }

            // Handle Drag & Drop Interaction
            if (_isDragging)
            {
                if (Input.GetMouseButtonUp(0))
                {
                    int dropIndex = GetRightPortUnderMouse(Input.mousePosition);
                    if (dropIndex >= 0)
                    {
                        TryConnect(_dragSourceIndex, dropIndex);
                    }
                    
                    _isDragging = false;
                    _dragSourceIndex = -1;
                    if (_dragLine != null)
                        _dragLine.gameObject.SetActive(false);
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    int startIndex = GetLeftPortUnderMouse(Input.mousePosition);
                    if (startIndex >= 0 && !_connected[startIndex])
                    {
                        _isDragging = true;
                        _dragSourceIndex = startIndex;
                        if (_dragLineImage != null)
                        {
                            _dragLineImage.color = WireColors[startIndex % WireColors.Length];
                            _dragLine.gameObject.SetActive(true);
                            UpdateDragLine(Input.mousePosition);
                        }
                    }
                }
            }
        }

        // =================================================================
        //  MAPPING
        // =================================================================

        private void SetupMapping()
        {
            _correctMapping = new int[_wireCount];
            _shuffledRight = new int[_wireCount];
            _connected = new bool[_wireCount];

            // Create identity mapping first
            for (int i = 0; i < _wireCount; i++)
            {
                _shuffledRight[i] = i;
            }

            // Fisher-Yates shuffle for right side
            for (int i = _wireCount - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int tmp = _shuffledRight[i];
                _shuffledRight[i] = _shuffledRight[j];
                _shuffledRight[j] = tmp;
            }

            // correctMapping: for left index i, the correct right PORT index is
            // the position of i in _shuffledRight
            for (int rightIdx = 0; rightIdx < _wireCount; rightIdx++)
            {
                int colorIdx = _shuffledRight[rightIdx];
                _correctMapping[colorIdx] = rightIdx;
            }
        }

        // =================================================================
        //  UI BUILDING
        // =================================================================

        private void BuildUI()
        {
            if (_wirePanel == null)
            {
                // Create wire panel if not assigned
                var go = new GameObject("WirePanel", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                _wirePanel = go.GetComponent<RectTransform>();
                _wirePanel.anchorMin = new Vector2(0.1f, 0.15f);
                _wirePanel.anchorMax = new Vector2(0.9f, 0.85f);
                _wirePanel.offsetMin = Vector2.zero;
                _wirePanel.offsetMax = Vector2.zero;

                var img = go.GetComponent<Image>();
                img.color = new Color(0.1f, 0.1f, 0.15f, 0.95f); // Dark blue-grey background
            }

            // Title
            _titleText = CreateText("CONNECT THE WIRES", 28, TextAlignmentOptions.Center);
            var titleRT = _titleText.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.2f, 0.88f);
            titleRT.anchorMax = new Vector2(0.8f, 0.98f);
            titleRT.offsetMin = Vector2.zero;
            titleRT.offsetMax = Vector2.zero;

            // Timer
            _timerDisplay = CreateText($"{_maxTime:F1}s", 24, TextAlignmentOptions.Center);
            _timerDisplay.color = Color.white;
            var timerRT = _timerDisplay.GetComponent<RectTransform>();
            timerRT.anchorMin = new Vector2(0.4f, 0.02f);
            timerRT.anchorMax = new Vector2(0.6f, 0.12f);
            timerRT.offsetMin = Vector2.zero;
            timerRT.offsetMax = Vector2.zero;

            // Create left and right ports
            float startY = 0.8f;
            float step = 0.6f / Mathf.Max(1, _wireCount - 1);

            for (int i = 0; i < _wireCount; i++)
            {
                float yPos = startY - step * i;
                Color wireColor = WireColors[i % WireColors.Length];

                // Left port
                var leftPort = CreatePort(wireColor, new Vector2(0.08f, yPos - 0.03f), new Vector2(0.15f, yPos + 0.03f));
                int leftIdx = i;

                // Add click handler for left port (start drag)
                // Obsolete: interaction is now handled in Update()
                _leftPorts.Add(leftPort);

                // Right port (shuffled color)
                int rightColorIdx = _shuffledRight[i];
                Color rightColor = WireColors[rightColorIdx % WireColors.Length];
                var rightPort = CreatePort(rightColor, new Vector2(0.85f, yPos - 0.03f), new Vector2(0.92f, yPos + 0.03f));

                int rightIdx = i;
                // Obsolete: interaction is now handled in Update()
                _rightPorts.Add(rightPort);
            }

            // Create drag line (hidden initially)
            var dragGo = new GameObject("DragLine", typeof(RectTransform), typeof(Image));
            dragGo.transform.SetParent(_wirePanel, false);
            _dragLine = dragGo.GetComponent<RectTransform>();
            _dragLineImage = dragGo.GetComponent<Image>();
            _dragLineImage.color = Color.white;
            _dragLine.sizeDelta = new Vector2(0, 4);
            _dragLine.gameObject.SetActive(false);
        }

        private Image CreatePort(Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject("Port", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_wirePanel, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.color = color;

            return img;
        }

        private TextMeshProUGUI CreateText(string text, int fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(_wirePanel, false);

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;

            return tmp;
        }

        private void ClearUI()
        {
            _leftPorts.Clear();
            _rightPorts.Clear();
            _connectionLines.Clear();

            if (_wirePanel != null)
            {
                for (int i = _wirePanel.childCount - 1; i >= 0; i--)
                {
                    Destroy(_wirePanel.GetChild(i).gameObject);
                }
            }

            _dragLine = null;
            _dragLineImage = null;
            _timerDisplay = null;
            _titleText = null;
        }

        // =================================================================
        //  INTERACTION
        // =================================================================

        private int GetLeftPortUnderMouse(Vector2 mousePos)
        {
            for (int i = 0; i < _leftPorts.Count; i++)
            {
                if (_leftPorts[i] != null && RectTransformUtility.RectangleContainsScreenPoint(_leftPorts[i].rectTransform, mousePos, null))
                    return i;
            }
            return -1;
        }

        private int GetRightPortUnderMouse(Vector2 mousePos)
        {
            for (int i = 0; i < _rightPorts.Count; i++)
            {
                if (_rightPorts[i] != null && RectTransformUtility.RectangleContainsScreenPoint(_rightPorts[i].rectTransform, mousePos, null))
                    return i;
            }
            return -1;
        }

        private void TryConnect(int leftIndex, int rightIndex)
        {
            if (_correctMapping[leftIndex] == rightIndex)
            {
                // Correct!
                _connected[leftIndex] = true;
                _connectedCount++;

                DrawConnectionLine(leftIndex, rightIndex);

                if (_leftPorts[leftIndex] != null)
                    _leftPorts[leftIndex].color *= 0.6f;
                if (_rightPorts[rightIndex] != null)
                    _rightPorts[rightIndex].color *= 0.6f;

                Debug.Log($"[WireConnect] Connected wire {leftIndex} → {rightIndex}");

                if (_connectedCount >= _wireCount)
                {
                    Complete(true);
                }
            }
            else
            {
                Debug.Log($"[WireConnect] Wrong connection: {leftIndex} → {rightIndex}");
            }
        }

        private void UpdateDragLine(Vector2 mousePos)
        {
            if (_dragLine == null || _dragSourceIndex < 0 || _leftPorts[_dragSourceIndex] == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_wirePanel, _leftPorts[_dragSourceIndex].rectTransform.position, null, out Vector2 localStart);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_wirePanel, mousePos, null, out Vector2 localEnd);

            Vector2 dir = localEnd - localStart;
            
            _dragLine.localPosition = localStart + dir / 2f;
            
            // Un-anchor dragline so sizeDelta behaves like absolute pixels
            _dragLine.anchorMin = new Vector2(0.5f, 0.5f);
            _dragLine.anchorMax = new Vector2(0.5f, 0.5f);
            _dragLine.sizeDelta = new Vector2(dir.magnitude, 12f);
            _dragLine.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        }

        private void DrawConnectionLine(int leftIndex, int rightIndex)
        {
            if (_wirePanel == null) return;

            var go = new GameObject("Connection", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_wirePanel, false);

            var rt = go.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_wirePanel, _leftPorts[leftIndex].rectTransform.position, null, out Vector2 localStart);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_wirePanel, _rightPorts[rightIndex].rectTransform.position, null, out Vector2 localEnd);

            Vector2 dir = localEnd - localStart;
            
            rt.localPosition = localStart + dir / 2f;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(dir.magnitude, 12f);
            rt.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

            var img = go.GetComponent<Image>();
            Color lineColor = WireColors[leftIndex % WireColors.Length];
            img.color = lineColor;

            _connectionLines.Add(img);
        }
    }
}
