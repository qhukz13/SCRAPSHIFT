using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;

namespace SpaceMaintenance.Hub
{
    public class MissionSetupUI : MonoBehaviour
    {
        public static MissionSetupUI Instance { get; private set; }

        private GameObject _uiPanel;
        private MissionLaunchConsole _console;

        public bool IsOpen => _uiPanel != null && _uiPanel.activeSelf;

        private Transform _playersContainer;
        private TextMeshProUGUI _readyStatusText;
        private Button _startBtn;
        private TextMeshProUGUI _modeText;
        private TextMeshProUGUI _settingText;
        
        private GameObject _infoDimming;
        private GameObject _infoPanel;
        
        private Dictionary<ulong, TextMeshProUGUI> _playerSlots = new Dictionary<ulong, TextMeshProUGUI>();

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
            var canvasGO = new GameObject("MissionSetupCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform);
            
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 950; 

            _uiPanel = new GameObject("SetupPanel", typeof(RectTransform), typeof(Image));
            _uiPanel.transform.SetParent(canvasGO.transform, false);
            
            var rt = _uiPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, 0.05f);
            rt.anchorMax = new Vector2(0.95f, 0.95f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            
            _uiPanel.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.2f, 0.98f);

            // Close button
            var closeBtn = CreateButton("X", new Vector2(0.95f, 0.9f), new Vector2(0.99f, 0.98f), CloseUI, _uiPanel.transform);
            closeBtn.GetComponent<Image>().color = Color.red;

            // Title
            CreateText("MISSION SETUP", 48, new Vector2(0, 0.85f), new Vector2(1, 0.95f), _uiPanel.transform);

            // Players Area (Left Half)
            var pArea = new GameObject("PlayersArea", typeof(RectTransform));
            pArea.transform.SetParent(_uiPanel.transform, false);
            var pAreaRt = pArea.GetComponent<RectTransform>();
            pAreaRt.anchorMin = new Vector2(0.05f, 0.1f);
            pAreaRt.anchorMax = new Vector2(0.5f, 0.8f);
            pAreaRt.offsetMin = pAreaRt.offsetMax = Vector2.zero;
            _playersContainer = pArea.transform;

            // Settings Area (Right Half)
            var sArea = new GameObject("SettingsArea", typeof(RectTransform));
            sArea.transform.SetParent(_uiPanel.transform, false);
            var sAreaRt = sArea.GetComponent<RectTransform>();
            sAreaRt.anchorMin = new Vector2(0.55f, 0.1f);
            sAreaRt.anchorMax = new Vector2(0.95f, 0.8f);
            sAreaRt.offsetMin = sAreaRt.offsetMax = Vector2.zero;

            CreateText("Select Mission Mode:", 32, new Vector2(0, 0.8f), new Vector2(0.9f, 0.9f), sArea.transform);
            
            var infoBtn = CreateButton("i", new Vector2(0.9f, 0.82f), new Vector2(0.98f, 0.88f), ToggleInfo, sArea.transform);
            infoBtn.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.8f);

            _modeText = CreateText("MODE: CHAOS", 30, new Vector2(0.2f, 0.65f), new Vector2(0.8f, 0.75f), sArea.transform);
            CreateButton("<", new Vector2(0.05f, 0.65f), new Vector2(0.15f, 0.75f), () => ChangeMode(-1), sArea.transform);
            CreateButton(">", new Vector2(0.85f, 0.65f), new Vector2(0.95f, 0.75f), () => ChangeMode(1), sArea.transform);

            CreateText("Select Mission Settings:", 32, new Vector2(0, 0.45f), new Vector2(1, 0.55f), sArea.transform);
            _settingText = CreateText("10", 36, new Vector2(0, 0.3f), new Vector2(1, 0.4f), sArea.transform);
            CreateButton("-", new Vector2(0.1f, 0.3f), new Vector2(0.2f, 0.4f), () => ChangeSetting(-5), sArea.transform);
            CreateButton("+", new Vector2(0.8f, 0.3f), new Vector2(0.9f, 0.4f), () => ChangeSetting(5), sArea.transform);

            // Bottom Right buttons
            _readyStatusText = CreateText("0/1 Ready", 24, new Vector2(0.1f, 0.15f), new Vector2(0.4f, 0.25f), sArea.transform);
            var readyBtn = CreateButton("READY", new Vector2(0.1f, 0f), new Vector2(0.4f, 0.15f), ToggleReady, sArea.transform);
            readyBtn.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.2f);

            _startBtn = CreateButton("START", new Vector2(0.6f, 0f), new Vector2(0.9f, 0.15f), LaunchMission, sArea.transform);
            _startBtn.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f);

            // Info Dimming Background
            _infoDimming = new GameObject("InfoDimming", typeof(RectTransform), typeof(Image), typeof(Button));
            _infoDimming.transform.SetParent(canvasGO.transform, false);
            var dimRt = _infoDimming.GetComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = dimRt.offsetMax = Vector2.zero;
            _infoDimming.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);
            _infoDimming.GetComponent<Button>().onClick.AddListener(ToggleInfo);

            // Info Panel
            _infoPanel = new GameObject("InfoPanel", typeof(RectTransform), typeof(Image));
            _infoPanel.transform.SetParent(_infoDimming.transform, false);
            var infoRt = _infoPanel.GetComponent<RectTransform>();
            infoRt.anchorMin = new Vector2(0.2f, 0.25f);
            infoRt.anchorMax = new Vector2(0.8f, 0.75f);
            infoRt.offsetMin = infoRt.offsetMax = Vector2.zero;
            _infoPanel.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 1f);

            CreateText("MODES INFO\n\nCHAOS: The ship constantly breaks down. Survive for the selected amount of time.\n\nMAINTENANCE: Complete all pre-installed tasks to win the mission.", 
                28, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.85f), _infoPanel.transform).alignment = TextAlignmentOptions.TopLeft;

            var closeInfoBtn = CreateButton("X", new Vector2(0.92f, 0.88f), new Vector2(0.98f, 0.96f), ToggleInfo, _infoPanel.transform);
            closeInfoBtn.GetComponent<Image>().color = Color.red;

            _uiPanel.SetActive(false);
            _infoDimming.SetActive(false);
        }

        private TextMeshProUGUI CreateText(string text, int size, Vector2 min, Vector2 max, Transform parent)
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
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 14;
            tmp.fontSizeMax = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            return tmp;
        }

        private Button CreateButton(string text, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action, Transform parent)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.color = new Color(0.3f, 0.3f, 0.4f);
            
            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(action);

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(go.transform, false);
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = textRT.offsetMax = Vector2.zero;

            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btn;
        }

        public void OpenUI(MissionLaunchConsole console)
        {
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            _console = console;
            _uiPanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            _startBtn.gameObject.SetActive(NetworkManager.Singleton.IsServer);
            UpdatePlayerSlots();
            RefreshData();
        }

        public void CloseUI()
        {
            _uiPanel.SetActive(false);
            if (_infoDimming != null) _infoDimming.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void ToggleInfo()
        {
            if (_infoDimming != null) _infoDimming.SetActive(!_infoDimming.activeSelf);
        }

        private void ChangeMode(int dir)
        {
            if (_console == null || !NetworkManager.Singleton.IsServer) return;
            int current = _console.SelectedMode.Value;
            current = current == 0 ? 1 : 0;
            _console.ChangeModeServerRpc(current);
        }

        private void ChangeSetting(int diff)
        {
            if (_console == null || !NetworkManager.Singleton.IsServer) return;
            int current = _console.SettingValue.Value + diff;
            if (current < 5) current = 5;
            _console.ChangeSettingServerRpc(current);
        }

        private void ToggleReady()
        {
            if (_console == null) return;
            _console.ToggleReadyServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        private void LaunchMission()
        {
            if (_console == null || !NetworkManager.Singleton.IsServer) return;
            _console.LaunchMissionServerRpc();
        }

        private void Update()
        {
            if (!_uiPanel.activeSelf || _console == null) return;

            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame) CloseUI();
            
            RefreshData();
            UpdatePlayerSlots();
        }

        private void RefreshData()
        {
            if (_console.SelectedMode.Value == 0) {
                _modeText.text = "MODE: CHAOS";
                _settingText.text = $"{_console.SettingValue.Value} MIN";
            } else {
                _modeText.text = "MODE: MAINTENANCE";
                _settingText.text = $"{_console.SettingValue.Value} TASKS";
            }

            int readyCount = _console.ReadyPlayers.Count;
            int total = NetworkManager.Singleton.ConnectedClientsList.Count;
            _readyStatusText.text = $"{readyCount}/{total} READY";
        }

        private void UpdatePlayerSlots()
        {
            var clients = NetworkManager.Singleton.ConnectedClientsList;
            
            // Rebuild if count differs
            if (clients.Count != _playerSlots.Count)
            {
                foreach (Transform child in _playersContainer) Destroy(child.gameObject);
                _playerSlots.Clear();

                float width = 1f / Mathf.Max(1, clients.Count);
                for (int i = 0; i < clients.Count; i++)
                {
                    ulong clientId = clients[i].ClientId;
                    
                    var slot = new GameObject($"Slot_{clientId}", typeof(RectTransform), typeof(Image));
                    slot.transform.SetParent(_playersContainer, false);
                    var rt = slot.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(i * width + 0.05f, 0f);
                    rt.anchorMax = new Vector2((i + 1) * width - 0.05f, 1f);
                    rt.offsetMin = rt.offsetMax = Vector2.zero;
                    slot.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);

                    var nameTxt = CreateText($"Player {clientId}", 24, new Vector2(0, 0.8f), new Vector2(1, 1f), slot.transform);
                    var statusTxt = CreateText("NOT READY", 24, new Vector2(0, 0f), new Vector2(1, 0.2f), slot.transform);
                    statusTxt.color = Color.red;

                    _playerSlots.Add(clientId, statusTxt);
                }
            }

            // Update statuses
            foreach (var kvp in _playerSlots)
            {
                bool isReady = _console.ReadyPlayers.Contains(kvp.Key);
                if (isReady)
                {
                    kvp.Value.text = "READY";
                    kvp.Value.color = Color.green;
                }
                else
                {
                    kvp.Value.text = "NOT READY";
                    kvp.Value.color = Color.red;
                }
            }
        }
    }
}
