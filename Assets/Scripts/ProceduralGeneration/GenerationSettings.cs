using System;
using UnityEngine;

namespace ProceduralGeneration
{
    [Serializable]
    public class GenerationSettings
    {
        public int Seed = 0;
        public bool UseRandomSeed = true;
        
        [Header("Limits")]
        public int MaxRetriesPerRoom = 10;
        public int MaxGraphGenerationRetries = 50;
        
        [Header("Physical Spacing")]
        public float RoomPadding = 0.1f;
        public float FloorHeight = 10f;
    }
}
