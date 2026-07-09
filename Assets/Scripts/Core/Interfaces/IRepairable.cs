// ============================================================================
// Space Maintenance — IRepairable.cs
// Core interface for objects that can be repaired by players.
// ============================================================================

using UnityEngine;

namespace SpaceMaintenance.Core
{
    /// <summary>
    /// Implement on any system that can be repaired:
    /// reactors, generators, doors, pipes, etc.
    /// Works with RepairController for hold-to-repair mechanic.
    /// </summary>
    public interface IRepairable
    {
        /// <summary>Time in seconds to fully repair this object.</summary>
        float RepairTime { get; }

        /// <summary>Current repair progress (0..1).</summary>
        float RepairProgress { get; }

        /// <summary>True while a player is actively repairing.</summary>
        bool IsBeingRepaired { get; }

        /// <summary>True if this object needs repair.</summary>
        bool NeedsRepair { get; }

        /// <summary>Begin repair process. Called when player starts holding.</summary>
        void StartRepair(GameObject repairer);

        /// <summary>Update repair progress. Called each frame during hold.</summary>
        void UpdateRepair(float deltaTime);

        /// <summary>Cancel repair. Called when player releases early or moves away.</summary>
        void CancelRepair();

        /// <summary>Complete the repair. Called when progress reaches 1.0.</summary>
        void CompleteRepair();
    }
}
