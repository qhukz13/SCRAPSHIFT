using SpaceMaintenance.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceMaintenance.Missions.UI
{
    public class MissionResultUI : MonoBehaviour
    {
        [SerializeField] private GameObject _resultPanel;
        [SerializeField] private TextMeshProUGUI _resultTitle;
        [SerializeField] private TextMeshProUGUI _resultDescription;
        [SerializeField] private Button _restartButton;

        private void Awake()
        {
            if (_resultPanel != null) _resultPanel.SetActive(false);
            if (_restartButton != null) _restartButton.onClick.AddListener(OnRestartClicked);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<Core.Data.ChaosEventTriggered>(OnGameOver);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<Core.Data.ChaosEventTriggered>(OnGameOver);
        }

        private void OnGameOver(Core.Data.ChaosEventTriggered evt)
        {
            if (evt.EventName.Contains("Game Over"))
            {
                ShowResult(false, "Hull Integrity Reached 0. Ship Destroyed.");
            }
            else if (evt.EventName.Contains("Victory"))
            {
                ShowResult(true, "You successfully completed the mission!");
            }
        }

        public void ShowResult(bool isVictory, string message)
        {
            if (_resultPanel != null)
            {
                _resultPanel.SetActive(true);
                
                if (_resultTitle != null)
                {
                    _resultTitle.text = isVictory ? "VICTORY" : "DEFEAT";
                    _resultTitle.color = isVictory ? Color.green : Color.red;
                }

                if (_resultDescription != null)
                {
                    _resultDescription.text = message;
                }
                
                Cursor.lockState = CursorLockMode.None;
            }
        }

        private void OnRestartClicked()
        {
            // Simple reload of the active scene
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }
}
