using UnityEngine;

public class SwipeDetector
{
    private Camera arCamera;
    
    public SwipeDetector(Camera arCamera)
    {
        this.arCamera = arCamera;
    }
    
    public Vector3 ConvertSwipeToWorldDirection(Vector2 screenSwipe, Vector3 surfaceNormal)
    {
        // Project the swipe direction onto the plane defined by the surface normal
        Vector3 cameraForward = arCamera.transform.forward;
        Vector3 cameraRight = arCamera.transform.right;
        Vector3 cameraUp = arCamera.transform.up;

        Vector3 worldSwipe = screenSwipe.x * cameraRight + screenSwipe.y * cameraUp;

        // Project onto the plane
        Vector3 projectedSwipe = Vector3.ProjectOnPlane(worldSwipe, surfaceNormal);

        return projectedSwipe.normalized;
    }
    
    public bool IsPeelingSwipe(Vector3 worldSwipeDir, Vector3 surfaceNormal)
    {
        // Check if we're swiping away from the center (dot product > 0)
        return Vector3.Dot(worldSwipeDir, surfaceNormal) > 0;
    }
} 