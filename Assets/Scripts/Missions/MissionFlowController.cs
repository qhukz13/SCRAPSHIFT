// ============================================================================
// SCRAPSHIFT — MissionFlowController.cs
// Manages mission phase transitions: DarkShip → ReactorStartup → Active →
// Completed / Failed. Coordinates RoundManager, ChaosManager, TaskManager,
// and HUD via MissionPhaseChangedEvent.
// ============================================================================

using SpaceMaintenance.Core;
using SpaceMaintenance.Core.Data;
using Unity.Netcode;
using UnityEngine;

namespace SpaceMaintenance.Missions
{
    [RequireComponent(typeof(NetworkObject))]
    public class MissionFlowController : NetworkBehaviour
    {
        public static MissionFlowController Instance { get; private set; }

        // ─── Inspector ──────────────────────────────────────────────────
        [SerializeField] private MissionConfig _config;

        // ─── Networked State ────────────────────────────────────────────
        public NetworkVariable<MissionPhase> CurrentPhase =
            new NetworkVariable<MissionPhase>(MissionPhase.DarkShip);

        // ─── Chaos delay ────────────────────────────────────────────────
        private float _chaosDelayTimer;
        private bool _chaosStarted;

        // =================================================================
        //  LIFECYCLE
        // =================================================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            Debug.Log($"[MissionFlow] OnNetworkSpawn called! IsServer: {IsServer}, StartDark: {_config != null && _config.StartDark}");

            // All clients listen for phase changes
            CurrentPhase.OnValueChanged += OnPhaseChangedCallback;

            if (IsServer)
            {
                // Subscribe to reactor events to drive phase transitions
                EventBus.Subscribe<ReactorStateChangedEvent>(OnReactorStateChanged);
                EventBus.Subscribe<GameOverEvent>(OnGameOver);
                EventBus.Subscribe<AllTasksCompletedEvent>(OnAllTasksCompleted);
                EventBus.Subscribe<CriticalTaskFailedEvent>(OnCriticalTaskFailed);

                // Start in DarkShip phase
                bool startDark = _config != null && _config.StartDark;
                if (startDark)
                {
                    Debug.Log($"[MissionFlow] startDark is true, calling TransitionTo(DarkShip)");
                    TransitionTo(MissionPhase.DarkShip);
                }
                else
                {
                    // Skip dark ship — go straight to active
                    Debug.Log($"[MissionFlow] startDark is false, calling TransitionTo(Active)");
                    TransitionTo(MissionPhase.Active);
                    ActivateMission();
                }
            }

            // Fire initial phase for late joiners
            Debug.Log($"[MissionFlow] Firing initial phase callback: {CurrentPhase.Value}");
            OnPhaseChangedCallback(CurrentPhase.Value, CurrentPhase.Value);
        }

        public override void OnNetworkDespawn()
        {
            CurrentPhase.OnValueChanged -= OnPhaseChangedCallback;

            if (IsServer)
            {
                EventBus.Unsubscribe<ReactorStateChangedEvent>(OnReactorStateChanged);
                EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
                EventBus.Unsubscribe<AllTasksCompletedEvent>(OnAllTasksCompleted);
                EventBus.Unsubscribe<CriticalTaskFailedEvent>(OnCriticalTaskFailed);
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            // Delayed chaos activation after entering Active phase
            if (CurrentPhase.Value == MissionPhase.Active && !_chaosStarted)
            {
                float delay = _config != null ? _config.ChaosStartDelay : 30f;
                _chaosDelayTimer += Time.deltaTime;

                if (_chaosDelayTimer >= delay)
                {
                    _chaosStarted = true;
                    ActivateChaos();
                }
            }
        }

        // =================================================================
        //  REACTOR EVENT HANDLING (Server)
        // =================================================================

        private void OnReactorStateChanged(ReactorStateChangedEvent evt)
        {
            Debug.Log($"[MissionFlow] OnReactorStateChanged! IsServer: {IsServer}, CurrentPhase: {CurrentPhase.Value}, NewState: {evt.NewState}");
            if (!IsServer) return;

            switch (CurrentPhase.Value)
            {
                case MissionPhase.DarkShip:
                    // Player started the reactor → transition to Startup
                    if (evt.NewState == ReactorState.Starting)
                    {
                        Debug.Log($"[MissionFlow] Transitioning to ReactorStartup!");
                        TransitionTo(MissionPhase.ReactorStartup);
                    }
                    break;

                case MissionPhase.ReactorStartup:
                    // Reactor finished booting → go Active
                    if (evt.NewState == ReactorState.Running)
                    {
                        TransitionTo(MissionPhase.Active);
                        ActivateMission();
                    }
                    // If reactor goes back offline during startup (e.g. failed), revert
                    else if (evt.NewState == ReactorState.Offline)
                    {
                        TransitionTo(MissionPhase.DarkShip);
                    }
                    break;

                case MissionPhase.Active:
                    // Meltdown during active play → mission failed
                    if (evt.NewState == ReactorState.Meltdown)
                    {
                        TransitionTo(MissionPhase.Failed);
                    }
                    break;
            }
        }

        // =================================================================
        //  GAME OVER EVENTS (Server)
        // =================================================================

        private void OnGameOver(GameOverEvent evt)
        {
            if (!IsServer) return;
            TransitionTo(evt.IsVictory ? MissionPhase.Completed : MissionPhase.Failed);
        }

        private void OnAllTasksCompleted(AllTasksCompletedEvent evt)
        {
            if (!IsServer) return;
            // Don't auto-win — player still needs to evacuate or timer decides
            // But we can signal the evaluator
            Debug.Log("[MissionFlow] All tasks completed — awaiting extraction.");
        }

        private void OnCriticalTaskFailed(CriticalTaskFailedEvent evt)
        {
            if (!IsServer) return;
            // Critical task failure is handled by WinLoseEvaluator publishing GameOverEvent
        }

        // =================================================================
        //  PHASE TRANSITIONS
        // =================================================================

        private void TransitionTo(MissionPhase newPhase)
        {
            if (!IsServer) return;

            var oldPhase = CurrentPhase.Value;
            if (oldPhase == newPhase) return;

            CurrentPhase.Value = newPhase;

            EventBus.Publish(new MissionPhaseChangedEvent
            {
                OldPhase = oldPhase,
                NewPhase = newPhase
            });

            Debug.Log($"[MissionFlow] {oldPhase} → {newPhase}");
        }

        // =================================================================
        //  ACTIVATION HELPERS (Server)
        // =================================================================

        /// <summary>Called when entering Active phase — starts timer, tasks, etc.</summary>
        private void ActivateMission()
        {
            // Start the mission timer
            if (RoundManager.Instance != null)
            {
                RoundManager.Instance.StartMissionTimer();
            }

            // Generate tasks
            if (Tasks.TaskManager.Instance != null)
            {
                Tasks.TaskManager.Instance.GenerateTasks();
            }

            // Chaos starts after a delay (handled in Update)
            _chaosDelayTimer = 0f;
            _chaosStarted = false;

            Debug.Log("[MissionFlow] Mission activated — timer started, tasks generated.");
        }

        /// <summary>Enable the chaos system after the configured delay.</summary>
        private void ActivateChaos()
        {
            if (Chaos.ChaosManager.Instance != null)
            {
                Chaos.ChaosManager.Instance.Activate();
            }
            Debug.Log("[MissionFlow] Chaos system activated.");
        }

        // =================================================================
        //  CLIENT CALLBACK
        // =================================================================

        private void OnPhaseChangedCallback(MissionPhase oldPhase, MissionPhase newPhase)
        {
            // Clients publish the event locally so HUD and other UI can react
            EventBus.Publish(new MissionPhaseChangedEvent
            {
                OldPhase = oldPhase,
                NewPhase = newPhase
            });
        }
    }
}
