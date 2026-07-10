using UnityEngine;
using UnityEngine.UI;

namespace SpaceMaintenance.Player.UI
{
    public class RepairProgressUI : MonoBehaviour
    {
        [SerializeField] private RepairController _repairController;
        [SerializeField] private GameObject _uiPanel;
        [SerializeField] private Image _progressBar;

        private void Awake()
        {
            if (_progressBar != null && _progressBar.sprite == null)
            {
                // Auto-generate a perfect white square sprite in code so we don't need to assign one in the Editor
                Texture2D tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                _progressBar.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
                _progressBar.type = Image.Type.Filled;
                _progressBar.fillMethod = Image.FillMethod.Horizontal;
            }
        }

        private void Update()
        {
            if (_repairController == null || _uiPanel == null || _progressBar == null) return;

            var target = _repairController.CurrentRepairTarget;
            
            if (target != null && target.NeedsRepair)
            {
                if (_uiPanel != gameObject) _uiPanel.SetActive(true);
                _progressBar.enabled = true;
                _progressBar.fillAmount = target.RepairProgress;
            }
            else
            {
                if (_uiPanel != gameObject) _uiPanel.SetActive(false);
                _progressBar.enabled = false;
            }
        }
    }
}
