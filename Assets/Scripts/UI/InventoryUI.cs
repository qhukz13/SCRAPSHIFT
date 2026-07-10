using SpaceMaintenance.Player.Inventory;
using SpaceMaintenance.Core.Data;
using Unity.Netcode;
using UnityEngine;

namespace SpaceMaintenance.UI
{
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] private PlayerInventory _playerInventory;
        [SerializeField] private ItemDatabase _itemDatabase;
        [SerializeField] private InventorySlotUI[] _slots;
        
        private int _selectedIndex = 0;

        private void Start()
        {
            if (_playerInventory == null)
                _playerInventory = GetComponentInParent<PlayerInventory>();
                
            if (_playerInventory != null && _playerInventory.IsOwner)
            {
                _playerInventory.NetworkItems.OnListChanged += OnInventoryChanged;
                UpdateUI();
            }
            else
            {
                // Disable UI if not the owner
                gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_playerInventory != null)
            {
                _playerInventory.NetworkItems.OnListChanged -= OnInventoryChanged;
            }
        }

        private void Update()
        {
            if (_playerInventory == null || !_playerInventory.IsOwner) return;

            // Handle Selection
            if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);

            // Handle scroll wheel
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f)
            {
                SelectSlot(_selectedIndex - 1);
            }
            else if (scroll < 0f)
            {
                SelectSlot(_selectedIndex + 1);
            }

            // Handle Dropping
            if (Input.GetKeyDown(KeyCode.Q))
            {
                if (_selectedIndex < _playerInventory.NetworkItems.Count)
                {
                    Transform camTransform = null;
                    if (Camera.main != null) camTransform = Camera.main.transform;
                    else
                    {
                        var cam = _playerInventory.GetComponentInChildren<Camera>();
                        if (cam != null) camTransform = cam.transform;
                        else camTransform = _playerInventory.transform;
                    }
                    
                    _playerInventory.RequestDropItemServerRpc(_selectedIndex, camTransform.position + camTransform.forward, camTransform.forward);
                }
            }
        }

        private void SelectSlot(int index)
        {
            if (index < 0) index = _slots.Length - 1;
            if (index >= _slots.Length) index = 0;
            
            _selectedIndex = index;
            UpdateSelectionVisuals();
        }

        private void OnInventoryChanged(NetworkListEvent<NetworkInventoryItem> changeEvent)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (_itemDatabase == null) return;

            for (int i = 0; i < _slots.Length; i++)
            {
                if (i < _playerInventory.NetworkItems.Count)
                {
                    var item = _playerInventory.NetworkItems[i];
                    var itemData = _itemDatabase.GetItem(item.ItemID.ToString());
                    _slots[i].Setup(itemData, item.Amount);
                }
                else
                {
                    _slots[i].Clear();
                }
            }
            
            UpdateSelectionVisuals();
        }

        private void UpdateSelectionVisuals()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].SetSelected(i == _selectedIndex);
            }
        }
    }
}
