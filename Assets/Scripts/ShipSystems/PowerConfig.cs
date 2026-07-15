using UnityEngine;

namespace SpaceMaintenance.ShipSystems
{
    [CreateAssetMenu(fileName = "PowerConfig", menuName = "SpaceMaintenance/Ship/Power Config")]
    public class PowerConfig : ScriptableObject
    {
        [Header("Power Grid")]
        public float MaxReactorPower = 1000f;
        public float GeneratorPowerOutput = 200f;

        [Header("Reactor — Heat")]
        [Tooltip("Heat gain per second during normal operation.")]
        public float ReactorHeatRate = 0.002f;

        [Tooltip("Heat percentage (0–1) that triggers Overheating state.")]
        [Range(0.3f, 0.7f)]
        public float ReactorHeatWarningThreshold = 0.5f;

        [Tooltip("Heat percentage (0–1) that triggers Critical state.")]
        [Range(0.6f, 0.95f)]
        public float ReactorHeatCriticalThreshold = 0.8f;

        [Tooltip("Heat reduction per second while being cooled / repaired.")]
        public float ReactorCooldownRate = 0.2f;

        [Tooltip("Instant heat added by a Chaos surge event.")]
        public float ReactorSurgePenalty = 0.3f;

        [Header("Reactor — SCRAM")]
        [Tooltip("Seconds the reactor stays offline after an emergency SCRAM.")]
        public float ReactorScramCooldownTime = 10f;

        [Tooltip("Seconds for the startup sequence (Offline → Running).")]
        public float ReactorStartupTime = 3f;

        [Header("Reactor — Damage")]
        [Tooltip("Hull damage applied on meltdown.")]
        public float ReactorMeltdownDamage = 100f;

        [Header("Doors")]
        public float DoorPowerConsumption = 10f;

        [Tooltip("Seconds to force-open a door without power.")]
        public float DoorForceOpenTime = 3f;

        [Tooltip("Seconds to repair (unjam) a broken door.")]
        public float DoorUnjamRepairTime = 5f;

        [Tooltip("Seconds to bypass a locked door without a key.")]
        public float DoorLockBypassTime = 8f;

        [Tooltip("If true, closed doors auto-open when the ship loses all power (fire safety).")]
        public bool DoorsOpenOnPowerLoss = true;
    }
}
