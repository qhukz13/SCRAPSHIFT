// ============================================================================
// SCRAPSHIFT — PipeAlignMinigame.cs
// Rotate pipe segments on a grid to connect the start node to the end node.
// Generates a valid path programmatically, then scrambles the rotations.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using SpaceMaintenance.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SpaceMaintenance.Minigames.Games
{
    public class PipeAlignMinigame : MinigameBase
    {
        // ─── Config ─────────────────────────────────────────────────────
        [Header("Pipe Align Settings")]

        [Header("Visual")]
        [SerializeField] private RectTransform _gridPanel;
        [SerializeField] private Color _pipeEmptyColor = new Color(0.4f, 0.4f, 0.4f);
        [SerializeField] private Color _pipeFilledColor = new Color(0.2f, 0.8f, 1f);
        [SerializeField] private Color _backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        // ─── Enums & Classes ────────────────────────────────────────────
        [System.Flags]
        public enum PipeDirs
        {
            None = 0,
            Up = 1 << 0,
            Right = 1 << 1,
            Down = 1 << 2,
            Left = 1 << 3
        }

        private class PipeCell
        {
            public int x, y;
            public PipeDirs BaseShape; // The original shape before rotation
            public int RotationCount;  // 0 to 3 (0=0, 1=90, 2=180, 3=270 clockwise)
            public bool IsFilled;
            
            public GameObject ButtonGO;
            public RectTransform[] Visuals = new RectTransform[4]; // 0=Up, 1=Right, 2=Down, 3=Left

            public PipeDirs CurrentDirs => RotateDirs(BaseShape, RotationCount);

            public static PipeDirs RotateDirs(PipeDirs dirs, int count)
            {
                int val = (int)dirs;
                // Since there are 4 directions, 90 deg clockwise means shifting bits:
                // Up(0) -> Right(1), Right(1) -> Down(2), Down(2) -> Left(3), Left(3) -> Up(0)
                for (int i = 0; i < count; i++)
                {
                    int newVal = 0;
                    if ((val & (int)PipeDirs.Up) != 0) newVal |= (int)PipeDirs.Right;
                    if ((val & (int)PipeDirs.Right) != 0) newVal |= (int)PipeDirs.Down;
                    if ((val & (int)PipeDirs.Down) != 0) newVal |= (int)PipeDirs.Left;
                    if ((val & (int)PipeDirs.Left) != 0) newVal |= (int)PipeDirs.Up;
                    val = newVal;
                }
                return (PipeDirs)val;
            }
        }

        // ─── Runtime ────────────────────────────────────────────────────
        private int _width;
        private int _height;
        private PipeCell[,] _grid;
        private int _startY, _endY;
        private bool _completed;

        private TextMeshProUGUI _timerDisplay;
        private TextMeshProUGUI _titleText;
        private RectTransform _startPortVisual;
        private RectTransform _endPortVisual;
        private GameObject _completionOverlay;

        // =================================================================
        //  MINIGAME LIFECYCLE
        // =================================================================

        private void Awake()
        {
            Type = MinigameType.PipeAlign;
        }

        protected override void OnStart()
        {
            _completed = false;

            // Grid size scales progressively with difficulty (ignore serialized values):
            // Diff 1 => 3x3, Diff 2 => 3x3, Diff 3 => 4x3, Diff 4 => 4x4, Diff 5+ => 5x4
            _width = 3 + Mathf.FloorToInt((Difficulty - 1) / 2f);
            _height = 3 + Mathf.FloorToInt(Mathf.Max(0, Difficulty - 2) / 2f);
            _width = Mathf.Clamp(_width, 3, 6);
            _height = Mathf.Clamp(_height, 3, 5);

            // Timer: generous at low difficulty, tighter as it scales
            _maxTime = Mathf.Lerp(35f, 15f, (Difficulty - 1) / 5f);

            _startY = Random.Range(0, _height);
            _endY = Random.Range(0, _height);

            ClearUI();
            BuildGridLogic();
            BuildUI();
            UpdateFlow();
        }

        protected override void OnCancel()
        {
            StopAllCoroutines();
            if (_completionOverlay != null) Destroy(_completionOverlay);
            ClearUI();
        }

        protected override void OnTick(float deltaTime)
        {
            if (_completed) return; // Stop ticking during completion delay

            if (_timerDisplay != null)
            {
                float remaining = _maxTime - _elapsedTime;
                _timerDisplay.text = $"{remaining:F1}s";

                if (remaining <= 5f)
                    _timerDisplay.color = Color.Lerp(Color.red, Color.white, Mathf.PingPong(Time.unscaledTime * 3f, 1f));
            }
        }

        // =================================================================
        //  LOGIC BUILDING
        // =================================================================

        private void BuildGridLogic()
        {
            _grid = new PipeCell[_width, _height];

            // 1. Generate path from (0, _startY) to (_width-1, _endY)
            var path = new List<Vector2Int>();
            Vector2Int current = new Vector2Int(0, _startY);
            path.Add(current);

            while (current.x < _width - 1 || current.y != _endY)
            {
                var options = new List<Vector2Int>();
                if (current.x < _width - 1) options.Add(current + Vector2Int.right);
                if (current.y < _endY) options.Add(current + Vector2Int.up);
                if (current.y > _endY) options.Add(current + Vector2Int.down);

                // Prevent immediate doubling back
                var next = options[Random.Range(0, options.Count)];
                
                // If it loops or goes back, just force right
                if (path.Contains(next)) next = current + Vector2Int.right;

                path.Add(next);
                current = next;
            }

            // 2. Initialize cells
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    _grid[x, y] = new PipeCell { x = x, y = y };
                }
            }

            // 3. Assign required connections along the path
            for (int i = 0; i < path.Count; i++)
            {
                PipeDirs dirs = PipeDirs.None;

                // Connection from previous
                if (i > 0) dirs |= GetDirTo(path[i], path[i - 1]);
                else dirs |= PipeDirs.Left; // Start connects to left

                // Connection to next
                if (i < path.Count - 1) dirs |= GetDirTo(path[i], path[i + 1]);
                else dirs |= PipeDirs.Right; // End connects to right

                // At higher difficulties, add random extra arms to confuse
                float extraArmChance = Mathf.Clamp01((Difficulty - 2) * 0.15f);
                if (Random.value < extraArmChance) dirs |= GetRandomDir();

                _grid[path[i].x, path[i].y].BaseShape = dirs;
            }

            // 4. Fill remaining cells with random pipes
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (_grid[x, y].BaseShape == PipeDirs.None)
                    {
                        // Generate random valid shape (I, L, T, or Cross)
                        PipeDirs randomShape = PipeDirs.None;
                        int arms = Random.Range(2, 4); // 2 or 3 arms usually
                        for (int a = 0; a < arms; a++) randomShape |= GetRandomDir();
                        
                        // Ensure at least 2 connections
                        if (randomShape == PipeDirs.Up || randomShape == PipeDirs.Down || randomShape == PipeDirs.Left || randomShape == PipeDirs.Right)
                        {
                            randomShape |= GetRandomDir();
                        }
                        
                        _grid[x, y].BaseShape = randomShape;
                    }

                    // Scramble rotation
                    _grid[x, y].RotationCount = Random.Range(0, 4);
                }
            }
        }

        private PipeDirs GetDirTo(Vector2Int from, Vector2Int to)
        {
            if (to.x > from.x) return PipeDirs.Right;
            if (to.x < from.x) return PipeDirs.Left;
            if (to.y > from.y) return PipeDirs.Up;
            if (to.y < from.y) return PipeDirs.Down;
            return PipeDirs.None;
        }

        private PipeDirs GetRandomDir()
        {
            int r = Random.Range(0, 4);
            if (r == 0) return PipeDirs.Up;
            if (r == 1) return PipeDirs.Right;
            if (r == 2) return PipeDirs.Down;
            return PipeDirs.Left;
        }

        // =================================================================
        //  UI BUILDING
        // =================================================================

        private void BuildUI()
        {
            if (_gridPanel == null)
            {
                var go = new GameObject("GridPanel", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                _gridPanel = go.GetComponent<RectTransform>();
                _gridPanel.anchorMin = new Vector2(0.1f, 0.15f);
                _gridPanel.anchorMax = new Vector2(0.9f, 0.85f);
                _gridPanel.offsetMin = Vector2.zero;
                _gridPanel.offsetMax = Vector2.zero;

                go.GetComponent<Image>().color = _backgroundColor;
            }

            // Title
            _titleText = CreateText("ALIGN THE PIPES", 28, TextAlignmentOptions.Center, new Vector2(0.2f, 0.88f), new Vector2(0.8f, 0.98f));
            _timerDisplay = CreateText($"{_maxTime:F1}s", 24, TextAlignmentOptions.Center, new Vector2(0.4f, 0.02f), new Vector2(0.6f, 0.12f));

            float padding = 20f;
            float usableWidth = _gridPanel.rect.width - padding * 2 - 80f; // Leave space for start/end ports
            float usableHeight = _gridPanel.rect.height - padding * 2;
            
            float cellSize = Mathf.Min(usableWidth / _width, usableHeight / _height);
            float startX = (_gridPanel.rect.width - (cellSize * _width)) / 2f;
            float startY = (_gridPanel.rect.height - (cellSize * _height)) / 2f;

            // Start/End ports
            _startPortVisual = CreateRectObject("StartPort", _gridPanel, new Color(0.2f, 1f, 0.3f));
            _startPortVisual.anchorMin = _startPortVisual.anchorMax = Vector2.zero;
            _startPortVisual.sizeDelta = new Vector2(30, cellSize * 0.4f);
            _startPortVisual.anchoredPosition = new Vector2(startX - 20, startY + cellSize * _startY + cellSize / 2f);

            _endPortVisual = CreateRectObject("EndPort", _gridPanel, new Color(1f, 0.2f, 0.2f));
            _endPortVisual.anchorMin = _endPortVisual.anchorMax = Vector2.zero;
            _endPortVisual.sizeDelta = new Vector2(30, cellSize * 0.4f);
            _endPortVisual.anchoredPosition = new Vector2(startX + cellSize * _width + 20, startY + cellSize * _endY + cellSize / 2f);

            // Cells
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    var cell = _grid[x, y];
                    var cellGO = new GameObject($"Cell_{x}_{y}", typeof(RectTransform), typeof(Image), typeof(Button));
                    cellGO.transform.SetParent(_gridPanel, false);
                    cell.ButtonGO = cellGO;

                    var rt = cellGO.GetComponent<RectTransform>();
                    rt.anchorMin = rt.anchorMax = Vector2.zero;
                    float gap = 10f; // Visible gap between tiles
                    rt.sizeDelta = new Vector2(cellSize - gap, cellSize - gap);
                    rt.anchoredPosition = new Vector2(startX + x * cellSize + cellSize / 2f, startY + y * cellSize + cellSize / 2f);

                    var img = cellGO.GetComponent<Image>();
                    img.color = new Color(0.12f, 0.12f, 0.18f); // Dark tile background

                    // Add a visible border/outline using an Outline component
                    var outline = cellGO.AddComponent<Outline>();
                    outline.effectColor = new Color(0.3f, 0.35f, 0.45f, 0.8f);
                    outline.effectDistance = new Vector2(2, 2);

                    var btn = cellGO.GetComponent<Button>();
                    int cx = x, cy = y; // Capture for lambda
                    btn.onClick.AddListener(() => OnCellClicked(cx, cy));

                    // Center dot
                    var centerGO = CreateRectObject("Center", rt, _pipeEmptyColor);
                    centerGO.anchorMin = centerGO.anchorMax = new Vector2(0.5f, 0.5f);
                    centerGO.sizeDelta = new Vector2(cellSize * 0.3f, cellSize * 0.3f);
                    centerGO.anchoredPosition = Vector2.zero;

                    // Pipe arms
                    float armLength = cellSize * 0.35f;
                    float armThick = cellSize * 0.3f;
                    
                    cell.Visuals[0] = CreateRectObject("Up", centerGO, _pipeEmptyColor);
                    cell.Visuals[0].sizeDelta = new Vector2(armThick, armLength);
                    cell.Visuals[0].anchoredPosition = new Vector2(0, (cellSize * 0.15f + armLength / 2f));

                    cell.Visuals[1] = CreateRectObject("Right", centerGO, _pipeEmptyColor);
                    cell.Visuals[1].sizeDelta = new Vector2(armLength, armThick);
                    cell.Visuals[1].anchoredPosition = new Vector2((cellSize * 0.15f + armLength / 2f), 0);

                    cell.Visuals[2] = CreateRectObject("Down", centerGO, _pipeEmptyColor);
                    cell.Visuals[2].sizeDelta = new Vector2(armThick, armLength);
                    cell.Visuals[2].anchoredPosition = new Vector2(0, -(cellSize * 0.15f + armLength / 2f));

                    cell.Visuals[3] = CreateRectObject("Left", centerGO, _pipeEmptyColor);
                    cell.Visuals[3].sizeDelta = new Vector2(armLength, armThick);
                    cell.Visuals[3].anchoredPosition = new Vector2(-(cellSize * 0.15f + armLength / 2f), 0);

                    UpdateCellVisuals(cell);
                }
            }
        }

        private TextMeshProUGUI CreateText(string text, int fontSize, TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(_gridPanel, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;

            return tmp;
        }

        private RectTransform CreateRectObject(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            go.GetComponent<Image>().color = color;
            return rt;
        }

        private void ClearUI()
        {
            if (_gridPanel != null)
            {
                for (int i = _gridPanel.childCount - 1; i >= 0; i--)
                {
                    Destroy(_gridPanel.GetChild(i).gameObject);
                }
            }
            _grid = null;
        }

        // =================================================================
        //  INTERACTION & FLOW
        // =================================================================

        private void OnCellClicked(int x, int y)
        {
            if (!IsActive || _completed) return;

            var cell = _grid[x, y];
            cell.RotationCount = (cell.RotationCount + 1) % 4;

            UpdateFlow();
        }

        private void UpdateCellVisuals(PipeCell cell)
        {
            PipeDirs dirs = cell.CurrentDirs;
            
            cell.Visuals[0].gameObject.SetActive((dirs & PipeDirs.Up) != 0);
            cell.Visuals[1].gameObject.SetActive((dirs & PipeDirs.Right) != 0);
            cell.Visuals[2].gameObject.SetActive((dirs & PipeDirs.Down) != 0);
            cell.Visuals[3].gameObject.SetActive((dirs & PipeDirs.Left) != 0);

            Color color = cell.IsFilled ? _pipeFilledColor : _pipeEmptyColor;
            
            // Set arm colors
            for (int i = 0; i < 4; i++) cell.Visuals[i].GetComponent<Image>().color = color;
            // Set center color
            cell.Visuals[0].parent.GetComponent<Image>().color = color;
        }

        private void UpdateFlow()
        {
            // Reset all fill states
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    _grid[x, y].IsFilled = false;
                }
            }

            // Recursive fill from start
            var cell = _grid[0, _startY];
            if ((cell.CurrentDirs & PipeDirs.Left) != 0)
            {
                FillCell(0, _startY);
            }

            // Update UI visuals
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    UpdateCellVisuals(_grid[x, y]);
                }
            }

            // Check if end is reached
            var endCell = _grid[_width - 1, _endY];
            if (endCell.IsFilled && (endCell.CurrentDirs & PipeDirs.Right) != 0)
            {
                _endPortVisual.GetComponent<Image>().color = _pipeFilledColor;
                _completed = true;
                StartCoroutine(CompleteWithDelay());
            }
        }

        private void FillCell(int x, int y)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height) return;
            var cell = _grid[x, y];
            if (cell.IsFilled) return;

            cell.IsFilled = true;
            PipeDirs dirs = cell.CurrentDirs;

            // Check neighbors
            if ((dirs & PipeDirs.Up) != 0 && y < _height - 1)
            {
                if ((_grid[x, y + 1].CurrentDirs & PipeDirs.Down) != 0) FillCell(x, y + 1);
            }
            if ((dirs & PipeDirs.Right) != 0 && x < _width - 1)
            {
                if ((_grid[x + 1, y].CurrentDirs & PipeDirs.Left) != 0) FillCell(x + 1, y);
            }
            if ((dirs & PipeDirs.Down) != 0 && y > 0)
            {
                if ((_grid[x, y - 1].CurrentDirs & PipeDirs.Up) != 0) FillCell(x, y - 1);
            }
            if ((dirs & PipeDirs.Left) != 0 && x > 0)
            {
                if ((_grid[x - 1, y].CurrentDirs & PipeDirs.Right) != 0) FillCell(x - 1, y);
            }
        }

        // =================================================================
        //  COMPLETION DELAY
        // =================================================================

        private IEnumerator CompleteWithDelay()
        {
            // Create dark overlay
            _completionOverlay = new GameObject("CompletionOverlay", typeof(RectTransform), typeof(Image), typeof(CanvasRenderer));
            _completionOverlay.transform.SetParent(_gridPanel, false);

            var overlayRT = _completionOverlay.GetComponent<RectTransform>();
            overlayRT.anchorMin = Vector2.zero;
            overlayRT.anchorMax = Vector2.one;
            overlayRT.offsetMin = Vector2.zero;
            overlayRT.offsetMax = Vector2.zero;

            var overlayImg = _completionOverlay.GetComponent<Image>();
            overlayImg.color = new Color(0f, 0f, 0f, 0.6f); // Semi-transparent dark

            // "PIPE FIXED!" text
            var textGO = new GameObject("CompletionText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(_completionOverlay.transform, false);

            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = new Vector2(0.1f, 0.3f);
            textRT.anchorMax = new Vector2(0.9f, 0.7f);
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = "PIPE FIXED!";
            tmp.fontSize = 48;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.2f, 1f, 0.4f); // Bright green
            tmp.fontStyle = FontStyles.Bold;

            // Wait 2 seconds using unscaled time (in case game is paused)
            yield return new WaitForSecondsRealtime(2f);

            Complete(true);
        }
    }
}
