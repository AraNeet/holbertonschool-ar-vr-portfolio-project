using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class AreaManager : MonoBehaviour
{
    [Header("AR Components")]
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private Camera arCamera;

    [Header("Prefabs")]
    [SerializeField] private GameObject pointPrefab;
    [SerializeField] private LineRenderer rayVisualizer;

    [Header("UI Elements")]
    [SerializeField] private Text statusText;
    [SerializeField] private Button resetButton;

    // Runtime variables
    private List<GameObject> placedPoints = new List<GameObject>();
    private GameObject createdPlane;
    private bool isPlacingPoints = true;

    void Start()
    {
        // Initialize components
        if (raycastManager == null)
            raycastManager = FindObjectOfType<ARRaycastManager>();

        if (arCamera == null)
            arCamera = Camera.main;

        if (resetButton != null)
            resetButton.onClick.AddListener(Reset);

        // Initialize ray visualizer if present
        if (rayVisualizer != null)
        {
            rayVisualizer.positionCount = 2;
            rayVisualizer.enabled = false;
        }

        UpdateStatusText();
    }

    void Update()
    {
        if (isPlacingPoints)
        {
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                PlacePointUsingScreenPosition(Input.GetTouch(0).position);
            }
            // Allow placing points with mouse click for editor testing
#if UNITY_EDITOR
            else if (Input.GetMouseButtonDown(0))
            {
                PlacePointUsingScreenPosition(Input.mousePosition);
            }
#endif

            // After placing 4 points, create the plane
            if (placedPoints.Count >= 4)
            {
                CreatePlane();
                isPlacingPoints = false;
            }

            UpdateStatusText();
        }
    }

    private void PlacePointUsingScreenPosition(Vector2 screenPosition)
    {
        List<ARRaycastHit> hits = new List<ARRaycastHit>();

        // Show ray visualization for debugging
        if (rayVisualizer != null)
        {
            ShowRayVisualizer(screenPosition);
        }

        if (raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            // Get the first hit
            ARRaycastHit hit = hits[0];

            // Debug visualization in Scene view
            Debug.DrawRay(arCamera.transform.position,
                         arCamera.ScreenPointToRay(screenPosition).direction * 10,
                         Color.red, 3f);

            // Create the point exactly at hit position
            GameObject point = Instantiate(pointPrefab, hit.pose.position, hit.pose.rotation);
            
            // Set scale to 0.5
            point.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            // Add visual feedback
            StartCoroutine(PulsePointOnCreation(point));

            placedPoints.Add(point);

            // Log debug info
            Debug.Log($"Point {placedPoints.Count} placed at: {hit.pose.position}. " +
                     $"Distance from camera: {Vector3.Distance(arCamera.transform.position, hit.pose.position):F2}m");
        }
        else
        {
            // Provide feedback when no plane is detected
            Debug.Log("No plane detected at touch position");

            if (statusText != null)
            {
                string currentText = statusText.text;
                statusText.text = "No surface detected. Try pointing at a flat surface.";
                StartCoroutine(ResetTextAfterDelay(currentText, 2f));
            }
        }
    }

    private IEnumerator PulsePointOnCreation(GameObject point)
    {
        // Store original scale
        Vector3 originalScale = point.transform.localScale;
        point.transform.localScale = originalScale * 1.5f;

        float duration = 0.3f;
        float elapsed = 0;

        // Animate scale down
        while (elapsed < duration)
        {
            point.transform.localScale = Vector3.Lerp(
                originalScale * 1.5f,
                originalScale,
                elapsed / duration
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        point.transform.localScale = originalScale;
    }

    private void ShowRayVisualizer(Vector2 touchPosition)
    {
        Ray ray = arCamera.ScreenPointToRay(touchPosition);
        rayVisualizer.SetPosition(0, ray.origin);
        rayVisualizer.SetPosition(1, ray.origin + ray.direction * 10);
        rayVisualizer.enabled = true;

        // Disable after short delay
        StartCoroutine(DisableRayAfterDelay(0.5f));
    }

    private IEnumerator DisableRayAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        rayVisualizer.enabled = false;
    }

    private void CreatePlane()
    {
        if (placedPoints.Count < 4) return;

        // Calculate center position
        Vector3 center = Vector3.zero;
        foreach (GameObject point in placedPoints)
        {
            center += point.transform.position;
        }
        center /= placedPoints.Count;

        // Create plane primitive instead of using prefab
        createdPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        createdPlane.transform.position = center;
        
        // Set plane material (optional - can be customized)
        Renderer renderer = createdPlane.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Set a semi-transparent material
            Material material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.0f, 0.5f, 1.0f, 0.5f); // Semi-transparent blue
            material.SetFloat("_Mode", 3); // Transparent mode
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            renderer.material = material;
        }

        // Calculate dimensions and orientation
        CalculatePlaneTransform();

        if (statusText != null)
        {
            statusText.text = "Area created! Tap Reset to create another.";
        }
    }

    private void CalculatePlaneTransform()
    {
        // Assuming the points form a quadrilateral in 3D space
        // This approach works with rectangular or trapezoidal formations

        Vector3 p0 = placedPoints[0].transform.position;
        Vector3 p1 = placedPoints[1].transform.position;
        Vector3 p2 = placedPoints[2].transform.position;
        Vector3 p3 = placedPoints[3].transform.position;

        // Calculate center position more accurately
        Vector3 center = (p0 + p1 + p2 + p3) / 4f;
        createdPlane.transform.position = center;

        // Calculate normal vector (plane orientation)
        Vector3 normal = Vector3.Cross(p1 - p0, p3 - p0).normalized;

        // Handle case when points are coplanar and normal is zero
        if (normal.magnitude < 0.001f)
        {
            // Try different edges
            normal = Vector3.Cross(p2 - p1, p0 - p1).normalized;
        }

        // If still zero, default to up
        if (normal.magnitude < 0.001f)
        {
            normal = Vector3.up;
        }

        // Determine orientation
        Vector3 forward = (p1 - p0).normalized;
        Vector3 up = normal;
        Vector3 right = Vector3.Cross(up, forward).normalized;
        forward = Vector3.Cross(right, up).normalized;

        // Set rotation
        Quaternion rotation = Quaternion.LookRotation(forward, up);
        createdPlane.transform.rotation = rotation;

        // Calculate dimensions by projecting points onto the plane
        float maxWidth = 0f;
        float maxHeight = 0f;

        // Project each point to the plane's local space
        Vector3[] localPoints = new Vector3[4];
        localPoints[0] = createdPlane.transform.InverseTransformPoint(p0);
        localPoints[1] = createdPlane.transform.InverseTransformPoint(p1);
        localPoints[2] = createdPlane.transform.InverseTransformPoint(p2);
        localPoints[3] = createdPlane.transform.InverseTransformPoint(p3);

        // Find the min/max bounds in local space
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        foreach (Vector3 pt in localPoints)
        {
            minX = Mathf.Min(minX, pt.x);
            maxX = Mathf.Max(maxX, pt.x);
            minZ = Mathf.Min(minZ, pt.z);
            maxZ = Mathf.Max(maxZ, pt.z);
        }

        // Calculate width and height based on local bounds
        float width = maxX - minX;
        float height = maxZ - minZ;

        // Adjust center position to be in the middle of the local bounds
        Vector3 localCenter = new Vector3((minX + maxX) / 2f, 0, (minZ + maxZ) / 2f);
        Vector3 worldCenterOffset = createdPlane.transform.TransformPoint(localCenter) - createdPlane.transform.position;
        createdPlane.transform.position += worldCenterOffset;

        // Apply scale (Unity plane is 10x10 units by default)
        createdPlane.transform.localScale = new Vector3(width / 10f, 1f, height / 10f);

        // Log results for debugging
        Debug.Log($"Created plane with dimensions: {width:F2}m x {height:F2}m");
    }

    private void UpdateStatusText()
    {
        if (statusText != null)
        {
            if (isPlacingPoints)
            {
                int remainingPoints = 4 - placedPoints.Count;
                statusText.text = $"Tap to place corner {placedPoints.Count + 1}/4";

                if (placedPoints.Count > 0)
                {
                    statusText.text += $" ({remainingPoints} remaining)";
                }
            }
            else
            {
                statusText.text = "Area created!";
            }
        }
    }

    private IEnumerator ResetTextAfterDelay(string originalText, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (statusText != null)
        {
            statusText.text = originalText;
        }
    }

    public void Reset()
    {
        // Destroy all placed points
        foreach (GameObject point in placedPoints)
        {
            Destroy(point);
        }

        // Destroy created plane
        if (createdPlane != null)
        {
            Destroy(createdPlane);
        }

        // Reset state
        placedPoints.Clear();
        isPlacingPoints = true;

        UpdateStatusText();
        Debug.Log("Area Manager Reset");
    }

    // Utility method to check plane detection status
    public void LogPlaneDetectionStatus()
    {
        if (planeManager == null) return;

        int planeCount = 0;
        foreach (var plane in planeManager.trackables)
        {
            planeCount++;
        }

        Debug.Log($"AR Plane Detection: {planeCount} planes detected");
    }
}

