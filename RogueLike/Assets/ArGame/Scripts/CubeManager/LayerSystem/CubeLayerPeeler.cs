using System.Collections.Generic;
using UnityEngine;

public class CubeLayerPeeler : MonoBehaviour
{
    [Header("Layer References")]
    [SerializeField] private List<GameObject> cubeLayers = new List<GameObject>();

    [Header("Peeling Settings")]
    [SerializeField] private float peelDuration = 0.3f;
    [SerializeField] private float maxPeelDistance = 0.5f;

    // Component classes
    private List<CubeLayer> layers = new List<CubeLayer>();
    private LayerAnimator layerAnimator;
    private SwipeDetector swipeDetector;
    private CubeFaceDetector faceDetector;

    private void Start()
    {
        // Initialize components
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        // Initialize cube layers
        foreach (var layerObject in cubeLayers)
        {
            layers.Add(new CubeLayer(layerObject));
        }

        // Initialize other components
        layerAnimator = new LayerAnimator(this, peelDuration, maxPeelDistance);
        swipeDetector = new SwipeDetector(Camera.main); // Using main camera by default
        faceDetector = new CubeFaceDetector(layers);
    }

    public void PeelLayer(int layerIndex, Vector3 peelDirection)
    {
        if (layerIndex < 0 || layerIndex >= layers.Count)
            return;

        CubeLayer layer = layers[layerIndex];

        // Store the peel direction
        layer.PeelDirection = peelDirection.normalized;

        // Start the peeling animation
        layer.ActiveCoroutine = layerAnimator.StartPeelAnimation(layer, true);
    }

    public void UnpeelLayer(int layerIndex)
    {
        if (layerIndex < 0 || layerIndex >= layers.Count)
            return;

        CubeLayer layer = layers[layerIndex];

        // Start the unpeeling animation
        layer.ActiveCoroutine = layerAnimator.StartPeelAnimation(layer, false);
    }

    // Helper method to detect swipe direction and trigger peeling
    public void DetectSwipeAndPeel(Vector2 startPos, Vector2 endPos, Camera arCamera)
    {
        // Update the swipe detector with the current camera
        swipeDetector = new SwipeDetector(arCamera);
        
        Vector2 swipeDirection = (endPos - startPos).normalized;

        // Determine which layer to peel based on camera view
        Ray ray = arCamera.ScreenPointToRay(startPos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            // Determine which face was hit
            int layerIndex = faceDetector.DetermineCubeFace(hit);

            if (layerIndex >= 0)
            {
                // Convert screen swipe to world space direction
                Vector3 worldSwipeDir = swipeDetector.ConvertSwipeToWorldDirection(swipeDirection, hit.normal);

                // Check if we're swiping away from the center
                if (swipeDetector.IsPeelingSwipe(worldSwipeDir, hit.normal))
                {
                    PeelLayer(layerIndex, worldSwipeDir);
                }
                else
                {
                    UnpeelLayer(layerIndex);
                }
            }
        }
    }
}
