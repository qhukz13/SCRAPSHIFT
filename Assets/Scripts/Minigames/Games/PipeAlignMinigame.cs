using System.Collections.Generic;
using SpaceMaintenance.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SpaceMaintenance.Minigames.Games
{
    public class PipeAlignMinigame : MinigameBase
    {
        [Header("Config")]
        [SerializeField] private int _gridSize = 3;
        
        private List<Button> _pipes = new List<Button>();
        private List<RectTransform> _pipeTransforms = new List<RectTransform>();
        private int[] _rotations; // in steps of 90 degrees (0, 1, 2, 3)
        private int _totalPipes;
        private TextMeshProUGUI _timerText;

        protected override void OnStart()
        {
            Type = MinigameType.PipeAlign;
            _maxTime = Mathf.Max(10f, 20f - Difficulty * 2f);
            _totalPipes = _gridSize * _gridSize;
            _rotations = new int[_totalPipes];
            
            BuildUI();
        }

        private void BuildUI()
        {
            // Clear old
            foreach (Transform child in transform) { Destroy(child.gameObject); }
            _pipes.Clear();
            _pipeTransforms.Clear();

            // Background panel
            var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(transform, false);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            // Title
            var title = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            title.transform.SetParent(bg.transform, false);
            var titleText = title.GetComponent<TextMeshProUGUI>();
            titleText.text = "ALIGN PIPES";
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontSize = 32;
            titleText.color = Color.white;
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.9f); titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = new Vector2(0, -50); titleRect.offsetMax = Vector2.zero;

            // Timer
            var timer = new GameObject("Timer", typeof(RectTransform), typeof(TextMeshProUGUI));
            timer.transform.SetParent(bg.transform, false);
            _timerText = timer.GetComponent<TextMeshProUGUI>();
            _timerText.alignment = TextAlignmentOptions.Center;
            _timerText.fontSize = 24;
            _timerText.color = Color.red;
            var timerRect = timer.GetComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(0, 0.8f); timerRect.anchorMax = new Vector2(1, 0.9f);
            timerRect.offsetMin = Vector2.zero; timerRect.offsetMax = Vector2.zero;

            // Grid Panel
            var gridObj = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup));
            gridObj.transform.SetParent(bg.transform, false);
            var gridRect = gridObj.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.5f, 0.5f); gridRect.anchorMax = new Vector2(0.5f, 0.5f);
            gridRect.sizeDelta = new Vector2(400, 400);
            gridRect.anchoredPosition = new Vector2(0, -50);

            var gridLayout = gridObj.GetComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(120, 120);
            gridLayout.spacing = new Vector2(10, 10);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = _gridSize;

            for (int i = 0; i < _totalPipes; i++)
            {
                int index = i;
                var pipeObj = new GameObject($"Pipe_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                pipeObj.transform.SetParent(gridObj.transform, false);
                
                var img = pipeObj.GetComponent<Image>();
                img.color = new Color(0.2f, 0.2f, 0.2f);

                // Create inner pipe visual
                var innerObj = new GameObject("Inner", typeof(RectTransform), typeof(Image));
                innerObj.transform.SetParent(pipeObj.transform, false);
                var innerImg = innerObj.GetComponent<Image>();
                innerImg.color = Color.cyan;
                var innerRect = innerObj.GetComponent<RectTransform>();
                
                // Straight or L-shape
                bool isStraight = (i % 2 == 0);
                if (isStraight)
                {
                    innerRect.anchorMin = new Vector2(0.3f, 0f); innerRect.anchorMax = new Vector2(0.7f, 1f);
                    innerRect.offsetMin = Vector2.zero; innerRect.offsetMax = Vector2.zero;
                }
                else
                {
                    // L-shape part 1
                    innerRect.anchorMin = new Vector2(0.3f, 0.3f); innerRect.anchorMax = new Vector2(1f, 0.7f);
                    innerRect.offsetMin = Vector2.zero; innerRect.offsetMax = Vector2.zero;

                    // L-shape part 2
                    var inner2 = new GameObject("Inner2", typeof(RectTransform), typeof(Image));
                    inner2.transform.SetParent(pipeObj.transform, false);
                    inner2.GetComponent<Image>().color = Color.cyan;
                    var inner2Rect = inner2.GetComponent<RectTransform>();
                    inner2Rect.anchorMin = new Vector2(0.3f, 0f); inner2Rect.anchorMax = new Vector2(0.7f, 0.7f);
                    inner2Rect.offsetMin = Vector2.zero; inner2Rect.offsetMax = Vector2.zero;
                }

                var btn = pipeObj.GetComponent<Button>();
                btn.onClick.AddListener(() => OnPipeClicked(index));
                
                _pipes.Add(btn);
                _pipeTransforms.Add(pipeObj.GetComponent<RectTransform>());

                // Randomize rotation
                _rotations[i] = Random.Range(1, 4);
                UpdatePipeRotation(i);
            }
        }

        private void OnPipeClicked(int index)
        {
            if (!IsActive) return;
            
            _rotations[index] = (_rotations[index] + 1) % 4;
            UpdatePipeRotation(index);
            CheckWinCondition();
        }

        private void UpdatePipeRotation(int index)
        {
            _pipeTransforms[index].localRotation = Quaternion.Euler(0, 0, -90f * _rotations[index]);
        }

        private void CheckWinCondition()
        {
            foreach (var r in _rotations)
            {
                if (r != 0) return;
            }
            Complete(true);
        }

        protected override void OnTick(float deltaTime)
        {
            if (_timerText != null)
            {
                float remaining = Mathf.Max(0, _maxTime - _elapsedTime);
                _timerText.text = $"TIME: {remaining:F1}s";
            }
        }
    }
}