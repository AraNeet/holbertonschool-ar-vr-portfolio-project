using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeLayerPeeler : MonoBehaviour
{
    [Header("Layer References")]
    [SerializeField] private List<GameObject> cubeLayers = new List<GameObject>();

    [Header("Peeling Settings")]
    [SerializeField] private float peelDuration = 0.3f;
    [SerializeField] private float maxPeelDistance = 0.5f;

    // Internal state
    private Dictionary<GameObject, Vector3> originalPositions = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, Vector3> peelDirections = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, Coroutine> activeCoroutines = new Dictionary<GameObject, Coroutine>();

    private void Start()
    {
        // Store original positions
        foreach (var layer in cubeLayers)
        {
            originalPositions[layer] = layer.transform.localPosition;
        }
    }

    public void PeelLayer(int layerIndex, Vector3 peelDirection)
    {
        if (layerIndex < 0 || layerIndex >= cubeLayers.Count)
            return;

        GameObject layer = cubeLayers[layerIndex];

        // Store the peel direction
        peelDirections[layer] = peelDirection.normalized;

        // If a coroutine is already active for this layer, stop it
        if (activeCoroutines.ContainsKey(layer) && activeCoroutines[layer] != null)
        {
            StopCoroutine(activeCoroutines[layer]);
        }

        // Start the peeling coroutine
        activeCoroutines[layer] = StartCoroutine(AnimatePeel(layer, true));
    }

    public void UnpeelLayer(int layerIndex)
    {
        if (layerIndex < 0 || layerIndex >= cubeLayers.Count)
            return;

        GameObject layer = cubeLayers[layerIndex];

        // If a coroutine is already active for this layer, stop it
        if (activeCoroutines.ContainsKey(layer) && activeCoroutines[layer] != null)
        {
            StopCoroutine(activeCoroutines[layer]);
        }

        // Start the unpeeling coroutine
        activeCoroutines[layer] = StartCoroutine(AnimatePeel(layer, false));
    }

    private IEnumerator AnimatePeel(GameObject layer, bool isPeel)
    {
        Vector3 startPos = layer.transform.localPosition;
        Vector3 endPos;

        if (isPeel)
        {
            // Peel away from center
            endPos = originalPositions[layer] + peelDirections[layer] * maxPeelDistance;
        }
        else
        {
            // Return to original position
            endPos = originalPositions[layer];
        }

        float elapsedTime = 0;

        while (elapsedTime < peelDuration)
        {
            layer.transform.localPosition = Vector3.Lerp(startPos, endPos, elapsedTime / peelDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure final position is exact
        layer.transform.localPosition = endPos;

        // Clear the active coroutine reference
        activeCoroutines[layer] = null;
    }

    // Helper method to detect swipe direction and trigger peeling
    public void DetectSwipeAndPeel(Vector2 startPos, Vector2 endPos, Camera arCamera)
    {
        Vector2 swipeDirection = (endPos - startPos).normalized;

        // Determine which layer to peel based on camera view
        Ray ray = arCamera.ScreenPointToRay(startPos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            // Determine which face was hit
            int layerIndex = DetermineCubeFace(hit);

            if (layerIndex >= 0)
            {
                // Convert screen swipe to world space direction
                Vector3 worldSwipeDir = ConvertSwipeToWorldDirection(swipeDirection, arCamera, hit.normal);

                // Check if we're swiping away from the center
                if (Vector3.Dot(worldSwipeDir, hit.normal) > 0)
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

    private int DetermineCubeFace(RaycastHit hit)
    {
        // Determine which layer was hit by comparing gameObjects
        for (int i = 0; i < cubeLayers.Count; i++)
        {
            if (hit.transform.gameObject == cubeLayers[i])
                return i;
        }

        // Alternative approach using normal vector
        // This is a backup in case direct object comparison fails
        Vector3 normal = hit.normal;

        if (Vector3.Dot(normal, Vector3.up) > 0.7f)
            return cubeLayers.IndexOf(cubeLayers.Find(layer => layer.name == "TopFace"));
        if (Vector3.Dot(normal, Vector3.down) > 0.7f)
            return cubeLayers.IndexOf(cubeLayers.Find(layer => layer.name == "BottomFace"));
        if (Vector3.Dot(normal, Vector3.forward) > 0.7f)
            return cubeLayers.IndexOf(cubeLayers.Find(layer => layer.name == "FrontFace"));
        if (Vector3.Dot(normal, Vector3.back) > 0.7f)
            return cubeLayers.IndexOf(cubeLayers.Find(layer => layer.name == "BackFace"));
        if (Vector3.Dot(normal, Vector3.right) > 0.7f)
            return cubeLayers.IndexOf(cubeLayers.Find(layer => layer.name == "RightFace"));
        if (Vector3.Dot(normal, Vector3.left) > 0.7f)
            return cubeLayers.IndexOf(cubeLayers.Find(layer => layer.name == "LeftFace"));

        return -1; // No face detected
    }

    private Vector3 ConvertSwipeToWorldDirection(Vector2 screenSwipe, Camera cam, Vector3 surfaceNormal)
    {
        // Project the swipe direction onto the plane defined by the surface normal
        Vector3 cameraForward = cam.transform.forward;
        Vector3 cameraRight = cam.transform.right;
        Vector3 cameraUp = cam.transform.up;

        Vector3 worldSwipe = screenSwipe.x * cameraRight + screenSwipe.y * cameraUp;

        // Project onto the plane
        Vector3 projectedSwipe = Vector3.ProjectOnPlane(worldSwipe, surfaceNormal);

        return projectedSwipe.normalized;
    }
}
