using System.Collections.Generic;
using UnityEngine;

namespace ProceduralGeneration
{
    public class RoomDefinition : MonoBehaviour
    {
        [Header("Room Identity")]
        public RoomType RoomType;
        public RoomCategory RoomCategory;
        public RoomTags RoomTags;

        [Header("Dimensions")]
        public Vector3Int RoomSize = new Vector3Int(1, 1, 1);
        public Bounds RoomBounds = new Bounds(Vector3.zero, Vector3.one);
        public int Floor = 1;

        [Header("Connections")]
        public List<DoorSocket> DoorSockets = new List<DoorSocket>();
        public List<RoomType> AllowedConnections = new List<RoomType>();

        [Header("Points of Interest")]
        public List<Transform> SpawnPoints = new List<Transform>();
        public List<Transform> RepairPoints = new List<Transform>();
        public List<Transform> LootPoints = new List<Transform>();
        public List<Transform> DecorationPoints = new List<Transform>();
        public List<Transform> LightPoints = new List<Transform>();
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position + RoomBounds.center, RoomBounds.size);
            
            foreach (var socket in DoorSockets)
            {
                Gizmos.color = Color.green;
                Vector3 socketPos = transform.position + socket.LocalPosition;
                Gizmos.DrawSphere(socketPos, 0.5f);
                Gizmos.DrawRay(socketPos, socket.LocalDirection * 2f);
            }
        }
    }
}
