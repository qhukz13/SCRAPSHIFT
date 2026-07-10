using SpaceMaintenance.Core;
using Unity.Netcode;
using UnityEngine;
using SpaceMaintenance.Damage;

namespace SpaceMaintenance.Chaos
{
    public class ChaosManager : NetworkBehaviour
    {
        public static ChaosManager Instance { get; private set; }
        
        [SerializeField] private ChaosEventConfig _config;

        private float _timeSinceLastEvent = 0f;
        private float _timeSinceLastDamage = 0f;
        private int _activeDisasters = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                EventBus.Subscribe<SpaceMaintenance.Core.Data.SystemRepairedEvent>(OnSystemRepaired);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                EventBus.Unsubscribe<SpaceMaintenance.Core.Data.SystemRepairedEvent>(OnSystemRepaired);
            }
        }

        private void Update()
        {
            if (!IsServer || _config == null) return;

            // Spawn random events
            _timeSinceLastEvent += Time.deltaTime;
            if (_timeSinceLastEvent >= _config.TimeBetweenEvents)
            {
                TriggerRandomEvent();
                _timeSinceLastEvent = 0f;
            }

            // Damage ship over time if disasters are active
            if (_activeDisasters > 0)
            {
                _timeSinceLastDamage += Time.deltaTime;
                if (_timeSinceLastDamage >= _config.DamageInterval)
                {
                    if (DamageManager.Instance != null)
                    {
                        DamageManager.Instance.TakeDamage(_config.DamagePerUnresolvedEvent * _activeDisasters);
                    }
                    _timeSinceLastDamage = 0f;
                }
            }
        }

        private void TriggerRandomEvent()
        {
            int eventType = Random.Range(0, 3);
            
            if (eventType == 0) // Generator Break
            {
                var gens = FindObjectsByType<SpaceMaintenance.ShipSystems.GeneratorController>(FindObjectsSortMode.None);
                foreach (var gen in gens)
                {
                    if (!gen.NeedsRepair)
                    {
                        gen.Break();
                        _activeDisasters++;
                        Debug.Log("Chaos: Generator broken!");
                        EventBus.Publish(new SpaceMaintenance.Core.Data.ChaosEventTriggered { EventName = "Generator Break" });
                        return;
                    }
                }
            }
            else if (eventType == 1) // Door Jam
            {
                var doors = FindObjectsByType<SpaceMaintenance.ShipSystems.DoorController>(FindObjectsSortMode.None);
                foreach (var door in doors)
                {
                    if (!door.IsJammed.Value)
                    {
                        door.JamDoor();
                        _activeDisasters++;
                        Debug.Log("Chaos: Door jammed!");
                        EventBus.Publish(new SpaceMaintenance.Core.Data.ChaosEventTriggered { EventName = "Door Jam" });
                        return;
                    }
                }
            }
            else if (eventType == 2) // Reactor Surge
            {
                var reactors = FindObjectsByType<SpaceMaintenance.ShipSystems.ReactorController>(FindObjectsSortMode.None);
                if (reactors.Length > 0)
                {
                    reactors[0].SurgeHeat();
                    Debug.Log("Chaos: Reactor heat surge!");
                    EventBus.Publish(new SpaceMaintenance.Core.Data.ChaosEventTriggered { EventName = "Reactor Surge" });
                    return; // Surge doesn't add to _activeDisasters directly, reactor handles its own melt
                }
            }
        }

        private void OnSystemRepaired(SpaceMaintenance.Core.Data.SystemRepairedEvent evt)
        {
            if (evt.SystemName == "Backup Generator" || evt.SystemName == "Door Unjammed")
            {
                _activeDisasters = Mathf.Max(0, _activeDisasters - 1);
            }
        }
    }
}
