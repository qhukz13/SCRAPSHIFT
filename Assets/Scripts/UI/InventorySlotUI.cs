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
        [SerializeField] private Image _selectionBorder;
        [SerializeField] private Color _selectedColor = Color.white;
        [SerializeField] private Color _unselectedColor = new Color(1, 1, 1, 0.2f);

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
            if (_selectionBorder != null)
            {
                _selectionBorder.color = isSelected ? _selectedColor : _unselectedColor;
            }
        }
    }
}
