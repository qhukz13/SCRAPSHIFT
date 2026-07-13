using UnityEngine;

namespace SpaceMaintenance.Core.Data
{
    public struct PlayerSpawnedEvent 
    {
        public GameObject Player;
    }

    public struct DamageTakenEvent
    {
        public GameObject Target;
        public float Amount;
    }

    public struct SystemRepairedEvent
    {
        public string SystemName;
    }

    public struct ChaosEventTriggered
    {
        public string EventName;
    }

    /// <summary>Fired by WinLoseEvaluator when the game ends.</summary>
    public struct GameOverEvent
    {
        public bool IsVictory;
        public string Reason;
    }

    /// <summary>Fired by RoundManager every frame so the HUD can update the timer.</summary>
    public struct MissionTimerUpdatedEvent
    {
        public float TimeRemaining;
        public float TotalTime;
    }

    /// <summary>Fired by DamageManager when hull integrity changes.</summary>
    public struct HullIntegrityUpdatedEvent
    {
        public float Current;
        public float Max;
    }

    /// <summary>Fired by MissionManager when task progress changes.</summary>
    public struct TaskProgressUpdatedEvent
    {
        public int Completed;
        public int Required;
    }

    // =========================================================================
    // New events for full ship-system mechanics
    // =========================================================================

    /// <summary>Fired by ReactorController when reactor transitions between states.</summary>
    public struct ReactorStateChangedEvent
    {
        public ReactorState OldState;
        public ReactorState NewState;
        public float HeatLevel;
    }

    /// <summary>Fired by DoorController when a door changes state.</summary>
    public struct DoorStateChangedEvent
    {
        public string DoorId;
        public DoorState OldState;
        public DoorState NewState;
    }

    /// <summary>Fired by PowerManager when power supply/demand changes.</summary>
    public struct PowerStateChangedEvent
    {
        public float CurrentPower;
        public float MaxPower;
        public float Demand;
    }

    // =========================================================================
    // Dark Ship / Mission Phase events
    // =========================================================================

    /// <summary>Fired by MissionFlowController when the mission transitions between phases.</summary>
    public struct MissionPhaseChangedEvent
    {
        public MissionPhase OldPhase;
        public MissionPhase NewPhase;
    }

    // =========================================================================
    // Task System events
    // =========================================================================

    /// <summary>Fired by TaskManager when a new task is created.</summary>
    public struct TaskCreatedEvent
    {
        public string TaskId;
        public string DisplayName;
        public TaskPriority Priority;
        public float TimeLimit; // 0 = no limit
    }

    /// <summary>Fired by TaskManager when a task's status changes.</summary>
    public struct TaskStatusChangedEvent
    {
        public string TaskId;
        public TaskStatus OldStatus;
        public TaskStatus NewStatus;
    }

    /// <summary>Fired by TaskManager when every required task has been completed.</summary>
    public struct AllTasksCompletedEvent { }

    /// <summary>Fired by TaskManager when a Critical task's timer expires.</summary>
    public struct CriticalTaskFailedEvent
    {
        public string TaskId;
    }

    // =========================================================================
    // Minigame events
    // =========================================================================

    /// <summary>Fired when a minigame UI opens for a repairable system.</summary>
    public struct MinigameStartedEvent
    {
        public string SystemName;
        public GameObject Target;
    }

    /// <summary>Fired when a minigame finishes (success or failure).</summary>
    public struct MinigameCompletedEvent
    {
        public string SystemName;
        public bool Success;
    }
}
