using SpaceMaintenance.Core;
using SpaceMaintenance.Damage;
using Unity.Netcode;
using UnityEngine;

namespace SpaceMaintenance.Missions
{
    public class WinLoseEvaluator : NetworkBehaviour
    {
        private void Update()
        {
            if (!IsServer || RoundManager.Instance == null || !RoundManager.Instance.IsGameRunning.Value) return;

            // Check Lose Condition (Hull Integrity)
            if (DamageManager.Instance != null && DamageManager.Instance.HullIntegrity.Value <= 0)
            {
                TriggerGameOver(false);
                return;
            }

            var config = RoundManager.Instance.GetConfig();
            if (config == null) return;

            // Check Win Condition based on Game Mode
            if (config.Mode == SpaceMaintenance.Core.GameMode.Survival)
            {
                if (RoundManager.Instance.TimeRemaining.Value <= 0)
                {
                    TriggerGameOver(true);
                }
            }
            else if (config.Mode == SpaceMaintenance.Core.GameMode.Tasks)
            {
                if (MissionManager.Instance != null && MissionManager.Instance.TasksCompleted.Value >= config.TasksRequired)
                {
                    TriggerGameOver(true);
                }
                else if (RoundManager.Instance.TimeRemaining.Value <= 0)
                {
                    // In Task mode, running out of time is a lose
                    TriggerGameOver(false);
                }
            }
        }

        private void TriggerGameOver(bool won)
        {
            RoundManager.Instance.EndRound();
            string result = won ? "VICTORY!" : "DEFEAT! Ship Destroyed.";
            Debug.Log($"<color={(won ? "green" : "red")}>{result}</color>");
            
            // Here we would typically spawn a WinLose UI prefab or trigger a ClientRpc
        }
    }
}
