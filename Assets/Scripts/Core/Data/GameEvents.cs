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
}
