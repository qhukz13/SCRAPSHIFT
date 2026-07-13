// ============================================================================
// Space Maintenance — ChaosManager.cs
// Periodically injects random failure events: generator breaks, door jams,
// door locks, reactor surges, and power drains.
// Active disasters apply continuous hull damage.
// ============================================================================

using SpaceMaintenance.Core;
using SpaceMaintenance.Core.Data;
using Unity.Netcode;
using UnityEngine;
using SpaceMaintenance.Damage;

namespace SpaceMaintenance.Chaos
{
    public class ChaosManager : NetworkBehaviour
    {
        public static ChaosManager Instance { get; private set; }
        
        [SerializeField] private ChaosEventConfig _config;

        // ─── Networked State ────────────────────────────────────────────
        public NetworkVariable<bool> IsActive = new NetworkVariable<bool>(false);

        // ─── Server-only ────────────────────────────────────────────────
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
                IsActive.Value = false; // Starts inactive — MissionFlowController activates
                EventBus.Subscribe<SystemRepairedEvent>(OnSystemRepaired);
                EventBus.Subscribe<MissionPhaseChangedEvent>(OnMissionPhaseChanged);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                EventBus.Unsubscribe<SystemRepairedEvent>(OnSystemRepaired);
                EventBus.Unsubscribe<MissionPhaseChangedEvent>(OnMissionPhaseChanged);
            }
        }

        private void Update()
        {
            if (!IsServer || _config == null || !IsActive.Value) return;

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

        // =================================================================
        //  EVENT SELECTION
        // =================================================================

        private void TriggerRandomEvent()
        {
            // Weighted random — 5 event types
            int eventType = Random.Range(0, 5);
            
            switch (eventType)
            {
                case 0:
                    TryGeneratorBreak();
                    break;
                case 1:
                    TryDoorJam();
                    break;
                case 2:
                    TryReactorSurge();
                    break;
                case 3:
                    TryDoorLock();
                    break;
                case 4:
                    TryPowerDrain();
                    break;
            }
        }

        // =================================================================
        //  EVENT IMPLEMENTATIONS
        // =================================================================

        /// <summary>Break a random healthy generator.</summary>
        private void TryGeneratorBreak()
        {
            var gens = FindObjectsByType<SpaceMaintenance.ShipSystems.GeneratorController>(FindObjectsSortMode.None);
            foreach (var gen in gens)
            {
                if (!gen.NeedsRepair)
                {
                    gen.Break();
                    _activeDisasters++;
                    Debug.Log("[Chaos] Generator broken!");
                    EventBus.Publish(new ChaosEventTriggered { EventName = "Generator Break" });
                    NotifyChaosEventClientRpc("Generator Break");
                    return;
                }
            }
        }

        /// <summary>Jam a random unjammed door.</summary>
        private void TryDoorJam()
        {
            var doors = FindObjectsByType<SpaceMaintenance.ShipSystems.DoorController>(FindObjectsSortMode.None);
            // Shuffle pick — find a non-broken, non-locked door
            foreach (var door in doors)
            {
                if (door.State.Value != SpaceMaintenance.Core.DoorState.Broken &&
                    door.State.Value != SpaceMaintenance.Core.DoorState.Locked)
                {
                    door.JamDoor();
                    _activeDisasters++;
                    Debug.Log("[Chaos] Door jammed!");
                    EventBus.Publish(new ChaosEventTriggered { EventName = "Door Jam" });
                    NotifyChaosEventClientRpc("Door Jam");
                    return;
                }
            }
        }

        /// <summary>Lock a random unlocked door.</summary>
        private void TryDoorLock()
        {
            var doors = FindObjectsByType<SpaceMaintenance.ShipSystems.DoorController>(FindObjectsSortMode.None);
            foreach (var door in doors)
            {
                if (door.State.Value == SpaceMaintenance.Core.DoorState.Closed ||
                    door.State.Value == SpaceMaintenance.Core.DoorState.Open)
                {
                    door.LockDoor();
                    _activeDisasters++;
                    Debug.Log("[Chaos] Door locked!");
                    EventBus.Publish(new ChaosEventTriggered { EventName = "Door Lock" });
                    NotifyChaosEventClientRpc("Door Lock");
                    return;
                }
            }
        }

        /// <summary>Spike reactor heat.</summary>
        private void TryReactorSurge()
        {
            var reactors = FindObjectsByType<SpaceMaintenance.ShipSystems.ReactorController>(FindObjectsSortMode.None);
            if (reactors.Length > 0)
            {
                reactors[0].SurgeHeat();
                Debug.Log("[Chaos] Reactor heat surge!");
                EventBus.Publish(new ChaosEventTriggered { EventName = "Reactor Surge" });
                NotifyChaosEventClientRpc("Reactor Surge");
                // Surge doesn't directly add to _activeDisasters — reactor handles its own state
            }
        }

        /// <summary>Drain a chunk of power from the grid.</summary>
        private void TryPowerDrain()
        {
            if (SpaceMaintenance.ShipSystems.PowerManager.Instance == null) return;

            float drainAmount = Random.Range(50f, 200f);
            SpaceMaintenance.ShipSystems.PowerManager.Instance.DrainPower(drainAmount);
            Debug.Log($"[Chaos] Power drain! Lost {drainAmount} units.");
            EventBus.Publish(new ChaosEventTriggered { EventName = "Power Drain" });
            NotifyChaosEventClientRpc($"Power Drain ({drainAmount:F0})");
            // Power drain is instant, not a persistent disaster
        }

        // =================================================================
        //  CLIENT NOTIFICATION
        // =================================================================

        [ClientRpc]
        private void NotifyChaosEventClientRpc(string eventName)
        {
            // Placeholder: hook UI warning system here
            Debug.Log($"[Chaos Alert] {eventName}!");
        }

        // =================================================================
        //  ACTIVATION (called by MissionFlowController)
        // =================================================================

        /// <summary>Enable chaos events. Called when the mission enters Active phase.</summary>
        public void Activate()
        {
            if (!IsServer) return;
            IsActive.Value = true;
            _timeSinceLastEvent = 0f;
            Debug.Log("[Chaos] Activated.");
        }

        /// <summary>Disable chaos events. Called when the mission ends.</summary>
        public void Deactivate()
        {
            if (!IsServer) return;
            IsActive.Value = false;
            Debug.Log("[Chaos] Deactivated.");
        }

        // =================================================================
        //  MISSION PHASE HANDLER
        // =================================================================

        private void OnMissionPhaseChanged(MissionPhaseChangedEvent evt)
        {
            if (!IsServer) return;

            // Auto-deactivate when mission ends
            if (evt.NewPhase == MissionPhase.Completed || evt.NewPhase == MissionPhase.Failed)
            {
                Deactivate();
            }
        }

        // =================================================================
        //  RESOLUTION
        // =================================================================

        private void OnSystemRepaired(SystemRepairedEvent evt)
        {
            // Decrement active disasters for repairable events
            if (evt.SystemName == "Backup Generator" ||
                evt.SystemName.Contains("Unjammed"))
            {
                _activeDisasters = Mathf.Max(0, _activeDisasters - 1);
            }
        }
    }
}
