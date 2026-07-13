// ============================================================================
// SCRAPSHIFT — SequenceInputMinigame.cs
// Minigame: Players must memorize and enter a random sequence of numbers.
// Difficulty scales the sequence length.
// ============================================================================

using System.Collections.Generic;
using SpaceMaintenance.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SpaceMaintenance.Minigames.Games
{
    public class SequenceInputMinigame : MinigameBase
    {
        // ─── Config ─────────────────────────────────────────────────────
        [Header("Sequence Settings")]
        [SerializeField] private int _baseSequenceLength = 4;
        [SerializeField] private float _displayTime = 2.5f;

        [Header("Visual")]
        [SerializeField] private RectTransform _panel;

        // ─── Runtime ────────────────────────────────────────────────────
        private string _targetSequence;
        private string _currentInput;
        private float _sequenceDisplayTimer;
        private bool _isMemorizing;

        // UI elements created at runtime
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _timerDisplay;
        private TextMeshProUGUI _sequenceDisplay;
        private readonly List<Button> _numpadButtons = new List<Button>();

        // =================================================================
        //  MINIGAME LIFECYCLE
        // =================================================================

        private void Awake()
        {
            Type = MinigameType.SequenceInput;
        }

        protected override void OnStart()
        {
            // Calculate sequence length based on difficulty
            int length = _baseSequenceLength + (Difficulty - 1);
            _targetSequence = GenerateSequence(length);
            _currentInput = "";
            
            _isMemorizing = true;
            _sequenceDisplayTimer = _displayTime;
            
            // Time limit
            _maxTime = Mathf.Lerp(12f, 8f, (Difficulty - 1) / 3f) + _displayTime;

            ClearUI();
            BuildUI();
            
            UpdateSequenceDisplay();
        }

        protected override void OnCancel()
        {
            ClearUI();
        }

        protected override void OnTick(float deltaTime)
        {
            if (_timerDisplay != null)
            {
                float remaining = _maxTime - _elapsedTime;
                _timerDisplay.text = $"{Mathf.Max(0, remaining):F1}s";

                if (remaining <= 3f)
                    _timerDisplay.color = Color.Lerp(Color.red, Color.white, Mathf.PingPong(Time.unscaledTime * 4f, 1f));
            }

            if (_isMemorizing)
            {
                _sequenceDisplayTimer -= deltaTime;
                if (_sequenceDisplayTimer <= 0)
                {
                    _isMemorizing = false;
                    _currentInput = "";
                    UpdateSequenceDisplay();
                    EnableNumpad(true);
                }
            }
        }

        // =================================================================
        //  LOGIC
        // =================================================================

        private string GenerateSequence(int length)
        {
            string seq = "";
            for (int i = 0; i < length; i++)
            {
                seq += Random.Range(0, 10).ToString();
            }
            return seq;
        }

        private void OnNumberPressed(int number)
        {
            if (!IsActive || _isMemorizing) return;

            _currentInput += number.ToString();
            UpdateSequenceDisplay();

            // Check if input is correct so far
            if (!_targetSequence.StartsWith(_currentInput))
            {
                // Failed — reset input and show error visually
                Debug.Log("[SequenceInput] Wrong input! Resetting...");
                _currentInput = "";
                UpdateSequenceDisplay();
                _sequenceDisplay.color = Color.red;
                Invoke(nameof(ResetDisplayColor), 0.5f);
                
                // Penalize time
                _elapsedTime += 2f; 
                return;
            }

            // Check win
            if (_currentInput == _targetSequence)
            {
                _sequenceDisplay.color = Color.green;
                EnableNumpad(false);
                Complete(true);
            }
        }

        private void ResetDisplayColor()
        {
            if (_sequenceDisplay != null)
                _sequenceDisplay.color = Color.white;
        }

        private void UpdateSequenceDisplay()
        {
            if (_sequenceDisplay == null) return;

            if (_isMemorizing)
            {
                _sequenceDisplay.text = _targetSequence;
                _sequenceDisplay.color = Color.yellow;
            }
            else
            {
                // Show entered digits, pad with underscores
                string display = _currentInput;
                while (display.Length < _targetSequence.Length)
                {
                    display += "_";
                }
                
                // Add spaces between characters for readability
                _sequenceDisplay.text = string.Join(" ", display.ToCharArray());
            }
        }

        private void EnableNumpad(bool enable)
        {
            foreach (var btn in _numpadButtons)
            {
                btn.interactable = enable;
            }
        }

        // =================================================================
        //  UI BUILDING
        // =================================================================

        private void BuildUI()
        {
            if (_panel == null)
            {
                var go = new GameObject("SequencePanel", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                _panel = go.GetComponent<RectTransform>();
                _panel.anchorMin = new Vector2(0.2f, 0.1f);
                _panel.anchorMax = new Vector2(0.8f, 0.9f);
                _panel.offsetMin = Vector2.zero;
                _panel.offsetMax = Vector2.zero;
            }

            // Title
            _titleText = CreateText("MEMORIZE SEQUENCE", 28, TextAlignmentOptions.Center);
            SetRect(_titleText.GetComponent<RectTransform>(), new Vector2(0f, 0.9f), new Vector2(1f, 1f));

            // Timer
            _timerDisplay = CreateText($"{_maxTime:F1}s", 24, TextAlignmentOptions.Center);
            SetRect(_timerDisplay.GetComponent<RectTransform>(), new Vector2(0.4f, 0.0f), new Vector2(0.6f, 0.1f));

            // Sequence Display
            _sequenceDisplay = CreateText("", 40, TextAlignmentOptions.Center);
            SetRect(_sequenceDisplay.GetComponent<RectTransform>(), new Vector2(0f, 0.75f), new Vector2(1f, 0.85f));

            // Numpad Grid
            float startX = 0.25f;
            float startY = 0.65f;
            float stepX = 0.2f;
            float stepY = 0.15f;
            
            int btnSize = 60;

            for (int i = 1; i <= 9; i++)
            {
                int row = (i - 1) / 3;
                int col = (i - 1) % 3;
                
                float x = startX + col * stepX;
                float y = startY - row * stepY;
                
                CreateNumpadButton(i, x, y, btnSize);
            }
            
            // 0 button at the bottom center
            CreateNumpadButton(0, startX + stepX, startY - 3 * stepY, btnSize);
            
            EnableNumpad(false);
        }

        private void CreateNumpadButton(int number, float anchorX, float anchorY, int size)
        {
            var go = new GameObject($"Btn_{number}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_panel, false);
            
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchorX, anchorY);
            rt.anchorMax = new Vector2(anchorX, anchorY);
            rt.sizeDelta = new Vector2(size, size);
            
            var img = go.GetComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.3f);
            
            var btn = go.GetComponent<Button>();
            int num = number; // capture for closure
            btn.onClick.AddListener(() => OnNumberPressed(num));
            
            _numpadButtons.Add(btn);
            
            // Label
            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
            
            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = number.ToString();
            tmp.fontSize = 28;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }

        private TextMeshProUGUI CreateText(string text, int fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(_panel, false);

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;

            return tmp;
        }
        
        private void SetRect(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void ClearUI()
        {
            _numpadButtons.Clear();

            if (_panel != null)
            {
                for (int i = _panel.childCount - 1; i >= 0; i--)
                {
                    Destroy(_panel.GetChild(i).gameObject);
                }
            }

            _titleText = null;
            _timerDisplay = null;
            _sequenceDisplay = null;
        }
    }
}
