using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPlacementManager
{
    private ARRaycastManager raycastManager;
    private ARAnchorManager anchorManager;
    private ARPlaneManager planeManager;
    private Camera xrCamera;
    private GameObject placementIndicator;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    
    public ARPlacementManager(ARRaycastManager raycastManager, ARAnchorManager anchorManager, 
                             ARPlaneManager planeManager, Camera xrCamera, GameObject placementIndicator)
    {
        this.raycastManager = raycastManager;
        this.anchorManager = anchorManager;
        this.planeManager = planeManager;
        this.xrCamera = xrCamera;
        this.placementIndicator = placementIndicator;
    }
    
    public void UpdatePlacementIndicator()
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
    
    public bool TryGetPlacementPose(Vector2 screenPosition, out Pose pose)
    {
        pose = new Pose();
        
        if (raycastManager.Raycast(screenPosition, hits, TrackableType.Planes))
        {
            pose = hits[0].pose;
            return true;
        }
        
        return false;
    }
    
    public void HidePlanes()
    {
        foreach (var plane in planeManager.trackables)
        {
            plane.gameObject.SetActive(false);
        }
        planeManager.enabled = false;
    }
    
    public void ShowPlanes()
    {
        planeManager.enabled = true;
        foreach (var plane in planeManager.trackables)
        {
            plane.gameObject.SetActive(true);
        }
    }
    
    public ARAnchor CreateAnchor(Pose pose)
    {
        GameObject anchorObject = new GameObject("DungeonAnchor");
        anchorObject.transform.position = pose.position;
        anchorObject.transform.rotation = pose.rotation;
        return anchorObject.AddComponent<ARAnchor>();
    }
} 