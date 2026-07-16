using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using SpaceMaintenance.Core;

namespace ShipGeneration {
    public class ShipGenerator : NetworkBehaviour {
        public event System.Action OnGenerationComplete;
        public bool IsGenerationComplete { get; private set; }
        
        [Header("Grid Settings")]
        public int GridWidth = 30;
        public int GridHeight = 30;
        public float CellSize = 10f;
        
        [Header("Room Prefabs")]
        public RoomData ReactorRoomPrefab;
        public RoomData GeneratorRoomPrefab;
        public RoomData SpawnRoomPrefab;
        public RoomData CorridorPrefab;
        
        [Header("Interactive Prefabs")]
        public GameObject DoorPrefab;
        public GameObject ReactorPrefab;
        public GameObject GeneratorPrefab;
        public GameObject PipePrefab;
        
        [Header("Generic Room Prefabs")]
        public RoomData[] GenericRoomPrefabs;
        
        private GridNode[,] grid;
        private List<RoomInstanceData> plannedRooms = new List<RoomInstanceData>();
        private List<RoomData> spawnedRooms = new List<RoomData>();
        public IReadOnlyList<RoomData> SpawnedRooms => spawnedRooms;
        
        private class RoomInstanceData {
            public RoomData Prefab;
            public int X;
            public int Y;
            public List<GridNode> Nodes = new List<GridNode>();
        }
        
        public override void OnNetworkSpawn() {
            if (IsServer) {
                GenerateShip();
            }
        }
        
        private void GenerateShip() {
            InitializeGrid();
            
            int centerX = GridWidth / 2;
            int centerY = GridHeight / 2;
            
            // 1. Place Reactor in center
            PlanRoom(ReactorRoomPrefab, centerX - 1, centerY - 1);
            
            // 2. Prepare rooms to attach
            List<RoomData> roomsToAttach = new List<RoomData>();
            roomsToAttach.Add(SpawnRoomPrefab);
            roomsToAttach.Add(GeneratorRoomPrefab);
            
            int numGenerics = Random.Range(4, 7);
            if (GenericRoomPrefabs != null && GenericRoomPrefabs.Length > 0) {
                for (int i = 0; i < numGenerics; i++) {
                    roomsToAttach.Add(GenericRoomPrefabs[Random.Range(0, GenericRoomPrefabs.Length)]);
                }
            }
            
            // 3. Branching growth
            foreach(var roomPrefab in roomsToAttach) {
                TryAttachRoom(roomPrefab);
            }
            
            InstantiatePlannedRooms();
            InstantiateCorridors();
            SpawnDoors();
            SpawnProps();
            
            Debug.Log("Ship Generation Complete.");
            IsGenerationComplete = true;
            OnGenerationComplete?.Invoke();

            if (SpaceMaintenance.Tasks.TaskManager.Instance != null) {
                SpaceMaintenance.Tasks.TaskManager.Instance.InitializeShipSystems();
            }
            if (SpaceMaintenance.Chaos.ChaosManager.Instance != null) {
                SpaceMaintenance.Chaos.ChaosManager.Instance.InitializeShipSystems();
            }
        }
        
        private void InitializeGrid() {
            grid = new GridNode[GridWidth, GridHeight];
            for (int x = 0; x < GridWidth; x++) {
                for (int y = 0; y < GridHeight; y++) {
                    grid[x, y] = new GridNode(x, y);
                }
            }
        }
        
        private RoomInstanceData PlanRoom(RoomData prefab, int startX, int startY) {
            int w = prefab.SizeInCells.x;
            int h = prefab.SizeInCells.y;
            
            var instance = new RoomInstanceData { Prefab = prefab, X = startX, Y = startY };
            
            for (int x = startX; x < startX + w; x++) {
                for (int y = startY; y < startY + h; y++) {
                    if (x >= 0 && x < GridWidth && y >= 0 && y < GridHeight) {
                        grid[x, y].IsOccupied = true;
                        grid[x, y].RoomType = prefab.RoomType;
                        instance.Nodes.Add(grid[x, y]);
                    }
                }
            }
            plannedRooms.Add(instance);
            return instance;
        }

        private struct Anchor {
            public int X;
            public int Y;
            public Vector2Int Dir;
        }

        private bool TryAttachRoom(RoomData prefab) {
            var anchors = GetValidAnchors();
            anchors = anchors.OrderBy(x => Random.value).ToList();

            foreach(var anchor in anchors) {
                int corrX = anchor.X + anchor.Dir.x;
                int corrY = anchor.Y + anchor.Dir.y;
                
                if (!IsCellEmpty(corrX, corrY)) continue;
                
                int attachX = corrX + anchor.Dir.x;
                int attachY = corrY + anchor.Dir.y;
                
                int w = prefab.SizeInCells.x;
                int h = prefab.SizeInCells.y;
                
                List<Vector2Int> validOffsets = new List<Vector2Int>();
                
                if (anchor.Dir.x == 1) { // Room to the right
                    int rx = attachX;
                    for (int ry = attachY - h + 1; ry <= attachY; ry++) {
                        if (IsAreaEmpty(rx, ry, w, h, corrX, corrY)) validOffsets.Add(new Vector2Int(rx, ry));
                    }
                } else if (anchor.Dir.x == -1) { // Room to the left
                    int rx = attachX - w + 1;
                    for (int ry = attachY - h + 1; ry <= attachY; ry++) {
                        if (IsAreaEmpty(rx, ry, w, h, corrX, corrY)) validOffsets.Add(new Vector2Int(rx, ry));
                    }
                } else if (anchor.Dir.y == 1) { // Room above
                    int ry = attachY;
                    for (int rx = attachX - w + 1; rx <= attachX; rx++) {
                        if (IsAreaEmpty(rx, ry, w, h, corrX, corrY)) validOffsets.Add(new Vector2Int(rx, ry));
                    }
                } else if (anchor.Dir.y == -1) { // Room below
                    int ry = attachY - h + 1;
                    for (int rx = attachX - w + 1; rx <= attachX; rx++) {
                        if (IsAreaEmpty(rx, ry, w, h, corrX, corrY)) validOffsets.Add(new Vector2Int(rx, ry));
                    }
                }
                
                if (validOffsets.Count > 0) {
                    var offset = validOffsets[Random.Range(0, validOffsets.Count)];
                    grid[corrX, corrY].IsOccupied = true;
                    grid[corrX, corrY].RoomType = RoomType.Corridor;
                    PlanRoom(prefab, offset.x, offset.y);
                    return true;
                }
            }
            return false;
        }

        private List<Anchor> GetValidAnchors() {
            List<Anchor> anchors = new List<Anchor>();
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            
            for (int x = 0; x < GridWidth; x++) {
                for (int y = 0; y < GridHeight; y++) {
                    if (grid[x, y].IsOccupied && grid[x, y].RoomType != RoomType.Corridor) {
                        foreach (var d in dirs) {
                            if (IsCellEmpty(x + d.x, y + d.y)) {
                                anchors.Add(new Anchor { X = x, Y = y, Dir = d });
                            }
                        }
                    }
                }
            }
            return anchors;
        }

        private bool IsCellEmpty(int x, int y) {
            if (x < 0 || x >= GridWidth || y < 0 || y >= GridHeight) return false;
            return !grid[x, y].IsOccupied;
        }

        private bool IsAreaEmpty(int startX, int startY, int w, int h, int corrX, int corrY) {
            for (int x = startX - 1; x <= startX + w; x++) {
                for (int y = startY - 1; y <= startY + h; y++) {
                    if (x < 0 || x >= GridWidth || y < 0 || y >= GridHeight) return false;
                    if (x == corrX && y == corrY) continue;
                    if (grid[x, y].IsOccupied) return false;
                }
            }
            return true;
        }
        
        private void InstantiatePlannedRooms() {
            foreach (var plan in plannedRooms) {
                Vector3 pos = new Vector3(plan.X * CellSize, 0, plan.Y * CellSize);
                GameObject roomObj = Instantiate(plan.Prefab.gameObject, pos, Quaternion.identity);
                roomObj.GetComponent<NetworkObject>().Spawn();
                
                var roomData = roomObj.GetComponent<RoomData>();
                spawnedRooms.Add(roomData);
                
                foreach (var node in plan.Nodes) {
                    node.RoomInstance = roomData;
                }
            }
        }
        
        private void InstantiateCorridors() {
            for (int x = 0; x < GridWidth; x++) {
                for (int y = 0; y < GridHeight; y++) {
                    if (grid[x, y].RoomType == RoomType.Corridor) {
                        Vector3 pos = new Vector3(x * CellSize, 0, y * CellSize);
                        GameObject corridorObj = Instantiate(CorridorPrefab.gameObject, pos, Quaternion.identity);
                        corridorObj.GetComponent<NetworkObject>().Spawn();
                        
                        var roomData = corridorObj.GetComponent<RoomData>();
                        grid[x, y].RoomInstance = roomData;
                        spawnedRooms.Add(roomData);
                    }
                }
            }
        }
        
        private HashSet<string> doorEdges = new HashSet<string>();
        
        private string GetEdgeKey(GridNode a, GridNode b) {
            if (a.X < b.X || (a.X == b.X && a.Y < b.Y)) 
                return $"{a.X},{a.Y}-{b.X},{b.Y}";
            return $"{b.X},{b.Y}-{a.X},{a.Y}";
        }

        private void SpawnDoors() {
            if (DoorPrefab == null) return;
            doorEdges.Clear();
            
            for (int x = 0; x < GridWidth; x++) {
                for (int y = 0; y < GridHeight; y++) {
                    GridNode node = grid[x, y];
                    if (!node.IsOccupied) continue;
                    
                    if (x + 1 < GridWidth && grid[x+1, y].IsOccupied) {
                        GridNode right = grid[x+1, y];
                        if (node.RoomInstance != right.RoomInstance) {
                            if (!(node.RoomType == RoomType.Corridor && right.RoomType == RoomType.Corridor)) {
                                SpawnDoorBetween(node, right);
                            }
                        }
                    }
                    
                    if (y + 1 < GridHeight && grid[x, y+1].IsOccupied) {
                        GridNode up = grid[x, y+1];
                        if (node.RoomInstance != up.RoomInstance) {
                            if (!(node.RoomType == RoomType.Corridor && up.RoomType == RoomType.Corridor)) {
                                SpawnDoorBetween(node, up);
                            }
                        }
                    }
                }
            }
        }
        
        private void SpawnDoorBetween(GridNode a, GridNode b) {
            doorEdges.Add(GetEdgeKey(a, b));
            Vector3 posA = new Vector3(a.X * CellSize + CellSize/2f, 0, a.Y * CellSize + CellSize/2f);
            Vector3 posB = new Vector3(b.X * CellSize + CellSize/2f, 0, b.Y * CellSize + CellSize/2f);
            Vector3 pos = Vector3.Lerp(posA, posB, 0.5f);
            
            Vector3 dir = (posB - posA).normalized;
            Quaternion rot = Quaternion.LookRotation(dir);
            
            GameObject doorObj = Instantiate(DoorPrefab, pos, rot);
            doorObj.GetComponent<NetworkObject>().Spawn();
            
            var doorCtrl = doorObj.GetComponent<ProceduralDoor>();
            if (doorCtrl != null) {
                if (a.RoomType == RoomType.Spawn || b.RoomType == RoomType.Spawn) {
                    doorCtrl.IsSpawnDoor = true;
                }
            }
        }
        
        private void SpawnProps() {
            // Spawn internal room props
            foreach (var room in spawnedRooms) {
                if (room.RoomType == RoomType.Reactor && ReactorPrefab != null && room.ReactorSpawn != null) {
                    GameObject r = Instantiate(ReactorPrefab, room.ReactorSpawn.position, room.ReactorSpawn.rotation);
                    r.GetComponent<NetworkObject>().Spawn();
                }
                
                if (room.RoomType == RoomType.Generator && GeneratorPrefab != null) {
                    int genCount = Mathf.Clamp(2 + GlobalMissionParameters.MissionsCompleted / 2, 2, 4);
                    int spawnCount = Mathf.Min(genCount, room.GeneratorSpawns.Count);
                    for (int i = 0; i < spawnCount; i++) {
                        GameObject g = Instantiate(GeneratorPrefab, room.GeneratorSpawns[i].position, room.GeneratorSpawns[i].rotation);
                        g.GetComponent<NetworkObject>().Spawn();
                    }
                }
            }

            SpawnWallsAndPipes();
        }

        private void SpawnWallsAndPipes() {
            int[] dx = { 0, 1, 0, -1 };
            int[] dy = { 1, 0, -1, 0 };
            
            for (int x = 0; x < GridWidth; x++) {
                for (int y = 0; y < GridHeight; y++) {
                    GridNode node = grid[x, y];
                    if (!node.IsOccupied) continue;
                    
                    for (int d = 0; d < 4; d++) {
                        int nx = x + dx[d];
                        int ny = y + dy[d];
                        
                        bool needsWall = false;
                        if (nx < 0 || nx >= GridWidth || ny < 0 || ny >= GridHeight) {
                            needsWall = true;
                        } else {
                            GridNode neighbor = grid[nx, ny];
                            if (!neighbor.IsOccupied) {
                                needsWall = true;
                            } else if (node.RoomInstance != neighbor.RoomInstance) {
                                if (!doorEdges.Contains(GetEdgeKey(node, neighbor))) {
                                    needsWall = true;
                                }
                            }
                        }
                        
                        if (needsWall) {
                            Vector3 centerPos = new Vector3(x * CellSize + CellSize / 2f, 2f, y * CellSize + CellSize / 2f);
                            Vector3 wallPos = centerPos + new Vector3(dx[d] * CellSize / 2f, 0, dy[d] * CellSize / 2f);
                            
                            float thick = 1f;
                            Vector3 scale = new Vector3(
                                dx[d] == 0 ? CellSize : thick,
                                4f,
                                dy[d] == 0 ? CellSize : thick
                            );
                            
                            // Build on server and broadcast to clients
                            BuildWallClientRpc(wallPos, scale);
                            
                            if (PipePrefab != null && node.RoomType != RoomType.Spawn && node.RoomType != RoomType.Corridor) {
                                if (Random.value < 0.2f) { // 20% chance
                                    Vector3 pipePos = wallPos - new Vector3(dx[d] * thick, 1f, dy[d] * thick);
                                    Quaternion rot = Quaternion.LookRotation(new Vector3(-dx[d], 0, -dy[d]));
                                    GameObject pipe = Instantiate(PipePrefab, pipePos, rot);
                                    if (pipe.TryGetComponent<NetworkObject>(out var netObj)) {
                                        netObj.Spawn();
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        [ClientRpc]
        private void BuildWallClientRpc(Vector3 pos, Vector3 scale) {
            GameObject wallObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallObj.transform.position = pos;
            wallObj.transform.localScale = scale;
            wallObj.name = "ProceduralWall";
            
            var renderer = wallObj.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = Color.gray;
        }
    }
}
