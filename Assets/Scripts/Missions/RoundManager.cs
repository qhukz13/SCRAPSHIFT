using SpaceMaintenance.Core;
using SpaceMaintenance.Core.Data;
using SpaceMaintenance.Missions.UI;
using Unity.Netcode;
using UnityEngine;

namespace SpaceMaintenance.Missions
{
    public class RoundManager : NetworkBehaviour
    {
        public static RoundManager Instance { get; private set; }

        [SerializeField] private MissionConfig _config;
        
        public NetworkVariable<float> TimeRemaining = new NetworkVariable<float>(0f);
        public NetworkVariable<bool> IsGameRunning = new NetworkVariable<bool>(false);

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;

            // Instantiate Mission UI for local client
            if (FindFirstObjectByType<MissionResultUI>() == null)
            {
                var uiPrefab = Resources.Load<GameObject>("MissionCanvas");
                if (uiPrefab != null)
                {
                    Instantiate(uiPrefab);
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            // No longer auto-starting — MissionFlowController calls StartMissionTimer()
            if (IsServer)
            {
                TimeRemaining.Value = _config != null ? _config.SurvivalDuration : 300f;
                IsGameRunning.Value = false;
            }
        }

        /// <summary>Called by MissionFlowController when entering Active phase.</summary>
        public void StartMissionTimer()
        {
            if (!IsServer || _config == null) return;

            TimeRemaining.Value = _config.SurvivalDuration;
            IsGameRunning.Value = true;
            Debug.Log($"[RoundManager] Mission timer started! Mode: {_config.Mode}, Duration: {_config.SurvivalDuration}s");

            EventBus.Publish(new MissionTimerUpdatedEvent
            {
                TimeRemaining = TimeRemaining.Value,
                TotalTime = _config.SurvivalDuration
            });
        }

        /// <summary>Pause the mission timer (e.g. during cutscenes).</summary>
        public void PauseMissionTimer()
        {
            if (!IsServer) return;
            IsGameRunning.Value = false;
        }

        /// <summary>Resume a previously paused mission timer.</summary>
        public void ResumeMissionTimer()
        {
            if (!IsServer) return;
            IsGameRunning.Value = true;
        }

        private void Update()
        {
            if (!IsServer || !IsGameRunning.Value) return;

            TimeRemaining.Value -= Time.deltaTime;

            if (TimeRemaining.Value <= 0)
            {
                TimeRemaining.Value = 0;
            }

            // Publish timer update for HUD every frame
            EventBus.Publish(new MissionTimerUpdatedEvent
            {
                TimeRemaining = TimeRemaining.Value,
                TotalTime = _config != null ? _config.SurvivalDuration : 0f
            });
        }

        public void EndRound()
        {
            if (!IsServer) return;
            IsGameRunning.Value = false;
        }
        
        public MissionConfig GetConfig() => _config;
    }
}

