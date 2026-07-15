// ============================================================================
// SCRAPSHIFT — WinLoseEvaluator.cs
// Evaluates win/lose conditions. Integrates with TaskManager for task-based
// victory/failure, hull integrity for destruction loss, and mission timer.
// ============================================================================

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

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                EventBus.Subscribe<CriticalTaskFailedEvent>(OnCriticalTaskFailed);
                EventBus.Subscribe<AllTasksCompletedEvent>(OnAllTasksCompleted);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                EventBus.Unsubscribe<CriticalTaskFailedEvent>(OnCriticalTaskFailed);
                EventBus.Unsubscribe<AllTasksCompletedEvent>(OnAllTasksCompleted);
            }
        }

        private void Update()
        {
            if (!IsServer || _gameOverTriggered) return;
            if (RoundManager.Instance == null || !RoundManager.Instance.IsGameRunning.Value) return;

            // Check Lose Condition — Hull Integrity
            if (DamageManager.Instance != null && DamageManager.Instance.HullIntegrity.Value <= 0)
            {
                TriggerGameOver(false, "Hull integrity reached 0. The ship has been destroyed.");
                return;
            }

            // Check Win/Lose Condition — Timer expired
            if (RoundManager.Instance.TimeRemaining.Value <= 0)
            {
                int completed = 0;
                int total = 0;
                if (Tasks.TaskManager.Instance != null)
                {
                    completed = Tasks.TaskManager.Instance.GetCompletedCount();
                    total = Tasks.TaskManager.Instance.ActiveTasks.Count;
                }

                var mode = RoundManager.Instance.GetConfig()?.Mode ?? GameMode.Survival;
                if (mode == GameMode.Survival)
                {
                    TriggerGameOver(true, $"Time survived! Rescue has arrived. Tasks completed: {completed}/{total}.");
                }
                else
                {
                    TriggerGameOver(false, $"Time ran out before quota was met. Tasks completed: {completed}/{total}.");
                }
            }
        }

        // =================================================================
        //  EVENT HANDLERS
        // =================================================================

        private void OnCriticalTaskFailed(CriticalTaskFailedEvent evt)
        {
            if (!IsServer || _gameOverTriggered) return;
            TriggerGameOver(false, $"Critical task failed: {evt.TaskId}. Mission lost.");
        }

        private void OnAllTasksCompleted(AllTasksCompletedEvent evt)
        {
            if (!IsServer || _gameOverTriggered) return;

            var mode = RoundManager.Instance.GetConfig()?.Mode ?? GameMode.Survival;
            int completed = Tasks.TaskManager.Instance != null
                ? Tasks.TaskManager.Instance.GetCompletedCount() : 0;

            Debug.Log($"[WinLoseEvaluator] OnAllTasksCompleted triggered. Mode: {mode}, Completed: {completed}");

            if (mode == GameMode.Tasks)
            {
                TriggerGameOver(true, $"All {completed} tasks completed! Ship stabilized.");
            }
            else
            {
                Debug.Log($"[WinLoseEvaluator] All {completed} tasks completed, but playing in Survival mode. Keep surviving!");
            }
        }

        // =================================================================
        //  GAME OVER
        // =================================================================

        private void TriggerGameOver(bool won, string reason)
        {
            _gameOverTriggered = true;
            RoundManager.Instance.EndRound();

            if (won)
            {
                SpaceMaintenance.Core.GlobalMissionParameters.MissionsCompleted++;
            }

            int payout = CalculatePayout();
            if (EconomyManager.Instance != null && payout > 0)
            {
                EconomyManager.Instance.AddFunds(payout);
            }

            Debug.Log($"<color={(won ? "green" : "red")}>{(won ? "VICTORY!" : "DEFEAT!")} {reason} Payout: ${payout}</color>");

            EventBus.Publish(new GameOverEvent
            {
                IsVictory = won,
                Reason = reason
            });
        }

        private int CalculatePayout()
        {
            if (Tasks.TaskManager.Instance == null) return 0;

            int totalPayout = 0;
            var tasks = Tasks.TaskManager.Instance.ActiveTasks;

            foreach (var task in tasks)
            {
                if (task.Status == TaskStatus.Completed)
                {
                    totalPayout += task.Priority switch
                    {
                        TaskPriority.Critical => 500,
                        TaskPriority.High     => 300,
                        TaskPriority.Medium   => 150,
                        TaskPriority.Low      => 50,
                        _                     => 0
                    };
                }
            }

            return totalPayout;
        }
    }
}
