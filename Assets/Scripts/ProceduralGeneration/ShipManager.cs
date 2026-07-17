using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace ProceduralGeneration
{
    /// <summary>
    /// Global API for accessing the generated ship's runtime state.
    /// </summary>
    public class ShipManager : NetworkBehaviour
    {
        public static ShipManager Instance { get; private set; }

        private List<RoomInstance> allRooms = new List<RoomInstance>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void InitializeShip(List<RoomInstance> rooms)
        {
            allRooms = new List<RoomInstance>(rooms);
            Debug.Log($"[ShipManager] Ship API initialized with {allRooms.Count} rooms.");
        }

        public List<RoomInstance> GetAllRooms()
        {
            return new List<RoomInstance>(allRooms);
        }

        public List<RoomInstance> GetRoomsByTag(RoomTags tag)
        {
            return allRooms.Where(r => r.HasTag(tag)).ToList();
        }

        public RoomInstance GetRoomAtPosition(Vector3 position)
        {
            foreach (var room in allRooms)
            {
                // Localize position to the room's transform space
                Vector3 localPos = room.transform.InverseTransformPoint(position);
                if (room.Definition.RoomBounds.Contains(localPos))
                {
                    return room;
                }
            }
            return null;
        }

        public RoomInstance GetRandomRoom()
        {
            if (allRooms.Count == 0) return null;
            return allRooms[UnityEngine.Random.Range(0, allRooms.Count)];
        }
    }
}
