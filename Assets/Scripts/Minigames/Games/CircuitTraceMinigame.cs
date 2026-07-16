using System.Collections.Generic;
using SpaceMaintenance.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SpaceMaintenance.Minigames.Games
{
    public class CircuitTraceMinigame : MinigameBase
    {
        // ─── Config ─────────────────────────────────────────────────────
        [Header("Circuit Settings")]
        [SerializeField] private int _gridSizeX = 4;
        [SerializeField] private int _gridSizeY = 4;

        [Header("Visual")]
        [SerializeField] private RectTransform _panel;

        // ─── Runtime ────────────────────────────────────────────────────
        private List<Button> _nodes = new List<Button>();
        private List<int> _currentPath = new List<int>();
        private int _startNode;
        private int _endNode;

        private TextMeshProUGUI _timerDisplay;
        private TextMeshProUGUI _statusDisplay;

        // =================================================================
        //  MINIGAME LIFECYCLE
        // =================================================================

        private void Awake()
        {
            Type = MinigameType.CircuitTrace;
        }

        protected override void OnStart()
        {
            if (Difficulty > 1)
            {
                _gridSizeX = 4 + (Difficulty - 1);
                _gridSizeY = 4 + (Difficulty - 1);
            }
            
            _maxTime = 20f + (Difficulty * 5f);
            
            _currentPath.Clear();

            // Simple start/end
            _startNode = 0; // Top-left
            _endNode = (_gridSizeX * _gridSizeY) - 1; // Bottom-right

            ClearUI();
            BuildUI();

            UpdateVisuals();
        }

        protected override void OnCancel()
        {
            ClearUI();
        }

        protected override void OnTick(float deltaTime)
        {
            if (_timerDisplay != null)
            {
                float remaining = Mathf.Max(0, _maxTime - _elapsedTime);
                _timerDisplay.text = $"{remaining:F1}s";
                if (remaining <= 5f) _timerDisplay.color = Color.red;
            }
        }

        // =================================================================
        //  LOGIC
        // =================================================================

        private void OnNodeClicked(int index)
        {
            if (!IsActive) return;

            // If empty, must start at StartNode
            if (_currentPath.Count == 0)
            {
                if (index != _startNode)
                {
                    SetStatus("MUST START AT SOURCE", Color.red);
                    return;
                }
                _currentPath.Add(index);
                UpdateVisuals();
                return;
            }

            // Check if already in path (backtrack or cross)
            if (_currentPath.Contains(index))
            {
                // Allow backtracking by clicking the previous node
                if (_currentPath.Count > 1 && _currentPath[_currentPath.Count - 2] == index)
                {
                    _currentPath.RemoveAt(_currentPath.Count - 1);
                    UpdateVisuals();
                    return;
                }
                
                SetStatus("PATH CROSSED", Color.red);
                _currentPath.Clear(); // reset
                UpdateVisuals();
                return;
            }

            // Check adjacency to last node
            int last = _currentPath[_currentPath.Count - 1];
            if (IsAdjacent(last, index))
            {
                _currentPath.Add(index);
                UpdateVisuals();

                // Win check
                if (index == _endNode)
                {
                    SetStatus("CIRCUIT COMPLETE", Color.green);
                    Complete(true);
                }
            }
            else
            {
                SetStatus("INVALID MOVE", Color.red);
            }
        }

        private bool IsAdjacent(int a, int b)
        {
            int ax = a % _gridSizeX;
            int ay = a / _gridSizeX;
            
            int bx = b % _gridSizeX;
            int by = b / _gridSizeX;

            int dx = Mathf.Abs(ax - bx);
            int dy = Mathf.Abs(ay - by);

            return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
        }

        private void SetStatus(string msg, Color color)
        {
            if (_statusDisplay == null) return;
            _statusDisplay.text = msg;
            _statusDisplay.color = color;
        }

        // =================================================================
        //  UI BUILDING
        // =================================================================

        private void UpdateVisuals()
        {
            for (int i = 0; i < _nodes.Count; i++)
            {
                var img = _nodes[i].GetComponent<Image>();
                
                if (i == _startNode)
                {
                    img.color = _currentPath.Contains(i) ? Color.yellow : Color.blue;
                }
                else if (i == _endNode)
                {
                    img.color = _currentPath.Contains(i) ? Color.green : Color.red;
                }
                else
                {
                    if (_currentPath.Contains(i))
                    {
                        img.color = Color.yellow;
                    }
                    else
                    {
                        img.color = new Color(0.3f, 0.3f, 0.3f);
                    }
                }
            }
        }

        private void BuildUI()
        {
            if (_panel == null)
            {
                var go = new GameObject("CircuitPanel", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                _panel = go.GetComponent<RectTransform>();
                _panel.anchorMin = new Vector2(0.2f, 0.1f);
                _panel.anchorMax = new Vector2(0.8f, 0.9f);
                _panel.offsetMin = Vector2.zero;
                _panel.offsetMax = Vector2.zero;
                go.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
            }

            CreateText("TRACE CIRCUIT", 32, TextAlignmentOptions.Center, new Vector2(0, 0.85f), new Vector2(1, 0.95f));
            _timerDisplay = CreateText("", 24, TextAlignmentOptions.Center, new Vector2(0.4f, 0.05f), new Vector2(0.6f, 0.15f));
            _statusDisplay = CreateText("ROUTE POWER TO DESTINATION", 24, TextAlignmentOptions.Center, new Vector2(0, 0.75f), new Vector2(1, 0.85f));
            _statusDisplay.color = Color.yellow;

            // Build grid
            float startX = 0.1f;
            float endX = 0.9f;
            float startY = 0.7f;
            float endY = 0.2f;

            float stepX = (endX - startX) / Mathf.Max(1, _gridSizeX - 1);
            float stepY = (startY - endY) / Mathf.Max(1, _gridSizeY - 1);

            int btnSize = 40;

            for (int y = 0; y < _gridSizeY; y++)
            {
                for (int x = 0; x < _gridSizeX; x++)
                {
                    int index = y * _gridSizeX + x;
                    
                    float anchorX = startX + (x * stepX);
                    float anchorY = startY - (y * stepY);

                    var btnGo = new GameObject($"Node_{index}", typeof(RectTransform), typeof(Image), typeof(Button));
                    btnGo.transform.SetParent(_panel, false);
                    
                    var rt = btnGo.GetComponent<RectTransform>();
                    rt.anchorMin = rt.anchorMax = new Vector2(anchorX, anchorY);
                    rt.sizeDelta = new Vector2(btnSize, btnSize);

                    var btn = btnGo.GetComponent<Button>();
                    int idx = index;
                    btn.onClick.AddListener(() => OnNodeClicked(idx));

                    _nodes.Add(btn);
                }
            }
        }

        private TextMeshProUGUI CreateText(string text, int fontSize, TextAlignmentOptions alignment, Vector2 min, Vector2 max)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(_panel, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;

            return tmp;
        }

        private void ClearUI()
        {
            if (_panel != null)
            {
                for (int i = _panel.childCount - 1; i >= 0; i--)
                {
                    Destroy(_panel.GetChild(i).gameObject);
                }
            }
            _nodes.Clear();
            _timerDisplay = null;
            _statusDisplay = null;
        }
    }
}
