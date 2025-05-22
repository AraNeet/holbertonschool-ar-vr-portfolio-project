using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

/// <summary>
/// Debug UI helper for AR applications that shows plane detection status
/// and other helpful AR information on screen.
/// </summary>
public class ARDebugUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button placeButton;
    [SerializeField] private Button emergencyPlaceButton;

    [Header("AR References")]
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private ARRaycastManager raycastManager;

    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private bool showCoordinates = true;

    private float timer;
    private Camera arCamera;

    private void Start()
    {
        if (statusText == null)
        {
            Debug.LogError("Status text not assigned to ARDebugUI!");
            enabled = false;
            return;
        }

        arCamera = Camera.main;

        // Set up manual placement button if provided
        if (placeButton != null)
        {
            placeButton.onClick.AddListener(ManualPlacement);
        }

        // Set up emergency placement button if provided
        if (emergencyPlaceButton != null)
        {
            emergencyPlaceButton.onClick.AddListener(EmergencyPlacement);
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer < updateInterval) return;
        timer = 0;

        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        if (statusText == null) return;

        string status = "AR Debug Info:\n";

        // Basic AR State
        status += $"AR Enabled: {ARSession.state != ARSessionState.None}\n";

        // Plane detection
        if (planeManager != null)
        {
            status += $"Plane Detection: {(planeManager.enabled ? "Enabled" : "Disabled")}\n";
            status += $"Planes Detected: {planeManager.trackables.count}\n";

            int horizontal = 0, vertical = 0;
            foreach (var plane in planeManager.trackables)
            {
                if (plane.alignment == PlaneAlignment.HorizontalUp || plane.alignment == PlaneAlignment.HorizontalDown)
                    horizontal++;
                else if (plane.alignment == PlaneAlignment.Vertical)
                    vertical++;
            }
            status += $"Horizontal: {horizontal}, Vertical: {vertical}\n";
        }
        else
        {
            status += "Plane Manager: Not assigned\n";
        }

        // Camera info
        if (arCamera != null && showCoordinates)
        {
            status += $"Camera Pos: {arCamera.transform.position.ToString("F2")}\n";
        }

        // Touch info
        status += $"Touch Count: {Input.touchCount}\n";
        if (Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);
            status += $"Touch Pos: {touch.position}, Phase: {touch.phase}\n";
        }

        statusText.text = status;
    }

    private void ManualPlacement()
    {
        if (raycastManager == null || Camera.main == null) return;

        // Raycast from center of screen
        Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);

        Debug.Log($"Manual placement attempt at screen center: {screenCenter}");

        // Find GameManager and try to place
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            Debug.Log("Trying manual placement through GameManager");
            gameManager.TryPlaceGame(screenCenter);
            Debug.Log("Manual placement method called");
        }
        else
        {
            Debug.LogError("GameManager not found for manual placement");
        }
    }

    private void EmergencyPlacement()
    {
        Debug.Log("Emergency placement attempt");

        // Find GameManager and try emergency placement
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            Debug.Log("Trying emergency placement through GameManager");
            gameManager.PlaceGameAtCameraPosition();
            Debug.Log("Emergency placement method called");
        }
        else
        {
            Debug.LogError("GameManager not found for emergency placement");
        }
    }
}