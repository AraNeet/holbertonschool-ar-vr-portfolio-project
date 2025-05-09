using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class DungeonCubeManager : MonoBehaviour
{
    [Header("AR Components")]
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARAnchorManager anchorManager;
    [SerializeField] private ARPlaneManager planeManager;

    [Header("XR References")]
    [SerializeField] private Camera xrCamera;

    [Header("Cube Properties")]
    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private float cubeSize = 1.0f;
    [SerializeField] private float placementHeight = 1.0f; // Height above ground

    [Header("Player Settings")]
    [SerializeField] private GameObject playerPrefab; // Reference to player prefab
    [SerializeField] private float playerScale = 0.15f; // Scale factor for the player (15% of original size)

    [Header("UI References")]
    [SerializeField] private GameObject placementIndicator;
    [SerializeField] private GameObject instructionPanel;

    // Component classes
    private ARPlacementManager arPlacementManager;
    private CubeSpawner cubeSpawner;
    private PlayerSpawner playerSpawner;
    private UIManager uiManager;
    private DungeonInitializer dungeonInitializer;

    // State variables
    private GameObject dungeonCube;
    private ARAnchor cubeAnchor;
    private bool isPlaced = false;

    void Start()
    {
        // Auto-find components if not assigned
        if (xrCamera == null)
            xrCamera = Camera.main;

        if (raycastManager == null)
            raycastManager = FindObjectOfType<ARRaycastManager>();

        if (anchorManager == null)
            anchorManager = FindObjectOfType<ARAnchorManager>();

        if (planeManager == null)
            planeManager = FindObjectOfType<ARPlaneManager>();

        // Initialize component classes
        InitializeComponents();

        // Show placement instructions initially
        uiManager.ShowPlacementUI();
    }

    private void InitializeComponents()
    {
        arPlacementManager = new ARPlacementManager(raycastManager, anchorManager, planeManager, xrCamera, placementIndicator);
        cubeSpawner = new CubeSpawner(cubePrefab, cubeSize);
        playerSpawner = new PlayerSpawner(playerPrefab, playerScale);
        uiManager = new UIManager(placementIndicator, instructionPanel);
        dungeonInitializer = new DungeonInitializer();
    }

    void Update()
    {
        // If cube is already placed, skip placement logic
        if (isPlaced)
            return;

        // Update placement indicator if we have planes detected
        arPlacementManager.UpdatePlacementIndicator();

        // Handle placement tap
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            TryPlaceCube(Input.GetTouch(0).position);
        }
#if UNITY_EDITOR
        // Mouse input for editor testing
        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceCube(Input.mousePosition);
        }
#endif
    }

    private void TryPlaceCube(Vector2 screenPosition)
    {
        Pose pose;
        if (arPlacementManager.TryGetPlacementPose(screenPosition, out pose))
        {
            PlaceCube(pose);
        }
    }

    private void PlaceCube(Pose pose)
    {
        // Adjust pose to be at the desired height from the detected plane
        Vector3 position = pose.position;
        position.y += placementHeight;
        Pose adjustedPose = new Pose(position, pose.rotation);

        // Create anchor
        cubeAnchor = arPlacementManager.CreateAnchor(adjustedPose);

        if (cubeAnchor != null)
        {
            // Instantiate the cube and parent it to the anchor
            dungeonCube = cubeSpawner.SpawnCube(adjustedPose, cubeAnchor.transform);

            // Hide planes and UI
            arPlacementManager.HidePlanes();
            uiManager.HidePlacementUI();

            isPlaced = true;

            // Initialize the dungeon inside the cube
            dungeonInitializer.InitializeDungeon(dungeonCube, playerSpawner);
        }
    }

    // Reset the cube if needed
    public void ResetCube()
    {
        if (cubeAnchor != null)
        {
            Destroy(cubeAnchor.gameObject);
            cubeAnchor = null;
        }

        if (dungeonCube != null)
        {
            cubeSpawner.DestroyCube(dungeonCube);
            dungeonCube = null;
        }

        // Clean up player instance
        playerSpawner.DestroyPlayer();

        // Re-enable plane detection and UI
        arPlacementManager.ShowPlanes();
        uiManager.ShowPlacementUI();

        isPlaced = false;
    }
}
