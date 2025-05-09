using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class CubeInteractionHandler : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float rotationSpeed = 0.5f;
    [SerializeField] private float minScale = 0.3f;
    [SerializeField] private float maxScale = 2.0f;
    [SerializeField] private float scaleSpeed = 0.01f;

    // Internal state variables
    private bool isRotating = false;
    private bool isScaling = false;
    private Vector2 lastTouchPosition;
    private float initialTouchDistance;
    private Vector3 originalScale;
    private Quaternion targetRotation;

    private void Start()
    {
        originalScale = transform.localScale;
        targetRotation = transform.rotation;
    }

    private void Update()
    {
        // Handle rotation
        HandleRotation();

        // Handle scaling
        HandleScaling();

        // Smooth movement towards target rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
    }

    private void HandleRotation()
    {
        // Single touch rotation
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    isRotating = true;
                    lastTouchPosition = touch.position;
                    break;

                case TouchPhase.Moved:
                    if (isRotating && !isScaling)
                    {
                        Vector2 delta = touch.position - lastTouchPosition;

                        // Apply rotation based on touch movement
                        targetRotation *= Quaternion.Euler(
                            -delta.y * rotationSpeed,
                            delta.x * rotationSpeed,
                            0);

                        lastTouchPosition = touch.position;
                    }
                    break;

                case TouchPhase.Ended:
                    isRotating = false;
                    break;
            }
        }
    }

    private void HandleScaling()
    {
        // Two-finger pinch to scale
        if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
            {
                isScaling = true;
                isRotating = false;
                initialTouchDistance = Vector2.Distance(touch0.position, touch1.position);
            }

            if (isScaling && (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved))
            {
                float currentTouchDistance = Vector2.Distance(touch0.position, touch1.position);
                float scaleFactor = currentTouchDistance / initialTouchDistance;

                // Calculate new scale
                Vector3 newScale = originalScale * scaleFactor;

                // Clamp scale to min/max
                newScale = Vector3.ClampMagnitude(newScale, maxScale);
                if (newScale.magnitude < minScale)
                {
                    newScale = newScale.normalized * minScale;
                }

                // Apply new scale
                transform.localScale = Vector3.Lerp(transform.localScale, newScale, scaleSpeed);

                // Update for the next frame
                initialTouchDistance = currentTouchDistance;
            }

            if (touch0.phase == TouchPhase.Ended || touch1.phase == TouchPhase.Ended)
            {
                isScaling = false;
                originalScale = transform.localScale;
            }
        }
    }

    // Method for external rotation control (e.g., from swipe gestures)
    public void HandleManualRotation(Vector2 delta)
    {
        targetRotation *= Quaternion.Euler(
            -delta.y * rotationSpeed * 0.1f,
            delta.x * rotationSpeed * 0.1f,
            0);
    }
}
