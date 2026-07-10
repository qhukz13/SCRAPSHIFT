using SpaceMaintenance.Core;
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
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                StartRound();
            }
        }

        private void StartRound()
        {
            if (_config == null) return;
            TimeRemaining.Value = _config.SurvivalDuration;
            IsGameRunning.Value = true;
            Debug.Log($"Round Started! Mode: {_config.Mode}");
        }

        private void Update()
        {
            if (!IsServer || !IsGameRunning.Value) return;

            TimeRemaining.Value -= Time.deltaTime;

            if (TimeRemaining.Value <= 0)
            {
                TimeRemaining.Value = 0;
                // Handled by WinLoseEvaluator
            }
        }

        public void EndRound()
        {
            if (!IsServer) return;
            IsGameRunning.Value = false;
        }
        
        public MissionConfig GetConfig() => _config;
    }
}
