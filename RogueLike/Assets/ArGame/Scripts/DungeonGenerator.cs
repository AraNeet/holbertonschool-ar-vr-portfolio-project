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
    [SerializeField] private float roomDensity = 0.3f; // 0-1 value for how dense the dungeon is
    [SerializeField] private int minRooms = 10;
    [SerializeField] private int maxRooms = 20;
    [SerializeField] private float enemySpawnChance = 0.3f;
    [SerializeField] private float treasureSpawnChance = 0.2f;

    // Internal data structure for the dungeon
    private bool[,,] occupiedCells;
    private List<Room> rooms = new List<Room>();

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
}
