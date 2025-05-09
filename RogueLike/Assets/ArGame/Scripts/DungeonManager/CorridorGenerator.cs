using System.Collections.Generic;
using UnityEngine;

public class CorridorGenerator
{
    private GridManager gridManager;
    private GameObject corridorPrefab;
    private GameObject verticalCorridorPrefab;

    public CorridorGenerator(GridManager gridManager, GameObject corridorPrefab, GameObject verticalCorridorPrefab)
    {
        this.gridManager = gridManager;
        this.corridorPrefab = corridorPrefab;
        this.verticalCorridorPrefab = verticalCorridorPrefab;
    }

    public void ConnectRooms(List<Room> rooms, Transform parent)
    {
        if (rooms.Count == 0) return;

        // Create a minimum spanning tree to ensure connectivity
        List<Room> connectedRooms = new List<Room>();
        List<Room> unconnectedRooms = new List<Room>(rooms);

        // Start with the first room
        Room firstRoom = unconnectedRooms[0];
        connectedRooms.Add(firstRoom);
        unconnectedRooms.Remove(firstRoom);

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
        
        // Add vertical connections for rooms directly above/below each other
        CreateVerticalConnections(rooms, parent);
    }

    private void CreateVerticalConnections(List<Room> rooms, Transform parent)
    {
        // Find rooms that are directly above/below each other
        for (int i = 0; i < rooms.Count; i++)
        {
            for (int j = i + 1; j < rooms.Count; j++)
            {
                Room roomA = rooms[i];
                Room roomB = rooms[j];
                
                // Check if rooms have the same X and Z coordinates but different Y
                if (roomA.position.x == roomB.position.x && 
                    roomA.position.z == roomB.position.z && 
                    Mathf.Abs(roomA.position.y - roomB.position.y) == 1)
                {
                    // If they're not already connected, connect them
                    if (!roomA.connectedRooms.Contains(roomB))
                    {
                        // Create a direct vertical connection
                        CreateVerticalCorridor(roomA, roomB, parent);
                        
                        // Update connection records
                        roomA.connectedRooms.Add(roomB);
                        roomB.connectedRooms.Add(roomA);
                    }
                }
            }
        }
    }
    
    private void CreateVerticalCorridor(Room roomA, Room roomB, Transform parent)
    {
        // Determine which room is on top
        Room lowerRoom = roomA.position.y < roomB.position.y ? roomA : roomB;
        Room upperRoom = roomA.position.y > roomB.position.y ? roomA : roomB;
        
        // Create a corridor at the position of the lower room but with Y+1
        Vector3Int corridorPos = lowerRoom.position;
        corridorPos.y += 1; // Position between the two rooms
        
        // Calculate world position
        Vector3 worldPos = gridManager.GridToWorldPosition(corridorPos);
        
        // Instantiate vertical corridor segment
        GameObject corridorObj = Object.Instantiate(verticalCorridorPrefab, parent);
        corridorObj.transform.localPosition = worldPos;
        
        // Make vertical corridors visually distinct - taller and narrower
        float cellSize = gridManager.GetCellSize();
        Vector3 verticalScale = new Vector3(
            cellSize * 0.5f,  // Narrower in X
            cellSize * 0.9f,  // Taller in Y
            cellSize * 0.5f); // Narrower in Z
        
        corridorObj.transform.localScale = verticalScale;
        
        // Add visual distinction - rotate slightly
        corridorObj.transform.localRotation = Quaternion.Euler(0, 45, 0); // 45-degree rotation
        
        corridorObj.tag = "VerticalCorridor";
        
        // Mark the corridor position as occupied
        if (gridManager.IsValidPosition(corridorPos) && !gridManager.IsPositionOccupied(corridorPos))
        {
            gridManager.SetPositionOccupied(corridorPos, true);
        }
    }

    public void CreateCorridor(Room roomA, Room roomB, Transform parent)
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

            if (!gridManager.IsPositionOccupied(current))
            {
                PlaceCorridor(current, parent);
            }
        }

        // Move along Y
        while (current.y != posB.y)
        {
            current.y += (posB.y > current.y) ? 1 : -1;

            if (!gridManager.IsPositionOccupied(current))
            {
                PlaceCorridor(current, parent);
            }
        }

        // Move along Z
        while (current.z != posB.z)
        {
            current.z += (posB.z > current.z) ? 1 : -1;

            if (!gridManager.IsPositionOccupied(current))
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
        gridManager.SetPositionOccupied(pos, true);

        // Calculate world position
        Vector3 worldPos = gridManager.GridToWorldPosition(pos);

        // Instantiate corridor segment
        GameObject corridorObj = Object.Instantiate(corridorPrefab, parent);
        corridorObj.transform.localPosition = worldPos;
        corridorObj.transform.localScale = Vector3.one * gridManager.GetCellSize() * 0.7f; // Slightly smaller than rooms
        corridorObj.tag = "Corridor";
    }
} 