using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SpaceMaintenance.Core.Data;

namespace SpaceMaintenance.UI
{
    public class InventorySlotUI : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _amountText;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Outline _selectionOutline;
        [SerializeField] private Color _selectedColor = new Color(0, 1f, 1f, 1f); // Cyan
        [SerializeField] private Color _unselectedColor = new Color(0, 0, 0, 0); // Transparent outline

        public void Setup(ItemData itemData, int amount)
        {
            if (itemData != null)
            {
                _iconImage.sprite = itemData.Icon;
                _iconImage.enabled = true;
                _amountText.text = amount > 1 ? amount.ToString() : "";
            }
            else
            {
                Clear();
            }
        }

        public void Clear()
        {
            _iconImage.sprite = null;
            _iconImage.enabled = false;
            _amountText.text = "";
        }

        public void SetSelected(bool isSelected)
        {
            if (_selectionOutline != null)
            {
                _selectionOutline.effectColor = isSelected ? _selectedColor : _unselectedColor;
            }
            if (_backgroundImage != null)
            {
                // Slightly lighter background when selected
                _backgroundImage.color = isSelected ? new Color(0.15f, 0.25f, 0.35f, 0.8f) : new Color(0.05f, 0.1f, 0.15f, 0.8f);
            }
        }
    }
}
