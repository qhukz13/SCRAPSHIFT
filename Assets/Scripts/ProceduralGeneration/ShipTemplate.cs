using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralGeneration
{
    [Serializable]
    public class RoomSpawnRule
    {
        public RoomType RoomType;
        public int MinCount;
        public int MaxCount;
        public float SpawnWeight;
    }

    [CreateAssetMenu(fileName = "NewShipTemplate", menuName = "Scrapshift/Procedural Generation/Ship Template")]
    public class ShipTemplate : ScriptableObject
    {
        public string ShipName = "New Ship";
        public float Difficulty = 1.0f;
        public int NumberOfFloors = 1;
        
        public int MinimumRooms = 10;
        public int MaximumRooms = 20;

        public List<RoomType> RequiredRooms = new List<RoomType>();
        public List<RoomSpawnRule> OptionalRooms = new List<RoomSpawnRule>();
        
        [Tooltip("Defines global connection rules that apply across the entire ship.")]
        public List<string> GenerationRules = new List<string>();
    }
}
