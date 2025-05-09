using System.Collections.Generic;
using UnityEngine;


public class DungeonGenerator : MonoBehaviour
{
    [Header("Dungeon Settings")]
    [SerializeField] private int gridSize = 5; // 5x5x5 grid
    [SerializeField] private float cellSize = 0.2f; // Each cell size
    [SerializeField] private GameObject roomPrefab;
    [SerializeField] private GameObject corridorPrefab;
    [SerializeField] private GameObject verticalCorridorPrefab; // Dedicated prefab for vertical corridors
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject treasurePrefab;

    [Header("Generation Parameters")]
    [SerializeField] private int minRooms = 10;
    [SerializeField] private int maxRooms = 20;
    [SerializeField] private float enemySpawnChance = 0.3f;
    [SerializeField] private float treasureSpawnChance = 0.2f;
    [SerializeField] private bool enableRoomBarriers = true; // Added toggle for room barriers

    [Header("Debug")]
    [Tooltip("Enable to show debug visualization")]
    [SerializeField] private bool debug = false; // Enable debug visualization
    [SerializeField] private Material lineRendererMaterial; // Material for the line renderers
    [SerializeField] private float lineWidth = 0.01f; // Width of debug lines

    // Component classes
    private GridManager gridManager;
    private RoomGenerator roomGenerator;
    private CorridorGenerator corridorGenerator;
    private DungeonPopulator dungeonPopulator;
    private DebugVisualizer debugVisualizer;
    private RoomCollisionHandler roomCollisionHandler; // Added room collision handler

    // Cached objects
    private Transform dungeonContainer;

    public void GenerateDungeon()
    {
        // Initialize components
        InitializeComponents();

        // Clear existing dungeon
        ClearExistingDungeon();

        // Find or create the dungeon container
        EnsureDungeonContainer();

        // Generate random rooms
        int roomCount = Random.Range(minRooms, maxRooms + 1);

        // Start with a room in the center
        Room centerRoom = roomGenerator.PlaceRoomAtCenter(dungeonContainer);
        
        // Add barriers to the center room if enabled
        if (enableRoomBarriers)
        {
            roomCollisionHandler.AddBarriersToRoom(centerRoom);
        }

        // Add remaining rooms
        for (int i = 1; i < roomCount; i++)
        {
            Room newRoom = roomGenerator.TryPlaceRoom(dungeonContainer);
            
            // Add barriers to the new room if enabled and room was successfully created
            if (enableRoomBarriers && newRoom != null)
            {
                roomCollisionHandler.AddBarriersToRoom(newRoom);
            }
        }

        // Connect rooms with corridors
        corridorGenerator.ConnectRooms(roomGenerator.GetRooms(), dungeonContainer);

        // Add enemies and treasures
        dungeonPopulator.PopulateDungeon(roomGenerator.GetRooms());

        // Create debug visualization if enabled
        if (debug)
        {
            debugVisualizer.CreateDebugVisuals();
        }
    }

    private void InitializeComponents()
    {
        gridManager = new GridManager(gridSize, cellSize);
        roomGenerator = new RoomGenerator(gridManager, roomPrefab);
        corridorGenerator = new CorridorGenerator(gridManager, corridorPrefab, verticalCorridorPrefab);
        dungeonPopulator = new DungeonPopulator(enemyPrefab, treasurePrefab, enemySpawnChance, treasureSpawnChance);
        debugVisualizer = new DebugVisualizer(transform, gridSize, cellSize, gridManager.GetOccupiedCells(), lineRendererMaterial, lineWidth);
        roomCollisionHandler = new RoomCollisionHandler(); // Initialize the room collision handler
    }

    private void ClearExistingDungeon()
    {
        // Clean up the room collision handler to prevent memory leaks
        if (roomCollisionHandler != null)
        {
            roomCollisionHandler.Cleanup();
        }
        
        // Clear any existing rooms
        if (roomGenerator != null)
        {
            roomGenerator.ClearRooms();
        }

        // Clear debug visuals
        if (debugVisualizer != null)
        {
            debugVisualizer.ClearDebugVisuals();
        }
    }

    private void EnsureDungeonContainer()
    {
        // Find the dungeon container
        dungeonContainer = transform.Find("DungeonContainer");
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
    }

    private void OnDrawGizmos()
    {
        // Keep this for editor visualization, but the primary visualization will be LineRenderers
        if (!debug || Application.isPlaying)
            return;

        // If we have a debug visualizer, use it for gizmos
        if (debugVisualizer != null)
        {
            debugVisualizer.DrawDebugGrid();
        }
    }

    private void Update()
    {
        // Update debug visuals based on debug flag
        if (debugVisualizer != null)
        {
            if (debug && !debugVisualizer.IsDebugVisualsCreated() && gridManager != null)
            {
                debugVisualizer.UpdateOccupiedCells(gridManager.GetOccupiedCells());
                debugVisualizer.CreateDebugVisuals();
            }
            else if (!debug && debugVisualizer.IsDebugVisualsCreated())
            {
                debugVisualizer.ClearDebugVisuals();
            }
        }
    }

    // Add a public method to get the first room's position
    public Vector3 GetFirstRoomPosition()
    {
        if (roomGenerator != null && roomGenerator.GetRooms().Count > 0)
        {
            // Return the world position of the first room
            return roomGenerator.GetRooms()[0].roomObject.transform.position;
        }

        // Default position if no rooms exist
        return transform.position;
    }
    
    // Toggle debug visualization
    public void ToggleDebugVisualization()
    {
        debug = !debug;
        
        // Update debug visuals based on new state
        if (debugVisualizer != null)
        {
            if (debug && !debugVisualizer.IsDebugVisualsCreated() && gridManager != null)
            {
                debugVisualizer.UpdateOccupiedCells(gridManager.GetOccupiedCells());
                debugVisualizer.CreateDebugVisuals();
            }
            else if (!debug && debugVisualizer.IsDebugVisualsCreated())
            {
                debugVisualizer.ClearDebugVisuals();
            }
        }
    }
    
    // Clean up when the object is destroyed
    private void OnDestroy()
    {
        // Clean up the room collision handler
        if (roomCollisionHandler != null)
        {
            roomCollisionHandler.Cleanup();
        }
    }
}
