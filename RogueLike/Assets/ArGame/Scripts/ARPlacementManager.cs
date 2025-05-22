using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPlacementManager
{
    private ARRaycastManager raycastManager;
    private ARAnchorManager anchorManager;
    private ARPlaneManager planeManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private float gameSize = 5.0f;
    private bool debugMode = true; // Enable debug logs

    public ARPlacementManager(ARRaycastManager raycastManager, ARAnchorManager anchorManager, ARPlaneManager planeManager)
    {
        this.raycastManager = raycastManager;
        this.anchorManager = anchorManager;
        this.planeManager = planeManager;

        if (debugMode)
        {
            Debug.Log($"ARPlacementManager initialized with components: " +
                      $"raycastManager={raycastManager != null}, " +
                      $"anchorManager={anchorManager != null}, " +
                      $"planeManager={planeManager != null}");
        }
    }

    public bool TryGetPlacementPose(Vector2 screenPosition, out Pose pose)
    {
        pose = new Pose();

        if (raycastManager == null)
        {
            Debug.LogError("ARRaycastManager is null!");
            return false;
        }

        if (debugMode)
        {
            Debug.Log($"Attempting raycast at screen position {screenPosition}");
            int planeCount = 0;
            if (planeManager != null)
            {
                planeCount = planeManager.trackables.count;
            }
            Debug.Log($"Plane manager trackables count: {planeCount}");
        }

        // Try with plane detection first (most reliable)
        bool hitPlane = raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon);

        if (debugMode)
        {
            Debug.Log($"Raycast hit plane: {hitPlane}, hit count: {hits.Count}");
        }

        if (hitPlane && hits.Count > 0)
        {
            pose = hits[0].pose;
            if (debugMode)
            {
                Debug.Log($"Hit plane at pose: {pose.position}, trackableId: {hits[0].trackableId}");
            }
            return true;
        }

        // If plane detection fails, try with feature points (less reliable but works in some cases)
        hits.Clear();
        bool hitFeature = raycastManager.Raycast(screenPosition, hits, TrackableType.FeaturePoint);

        if (debugMode)
        {
            Debug.Log($"Raycast hit feature point: {hitFeature}, hit count: {hits.Count}");
        }

        if (hitFeature && hits.Count > 0)
        {
            pose = hits[0].pose;
            if (debugMode)
            {
                Debug.Log($"Hit feature point at pose: {pose.position}");
            }
            return true;
        }

        return false;
    }

    public void HidePlanes()
    {
        if (planeManager == null)
        {
            Debug.LogError("ARPlaneManager is null!");
            return;
        }

        foreach (var plane in planeManager.trackables)
        {
            plane.gameObject.SetActive(false);
        }
        planeManager.enabled = false;

        if (debugMode)
        {
            Debug.Log("AR planes hidden");
        }
    }

    public void ShowPlanes()
    {
        if (planeManager == null)
        {
            Debug.LogError("ARPlaneManager is null!");
            return;
        }

        planeManager.enabled = true;
        foreach (var plane in planeManager.trackables)
        {
            plane.gameObject.SetActive(true);
        }

        if (debugMode)
        {
            Debug.Log("AR planes shown");
        }
    }

    public ARAnchor CreateAnchor(Pose pose)
    {
        if (anchorManager == null)
        {
            Debug.LogError("ARAnchorManager is null!");
            GameObject anchorObject = new GameObject("GameAnchor");
            anchorObject.transform.position = pose.position;
            anchorObject.transform.rotation = pose.rotation;
            Debug.Log($"Created fallback non-AR anchor at {pose.position}");
            return null;
        }

        // Try to create a trackable anchor first (more stable)
        ARAnchor anchor = null;

        if (hits.Count > 0 && hits[0].trackable is ARPlane plane)
        {
            anchor = anchorManager.AttachAnchor(plane, pose);
            if (debugMode)
            {
                Debug.Log($"Created trackable anchor attached to plane at {pose.position}");
            }
        }

        // Fallback to a regular anchor
        if (anchor == null)
        {
            GameObject anchorObject = new GameObject("GameAnchor");
            anchorObject.transform.position = pose.position;
            anchorObject.transform.rotation = pose.rotation;
            anchor = anchorObject.AddComponent<ARAnchor>();
            if (debugMode)
            {
                Debug.Log($"Created regular anchor at {pose.position}");
            }
        }

        return anchor;
    }

    public void SetGameSize(float size)
    {
        gameSize = size;
    }

    public float GetGameSize()
    {
        return gameSize;
    }

    public void SetDebugMode(bool enabled)
    {
        debugMode = enabled;
    }
}