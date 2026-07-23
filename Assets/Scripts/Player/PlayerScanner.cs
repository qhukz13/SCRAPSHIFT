using SpaceMaintenance.Core;
using SpaceMaintenance.Player.Inventory;
using UnityEngine;
using System.Collections.Generic;

namespace SpaceMaintenance.Player
{
    public class PlayerScanner : MonoBehaviour
    {
        [SerializeField] private PlayerInventory _inventory;
        [SerializeField] private float _scanRadius = 50f;
        [SerializeField] private float _scanInterval = 1f;

        private float _nextScanTime;
        private List<LineRenderer> _lines = new List<LineRenderer>();

        private void Awake()
        {
            if (_inventory == null) _inventory = GetComponent<PlayerInventory>();
        }

        private void Update()
        {
            if (_inventory == null || !_inventory.IsOwner) return;

            bool hasScanner = _inventory.HasItem("Scanner");
            if (!hasScanner)
            {
                ClearLines();
                return;
            }

            if (Time.time >= _nextScanTime)
            {
                _nextScanTime = Time.time + _scanInterval;
                ScanForBrokenSystems();
            }
        }

        private void ScanForBrokenSystems()
        {
            ClearLines();
            
            var repairables = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var r in repairables)
            {
                if (r is IRepairable repairable && repairable.NeedsRepair)
                {
                    float dist = Vector3.Distance(transform.position, r.transform.position);
                    if (dist <= _scanRadius)
                    {
                        DrawLineTo(r.transform.position);
                    }
                }
            }
        }

        private void DrawLineTo(Vector3 target)
        {
            GameObject lineObj = new GameObject("ScannerLine");
            lineObj.transform.SetParent(transform);
            
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.startWidth = 0.05f;
            lr.endWidth = 0.05f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = new Color(0, 1, 1, 0.5f); // Cyan
            lr.endColor = new Color(0, 1, 1, 0.1f);
            
            // Just draw a line from player slightly below camera to the target
            lr.SetPosition(0, transform.position + Vector3.up);
            lr.SetPosition(1, target);

            _lines.Add(lr);
        }

        private void ClearLines()
        {
            foreach (var line in _lines)
            {
                if (line != null) Destroy(line.gameObject);
            }
            _lines.Clear();
        }
    }
}
