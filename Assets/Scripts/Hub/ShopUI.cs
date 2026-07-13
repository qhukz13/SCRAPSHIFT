using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpaceMaintenance.Core;

namespace SpaceMaintenance.Hub
{
    public class ShopUI : MonoBehaviour
    {
        public static ShopUI Instance { get; private set; }

        private GameObject _shopPanel;
        private TextMeshProUGUI _fundsText;
        private HubTerminal _currentTerminal;

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
            var canvasGO = new GameObject("ShopCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform);
            
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900; 

            _shopPanel = new GameObject("ShopPanel", typeof(RectTransform), typeof(Image));
            _shopPanel.transform.SetParent(canvasGO.transform, false);
            
            var rt = _shopPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.1f);
            rt.anchorMax = new Vector2(0.9f, 0.9f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            
            _shopPanel.GetComponent<Image>().color = new Color(0.1f, 0.2f, 0.1f, 0.95f);

            CreateText("COMPANY TERMINAL", 48, new Vector2(0, 0.85f), new Vector2(1, 0.95f));
            _fundsText = CreateText("FUNDS: $0", 36, new Vector2(0, 0.75f), new Vector2(1, 0.85f));
            _fundsText.color = Color.green;

            // Items
            CreateShopItem("Pro Flashlight ($100)", "Double battery capacity", 100, "ProFlashlight", new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.65f));
            CreateShopItem("Adrenaline ($150)", "Increases max sprint stamina", 150, "Adrenaline", new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.45f));
            CreateShopItem("Wrench Upgrade ($200)", "Repairs pipes 50% faster", 200, "WrenchUpgrade", new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.25f));

            // Close
            var closeBtn = CreateButton("CLOSE", new Vector2(0.85f, 0.9f), new Vector2(0.95f, 0.95f), CloseShop);
            closeBtn.GetComponentInChildren<TextMeshProUGUI>().fontSize = 24;

            _shopPanel.SetActive(false);
        }

        private TextMeshProUGUI CreateText(string text, int size, Vector2 min, Vector2 max)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(_shopPanel.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            return tmp;
        }

        private Button CreateButton(string text, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_shopPanel.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.color = new Color(0.2f, 0.3f, 0.2f);
            
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
            tmp.fontSize = 28;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btn;
        }

        private void CreateShopItem(string title, string desc, int price, string id, Vector2 min, Vector2 max)
        {
            var container = new GameObject("ItemContainer", typeof(RectTransform), typeof(Image));
            container.transform.SetParent(_shopPanel.transform, false);
            var rt = container.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            container.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);

            // Title
            var titleText = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleText.transform.SetParent(container.transform, false);
            var titleRt = titleText.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.05f, 0.5f);
            titleRt.anchorMax = new Vector2(0.7f, 0.9f);
            titleRt.offsetMin = titleRt.offsetMax = Vector2.zero;
            var tTmp = titleText.GetComponent<TextMeshProUGUI>();
            tTmp.text = title;
            tTmp.fontSize = 32;
            tTmp.color = Color.white;
            tTmp.alignment = TextAlignmentOptions.Left;

            // Desc
            var descText = new GameObject("DescText", typeof(RectTransform), typeof(TextMeshProUGUI));
            descText.transform.SetParent(container.transform, false);
            var descRt = descText.GetComponent<RectTransform>();
            descRt.anchorMin = new Vector2(0.05f, 0.1f);
            descRt.anchorMax = new Vector2(0.7f, 0.5f);
            descRt.offsetMin = descRt.offsetMax = Vector2.zero;
            var dTmp = descText.GetComponent<TextMeshProUGUI>();
            dTmp.text = desc;
            dTmp.fontSize = 20;
            dTmp.color = Color.gray;
            dTmp.alignment = TextAlignmentOptions.Left;

            // Buy Button
            var btnGo = new GameObject("BuyBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(container.transform, false);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.75f, 0.2f);
            btnRt.anchorMax = new Vector2(0.95f, 0.8f);
            btnRt.offsetMin = btnRt.offsetMax = Vector2.zero;
            btnGo.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.2f);
            var btn = btnGo.GetComponent<Button>();
            btn.onClick.AddListener(() => PurchaseItem(price, id));

            var btnText = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnText.transform.SetParent(btnGo.transform, false);
            var bTextRt = btnText.GetComponent<RectTransform>();
            bTextRt.anchorMin = Vector2.zero;
            bTextRt.anchorMax = Vector2.one;
            bTextRt.offsetMin = bTextRt.offsetMax = Vector2.zero;
            var bTmp = btnText.GetComponent<TextMeshProUGUI>();
            bTmp.text = "BUY";
            bTmp.fontSize = 28;
            bTmp.color = Color.white;
            bTmp.alignment = TextAlignmentOptions.Center;
        }

        public void OpenShop(HubTerminal terminal)
        {
            _currentTerminal = terminal;
            _shopPanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (EconomyManager.Instance != null)
            {
                _fundsText.text = $"FUNDS: ${EconomyManager.Instance.CompanyFunds.Value}";
            }
        }

        public void CloseShop()
        {
            _shopPanel.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            if (_currentTerminal != null)
            {
                _currentTerminal.OnShopClosed();
            }
        }

        private void PurchaseItem(int price, string id)
        {
            if (_currentTerminal != null)
            {
                _currentTerminal.RequestPurchaseServerRpc(price, id);
            }
        }

        private void Update()
        {
            if (_shopPanel.activeSelf && EconomyManager.Instance != null)
            {
                _fundsText.text = $"FUNDS: ${EconomyManager.Instance.CompanyFunds.Value}";
            }

            if (_shopPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            {
                CloseShop();
            }
        }
    }
}
