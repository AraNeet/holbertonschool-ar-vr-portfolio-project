using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Dungeon Settings")]
    [SerializeField] private int gridSize = 5; // 5x5x5 grid
    [SerializeField] private float cellSize = 0.2f; // Each cell size
    [SerializeField] private GameObject roomPrefab;
    [SerializeField] private GameObject corridorPrefab;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject treasurePrefab;

    [Header("Generation Parameters")]
    [SerializeField] private int minRooms = 10;
    [SerializeField] private int maxRooms = 20;
    [SerializeField] private float enemySpawnChance = 0.3f;
    [SerializeField] private float treasureSpawnChance = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool debug = false; // Enable debug visualization
    [SerializeField] private Material lineRendererMaterial; // Material for the line renderers
    [SerializeField] private float lineWidth = 0.01f; // Width of debug lines

    // Internal data structure for the dungeon
    private bool[,,] occupiedCells;
    private List<Room> rooms = new List<Room>();
    private Transform debugContainer;
    private bool debugVisualsCreated = false;

    // Simplified room class for tracking
    private class Room
    {
        public Vector3Int position;
        public GameObject roomObject;
        public List<Room> connectedRooms = new List<Room>();
    }

    public void GenerateDungeon()
    {
        // Initialize the grid
        occupiedCells = new bool[gridSize, gridSize, gridSize];

        // Clear any existing rooms
        foreach (var room in rooms)
        {
            if (room.roomObject != null)
                Destroy(room.roomObject);
        }
        rooms.Clear();

        // Find the dungeon container
        Transform dungeonContainer = transform.Find("DungeonContainer");
        if (dungeonContainer == null)
        {
            dungeonContainer = new GameObject("DungeonContainer").transform;
            dungeonContainer.parent = transform;
            dungeonContainer.localPosition = Vector3.zero;
        }
        else
        {
            // Clear any existing children
            foreach (Transform child in dungeonContainer)
            {
                Destroy(child.gameObject);
            }
        }

        // Clear any existing debug visuals
        ClearDebugVisuals();

        // Generate random rooms
        int roomCount = Random.Range(minRooms, maxRooms + 1);

        // Start with a room in the center
        Vector3Int center = new Vector3Int(gridSize / 2, gridSize / 2, gridSize / 2);
        PlaceRoom(center, dungeonContainer);

        // Add remaining rooms
        for (int i = 1; i < roomCount; i++)
        {
            TryPlaceRoom(dungeonContainer);
        }

        // Connect rooms with corridors
        ConnectRooms(dungeonContainer);

        // Add enemies and treasures
        PopulateDungeon();

        // Create debug visualization if enabled
        if (debug)
        {
            CreateDebugVisuals();
        }
    }

    private void TryPlaceRoom(Transform parent)
    {
        // Try to find a valid position
        for (int attempt = 0; attempt < 100; attempt++)
        {
            int x = Random.Range(0, gridSize);
            int y = Random.Range(0, gridSize);
            int z = Random.Range(0, gridSize);

            Vector3Int pos = new Vector3Int(x, y, z);

            if (!occupiedCells[x, y, z] && HasAdjacentEmptySpace(pos))
            {
                PlaceRoom(pos, parent);
                return;
            }
        }
    }

    private bool HasAdjacentEmptySpace(Vector3Int pos)
    {
        // Check if there's space around this position
        Vector3Int[] directions = new Vector3Int[]
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1)
        };

        foreach (var dir in directions)
        {
            Vector3Int checkPos = pos + dir;

            if (IsValidPosition(checkPos) && !occupiedCells[checkPos.x, checkPos.y, checkPos.z])
            {
                return true;
            }
        }

        return false;
    }

    private bool IsValidPosition(Vector3Int pos)
    {
        return pos.x >= 0 && pos.x < gridSize &&
               pos.y >= 0 && pos.y < gridSize &&
               pos.z >= 0 && pos.z < gridSize;
    }

    private void PlaceRoom(Vector3Int pos, Transform parent)
    {
        // Mark as occupied
        occupiedCells[pos.x, pos.y, pos.z] = true;

        // Calculate world position
        Vector3 worldPos = new Vector3(
            (pos.x - gridSize / 2) * cellSize,
            (pos.y - gridSize / 2) * cellSize,
            (pos.z - gridSize / 2) * cellSize
        );

        // Instantiate room
        GameObject roomObj = Instantiate(roomPrefab, parent);
        roomObj.transform.localPosition = worldPos;
        roomObj.transform.localScale = Vector3.one * cellSize * 0.9f; // Slightly smaller than cell
        roomObj.tag = "Room";

        // Add to room list
        Room newRoom = new Room
        {
            position = pos,
            roomObject = roomObj
        };

        rooms.Add(newRoom);
    }

    private void ConnectRooms(Transform parent)
    {
        // Create a minimum spanning tree to ensure connectivity
        // This is a simplified approach - you might want a more advanced algorithm

        List<Room> connectedRooms = new List<Room>();
        List<Room> unconnectedRooms = new List<Room>(rooms);

        // Start with the first room
        if (unconnectedRooms.Count > 0)
        {
            Room firstRoom = unconnectedRooms[0];
            connectedRooms.Add(firstRoom);
            unconnectedRooms.Remove(firstRoom);
        }

        // Connect all remaining rooms
        while (unconnectedRooms.Count > 0)
        {
            float closestDistance = float.MaxValue;
            Room closestUnconnected = null;
            Room closestConnected = null;

            // Find the closest pair of rooms (one connected, one unconnected)
            foreach (var connectedRoom in connectedRooms)
            {
                foreach (var unconnectedRoom in unconnectedRooms)
                {
                    float distance = Vector3Int.Distance(connectedRoom.position, unconnectedRoom.position);

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestConnected = connectedRoom;
                        closestUnconnected = unconnectedRoom;
                    }
                }
            }

            // Connect them
            if (closestConnected != null && closestUnconnected != null)
            {
                CreateCorridor(closestConnected, closestUnconnected, parent);

                // Update lists
                connectedRooms.Add(closestUnconnected);
                unconnectedRooms.Remove(closestUnconnected);
            }
        }

        // Add some random additional connections for loops (optional)
        int additionalConnections = Mathf.FloorToInt(rooms.Count * 0.3f); // 30% more connections

        for (int i = 0; i < additionalConnections; i++)
        {
            Room roomA = rooms[Random.Range(0, rooms.Count)];
            Room roomB = rooms[Random.Range(0, rooms.Count)];

            if (roomA != roomB && !roomA.connectedRooms.Contains(roomB))
            {
                CreateCorridor(roomA, roomB, parent);
            }
        }
    }

    private void CreateCorridor(Room roomA, Room roomB, Transform parent)
    {
        // This will create a simple L-shaped corridor between rooms
        Vector3Int posA = roomA.position;
        Vector3Int posB = roomB.position;

        // Create corridor along each axis (X, then Y, then Z)
        Vector3Int current = posA;

        // Move along X
        while (current.x != posB.x)
        {
            current.x += (posB.x > current.x) ? 1 : -1;

            if (!occupiedCells[current.x, current.y, current.z])
            {
                PlaceCorridor(current, parent);
            }
        }

        // Move along Y
        while (current.y != posB.y)
        {
            current.y += (posB.y > current.y) ? 1 : -1;

            if (!occupiedCells[current.x, current.y, current.z])
            {
                PlaceCorridor(current, parent);
            }
        }

        // Move along Z
        while (current.z != posB.z)
        {
            current.z += (posB.z > current.z) ? 1 : -1;

            if (!occupiedCells[current.x, current.y, current.z])
            {
                PlaceCorridor(current, parent);
            }
        }

        // Update the connection records
        roomA.connectedRooms.Add(roomB);
        roomB.connectedRooms.Add(roomA);
    }

    private void PlaceCorridor(Vector3Int pos, Transform parent)
    {
        // Mark as occupied
        occupiedCells[pos.x, pos.y, pos.z] = true;

        // Calculate world position
        Vector3 worldPos = new Vector3(
            (pos.x - gridSize / 2) * cellSize,
            (pos.y - gridSize / 2) * cellSize,
            (pos.z - gridSize / 2) * cellSize
        );

        // Instantiate corridor segment
        GameObject corridorObj = Instantiate(corridorPrefab, parent);
        corridorObj.transform.localPosition = worldPos;
        corridorObj.transform.localScale = Vector3.one * cellSize * 0.7f; // Slightly smaller than rooms
        corridorObj.tag = "Corridor";
    }

    private void PopulateDungeon()
    {
        // Add enemies and treasure to rooms
        foreach (var room in rooms)
        {
            // Chance to spawn an enemy
            if (Random.value < enemySpawnChance && enemyPrefab != null)
            {
                SpawnEnemy(room);
            }

            // Chance to spawn treasure
            if (Random.value < treasureSpawnChance && treasurePrefab != null)
            {
                SpawnTreasure(room);
            }
        }
    }

    private void SpawnEnemy(Room room)
    {
        GameObject enemy = Instantiate(enemyPrefab, room.roomObject.transform);
        enemy.transform.localPosition = Vector3.zero; // Center of room
        enemy.tag = "Enemy";

        // Randomize scale slightly
        float scale = Random.Range(0.8f, 1.2f);
        enemy.transform.localScale = Vector3.one * scale;

        // Random rotation
        enemy.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
    }

    private void SpawnTreasure(Room room)
    {
        GameObject treasure = Instantiate(treasurePrefab, room.roomObject.transform);
        treasure.transform.localPosition = Vector3.zero; // Center of room
        treasure.tag = "Treasure";

        // Random rotation
        treasure.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
    }

    private void OnDrawGizmos()
    {
        // Keep this for editor visualization, but the primary visualization will be LineRenderers
        if (!debug || occupiedCells == null || Application.isPlaying)
            return;
            
        // Draw the grid for visual debugging in editor only
        DrawDebugGrid();
    }
    
    private void Update()
    {
        // Update debug visuals based on debug flag
        if (debug && !debugVisualsCreated && occupiedCells != null)
        {
            CreateDebugVisuals();
        }
        else if (!debug && debugVisualsCreated)
        {
            ClearDebugVisuals();
        }
    }

    private void CreateDebugVisuals()
    {
        // Clear any existing debug visuals
        ClearDebugVisuals();

        // Create container for debug visualizations
        debugContainer = new GameObject("DebugVisuals").transform;
        debugContainer.SetParent(transform);
        debugContainer.localPosition = Vector3.zero;

        // Draw grid outline
        CreateGridOutline();

        // Draw axes
        CreateAxes();

        // Draw cell outlines
        CreateCellVisuals();

        debugVisualsCreated = true;
    }

    private void ClearDebugVisuals()
    {
        // Find and destroy any existing debug container
        Transform existingDebugContainer = transform.Find("DebugVisuals");
        if (existingDebugContainer != null)
        {
            Destroy(existingDebugContainer.gameObject);
        }

        debugVisualsCreated = false;
    }

    private void CreateGridOutline()
    {
        // Create the grid outline using LineRenderer
        GameObject gridOutline = new GameObject("GridOutline");
        gridOutline.transform.SetParent(debugContainer);
        gridOutline.transform.localPosition = Vector3.zero;

        LineRenderer lineRenderer = gridOutline.AddComponent<LineRenderer>();
        SetupLineRenderer(lineRenderer, Color.yellow, lineWidth * 2);

        // Calculate the grid corners
        float halfSize = gridSize * cellSize / 2;
        Vector3[] corners = new Vector3[8];
        corners[0] = new Vector3(-halfSize, -halfSize, -halfSize);
        corners[1] = new Vector3(halfSize, -halfSize, -halfSize);
        corners[2] = new Vector3(halfSize, -halfSize, halfSize);
        corners[3] = new Vector3(-halfSize, -halfSize, halfSize);
        corners[4] = new Vector3(-halfSize, halfSize, -halfSize);
        corners[5] = new Vector3(halfSize, halfSize, -halfSize);
        corners[6] = new Vector3(halfSize, halfSize, halfSize);
        corners[7] = new Vector3(-halfSize, halfSize, halfSize);

        // Draw the grid cube
        lineRenderer.positionCount = 16;
        // Bottom face
        lineRenderer.SetPosition(0, transform.TransformPoint(corners[0]));
        lineRenderer.SetPosition(1, transform.TransformPoint(corners[1]));
        lineRenderer.SetPosition(2, transform.TransformPoint(corners[2]));
        lineRenderer.SetPosition(3, transform.TransformPoint(corners[3]));
        lineRenderer.SetPosition(4, transform.TransformPoint(corners[0]));
        // Connect to top face
        lineRenderer.SetPosition(5, transform.TransformPoint(corners[4]));
        // Top face
        lineRenderer.SetPosition(6, transform.TransformPoint(corners[5]));
        lineRenderer.SetPosition(7, transform.TransformPoint(corners[6]));
        lineRenderer.SetPosition(8, transform.TransformPoint(corners[7]));
        lineRenderer.SetPosition(9, transform.TransformPoint(corners[4]));
        // Connect remaining edges
        lineRenderer.SetPosition(10, transform.TransformPoint(corners[5]));
        lineRenderer.SetPosition(11, transform.TransformPoint(corners[1]));
        lineRenderer.SetPosition(12, transform.TransformPoint(corners[2]));
        lineRenderer.SetPosition(13, transform.TransformPoint(corners[6]));
        lineRenderer.SetPosition(14, transform.TransformPoint(corners[7]));
        lineRenderer.SetPosition(15, transform.TransformPoint(corners[3]));
    }

    private void CreateAxes()
    {
        // Create axes for orientation
        float axisLength = gridSize * cellSize;
        
        // X Axis (Red)
        CreateAxisLine("XAxis", Vector3.zero, Vector3.right * axisLength, Color.red);
        
        // Y Axis (Green)
        CreateAxisLine("YAxis", Vector3.zero, Vector3.up * axisLength, Color.green);
        
        // Z Axis (Blue)
        CreateAxisLine("ZAxis", Vector3.zero, Vector3.forward * axisLength, Color.blue);
    }

    private void CreateAxisLine(string name, Vector3 start, Vector3 end, Color color)
    {
        GameObject axis = new GameObject(name);
        axis.transform.SetParent(debugContainer);
        axis.transform.localPosition = Vector3.zero;

        LineRenderer lineRenderer = axis.AddComponent<LineRenderer>();
        SetupLineRenderer(lineRenderer, color, lineWidth);

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, transform.TransformPoint(start));
        lineRenderer.SetPosition(1, transform.TransformPoint(end));
    }

    private void CreateCellVisuals()
    {
        if (occupiedCells == null)
            return;

        // Create container for cells
        GameObject cellsContainer = new GameObject("Cells");
        cellsContainer.transform.SetParent(debugContainer);
        cellsContainer.transform.localPosition = Vector3.zero;

        // Draw individual cells
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    Vector3 cellCenter = new Vector3(
                        (x - gridSize / 2) * cellSize,
                        (y - gridSize / 2) * cellSize,
                        (z - gridSize / 2) * cellSize
                    );

                    // Only draw cells that are occupied to avoid clutter
                    if (occupiedCells[x, y, z])
                    {
                        CreateCellCube(cellsContainer.transform, cellCenter, Color.red, x, y, z);
                    }
                }
            }
        }
    }

    private void CreateCellCube(Transform parent, Vector3 center, Color color, int x, int y, int z)
    {
        string cellName = $"Cell_{x}_{y}_{z}";
        GameObject cell = new GameObject(cellName);
        cell.transform.SetParent(parent);
        cell.transform.localPosition = Vector3.zero;

        LineRenderer lineRenderer = cell.AddComponent<LineRenderer>();
        SetupLineRenderer(lineRenderer, color, lineWidth * 0.8f);

        // Calculate the cell corners
        float halfCell = cellSize * 0.45f; // Slightly smaller than the actual cell
        Vector3[] corners = new Vector3[8];
        corners[0] = center + new Vector3(-halfCell, -halfCell, -halfCell);
        corners[1] = center + new Vector3(halfCell, -halfCell, -halfCell);
        corners[2] = center + new Vector3(halfCell, -halfCell, halfCell);
        corners[3] = center + new Vector3(-halfCell, -halfCell, halfCell);
        corners[4] = center + new Vector3(-halfCell, halfCell, -halfCell);
        corners[5] = center + new Vector3(halfCell, halfCell, -halfCell);
        corners[6] = center + new Vector3(halfCell, halfCell, halfCell);
        corners[7] = center + new Vector3(-halfCell, halfCell, halfCell);

        // Draw the cell cube
        lineRenderer.positionCount = 16;
        // Bottom face
        lineRenderer.SetPosition(0, transform.TransformPoint(corners[0]));
        lineRenderer.SetPosition(1, transform.TransformPoint(corners[1]));
        lineRenderer.SetPosition(2, transform.TransformPoint(corners[2]));
        lineRenderer.SetPosition(3, transform.TransformPoint(corners[3]));
        lineRenderer.SetPosition(4, transform.TransformPoint(corners[0]));
        // Connect to top face
        lineRenderer.SetPosition(5, transform.TransformPoint(corners[4]));
        // Top face
        lineRenderer.SetPosition(6, transform.TransformPoint(corners[5]));
        lineRenderer.SetPosition(7, transform.TransformPoint(corners[6]));
        lineRenderer.SetPosition(8, transform.TransformPoint(corners[7]));
        lineRenderer.SetPosition(9, transform.TransformPoint(corners[4]));
        // Connect remaining edges
        lineRenderer.SetPosition(10, transform.TransformPoint(corners[5]));
        lineRenderer.SetPosition(11, transform.TransformPoint(corners[1]));
        lineRenderer.SetPosition(12, transform.TransformPoint(corners[2]));
        lineRenderer.SetPosition(13, transform.TransformPoint(corners[6]));
        lineRenderer.SetPosition(14, transform.TransformPoint(corners[7]));
        lineRenderer.SetPosition(15, transform.TransformPoint(corners[3]));
    }

    private void SetupLineRenderer(LineRenderer lineRenderer, Color color, float width)
    {
        // Configure the LineRenderer
        lineRenderer.material = lineRendererMaterial != null ? lineRendererMaterial : new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        lineRenderer.useWorldSpace = true;
    }

    private void DrawDebugGrid()
    {
        // Calculate world offset from center
        Vector3 offset = new Vector3(
            -gridSize * cellSize / 2,
            -gridSize * cellSize / 2,
            -gridSize * cellSize / 2
        );
        
        // Draw the overall grid bounds with a thicker line
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one * gridSize * cellSize);
        
        // Draw grid axes for better orientation
        DrawGridAxes();
        
        // Draw individual cells
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    Vector3 cellCenter = new Vector3(
                        (x - gridSize / 2) * cellSize,
                        (y - gridSize / 2) * cellSize,
                        (z - gridSize / 2) * cellSize
                    );
                    
                    Vector3 worldPos = transform.position + cellCenter;
                    
                    // Set color based on occupancy
                    if (occupiedCells != null && occupiedCells.GetLength(0) > x && occupiedCells.GetLength(1) > y && occupiedCells.GetLength(2) > z)
                    {
                        if (occupiedCells[x, y, z])
                        {
                            // Occupied cell - bright red
                            Gizmos.color = new Color(1, 0, 0, 0.7f);
                            Gizmos.DrawCube(worldPos, Vector3.one * cellSize * 0.4f);
                            Gizmos.color = Color.red;
                            Gizmos.DrawWireCube(worldPos, Vector3.one * cellSize * 0.9f);
                        }
                        else
                        {
                            // Unoccupied cell - cyan wireframe
                            Gizmos.color = Color.cyan;
                            Gizmos.DrawWireCube(worldPos, Vector3.one * cellSize * 0.9f);
                        }
                        
                        // Draw coordinate text for better reference
                        if (x == 0 || y == 0 || z == 0 || x == gridSize-1 || y == gridSize-1 || z == gridSize-1)
                        {
                            DrawDebugCoordinates(worldPos, new Vector3Int(x, y, z));
                        }
                    }
                }
            }
        }
    }
    
    private void DrawGridAxes()
    {
        float axisLength = gridSize * cellSize;
        
        // X axis - Red
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * axisLength);
        
        // Y axis - Green
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * axisLength);
        
        // Z axis - Blue
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.forward * axisLength);
    }
    
    private void DrawDebugCoordinates(Vector3 position, Vector3Int coords)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        // Only show coordinates on edges of the grid
        if (coords.x == 0 || coords.y == 0 || coords.z == 0 || 
            coords.x == gridSize-1 || coords.y == gridSize-1 || coords.z == gridSize-1)
        {
            // Only show on corners for better visibility
            if ((coords.x == 0 || coords.x == gridSize-1) && 
                (coords.y == 0 || coords.y == gridSize-1) && 
                (coords.z == 0 || coords.z == gridSize-1))
            {
                UnityEditor.Handles.Label(position, $"{coords.x},{coords.y},{coords.z}");
            }
        }
#endif
    }

    // Add a public method to get the first room's position
    public Vector3 GetFirstRoomPosition()
    {
        if (rooms.Count > 0)
        {
            // Return the world position of the first room
            return rooms[0].roomObject.transform.position;
        }
        
        // Default position if no rooms exist
        return transform.position;
    }
}
