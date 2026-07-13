// ============================================================================
// SCRAPSHIFT — PauseMenu.cs
// Handles the ESC pause menu. Unlocks the cursor to stop player camera
// rotation, provides Resume, Settings (WIP), and Main Menu options.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

namespace SpaceMaintenance.UI
{
    public class PauseMenu : MonoBehaviour
    {
        public static PauseMenu Instance { get; private set; }

        private GameObject _pausePanel;
        private bool _isPaused = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            var go = new GameObject("PauseMenuManager");
            DontDestroyOnLoad(go);
            go.AddComponent<PauseMenu>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BuildUI();
        }

        private void BuildUI()
        {
            var canvasGO = new GameObject("PauseCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform);
            
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; // Very high, above HUD and Minigames

            _pausePanel = new GameObject("PausePanel", typeof(RectTransform), typeof(Image));
            _pausePanel.transform.SetParent(canvasGO.transform, false);
            
            var rt = _pausePanel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            
            _pausePanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.8f);

            // Title
            CreateText("PAUSED", 64, new Vector2(0, 0.7f), new Vector2(1, 0.9f));
            
            // Buttons
            CreateButton("Resume Game", new Vector2(0.35f, 0.5f), new Vector2(0.65f, 0.6f), Resume);
            CreateButton("Settings", new Vector2(0.35f, 0.35f), new Vector2(0.65f, 0.45f), () => Debug.Log("Settings not implemented yet!"));
            CreateButton("Main Menu", new Vector2(0.35f, 0.2f), new Vector2(0.65f, 0.3f), GoToMainMenu);

            _pausePanel.SetActive(false);
        }

        private TextMeshProUGUI CreateText(string text, int size, Vector2 min, Vector2 max)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(_pausePanel.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            return tmp;
        }

        private void CreateButton(string text, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_pausePanel.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.2f);
            
            var btn = go.GetComponent<Button>();
            
            // Set hover colors
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.8f, 0.8f, 1f);
            colors.pressedColor = new Color(0.5f, 0.5f, 0.8f);
            btn.colors = colors;
            
            btn.onClick.AddListener(action);

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(go.transform, false);
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = textRT.offsetMax = Vector2.zero;

            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 28;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // Prevent pause menu in MainMenu scene
                if (SceneManager.GetActiveScene().name == "MainMenu") return;

                // Don't pause if minigame is active
                if (SpaceMaintenance.Minigames.MinigameManager.Instance != null && 
                    SpaceMaintenance.Minigames.MinigameManager.Instance.IsMinigameActive)
                {
                    return;
                }

                if (_isPaused) Resume();
                else Pause();
            }
        }

        public void Pause()
        {
            if (SceneManager.GetActiveScene().name == "MainMenu") return;

            _isPaused = true;
            _pausePanel.SetActive(false); // Toggle to ensure layout update
            _pausePanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Resume()
        {
            _isPaused = false;
            _pausePanel.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void GoToMainMenu()
        {
            _isPaused = false;
            _pausePanel.SetActive(false);
            
            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                Unity.Netcode.NetworkManager.Singleton.Shutdown();
            }

            SceneManager.LoadScene("MainMenu");
        }
    }
}
