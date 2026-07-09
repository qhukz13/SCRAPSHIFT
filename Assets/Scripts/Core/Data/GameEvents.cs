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
}
