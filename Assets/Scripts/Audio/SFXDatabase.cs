using UnityEngine;

namespace SpaceMaintenance.Audio
{
    [CreateAssetMenu(fileName = "SFXDatabase", menuName = "SpaceMaintenance/Audio/SFX Database")]
    public class SFXDatabase : ScriptableObject
    {
        [Header("Doors")]
        public AudioClip DoorOpen;
        public AudioClip DoorClose;
        public AudioClip DoorLocked;
        public AudioClip DoorBroken;

        [Header("Reactor")]
        public AudioClip ReactorHum;
        public AudioClip ReactorAlarm;
        public AudioClip ReactorScram;
        
        [Header("Generators")]
        public AudioClip GeneratorFix;
        public AudioClip GeneratorBreak;
        
        [Header("Player")]
        public AudioClip[] Footsteps;
        public AudioClip PickupItem;
        public AudioClip DropItem;
        
        [Header("Global Alarms")]
        public AudioClip GlobalWarning;
        public AudioClip GlobalCritical;
    }
}
