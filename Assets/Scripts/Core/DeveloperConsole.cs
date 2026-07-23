using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpaceMaintenance.Player;
using SpaceMaintenance.Damage;

namespace SpaceMaintenance.Core
{
    public class DeveloperConsole : MonoBehaviour
    {
        public static DeveloperConsole Instance { get; private set; }

        private bool _isShowing;
        
        // UI Elements
        private Canvas _canvas;
        private TMP_InputField _inputField;
        private TextMeshProUGUI _logText;
        private ScrollRect _scrollRect;

        // Command registry
        private delegate void ConsoleCommand(string[] args);
        private Dictionary<string, ConsoleCommand> _commands = new Dictionary<string, ConsoleCommand>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeOnLoad()
        {
            var go = new GameObject("DeveloperConsole");
            go.AddComponent<DeveloperConsole>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            CreateUI();
            RegisterCommands();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.BackQuote) || Input.GetKeyDown(KeyCode.Tilde))
            {
                ToggleConsole();
            }

            if (_isShowing)
            {
                // Prevent closing when typing backquote
                if (_inputField.text.Contains("`") || _inputField.text.Contains("~"))
                {
                    _inputField.text = _inputField.text.Replace("`", "").Replace("~", "");
                }

                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    ExecuteCommand(_inputField.text);
                    _inputField.text = "";
                    _inputField.ActivateInputField();
                }
            }
        }

        private void ToggleConsole()
        {
            _isShowing = !_isShowing;
            _canvas.gameObject.SetActive(_isShowing);

            if (_isShowing)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                _inputField.ActivateInputField();
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public void Log(string message)
        {
            if (_logText != null)
            {
                _logText.text += "\n> " + message;
                
                // Scroll to bottom
                Canvas.ForceUpdateCanvases();
                if (_scrollRect != null) _scrollRect.verticalNormalizedPosition = 0f;
            }
            Debug.Log("[DevConsole] " + message);
        }

        private void ExecuteCommand(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return;

            string[] parts = input.Trim().Split(' ');
            string cmdName = parts[0].ToLower();

            if (_commands.TryGetValue(cmdName, out ConsoleCommand command))
            {
                // Pass args (excluding the command name)
                string[] args = new string[parts.Length - 1];
                System.Array.Copy(parts, 1, args, 0, args.Length);
                command.Invoke(args);
            }
            else
            {
                Log($"Unknown command: {cmdName}. Type 'help' for a list of commands.");
            }
        }

        // =================================================================
        //  COMMAND REGISTRY
        // =================================================================
        private void RegisterCommands()
        {
            _commands.Add("help", args => {
                Log("Available commands: help, noclip, tp, godmode, heal, money <amount>, speed <value>, gravity <value>, spawn <item_name>");
            });

            _commands.Add("noclip", args => {
                var player = FindObjectOfType<PlayerController>();
                if (player != null)
                {
                    player.IsNoclip = !player.IsNoclip;
                    Log($"Noclip turned {(player.IsNoclip ? "ON" : "OFF")}");
                }
                else Log("Player not found!");
            });

            _commands.Add("tp", args => ToggleThirdPerson());
            _commands.Add("thirdperson", args => ToggleThirdPerson());

            _commands.Add("godmode", args => {
                if (DamageManager.Instance != null)
                {
                    DamageManager.Instance.IsInvincible = !DamageManager.Instance.IsInvincible;
                    Log($"Godmode (Hull Invincibility) turned {(DamageManager.Instance.IsInvincible ? "ON" : "OFF")}");
                }
                else Log("DamageManager not found!");
            });

            _commands.Add("heal", args => {
                if (DamageManager.Instance != null)
                {
                    DamageManager.Instance.RepairHull(9999f);
                    Log("Hull repaired to maximum.");
                }
                else Log("DamageManager not found!");
            });

            _commands.Add("money", args => {
                if (args.Length > 0 && int.TryParse(args[0], out int amount))
                {
                    if (SpaceMaintenance.Core.EconomyManager.Instance != null)
                    {
                        SpaceMaintenance.Core.EconomyManager.Instance.CheatAddFundsServerRpc(amount);
                        Log($"Requested ${amount} funds from server.");
                    }
                    else Log("EconomyManager not found!");
                }
                else Log("Usage: money <amount>");
            });

            _commands.Add("spawn", args => {
                if (args.Length > 0)
                {
                    var player = FindObjectOfType<PlayerController>();
                    if (player != null)
                    {
                        string prefabName = args[0];
                        Vector3 spawnPos = player.transform.position + player.transform.forward * 2f + Vector3.up * 1f;
                        player.CheatSpawnItemServerRpc(prefabName, spawnPos);
                        Log($"Requested server to spawn '{prefabName}'.");
                    }
                    else Log("PlayerController not found!");
                }
                else Log("Usage: spawn <item_name> (e.g., spawn wrench, spawn scanner, spawn heavyfuse)");
            });

            _commands.Add("speed", args => {
                if (args.Length > 0 && float.TryParse(args[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float speed))
                {
                    var player = FindObjectOfType<PlayerController>();
                    if (player != null && player.Config != null)
                    {
                        player.Config.MoveSpeed = speed;
                        player.Config.SprintSpeed = speed * 1.5f;
                        Log($"Set movement speed to {speed} (Sprint: {speed * 1.5f})");
                    }
                    else Log("Player or PlayerMovementConfig not found!");
                }
                else Log("Usage: speed <value>");
            });

            _commands.Add("gravity", args => {
                if (args.Length > 0 && float.TryParse(args[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float grav))
                {
                    Physics.gravity = new Vector3(0, grav, 0);
                    Log($"Gravity set to {grav}");
                }
                else Log("Usage: gravity <value>");
            });
        }

        private void ToggleThirdPerson()
        {
            var cam = FindObjectOfType<PlayerCameraController>();
            if (cam != null)
            {
                cam.ToggleThirdPerson();
                Log($"Thirdperson turned {(cam.IsThirdPerson ? "ON" : "OFF")}");
            }
            else Log("Player camera not found!");
        }

        // =================================================================
        //  UI GENERATION
        // =================================================================
        private void CreateUI()
        {
            // Canvas
            var canvasGo = new GameObject("DevConsoleCanvas");
            canvasGo.transform.SetParent(transform);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 999;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();
            
            // EventSystem is assumed to be present in all scenes

            // Panel
            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(canvasGo.transform, false);
            var panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0f, 0.5f);
            panelRt.anchorMax = new Vector2(1f, 1f);
            panelRt.offsetMin = panelRt.offsetMax = Vector2.zero;
            panelGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.8f);

            // ScrollView
            var scrollGo = new GameObject("Scroll View", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(panelGo.transform, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0, 0.1f);
            scrollRt.anchorMax = new Vector2(1, 1);
            scrollRt.offsetMin = scrollRt.offsetMax = Vector2.zero;
            scrollGo.GetComponent<Image>().color = new Color(0, 0, 0, 0);

            // Viewport
            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = viewportRt.offsetMax = Vector2.zero;

            // Content
            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 0);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.offsetMin = contentRt.offsetMax = Vector2.zero;
            contentRt.pivot = new Vector2(0, 0);
            
            var csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(10, 10, 10, 10);

            _scrollRect = scrollGo.GetComponent<ScrollRect>();
            _scrollRect.content = contentRt;
            _scrollRect.viewport = viewportRt;
            _scrollRect.horizontal = false;

            // Log Text
            var textGo = new GameObject("LogText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(contentGo.transform, false);
            _logText = textGo.GetComponent<TextMeshProUGUI>();
            _logText.fontSize = 18;
            _logText.color = Color.white;
            _logText.alignment = TextAlignmentOptions.BottomLeft;
            _logText.text = "Developer Console Initialized.\nType 'help' for commands.";

            // Input Field Area
            var inputGo = new GameObject("InputField", typeof(RectTransform), typeof(Image));
            inputGo.transform.SetParent(panelGo.transform, false);
            var inputRt = inputGo.GetComponent<RectTransform>();
            inputRt.anchorMin = new Vector2(0, 0);
            inputRt.anchorMax = new Vector2(1, 0.1f);
            inputRt.offsetMin = inputRt.offsetMax = Vector2.zero;
            inputGo.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 1f);

            var inputTextGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            inputTextGo.transform.SetParent(inputGo.transform, false);
            var inputTextRt = inputTextGo.GetComponent<RectTransform>();
            inputTextRt.anchorMin = Vector2.zero;
            inputTextRt.anchorMax = Vector2.one;
            inputTextRt.offsetMin = new Vector2(10, 0);
            inputTextRt.offsetMax = new Vector2(-10, 0);
            
            var tmpInputText = inputTextGo.GetComponent<TextMeshProUGUI>();
            tmpInputText.fontSize = 20;
            tmpInputText.color = Color.white;
            tmpInputText.alignment = TextAlignmentOptions.Left;

            _inputField = inputGo.AddComponent<TMP_InputField>();
            _inputField.textComponent = tmpInputText;
            _inputField.textViewport = inputTextRt;

            // Hide by default
            _isShowing = false;
            canvasGo.SetActive(false);
        }
    }
}
