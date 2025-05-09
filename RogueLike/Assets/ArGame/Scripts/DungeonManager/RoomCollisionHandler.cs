using UnityEngine;

public class RoomCollisionHandler
{
    // Offset from edge of room for proper barrier placement
    private const float EDGE_OFFSET = 0.02f;
    // Height of the barriers from room floor
    private const float BARRIER_HEIGHT = 0.1f;
    // Thickness of collision barriers
    private const float BARRIER_THICKNESS = 0.01f;
    
    private GameObject barrierPrefab;
    
    public RoomCollisionHandler()
    {
        // Create a barrier prefab at runtime
        barrierPrefab = CreateBarrierPrefab();
    }
    
    // Add invisible barriers to a room to prevent falling
    public void AddBarriersToRoom(Room room)
    {
        if (room == null || room.roomObject == null)
            return;
            
        GameObject roomObj = room.roomObject;
        
        // Get room dimensions
        Vector3 roomScale = roomObj.transform.localScale;
        float halfWidth = roomScale.x / 2;
        float halfDepth = roomScale.z / 2;
        
        // Create container for barriers
        GameObject barriersContainer = new GameObject("Barriers");
        barriersContainer.transform.SetParent(roomObj.transform, false);
        barriersContainer.transform.localPosition = Vector3.zero;
        
        // Create north wall (+Z)
        CreateBarrier(barriersContainer.transform, new Vector3(0, 0, halfDepth - EDGE_OFFSET), 
            new Vector3(roomScale.x, BARRIER_HEIGHT, BARRIER_THICKNESS), "NorthBarrier");
            
        // Create south wall (-Z)
        CreateBarrier(barriersContainer.transform, new Vector3(0, 0, -halfDepth + EDGE_OFFSET), 
            new Vector3(roomScale.x, BARRIER_HEIGHT, BARRIER_THICKNESS), "SouthBarrier");
            
        // Create east wall (+X)
        CreateBarrier(barriersContainer.transform, new Vector3(halfWidth - EDGE_OFFSET, 0, 0), 
            new Vector3(BARRIER_THICKNESS, BARRIER_HEIGHT, roomScale.z), "EastBarrier");
            
        // Create west wall (-X)
        CreateBarrier(barriersContainer.transform, new Vector3(-halfWidth + EDGE_OFFSET, 0, 0), 
            new Vector3(BARRIER_THICKNESS, BARRIER_HEIGHT, roomScale.z), "WestBarrier");
            
        // Create floor barrier (optional - prevents falling through if needed)
        CreateBarrier(barriersContainer.transform, new Vector3(0, -roomScale.y/2 + EDGE_OFFSET, 0), 
            new Vector3(roomScale.x, BARRIER_THICKNESS, roomScale.z), "FloorBarrier");
            
        Debug.Log($"Added collision barriers to room at {room.position}");
    }
    
    // Create a single barrier
    private GameObject CreateBarrier(Transform parent, Vector3 localPosition, Vector3 size, string name)
    {
        // Instantiate from prefab
        GameObject barrier = Object.Instantiate(barrierPrefab, parent);
        barrier.name = name;
        
        // Position and scale
        barrier.transform.localPosition = localPosition;
        barrier.transform.localScale = size;
        
        return barrier;
    }
    
    // Create the barrier prefab dynamically
    private GameObject CreateBarrierPrefab()
    {
        // Create a simple cube
        GameObject prefab = new GameObject("BarrierPrefab");
        
        // Add box collider
        BoxCollider collider = prefab.AddComponent<BoxCollider>();
        collider.size = Vector3.one;
        collider.isTrigger = false;
        
        // Make it invisible but keep collision
        prefab.layer = LayerMask.NameToLayer("Ignore Raycast");
        
        // Make the original prefab inactive to keep it from being rendered
        prefab.SetActive(false);
        
        // Make sure it doesn't get destroyed between scenes
        Object.DontDestroyOnLoad(prefab);
        
        return prefab;
    }
    
    // Method to remove barriers if needed
    public void RemoveBarriersFromRoom(Room room)
    {
        if (room == null || room.roomObject == null)
            return;
            
        Transform barriersContainer = room.roomObject.transform.Find("Barriers");
        if (barriersContainer != null)
        {
            Object.Destroy(barriersContainer.gameObject);
        }
    }
    
    // Clean up the prefab on system destruction
    public void Cleanup()
    {
        if (barrierPrefab != null)
        {
            Object.Destroy(barrierPrefab);
        }
    }
} 