using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace ProceduralGeneration.Editor
{
    public static class RoomVisualsGenerator
    {
        private static Material wallMat;
        private static Material floorMat;
        private static Material reactorMat;
        private static Material generatorMat;
        
        [MenuItem("Tools/Generate Room Visuals")]
        public static void GenerateAll()
        {
            EnsureMaterials();
            
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/ShipGeneration" });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                
                RoomDefinition def = prefab.GetComponent<RoomDefinition>();
                if (def == null) continue;
                
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                RoomDefinition instanceDef = instance.GetComponent<RoomDefinition>();
                
                GenerateVisualsForRoom(instance, instanceDef);
                
                PrefabUtility.SaveAsPrefabAsset(instance, path);
                Object.DestroyImmediate(instance);
                
                Debug.Log($"Generated visuals for {prefab.name}");
            }
        }
        
        private static void EnsureMaterials()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");
                
            wallMat = GetOrCreateMaterial("Assets/Materials/WallMat.mat", new Color(0.3f, 0.3f, 0.35f));
            floorMat = GetOrCreateMaterial("Assets/Materials/FloorMat.mat", new Color(0.2f, 0.2f, 0.2f));
            reactorMat = GetOrCreateMaterial("Assets/Materials/ReactorMat.mat", new Color(0.8f, 0.1f, 0.1f));
            generatorMat = GetOrCreateMaterial("Assets/Materials/GeneratorMat.mat", new Color(0.8f, 0.8f, 0.1f));
        }
        
        private static Material GetOrCreateMaterial(string path, Color color)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        private static void GenerateVisualsForRoom(GameObject room, RoomDefinition def)
        {
            Transform visuals = room.transform.Find("Visuals");
            if (visuals != null)
            {
                Object.DestroyImmediate(visuals.gameObject);
            }
            
            GameObject visualsObj = new GameObject("Visuals");
            visualsObj.transform.SetParent(room.transform);
            visualsObj.transform.localPosition = Vector3.zero;
            visualsObj.transform.localRotation = Quaternion.identity;
            
            float doorWidth = 4f;
            float doorHeight = 4f;
            float wallThickness = 1f;
            float height = def.RoomBounds.size.y;
            Vector3 extents = def.RoomBounds.extents;
            
            GenerateEdge(visualsObj.transform, 0, extents.x, extents.z, height, wallThickness, doorWidth, doorHeight, def.DoorSockets);
            GenerateEdge(visualsObj.transform, 1, extents.x, -extents.z, height, wallThickness, doorWidth, doorHeight, def.DoorSockets);
            GenerateEdge(visualsObj.transform, 2, extents.z, extents.x, height, wallThickness, doorWidth, doorHeight, def.DoorSockets);
            GenerateEdge(visualsObj.transform, 3, extents.z, -extents.x, height, wallThickness, doorWidth, doorHeight, def.DoorSockets);
            
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(visualsObj.transform);
            ceiling.transform.localPosition = new Vector3(0, height + 0.5f, 0);
            ceiling.transform.localScale = new Vector3(extents.x * 2, 1, extents.z * 2);
            ceiling.GetComponent<Renderer>().sharedMaterial = floorMat;

            if (def.RoomType == RoomType.Corridor || def.RoomType == RoomType.Crossroad)
            {
                GameObject pipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pipe.name = "Pipe";
                pipe.transform.SetParent(visualsObj.transform);
                pipe.transform.localPosition = new Vector3(0, height - 0.5f, 0);
                pipe.transform.localRotation = Quaternion.Euler(0, 0, 90);
                pipe.transform.localScale = new Vector3(0.5f, extents.x, 0.5f);
                pipe.GetComponent<Renderer>().sharedMaterial = generatorMat;
            }
            
        }
        
        private static void GenerateEdge(Transform parent, int edgeType, float extentAlongEdge, float fixedAxisPos, float height, float thickness, float doorWidth, float doorHeight, List<DoorSocket> sockets)
        {
            bool isZAxisFixed = (edgeType == 0 || edgeType == 1);
            
            var edgeSockets = new List<DoorSocket>();
            foreach (var socket in sockets)
            {
                if (isZAxisFixed)
                {
                    if (Mathf.Abs(socket.LocalPosition.z - fixedAxisPos) < 0.1f)
                        edgeSockets.Add(socket);
                }
                else
                {
                    if (Mathf.Abs(socket.LocalPosition.x - fixedAxisPos) < 0.1f)
                        edgeSockets.Add(socket);
                }
            }
            
            if (isZAxisFixed)
                edgeSockets.Sort((a, b) => a.LocalPosition.x.CompareTo(b.LocalPosition.x));
            else
                edgeSockets.Sort((a, b) => a.LocalPosition.z.CompareTo(b.LocalPosition.z));
            
            float currentPos = -extentAlongEdge;
            foreach (var socket in edgeSockets)
            {
                float dc = isZAxisFixed ? socket.LocalPosition.x : socket.LocalPosition.z;
                float doorStart = dc - doorWidth / 2f;
                float doorEnd = dc + doorWidth / 2f;
                
                if (doorStart > currentPos)
                {
                    BuildWall(parent, isZAxisFixed, currentPos, doorStart, fixedAxisPos, 0, height, thickness);
                }
                
                BuildWall(parent, isZAxisFixed, doorStart, doorEnd, fixedAxisPos, doorHeight, height, thickness);
                
                string plugName = $"SocketPlug_{socket.LocalPosition.x:F1}_{socket.LocalPosition.y:F1}_{socket.LocalPosition.z:F1}";
                GameObject plug = BuildWall(parent, isZAxisFixed, doorStart, doorEnd, fixedAxisPos, 0, doorHeight, thickness);
                plug.name = plugName;
                
                currentPos = doorEnd;
            }
            
            if (extentAlongEdge > currentPos)
            {
                BuildWall(parent, isZAxisFixed, currentPos, extentAlongEdge, fixedAxisPos, 0, height, thickness);
            }
        }
        
        private static GameObject BuildWall(Transform parent, bool isZAxisFixed, float startA, float endA, float fixedAxisB, float startY, float endY, float thickness)
        {
            float width = endA - startA;
            if (width <= 0.01f) return new GameObject("EmptyWall");
            float h = endY - startY;
            if (h <= 0.01f) return new GameObject("EmptyWall");
            
            float centerA = (startA + endA) / 2f;
            float centerY = (startY + endY) / 2f;
            
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "WallSegment";
            wall.transform.SetParent(parent);
            
            if (isZAxisFixed)
            {
                wall.transform.localPosition = new Vector3(centerA, centerY, fixedAxisB);
                wall.transform.localScale = new Vector3(width, h, thickness);
            }
            else
            {
                wall.transform.localPosition = new Vector3(fixedAxisB, centerY, centerA);
                wall.transform.localScale = new Vector3(thickness, h, width);
            }
            
            wall.GetComponent<Renderer>().sharedMaterial = wallMat;
            return wall;
        }
    }
}
