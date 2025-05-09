using System.Collections.Generic;
using UnityEngine;

public class RoomGenerator
{
    private GridManager gridManager;
    private GameObject roomPrefab;
    private List<Room> rooms = new List<Room>();

    public RoomGenerator(GridManager gridManager, GameObject roomPrefab)
    {
        this.gridManager = gridManager;
        this.roomPrefab = roomPrefab;
    }

    public List<Room> GetRooms()
    {
        return rooms;
    }

    public void ClearRooms()
    {
        foreach (var room in rooms)
        {
            if (room.roomObject != null)
                Object.Destroy(room.roomObject);
        }
        rooms.Clear();
    }

    public Room PlaceRoomAtCenter(Transform parent)
    {
        int gridSize = gridManager.GetGridSize();
        Vector3Int center = new Vector3Int(gridSize / 2, gridSize / 2, gridSize / 2);
        return PlaceRoom(center, parent);
    }

    public Room TryPlaceRoom(Transform parent)
    {
        // Try to find a valid position
        int gridSize = gridManager.GetGridSize();
        for (int attempt = 0; attempt < 100; attempt++)
        {
            int x = Random.Range(0, gridSize);
            int y = Random.Range(0, gridSize);
            int z = Random.Range(0, gridSize);

            Vector3Int pos = new Vector3Int(x, y, z);

            if (!gridManager.IsPositionOccupied(pos) && gridManager.HasAdjacentEmptySpace(pos))
            {
                Room newRoom = PlaceRoom(pos, parent);
                return newRoom;
            }
        }
        
        // Failed to place room
        return null;
    }

    public Room PlaceRoom(Vector3Int pos, Transform parent)
    {
        // Mark as occupied
        gridManager.SetPositionOccupied(pos, true);

        // Calculate world position
        Vector3 worldPos = gridManager.GridToWorldPosition(pos);

        // Instantiate room
        GameObject roomObj = Object.Instantiate(roomPrefab, parent);
        roomObj.transform.localPosition = worldPos;
        roomObj.transform.localScale = Vector3.one * gridManager.GetCellSize() * 0.9f; // Slightly smaller than cell
        roomObj.tag = "Room";

        // Add to room list
        Room newRoom = new Room
        {
            position = pos,
            roomObject = roomObj
        };

        rooms.Add(newRoom);
        return newRoom;
    }
} 