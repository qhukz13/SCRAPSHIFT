using SpaceMaintenance.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SpaceMaintenance.Minigames.Games
{
    public class PressureBalanceMinigame : MinigameBase
    {
        // ─── Config ─────────────────────────────────────────────────────
        [Header("Pressure Settings")]
        [SerializeField] private float _targetHoldTime = 3f;
        [SerializeField] private float _driftSpeed = 0.5f;
        [SerializeField] private float _tolerance = 0.1f;

        [Header("Visual")]
        [SerializeField] private RectTransform _panel;

        // ─── Runtime ────────────────────────────────────────────────────
        private float _currentPressure;
        private float _targetPressure;
        private float _heldTime;

        private Slider[] _valves = new Slider[3];
        private TextMeshProUGUI _timerDisplay;
        private TextMeshProUGUI _statusDisplay;
        private RectTransform _targetZoneIndicator;
        private RectTransform _pressureIndicator;
        
        private RectTransform _barArea;

        // =================================================================
        //  MINIGAME LIFECYCLE
        // =================================================================

        private void Awake()
        {
            Type = MinigameType.PressureBalance;
        }

        protected override void OnStart()
        {
            _targetPressure = Random.Range(0.3f, 0.7f);
            _currentPressure = 0f;
            _heldTime = 0f;

            // Difficulty scales drift and tolerance
            _driftSpeed = 0.2f + (Difficulty * 0.15f);
            _tolerance = Mathf.Clamp(0.2f - (Difficulty * 0.03f), 0.05f, 0.2f);
            
            _maxTime = 20f + (Difficulty * 5f); // 25s - 35s

            ClearUI();
            BuildUI();
        }

        protected override void OnCancel()
        {
            ClearUI();
        }

        protected override void OnTick(float deltaTime)
        {
            // Drift target randomly
            _targetPressure += (Mathf.PerlinNoise(Time.time * _driftSpeed, 0) - 0.5f) * deltaTime * 0.5f;
            _targetPressure = Mathf.Clamp(_targetPressure, 0.1f, 0.9f);

            // Calculate current pressure from valves
            float sum = 0f;
            for (int i = 0; i < _valves.Length; i++)
            {
                if (_valves[i] != null)
                {
                    sum += _valves[i].value;
                }
            }
            _currentPressure = sum / _valves.Length;

            // Update Visuals
            UpdateBarVisuals();

            // Check tolerance
            if (Mathf.Abs(_currentPressure - _targetPressure) <= _tolerance)
            {
                _heldTime += deltaTime;
                _statusDisplay.text = "STABILIZING...";
                _statusDisplay.color = Color.yellow;
                
                if (_heldTime >= _targetHoldTime)
                {
                    _statusDisplay.text = "PRESSURE STABLE";
                    _statusDisplay.color = Color.green;
                    Complete(true);
                }
            }
            else
            {
                _heldTime = 0f;
                _statusDisplay.text = "PRESSURE CRITICAL";
                _statusDisplay.color = Color.red;
            }

            if (_timerDisplay != null)
            {
                float remaining = Mathf.Max(0, _maxTime - _elapsedTime);
                _timerDisplay.text = $"{remaining:F1}s";
                if (remaining <= 5f) _timerDisplay.color = Color.red;
            }
        }

        // =================================================================
        //  UI BUILDING
        // =================================================================

        private void UpdateBarVisuals()
        {
            if (_barArea == null) return;

            float height = _barArea.rect.height;

            // Target zone
            if (_targetZoneIndicator != null)
            {
                float y = (_targetPressure - 0.5f) * height;
                _targetZoneIndicator.anchoredPosition = new Vector2(0, y);
                _targetZoneIndicator.sizeDelta = new Vector2(_targetZoneIndicator.sizeDelta.x, height * (_tolerance * 2f));
            }

            // Current pressure
            if (_pressureIndicator != null)
            {
                float y = (_currentPressure - 0.5f) * height;
                _pressureIndicator.anchoredPosition = new Vector2(0, y);
            }
        }

        private void BuildUI()
        {
            if (_panel == null)
            {
                var go = new GameObject("PressurePanel", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                _panel = go.GetComponent<RectTransform>();
                _panel.anchorMin = new Vector2(0.2f, 0.1f);
                _panel.anchorMax = new Vector2(0.8f, 0.9f);
                _panel.offsetMin = Vector2.zero;
                _panel.offsetMax = Vector2.zero;
                go.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
            }

            // Title
            CreateText("BALANCE PRESSURE", 32, TextAlignmentOptions.Center, new Vector2(0, 0.85f), new Vector2(1, 0.95f));

            // Timer
            _timerDisplay = CreateText("", 24, TextAlignmentOptions.Center, new Vector2(0.4f, 0.05f), new Vector2(0.6f, 0.15f));

            // Status
            _statusDisplay = CreateText("PRESSURE CRITICAL", 28, TextAlignmentOptions.Center, new Vector2(0, 0.75f), new Vector2(1, 0.85f));
            _statusDisplay.color = Color.red;

            // Main Bar Area (Center)
            var barGo = new GameObject("BarArea", typeof(RectTransform), typeof(Image));
            barGo.transform.SetParent(_panel, false);
            _barArea = barGo.GetComponent<RectTransform>();
            _barArea.anchorMin = new Vector2(0.45f, 0.2f);
            _barArea.anchorMax = new Vector2(0.55f, 0.7f);
            _barArea.offsetMin = _barArea.offsetMax = Vector2.zero;
            barGo.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Target Zone
            var tzGo = new GameObject("TargetZone", typeof(RectTransform), typeof(Image));
            tzGo.transform.SetParent(_barArea, false);
            _targetZoneIndicator = tzGo.GetComponent<RectTransform>();
            _targetZoneIndicator.anchorMin = new Vector2(0, 0.5f);
            _targetZoneIndicator.anchorMax = new Vector2(1, 0.5f);
            _targetZoneIndicator.sizeDelta = new Vector2(0, 50); // Will update in UpdateBarVisuals
            var tzImg = tzGo.GetComponent<Image>();
            tzImg.color = new Color(0, 1, 0, 0.3f);

            // Current Pressure Indicator
            var piGo = new GameObject("PressureIndicator", typeof(RectTransform), typeof(Image));
            piGo.transform.SetParent(_barArea, false);
            _pressureIndicator = piGo.GetComponent<RectTransform>();
            _pressureIndicator.anchorMin = new Vector2(0, 0.5f);
            _pressureIndicator.anchorMax = new Vector2(1, 0.5f);
            _pressureIndicator.sizeDelta = new Vector2(0, 10);
            var piImg = piGo.GetComponent<Image>();
            piImg.color = Color.white;

            // 3 Valves (Sliders)
            for (int i = 0; i < 3; i++)
            {
                float x = 0.15f + (i * 0.35f);
                if (i == 1) continue; // Skip center, because that's where the bar is
                
                // If i == 0, left side. If i == 2, right side.
                // Let's position sliders: left, center-left, right?
                // Left = 0.2, Right = 0.8, Middle = wait, there are 3.
                // Pos: 0.2, 0.8, and maybe a 3rd slider at 0.3?
                // Let's do: Left = 0.2, Center-Right = 0.7, Right = 0.85
                float anchorX = (i == 0) ? 0.2f : (i == 1) ? 0.7f : 0.85f;

                var sliderGo = DefaultControls.CreateSlider(new DefaultControls.Resources());
                sliderGo.transform.SetParent(_panel, false);
                var sliderRt = sliderGo.GetComponent<RectTransform>();
                
                sliderRt.anchorMin = new Vector2(anchorX - 0.05f, 0.2f);
                sliderRt.anchorMax = new Vector2(anchorX + 0.05f, 0.7f);
                sliderRt.offsetMin = sliderRt.offsetMax = Vector2.zero;

                var slider = sliderGo.GetComponent<Slider>();
                slider.direction = Slider.Direction.BottomToTop;
                slider.value = Random.Range(0f, 1f);
                _valves[i] = slider;

                // Valve label
                CreateText($"V-{i+1}", 18, TextAlignmentOptions.Center, new Vector2(anchorX - 0.05f, 0.1f), new Vector2(anchorX + 0.05f, 0.18f));
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
            _timerDisplay = null;
            _statusDisplay = null;
            _targetZoneIndicator = null;
            _pressureIndicator = null;
            _barArea = null;
            for(int i=0; i<3; i++) _valves[i] = null;
        }
    }
}
