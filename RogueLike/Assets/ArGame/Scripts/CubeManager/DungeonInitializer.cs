using UnityEngine;

public class DungeonInitializer
{
    // Added constant for spawn height adjustment
    private const float SPAWN_HEIGHT_OFFSET = 0.05f; // Offset above room floor

    public void InitializeDungeon(GameObject dungeonCube, PlayerSpawner playerSpawner)
    {
        if (dungeonCube == null)
            return;
            
        // Get reference to the dungeon generator component
        DungeonGenerator dungeonGen = dungeonCube.GetComponent<DungeonGenerator>();
        if (dungeonGen != null)
        {
            // Generate the dungeon
            dungeonGen.GenerateDungeon();
            
            // Spawn player in the first room after dungeon is generated
            if (playerSpawner != null)
            {
                // Get spawn position
                Vector3 spawnPosition = GetSafeSpawnPosition(dungeonGen, dungeonCube.transform);
                
                // Log spawn position for debugging
                Debug.Log($"Spawning player at position: {spawnPosition} (local to cube: {dungeonCube.transform.InverseTransformPoint(spawnPosition)})");
                
                // Spawn player with appropriate scale
                playerSpawner.SpawnPlayer(spawnPosition, dungeonCube.transform);
                
                // Draw debug sphere at spawn point
                DrawDebugSphere(spawnPosition, 0.05f, Color.green, 5f);
            }
        }
        else
        {
            Debug.LogError("DungeonGenerator component not found on the cube!");
        }
    }
    
    private Vector3 GetSafeSpawnPosition(DungeonGenerator dungeonGen, Transform cubeTransform)
    {
        // Get the base position from the dungeon generator
        Vector3 basePosition = dungeonGen.GetFirstRoomPosition();
        
        // Add a small vertical offset to ensure player spawns above the floor
        basePosition.y += SPAWN_HEIGHT_OFFSET;
        
        // Ensure position is inside the cube's bounds
        Bounds cubeBounds = GetCubeBounds(cubeTransform);
        
        // Clamp the position to be within the cube bounds with a small margin
        float margin = 0.1f;
        basePosition.x = Mathf.Clamp(basePosition.x, 
            cubeBounds.min.x + margin, 
            cubeBounds.max.x - margin);
        basePosition.y = Mathf.Clamp(basePosition.y, 
            cubeBounds.min.y + margin, 
            cubeBounds.max.y - margin);
        basePosition.z = Mathf.Clamp(basePosition.z, 
            cubeBounds.min.z + margin, 
            cubeBounds.max.z - margin);
            
        return basePosition;
    }
    
    private Bounds GetCubeBounds(Transform cubeTransform)
    {
        // If the cube has a collider, use that for bounds
        Collider cubeCollider = cubeTransform.GetComponent<Collider>();
        if (cubeCollider != null)
        {
            return cubeCollider.bounds;
        }
        
        // Otherwise use renderer bounds
        Renderer cubeRenderer = cubeTransform.GetComponent<Renderer>();
        if (cubeRenderer != null)
        {
            return cubeRenderer.bounds;
        }
        
        // Fallback: create bounds based on transform
        Bounds bounds = new Bounds(cubeTransform.position, Vector3.one);
        return bounds;
    }
    
    private void DrawDebugSphere(Vector3 position, float radius, Color color, float duration)
    {
        // Create temporary sphere to visualize spawn point
        GameObject debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        debugSphere.transform.position = position;
        debugSphere.transform.localScale = Vector3.one * radius * 2; // diameter = radius * 2
        
        // Set color
        Renderer renderer = debugSphere.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
        
        // Remove collider to prevent physics interactions
        Collider collider = debugSphere.GetComponent<Collider>();
        if (collider != null)
        {
            Object.Destroy(collider);
        }
        
        // Destroy after duration
        Object.Destroy(debugSphere, duration);
    }
} 