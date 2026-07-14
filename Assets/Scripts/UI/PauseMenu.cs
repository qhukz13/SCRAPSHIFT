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
        private GameObject _settingsPanel;
        private bool _isPaused = false;
        private TextMeshProUGUI _volText;
        private TextMeshProUGUI _sensText;
        private TextMeshProUGUI _fsText;

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
            CreateText(_pausePanel.transform, "PAUSED", 64, new Vector2(0, 0.7f), new Vector2(1, 0.9f));
            
            // Buttons
            CreateButton(_pausePanel.transform, "Resume Game", new Vector2(0.35f, 0.5f), new Vector2(0.65f, 0.6f), Resume);
            CreateButton(_pausePanel.transform, "Settings", new Vector2(0.35f, 0.35f), new Vector2(0.65f, 0.45f), OpenSettings);
            CreateButton(_pausePanel.transform, "Main Menu", new Vector2(0.35f, 0.2f), new Vector2(0.65f, 0.3f), GoToMainMenu);

            _pausePanel.SetActive(false);

            BuildSettingsUI(canvasGO.transform);
        }

        private void BuildSettingsUI(Transform canvasTransform)
        {
            _settingsPanel = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image));
            _settingsPanel.transform.SetParent(canvasTransform, false);
            
            var rt = _settingsPanel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            
            _settingsPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.9f);

            CreateText(_settingsPanel.transform, "SETTINGS", 64, new Vector2(0, 0.8f), new Vector2(1, 0.9f));

            // Volume
            _volText = CreateText(_settingsPanel.transform, "Master Volume: 100%", 32, new Vector2(0.2f, 0.6f), new Vector2(0.8f, 0.7f));
            CreateButton(_settingsPanel.transform, "-", new Vector2(0.3f, 0.5f), new Vector2(0.4f, 0.58f), () => ChangeVolume(-0.1f));
            CreateButton(_settingsPanel.transform, "+", new Vector2(0.6f, 0.5f), new Vector2(0.7f, 0.58f), () => ChangeVolume(0.1f));

            // Sensitivity
            _sensText = CreateText(_settingsPanel.transform, "Sensitivity: 2.0", 32, new Vector2(0.2f, 0.4f), new Vector2(0.8f, 0.5f));
            CreateButton(_settingsPanel.transform, "-", new Vector2(0.3f, 0.3f), new Vector2(0.4f, 0.38f), () => ChangeSensitivity(-0.2f));
            CreateButton(_settingsPanel.transform, "+", new Vector2(0.6f, 0.3f), new Vector2(0.7f, 0.38f), () => ChangeSensitivity(0.2f));

            // Fullscreen
            _fsText = CreateText(_settingsPanel.transform, "Fullscreen: ON", 32, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.3f));
            CreateButton(_settingsPanel.transform, "Toggle Fullscreen", new Vector2(0.35f, 0.1f), new Vector2(0.65f, 0.18f), ToggleFullscreen);

            // Back
            CreateButton(_settingsPanel.transform, "Back", new Vector2(0.8f, 0.05f), new Vector2(0.95f, 0.15f), CloseSettings);

            _settingsPanel.SetActive(false);
        }

        private void OpenSettings()
        {
            _pausePanel.SetActive(false);
            _settingsPanel.SetActive(true);
            UpdateSettingsText();
        }

        private void CloseSettings()
        {
            _settingsPanel.SetActive(false);
            _pausePanel.SetActive(true);
        }

        private void ChangeVolume(float delta)
        {
            if (SpaceMaintenance.Core.SettingsManager.Instance == null) return;
            var current = SpaceMaintenance.Core.SettingsManager.Instance.MasterVolume;
            SpaceMaintenance.Core.SettingsManager.Instance.SetMasterVolume(current + delta);
            UpdateSettingsText();
        }

        private void ChangeSensitivity(float delta)
        {
            if (SpaceMaintenance.Core.SettingsManager.Instance == null) return;
            var current = SpaceMaintenance.Core.SettingsManager.Instance.Sensitivity;
            SpaceMaintenance.Core.SettingsManager.Instance.SetSensitivity(current + delta);
            UpdateSettingsText();
        }

        private void ToggleFullscreen()
        {
            if (SpaceMaintenance.Core.SettingsManager.Instance == null) return;
            var current = SpaceMaintenance.Core.SettingsManager.Instance.IsFullscreen;
            SpaceMaintenance.Core.SettingsManager.Instance.SetFullscreen(!current);
            UpdateSettingsText();
        }

        private void UpdateSettingsText()
        {
            if (SpaceMaintenance.Core.SettingsManager.Instance == null) return;
            var settings = SpaceMaintenance.Core.SettingsManager.Instance;
            
            _volText.text = $"Master Volume: {Mathf.RoundToInt(settings.MasterVolume * 100)}%";
            _sensText.text = $"Sensitivity: {settings.Sensitivity:F1}";
            _fsText.text = $"Fullscreen: {(settings.IsFullscreen ? "ON" : "OFF")}";
        }

        private TextMeshProUGUI CreateText(Transform parent, string text, int size, Vector2 min, Vector2 max)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
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

        private void CreateButton(Transform parent, string text, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
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
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                // Prevent pause menu in MainMenu scene
                if (SceneManager.GetActiveScene().name == "MainMenu") return;
                
                // Prevent pause if Hub UI is active
                if (SpaceMaintenance.Hub.ShopUI.Instance != null && SpaceMaintenance.Hub.ShopUI.Instance.IsOpen) return;
                if (SpaceMaintenance.Hub.MissionSetupUI.Instance != null && SpaceMaintenance.Hub.MissionSetupUI.Instance.IsOpen) return;

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

            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

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
            if (_settingsPanel != null) _settingsPanel.SetActive(false);
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
