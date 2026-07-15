using System.Collections.Generic;
using UnityEngine;

namespace ShipGeneration
{
    public class ShipSpawnPoint : MonoBehaviour
    {
        public static List<Transform> SpawnPoints = new List<Transform>();

        private void OnEnable()
        {
            if (!SpawnPoints.Contains(transform))
            {
                SpawnPoints.Add(transform);
            }
        }

        private void OnDisable()
        {
            if (SpawnPoints.Contains(transform))
            {
                SpawnPoints.Remove(transform);
            }
        }
    }
}
