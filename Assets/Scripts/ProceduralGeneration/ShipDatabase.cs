using System.Collections.Generic;
using UnityEngine;

namespace ProceduralGeneration
{
    [CreateAssetMenu(fileName = "NewShipDatabase", menuName = "Scrapshift/Procedural Generation/Ship Database")]
    public class ShipDatabase : ScriptableObject
    {
        public List<ShipTemplate> Ships = new List<ShipTemplate>();
    }
}
