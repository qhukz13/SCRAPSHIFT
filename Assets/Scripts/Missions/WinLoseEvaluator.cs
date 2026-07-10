using SpaceMaintenance.Core;
using SpaceMaintenance.Core.Data;
using SpaceMaintenance.Damage;
using Unity.Netcode;
using UnityEngine;

namespace SpaceMaintenance.Missions
{
    public class WinLoseEvaluator : NetworkBehaviour
    {
        private bool _gameOverTriggered = false;

        private void Update()
        {
            if (!IsServer || _gameOverTriggered) return;
            if (RoundManager.Instance == null || !RoundManager.Instance.IsGameRunning.Value) return;

            // Check Lose Condition (Hull Integrity)
            if (DamageManager.Instance != null && DamageManager.Instance.HullIntegrity.Value <= 0)
            {
                TriggerGameOver(false, "Hull integrity reached 0. The ship has been destroyed.");
                return;
            }

            var config = RoundManager.Instance.GetConfig();
            if (config == null) return;

            // Check Win/Lose Conditions based on Game Mode
            if (config.Mode == GameMode.Survival)
            {
                if (RoundManager.Instance.TimeRemaining.Value <= 0)
                {
                    TriggerGameOver(true, "You survived the entire mission duration!");
                }
            }
            else if (config.Mode == GameMode.Tasks)
            {
                if (MissionManager.Instance != null && MissionManager.Instance.TasksCompleted.Value >= config.TasksRequired)
                {
                    TriggerGameOver(true, $"All {config.TasksRequired} tasks completed!");
                }
                else if (RoundManager.Instance.TimeRemaining.Value <= 0)
                {
                    int completed = MissionManager.Instance != null ? MissionManager.Instance.TasksCompleted.Value : 0;
                    TriggerGameOver(false, $"Time ran out. Tasks completed: {completed}/{config.TasksRequired}.");
                }
            }
        }

        private void TriggerGameOver(bool won, string reason)
        {
            _gameOverTriggered = true;
            RoundManager.Instance.EndRound();

            Debug.Log($"<color={(won ? "green" : "red")}>{(won ? "VICTORY!" : "DEFEAT!")} {reason}</color>");

            EventBus.Publish(new GameOverEvent
            {
                IsVictory = won,
                Reason = reason
            });
        }
    }
}

