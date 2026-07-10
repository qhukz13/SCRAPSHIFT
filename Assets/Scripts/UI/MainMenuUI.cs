using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceMaintenance.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Managers")]
        [SerializeField] private MainMenuManager _menuManager;

        [Header("Panels")]
        [SerializeField] private GameObject _mainPanel;
        [SerializeField] private GameObject _joinPanel;
        [SerializeField] private GameObject _loadingPanel;

        [Header("Main Panel")]
        [SerializeField] private Button _btnHost;
        [SerializeField] private Button _btnJoinMenu;
        [SerializeField] private Button _btnQuit;

        [Header("Join Panel")]
        [SerializeField] private TMP_InputField _inputJoinCode;
        [SerializeField] private Button _btnSubmitJoin;
        [SerializeField] private Button _btnBackJoin;

        [Header("Loading Panel")]
        [SerializeField] private TextMeshProUGUI _txtLoadingMessage;

        private void Awake()
        {
            if (_menuManager == null) _menuManager = GetComponent<MainMenuManager>();

            // Main Panel
            if (_btnHost != null) _btnHost.onClick.AddListener(() => _menuManager.OnHostClicked());
            if (_btnJoinMenu != null) _btnJoinMenu.onClick.AddListener(() => _menuManager.OnJoinMenuClicked());
            if (_btnQuit != null) _btnQuit.onClick.AddListener(() => _menuManager.OnQuitClicked());

            // Join Panel
            if (_btnSubmitJoin != null) _btnSubmitJoin.onClick.AddListener(() => _menuManager.OnJoinSubmit(_inputJoinCode.text));
            if (_btnBackJoin != null) _btnBackJoin.onClick.AddListener(() => _menuManager.OnJoinBackClicked());
        }

        public void ShowMainPanel()
        {
            _mainPanel.SetActive(true);
            _joinPanel.SetActive(false);
            _loadingPanel.SetActive(false);
        }

        public void ShowJoinPanel()
        {
            _mainPanel.SetActive(false);
            _joinPanel.SetActive(true);
            _loadingPanel.SetActive(false);
        }

        public void ShowLoading(string message)
        {
            _mainPanel.SetActive(false);
            _joinPanel.SetActive(false);
            _loadingPanel.SetActive(true);

            if (_txtLoadingMessage != null)
            {
                _txtLoadingMessage.text = message;
            }
        }
    }
}
