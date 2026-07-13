// ============================================================================
// SCRAPSHIFT — IMinigameRepairable.cs
// Extended repair interface for systems that use interactive minigames
// instead of the basic hold-to-repair mechanic.
// ============================================================================

namespace SpaceMaintenance.Core
{
    /// <summary>
    /// Implement on any system that uses a minigame for repair instead of
    /// the basic hold-to-fix IRepairable flow. The MinigameManager opens
    /// the appropriate minigame UI when the player interacts.
    /// </summary>
    public interface IMinigameRepairable : IRepairable
    {
        /// <summary>Which minigame type to launch for this system.</summary>
        MinigameType MinigameType { get; }

        /// <summary>Difficulty tier (1 = easy, 3+ = hard). Scales with progression.</summary>
        int MinigameDifficulty { get; }

        /// <summary>Called when the player successfully completes the minigame.</summary>
        void OnMinigameCompleted();

        /// <summary>Called when the player fails or cancels the minigame.</summary>
        void OnMinigameFailed();
    }
}
