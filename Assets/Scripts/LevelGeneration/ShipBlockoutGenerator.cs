// ============================================================================
// SCRAPSHIFT — ShipBlockoutGenerator.cs
// Procedurally generates a dense, grid-based 3D ship blockout directly in 
// the Editor using a Subtractive BSP (Binary Space Partitioning) algorithm.
// Perfect for creating the massive, multi-room layout seen in references.
// ============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace SpaceMaintenance.LevelGeneration
{
    public class ShipBlockoutGenerator : MonoBehaviour
    {
        [Header("Ship Dimensions")]
        public int ShipWidth = 80;
        public int ShipLength = 30;
        public float CellSize = 2f;
        
        [Header("Room Settings")]
        public int MinRoomSize = 6;
        public int MaxRoomSize = 12;
        public int ReactorRadius = 6;
        
        [Header("Materials (Optional)")]
        public Material WallMaterial;
        public Material RoomFloorMaterial;
        public Material CorridorFloorMaterial;
        public Material ReactorFloorMaterial;

        // Grid Legend:
        // 0 = Space
        // 1 = Wall / Uncarved Hull
        // 2 = Room Floor
        // 3 = Corridor Floor
        // 4 = Reactor Floor
        private int[,] _grid;
        private List<RectInt> _rooms = new List<RectInt>();
        private GameObject _shipParent;

        [ContextMenu("Generate Blockout")]
        public void Generate()
        {
            InitializeGrid();
            CarveReactor();
            PartitionSpace(new RectInt(1, 1, ShipWidth - 2, ShipLength - 2));
            ConnectRooms();
            BuildMeshes();
            Debug.Log("[ShipBlockoutGenerator] Ship blockout generated successfully!");
        }

        private void InitializeGrid()
        {
            _grid = new int[ShipWidth, ShipLength];
            _rooms.Clear();
            
            // Fill with solid walls inside the ship bounds
            for (int x = 0; x < ShipWidth; x++)
            {
                for (int y = 0; y < ShipLength; y++)
                {
                    // Add some rounded corners by checking distance to corners
                    bool isCorner = (x < 3 && y < 3) || (x > ShipWidth - 4 && y < 3) ||
                                    (x < 3 && y > ShipLength - 4) || (x > ShipWidth - 4 && y > ShipLength - 4);
                    
                    _grid[x, y] = isCorner ? 0 : 1; 
                }
            }
        }

        private void CarveReactor()
        {
            int cx = ShipWidth / 2;
            int cy = ShipLength / 2;

            for (int x = cx - ReactorRadius; x <= cx + ReactorRadius; x++)
            {
                for (int y = cy - ReactorRadius; y <= cy + ReactorRadius; y++)
                {
                    if (Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) <= ReactorRadius)
                    {
                        if (x > 0 && x < ShipWidth - 1 && y > 0 && y < ShipLength - 1)
                            _grid[x, y] = 4; // Reactor floor
                    }
                }
            }
        }

        private void PartitionSpace(RectInt space)
        {
            // If space is small enough, make it a room
            if (space.width <= MaxRoomSize && space.height <= MaxRoomSize)
            {
                CreateRoom(space);
                return;
            }

            // Decide whether to split horizontally or vertically
            bool splitH = space.width > space.height;
            
            // If it's too skinny one way, force the split
            if (space.width > space.height && space.width / (float)space.height > 1.5f) splitH = true;
            else if (space.height > space.width && space.height / (float)space.width > 1.5f) splitH = false;

            if (splitH)
            {
                if (space.width < MinRoomSize * 2) { CreateRoom(space); return; }
                
                int splitPoint = Random.Range(MinRoomSize, space.width - MinRoomSize);
                RectInt left = new RectInt(space.x, space.y, splitPoint, space.height);
                RectInt right = new RectInt(space.x + splitPoint, space.y, space.width - splitPoint, space.height);
                
                PartitionSpace(left);
                PartitionSpace(right);
            }
            else
            {
                if (space.height < MinRoomSize * 2) { CreateRoom(space); return; }

                int splitPoint = Random.Range(MinRoomSize, space.height - MinRoomSize);
                RectInt bottom = new RectInt(space.x, space.y, space.width, splitPoint);
                RectInt top = new RectInt(space.x, space.y + splitPoint, space.width, space.height - splitPoint);
                
                PartitionSpace(bottom);
                PartitionSpace(top);
            }
        }

        private void CreateRoom(RectInt space)
        {
            // Shrink space to leave walls between rooms
            int shrink = 1;
            RectInt room = new RectInt(space.x + shrink, space.y + shrink, space.width - shrink * 2, space.height - shrink * 2);
            
            if (room.width < 3 || room.height < 3) return;

            // Check if it overlaps reactor
            bool overlapsReactor = false;
            for (int x = room.x; x < room.xMax; x++)
            {
                for (int y = room.y; y < room.yMax; y++)
                {
                    if (_grid[x, y] == 4 || _grid[x, y] == 0) overlapsReactor = true;
                }
            }

            if (overlapsReactor) return;

            _rooms.Add(room);

            for (int x = room.x; x < room.xMax; x++)
            {
                for (int y = room.y; y < room.yMax; y++)
                {
                    _grid[x, y] = 2; // Floor
                }
            }
        }

        private void ConnectRooms()
        {
            // Very simple corridor connection: connect every room's center to the ship's center (Reactor)
            // with a Manhattan path, carving corridors.
            Vector2Int center = new Vector2Int(ShipWidth / 2, ShipLength / 2);

            foreach (var room in _rooms)
            {
                Vector2Int start = new Vector2Int(Mathf.RoundToInt(room.center.x), Mathf.RoundToInt(room.center.y));
                CarvePath(start, center);
            }
        }

        private void CarvePath(Vector2Int from, Vector2Int to)
        {
            Vector2Int curr = from;
            while (curr != to)
            {
                if (Random.value > 0.5f)
                {
                    if (curr.x != to.x) curr.x += (to.x > curr.x) ? 1 : -1;
                    else curr.y += (to.y > curr.y) ? 1 : -1;
                }
                else
                {
                    if (curr.y != to.y) curr.y += (to.y > curr.y) ? 1 : -1;
                    else curr.x += (to.x > curr.x) ? 1 : -1;
                }

                if (_grid[curr.x, curr.y] == 1) // If wall, make it corridor
                {
                    _grid[curr.x, curr.y] = 3;
                }
            }
        }

        private void BuildMeshes()
        {
            if (_shipParent != null)
            {
                DestroyImmediate(_shipParent);
            }

            // Also try to find it by name in case of script recompile
            var old = GameObject.Find("GeneratedShipBlockout");
            if (old != null) DestroyImmediate(old);

            _shipParent = new GameObject("GeneratedShipBlockout");
            _shipParent.transform.position = Vector3.zero;

            GameObject wallParent = new GameObject("Walls");
            wallParent.transform.parent = _shipParent.transform;
            
            GameObject floorParent = new GameObject("Floors");
            floorParent.transform.parent = _shipParent.transform;

            for (int x = 0; x < ShipWidth; x++)
            {
                for (int y = 0; y < ShipLength; y++)
                {
                    int type = _grid[x, y];
                    if (type == 0) continue; // Space

                    Vector3 pos = new Vector3(x * CellSize, 0, y * CellSize);

                    if (type == 1) // Wall
                    {
                        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        wall.transform.parent = wallParent.transform;
                        wall.transform.position = pos + Vector3.up * (CellSize * 0.5f);
                        wall.transform.localScale = new Vector3(CellSize, CellSize * 2f, CellSize); // Tall walls
                        
                        var rend = wall.GetComponent<Renderer>();
                        if (WallMaterial != null) rend.sharedMaterial = WallMaterial;
                        else
                        {
                            var mat = new Material(Shader.Find("Standard"));
                            mat.color = new Color(0.2f, 0.2f, 0.25f);
                            rend.sharedMaterial = mat;
                        }
                    }
                    else if (type > 1) // Floors
                    {
                        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        floor.transform.parent = floorParent.transform;
                        floor.transform.position = pos;
                        floor.transform.rotation = Quaternion.Euler(90, 0, 0);
                        floor.transform.localScale = new Vector3(CellSize, CellSize, 1);

                        var rend = floor.GetComponent<Renderer>();
                        var mat = new Material(Shader.Find("Standard"));

                        if (type == 2)
                        {
                            if (RoomFloorMaterial != null) rend.sharedMaterial = RoomFloorMaterial;
                            else mat.color = new Color(0.5f, 0.5f, 0.5f);
                        }
                        else if (type == 3) // Corridor
                        {
                            if (CorridorFloorMaterial != null) rend.sharedMaterial = CorridorFloorMaterial;
                            else mat.color = new Color(0.3f, 0.3f, 0.4f);
                        }
                        else if (type == 4) // Reactor
                        {
                            if (ReactorFloorMaterial != null) rend.sharedMaterial = ReactorFloorMaterial;
                            else mat.color = new Color(0.8f, 0.4f, 0.1f); // Orange glow
                        }
                        
                        if (rend.sharedMaterial == null) rend.sharedMaterial = mat;
                    }
                }
            }
        }
    }
}
