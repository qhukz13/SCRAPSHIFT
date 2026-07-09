// ============================================================================
// Space Maintenance — IPowered.cs
// Core interface for objects that consume power.
// ============================================================================

namespace SpaceMaintenance.Core
{
    /// <summary>
    /// Implement on any ship system that requires power to function:
    /// doors, lights, ventilation, pumps, etc.
    /// PowerManager queries all IPowered objects to calculate total demand.
    /// </summary>
    public interface IPowered
    {
        /// <summary>How much power this system consumes per tick.</summary>
        float PowerConsumption { get; }

        /// <summary>Priority for power distribution (lower = more important).</summary>
        int PowerPriority { get; }

        /// <summary>Whether this system currently has power.</summary>
        bool IsPowered { get; }

        /// <summary>Called by PowerManager when power availability changes.</summary>
        void OnPowerStateChanged(bool hasPower);
    }
}
