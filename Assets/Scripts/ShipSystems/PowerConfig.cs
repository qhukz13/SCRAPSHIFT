using UnityEngine;

namespace SpaceMaintenance.ShipSystems
{
    [CreateAssetMenu(fileName = "PowerConfig", menuName = "SpaceMaintenance/Ship/Power Config")]
    public class PowerConfig : ScriptableObject
    {
        public float MaxReactorPower = 1000f;
        public float DoorPowerConsumption = 10f;
        public float GeneratorPowerOutput = 200f;
    }
}
