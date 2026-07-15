using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace ProceduralGeneration.Editor
{
    public class MockPrefabGenerator : MonoBehaviour
    {
#if UNITY_EDITOR
        [MenuItem("Scrapshift/Generate Mock Prefabs")]
        public static void GenerateMocks()
        {
            string folder = "Assets/Resources/MockRooms";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "MockRooms");
            }

            RoomDatabase db = ScriptableObject.CreateInstance<RoomDatabase>();
            
            RoomType[] types = { RoomType.Spawn, RoomType.Corridor, RoomType.Crossroad, RoomType.Reactor, RoomType.Bridge, RoomType.Storage };
            
            foreach (var t in types)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"Mock_{t}";
                
                RoomDefinition def = go.AddComponent<RoomDefinition>();
                def.RoomType = t;
                def.RoomBounds = new Bounds(Vector3.zero, new Vector3(10, 10, 10));
                
                // Add two opposing sockets for connections
                def.DoorSockets = new List<DoorSocket>
                {
                    new DoorSocket { LocalPosition = new Vector3(0, 0, 5), LocalDirection = Vector3.forward },
                    new DoorSocket { LocalPosition = new Vector3(0, 0, -5), LocalDirection = Vector3.back },
                    new DoorSocket { LocalPosition = new Vector3(5, 0, 0), LocalDirection = Vector3.right },
                    new DoorSocket { LocalPosition = new Vector3(-5, 0, 0), LocalDirection = Vector3.left }
                };

                // Remove collider to avoid physics issues during generation setup if using Math bounds
                DestroyImmediate(go.GetComponent<Collider>());

                string path = $"{folder}/{go.name}.prefab";
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
                DestroyImmediate(go);

                RoomEntry entry = new RoomEntry
                {
                    RoomType = t,
                    Prefab = prefab.GetComponent<RoomDefinition>()
                };
                
                db.Rooms.Add(entry);
            }

            AssetDatabase.CreateAsset(db, $"{folder}/MockRoomDatabase.asset");
            
            ShipTemplate template = ScriptableObject.CreateInstance<ShipTemplate>();
            template.ShipName = "Mock Cargo Ship";
            template.MinimumRooms = 5;
            template.MaximumRooms = 15;
            template.RequiredRooms = new List<RoomType> { RoomType.Spawn, RoomType.Reactor, RoomType.Bridge };
            template.OptionalRooms = new List<RoomSpawnRule> { 
                new RoomSpawnRule { RoomType = RoomType.Storage, MinCount = 1, MaxCount = 3 } 
            };
            
            AssetDatabase.CreateAsset(template, $"{folder}/MockShipTemplate.asset");
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("Mock prefabs and database created in Assets/Resources/MockRooms/");
        }
#endif
    }
}
