// ============================================================================
// Space Maintenance — GameEnums.cs
// Shared enumerations used across multiple systems.
// Centralized here to avoid circular dependencies.
// ============================================================================

namespace SpaceMaintenance.Core
{
    /// <summary>Types of damage that can be applied to IDamageable objects.</summary>
    public enum DamageType
    {
        Mechanical,     // Physical impact, wear and tear
        Electrical,     // Short circuit, power surge
        Fire,           // Burn damage from active fires
        Overload,       // System overload (reactor, generator)
        Impact          // Collision with physics objects
    }

    /// <summary>States of the ship's reactor.</summary>
    public enum ReactorState
    {
        Offline,        // Not running, no power generated
        Starting,       // Boot-up sequence
        Running,        // Normal operation
        Overheating,    // Temperature rising, needs attention
        Critical,       // About to meltdown, emergency
        Meltdown        // Game-ending catastrophe
    }

    /// <summary>States of a power generator.</summary>
    public enum GeneratorState
    {
        Offline,        // Not running
        Running,        // Generating power normally
        Damaged,        // Running at reduced capacity
        Broken          // Needs repair
    }

    /// <summary>States of a ship door.</summary>
    public enum DoorState
    {
        Open,           // Door is open, passage allowed
        Closed,         // Door is closed but can be opened
        Locked,         // Locked — requires power or key
        Broken          // Stuck, needs repair
    }

    /// <summary>Overall mission state managed by RoundManager.</summary>
    public enum MissionState
    {
        Briefing,       // Pre-mission, showing objectives
        Deploying,      // Players spawning onto the station
        InProgress,     // Active gameplay
        Extracting,     // Players returning to ship
        Completed,      // All objectives met, mission success
        Failed          // Timer expired, reactor meltdown, etc.
    }

    /// <summary>Types of ship systems that can be damaged.</summary>
    public enum ShipSystemType
    {
        Reactor,
        Generator,
        Door,
        Ventilation,
        Pump,
        Lighting,
        LifeSupport
    }

    /// <summary>Severity levels for chaos events.</summary>
    public enum ChaosSeverity
    {
        Minor,          // Inconvenience — flickering lights
        Moderate,       // Requires attention — door malfunction
        Major,          // Urgent — generator failure
        Critical        // Emergency — reactor overload
    }

    /// <summary>Types of items in the game.</summary>
    public enum ItemType
    {
        Tool,           // Wrench, welding torch, etc.
        Part,           // Replacement component
        Consumable,     // Fire extinguisher, battery, etc.
        Cargo,          // Heavy delivery item
        Key             // Access card, key
    }

    public enum GameMode
    {
        Survival,
        Tasks
    }

    /// <summary>Player state for the state machine.</summary>
    public enum PlayerStateType
    {
        Idle,
        Moving,
        Sprinting,
        Crouching,
        Jumping,
        Falling,
        Carrying,
        Interacting,
        Repairing,
        Dead
    }
}
