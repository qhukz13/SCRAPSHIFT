// ============================================================================
// SCRAPSHIFT — MinigameBase.cs
// Abstract base class for all repair minigames. Provides lifecycle management
// (start, cancel, complete) and an OnCompleted event for the MinigameManager.
// Concrete minigames inherit this and implement their own UI/logic.
// ============================================================================

using SpaceMaintenance.Core;
using SpaceMaintenance.Core.Data;
using UnityEngine;

namespace SpaceMaintenance.Minigames
{
    /// <summary>
    /// Base class for interactive repair minigames. Each minigame is a
    /// MonoBehaviour attached to a UI panel under the MinigameCanvas.
    /// </summary>
    public abstract class MinigameBase : MonoBehaviour
    {
        /// <summary>Fired when the minigame ends. Bool = success.</summary>
        public event System.Action<bool> OnCompleted;

        // ─── Properties ─────────────────────────────────────────────────
        public MinigameType Type { get; protected set; }
        public int Difficulty { get; protected set; }
        public bool IsActive { get; protected set; }

        /// <summary>Maximum time allowed to complete the minigame (0 = unlimited).</summary>
        [SerializeField] protected float _maxTime = 15f;

        protected float _elapsedTime;

        // =================================================================
        //  PUBLIC API
        // =================================================================

        /// <summary>Start the minigame at the given difficulty.</summary>
        public virtual void StartMinigame(int difficulty)
        {
            Difficulty = difficulty;
            IsActive = true;
            _elapsedTime = 0f;

            gameObject.SetActive(true);
            OnStart();

            Debug.Log($"[Minigame] {Type} started at difficulty {difficulty}.");
        }

        /// <summary>Cancel the minigame without completing it.</summary>
        public virtual void CancelMinigame()
        {
            if (!IsActive) return;
            IsActive = false;
            OnCancel();
            gameObject.SetActive(false);

            Debug.Log($"[Minigame] {Type} cancelled.");
        }

        // =================================================================
        //  LIFECYCLE (override in subclasses)
        // =================================================================

        /// <summary>Called when the minigame starts. Set up UI, randomize puzzle, etc.</summary>
        protected abstract void OnStart();

        /// <summary>Called when the minigame is cancelled.</summary>
        protected virtual void OnCancel() { }

        /// <summary>Called every frame while the minigame is active.</summary>
        protected virtual void OnTick(float deltaTime) { }

        // =================================================================
        //  COMPLETION
        // =================================================================

        /// <summary>End the minigame with a result.</summary>
        protected void Complete(bool success)
        {
            if (!IsActive) return;

            IsActive = false;
            OnCompleted?.Invoke(success);

            EventBus.Publish(new MinigameCompletedEvent
            {
                SystemName = gameObject.name,
                Success = success
            });

            Debug.Log($"[Minigame] {Type} completed — {(success ? "SUCCESS" : "FAILED")}");
            gameObject.SetActive(false);
        }

        // =================================================================
        //  UNITY UPDATE
        // =================================================================

        protected virtual void Update()
        {
            if (!IsActive) return;

            _elapsedTime += Time.unscaledDeltaTime;

            // Time limit check
            if (_maxTime > 0f && _elapsedTime >= _maxTime)
            {
                Complete(false);
                return;
            }

            OnTick(Time.unscaledDeltaTime);
        }
    }
}
