using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralGeneration
{
    [Serializable]
    public class RoomEntry
    {
        public RoomDefinition Prefab;
        public RoomType RoomType;
        public RoomCategory RoomCategory;
        public RoomTags RoomTags;

        public float DifficultyWeight = 1.0f;
        public float SpawnWeight = 1.0f;
        public List<int> AllowedFloors = new List<int> { 1, 2 };

        public float MinimumDifficulty = 0f;
        public float MaximumDifficulty = 100f;

        [Tooltip("Leave empty to allow any connection")]
        public List<RoomType> ConnectionRules = new List<RoomType>();
    }

    [CreateAssetMenu(fileName = "NewRoomDatabase", menuName = "Scrapshift/Procedural Generation/Room Database")]
    public class RoomDatabase : ScriptableObject
    {
        public List<RoomEntry> Rooms = new List<RoomEntry>();
    }
}
