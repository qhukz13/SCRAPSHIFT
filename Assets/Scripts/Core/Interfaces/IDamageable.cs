// ============================================================================
// Space Maintenance — IDamageable.cs
// Core interface for objects that can take damage.
// ============================================================================

namespace SpaceMaintenance.Core
{
    /// <summary>
    /// Implement on any object that can be damaged and restored.
    /// Used by DamageManager, ChaosManager, and RepairSystem.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>Current health points.</summary>
        float Health { get; }

        /// <summary>Maximum health points.</summary>
        float MaxHealth { get; }

        /// <summary>Normalized health (0..1).</summary>
        float HealthNormalized { get; }

        /// <summary>True when Health &lt;= 0.</summary>
        bool IsBroken { get; }

        /// <summary>Apply damage. Triggers cascade events when broken.</summary>
        void TakeDamage(float amount, DamageType damageType);

        /// <summary>Fully restore health and re-enable the system.</summary>
        void RestoreHealth(float amount);
    }
}
