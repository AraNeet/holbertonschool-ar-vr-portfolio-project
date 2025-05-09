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

    [Header("UI References")]
    [SerializeField] private GameObject placementIndicator;
    [SerializeField] private GameObject instructionPanel;

    // Private variables
    private GameObject dungeonCube;
    private ARAnchor cubeAnchor;
    private bool isPlaced = false;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

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

        // Show placement instructions initially
        if (instructionPanel != null)
            instructionPanel.SetActive(true);
    }

    void Update()
    {
        // If cube is already placed, skip placement logic
        if (isPlaced)
            return;

        // Update placement indicator if we have planes detected
        UpdatePlacementIndicator();

        // Handle placement tap
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            if (raycastManager.Raycast(Input.GetTouch(0).position, hits, TrackableType.Planes))
            {
                PlaceCube(hits[0].pose);
            }
        }
    }

    private void UpdatePlacementIndicator()
    {
        if (placementIndicator == null)
            return;

        // Center of screen raycast for placement preview
        Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);

        if (raycastManager.Raycast(screenCenter, hits, TrackableType.Planes))
        {
            placementIndicator.SetActive(true);
            placementIndicator.transform.position = hits[0].pose.position;
            placementIndicator.transform.rotation = hits[0].pose.rotation;
        }
        else
        {
            placementIndicator.SetActive(false);
        }
    }

    private void PlaceCube(Pose pose)
    {
        // Adjust pose to be at the desired height from the detected plane
        Vector3 position = pose.position;
        position.y += placementHeight;
        Pose adjustedPose = new Pose(position, pose.rotation);

        // Create anchor
        cubeAnchor = anchorManager.AddAnchor(adjustedPose);

        if (cubeAnchor != null)
        {
            // Instantiate the cube and parent it to the anchor
            dungeonCube = Instantiate(cubePrefab, adjustedPose.position, adjustedPose.rotation);
            dungeonCube.transform.parent = cubeAnchor.transform;
            dungeonCube.transform.localScale = Vector3.one * cubeSize;

            // Hide planes after placement for cleaner view
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(false);
            }
            planeManager.enabled = false;

            // Hide placement indicator and instructions
            if (placementIndicator != null)
                placementIndicator.SetActive(false);

            if (instructionPanel != null)
                instructionPanel.SetActive(false);

            isPlaced = true;

            // Initialize the dungeon inside the cube
            InitializeDungeon();
        }
    }

    // Reset the cube if needed
    public void ResetCube()
    {
        if (cubeAnchor != null)
        {
            Destroy(cubeAnchor.gameObject);
        }

        if (dungeonCube != null)
        {
            Destroy(dungeonCube);
        }

        // Re-enable plane detection
        planeManager.enabled = true;
        foreach (var plane in planeManager.trackables)
        {
            plane.gameObject.SetActive(true);
        }

        if (instructionPanel != null)
            instructionPanel.SetActive(true);

        isPlaced = false;
    }

    private void InitializeDungeon()
    {
        // Get reference to the dungeon generator component
        DungeonGenerator dungeonGen = dungeonCube.GetComponent<DungeonGenerator>();
        if (dungeonGen != null)
        {
            dungeonGen.GenerateDungeon();
        }
    }
}
