using UnityEngine;
using UnityEditor;
using SpaceMaintenance.LevelGeneration;

namespace SpaceMaintenance.EditorTools
{
    public class ShipLevelGeneratorWindow : EditorWindow
    {
        private ShipLayoutConfig _config;

        [MenuItem("SpaceMaintenance/Level Generator")]
        public static void ShowWindow()
        {
            GetWindow<ShipLevelGeneratorWindow>("Level Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("Greybox Level Generator", EditorStyles.boldLabel);

            _config = (ShipLayoutConfig)EditorGUILayout.ObjectField("Layout Config", _config, typeof(ShipLayoutConfig), false);

            if (GUILayout.Button("Generate Ship Layout"))
            {
                if (_config != null)
                {
                    GenerateLevel();
                }
                else
                {
                    Debug.LogWarning("Please assign a ShipLayoutConfig first.");
                }
            }
        }

        private void GenerateLevel()
        {
            // Create root object
            GameObject shipRoot = new GameObject("ShipLayout_Generated");
            Undo.RegisterCreatedObjectUndo(shipRoot, "Generate Ship Layout");
            
            // Generate Hub at origin
            GameObject hub = CreateRoom(shipRoot.transform, "Hub (Empty)", Vector3.zero, _config.HubSize);
            
            // Distance from hub center to room centers
            float corridorLen = 10f;
            
            // 1. North - Control Room
            Vector3 controlRoomPos = new Vector3(0, 0, _config.HubSize.y / 2f + corridorLen + _config.ControlRoomSize.y / 2f);
            CreateRoom(shipRoot.transform, "Control Room", controlRoomPos, _config.ControlRoomSize);
            CreateCorridor(shipRoot.transform, "Corridor_North", new Vector3(0, 0, _config.HubSize.y / 2f), new Vector3(0, 0, controlRoomPos.z - _config.ControlRoomSize.y / 2f));
            
            // 2. South - Reactor Room
            Vector3 reactorPos = new Vector3(0, 0, -(_config.HubSize.y / 2f + corridorLen + _config.ReactorRoomSize.y / 2f));
            CreateRoom(shipRoot.transform, "Reactor Room", reactorPos, _config.ReactorRoomSize);
            CreateCorridor(shipRoot.transform, "Corridor_South", new Vector3(0, 0, -_config.HubSize.y / 2f), new Vector3(0, 0, reactorPos.z + _config.ReactorRoomSize.y / 2f));
            
            // 3. East - Generator Room
            Vector3 generatorPos = new Vector3(_config.HubSize.x / 2f + corridorLen + _config.GeneratorRoomSize.x / 2f, 0, 0);
            CreateRoom(shipRoot.transform, "Generator Room", generatorPos, _config.GeneratorRoomSize);
            CreateCorridor(shipRoot.transform, "Corridor_East", new Vector3(_config.HubSize.x / 2f, 0, 0), new Vector3(generatorPos.x - _config.GeneratorRoomSize.x / 2f, 0, 0));
            
            // 4. West - Crew Quarters (Spawn Room)
            Vector3 crewPos = new Vector3(-(_config.HubSize.x / 2f + corridorLen + _config.CrewQuartersSize.x / 2f), 0, 0);
            CreateRoom(shipRoot.transform, "Crew Quarters", crewPos, _config.CrewQuartersSize);
            CreateCorridor(shipRoot.transform, "Corridor_West", new Vector3(-_config.HubSize.x / 2f, 0, 0), new Vector3(crewPos.x + _config.CrewQuartersSize.x / 2f, 0, 0));
            
            // 5. North-East - Storage Room
            Vector3 storagePos = controlRoomPos + new Vector3(_config.ControlRoomSize.x / 2f + corridorLen + _config.StorageRoomSize.x / 2f, 0, 0);
            CreateRoom(shipRoot.transform, "Storage", storagePos, _config.StorageRoomSize);
            CreateCorridor(shipRoot.transform, "Corridor_Storage", controlRoomPos + new Vector3(_config.ControlRoomSize.x / 2f, 0, 0), new Vector3(storagePos.x - _config.StorageRoomSize.x / 2f, 0, controlRoomPos.z));

            AssignSpawnPoints(shipRoot);

            Selection.activeGameObject = shipRoot;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }

            Debug.Log("Greybox layout generated successfully.");
        }

        private void AssignSpawnPoints(GameObject shipRoot)
        {
            var spawner = FindFirstObjectByType<SpaceMaintenance.Networking.PlayerSpawner>();
            if (spawner != null)
            {
                var beds = new System.Collections.Generic.List<Transform>();
                var crewQuarters = shipRoot.transform.Find("Crew Quarters");
                if (crewQuarters != null)
                {
                    foreach (Transform child in crewQuarters)
                    {
                        if (child.name.Contains("Bed") || child.name.Contains("Clone"))
                        {
                            beds.Add(child);
                        }
                    }
                }
                
                if (beds.Count > 0)
                {
                    var serializedObject = new SerializedObject(spawner);
                    var spawnPointsProp = serializedObject.FindProperty("_spawnPoints");
                    spawnPointsProp.ClearArray();
                    for (int i = 0; i < beds.Count; i++)
                    {
                        spawnPointsProp.InsertArrayElementAtIndex(i);
                        spawnPointsProp.GetArrayElementAtIndex(i).objectReferenceValue = beds[i];
                    }
                    serializedObject.ApplyModifiedProperties();
                    Debug.Log($"Assigned {beds.Count} spawn points to PlayerSpawner.");
                }
            }
        }

        private GameObject CreateRoom(Transform parent, string name, Vector3 center, Vector2 size)
        {
            GameObject room = new GameObject(name);
            room.transform.SetParent(parent);
            room.transform.position = center;
            
            // Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(room.transform);
            floor.transform.localPosition = new Vector3(0, -0.5f, 0); // 1 unit thick, top at y=0
            floor.transform.localScale = new Vector3(size.x, 1f, size.y);
            if (_config.FloorMaterial) floor.GetComponent<MeshRenderer>().material = _config.FloorMaterial;
            
            // Ceiling
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(room.transform);
            ceiling.transform.localPosition = new Vector3(0, _config.WallHeight + 0.5f, 0);
            ceiling.transform.localScale = new Vector3(size.x, 1f, size.y);
            if (_config.WallMaterial) ceiling.GetComponent<MeshRenderer>().material = _config.WallMaterial;
            
            // Walls (North, South, East, West) - simple box walls without doorway cutouts for now
            // To make doorways, we'd need more complex generation, but for a quick greybox, we can just leave gaps or use multiple wall segments.
            // For simplicity, we'll build 4 walls, and user can manually delete segments or we can leave small gaps.
            // Actually, let's just make the walls a bit shorter so they don't cover the corners completely, or better:
            // Just build the walls and we can manually edit them.
            
            CreateWall(room.transform, "Wall_North", new Vector3(0, _config.WallHeight / 2f, size.y / 2f), new Vector3(size.x, _config.WallHeight, _config.WallThickness));
            CreateWall(room.transform, "Wall_South", new Vector3(0, _config.WallHeight / 2f, -size.y / 2f), new Vector3(size.x, _config.WallHeight, _config.WallThickness));
            CreateWall(room.transform, "Wall_East", new Vector3(size.x / 2f, _config.WallHeight / 2f, 0), new Vector3(_config.WallThickness, _config.WallHeight, size.y));
            CreateWall(room.transform, "Wall_West", new Vector3(-size.x / 2f, _config.WallHeight / 2f, 0), new Vector3(_config.WallThickness, _config.WallHeight, size.y));

            // Populate specific rooms
            if (name == "Crew Quarters" && _config.BedPrefab != null)
            {
                for(int i=0; i<4; i++) // 4 beds
                {
                    var bed = (GameObject)PrefabUtility.InstantiatePrefab(_config.BedPrefab, room.transform);
                    bed.transform.localPosition = new Vector3(-size.x/2f + 2f, 0f, -size.y/2f + 2f + (i * 2.5f));
                }
            }
            else if (name == "Reactor Room")
            {
                // Placeholder for reactor
                GameObject reactor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                reactor.name = "Reactor_Placeholder";
                reactor.transform.SetParent(room.transform);
                reactor.transform.localPosition = new Vector3(0, 2f, 0);
                reactor.transform.localScale = new Vector3(4f, 2f, 4f);
                if (_config.HighlightMaterial) reactor.GetComponent<MeshRenderer>().material = _config.HighlightMaterial;
            }

            return room;
        }

        private void CreateWall(Transform parent, string name, Vector3 localPos, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.localPosition = localPos;
            wall.transform.localScale = scale;
            if (_config.WallMaterial) wall.GetComponent<MeshRenderer>().material = _config.WallMaterial;
        }

        private void CreateCorridor(Transform parent, string name, Vector3 start, Vector3 end)
        {
            GameObject corridor = new GameObject(name);
            corridor.transform.SetParent(parent);
            
            Vector3 center = (start + end) / 2f;
            corridor.transform.position = center;
            
            float length = Vector3.Distance(start, end);
            bool isZ = Mathf.Abs(start.z - end.z) > Mathf.Abs(start.x - end.x);
            
            Vector3 floorScale = isZ ? new Vector3(_config.CorridorWidth, 1f, length) : new Vector3(length, 1f, _config.CorridorWidth);
            
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(corridor.transform);
            floor.transform.localPosition = new Vector3(0, -0.5f, 0);
            floor.transform.localScale = floorScale;
            if (_config.FloorMaterial) floor.GetComponent<MeshRenderer>().material = _config.FloorMaterial;
            
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(corridor.transform);
            ceiling.transform.localPosition = new Vector3(0, _config.WallHeight + 0.5f, 0);
            ceiling.transform.localScale = floorScale;
            if (_config.WallMaterial) ceiling.GetComponent<MeshRenderer>().material = _config.WallMaterial;
            
            // Side walls for corridor
            if (isZ)
            {
                CreateWall(corridor.transform, "Wall_Left", new Vector3(-_config.CorridorWidth / 2f, _config.WallHeight / 2f, 0), new Vector3(_config.WallThickness, _config.WallHeight, length));
                CreateWall(corridor.transform, "Wall_Right", new Vector3(_config.CorridorWidth / 2f, _config.WallHeight / 2f, 0), new Vector3(_config.WallThickness, _config.WallHeight, length));
                
                // Add Door at start and end
                if (_config.DoorPrefab != null)
                {
                    var door1 = (GameObject)PrefabUtility.InstantiatePrefab(_config.DoorPrefab, corridor.transform);
                    door1.transform.position = start;
                    var door2 = (GameObject)PrefabUtility.InstantiatePrefab(_config.DoorPrefab, corridor.transform);
                    door2.transform.position = end;
                }
            }
            else
            {
                CreateWall(corridor.transform, "Wall_Top", new Vector3(0, _config.WallHeight / 2f, _config.CorridorWidth / 2f), new Vector3(length, _config.WallHeight, _config.WallThickness));
                CreateWall(corridor.transform, "Wall_Bottom", new Vector3(0, _config.WallHeight / 2f, -_config.CorridorWidth / 2f), new Vector3(length, _config.WallHeight, _config.WallThickness));
                
                if (_config.DoorPrefab != null)
                {
                    var door1 = (GameObject)PrefabUtility.InstantiatePrefab(_config.DoorPrefab, corridor.transform);
                    door1.transform.position = start;
                    door1.transform.rotation = Quaternion.Euler(0, 90, 0);
                    var door2 = (GameObject)PrefabUtility.InstantiatePrefab(_config.DoorPrefab, corridor.transform);
                    door2.transform.position = end;
                    door2.transform.rotation = Quaternion.Euler(0, 90, 0);
                }
            }
        }
    }
}
