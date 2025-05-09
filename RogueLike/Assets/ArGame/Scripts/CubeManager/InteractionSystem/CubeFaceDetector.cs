using System.Collections.Generic;
using UnityEngine;

public class CubeFaceDetector
{
    private List<CubeLayer> cubeLayers;
    
    public CubeFaceDetector(List<CubeLayer> cubeLayers)
    {
        this.cubeLayers = cubeLayers;
    }
    
    public int DetermineCubeFace(RaycastHit hit)
    {
        // Determine which layer was hit by comparing gameObjects
        for (int i = 0; i < cubeLayers.Count; i++)
        {
            if (hit.transform.gameObject == cubeLayers[i].LayerObject)
                return i;
        }

        // Alternative approach using normal vector
        // This is a backup in case direct object comparison fails
        Vector3 normal = hit.normal;

        if (Vector3.Dot(normal, Vector3.up) > 0.7f)
            return FindLayerByName("TopFace");
        if (Vector3.Dot(normal, Vector3.down) > 0.7f)
            return FindLayerByName("BottomFace");
        if (Vector3.Dot(normal, Vector3.forward) > 0.7f)
            return FindLayerByName("FrontFace");
        if (Vector3.Dot(normal, Vector3.back) > 0.7f)
            return FindLayerByName("BackFace");
        if (Vector3.Dot(normal, Vector3.right) > 0.7f)
            return FindLayerByName("RightFace");
        if (Vector3.Dot(normal, Vector3.left) > 0.7f)
            return FindLayerByName("LeftFace");

        return -1; // No face detected
    }
    
    private int FindLayerByName(string name)
    {
        for (int i = 0; i < cubeLayers.Count; i++)
        {
            if (cubeLayers[i].LayerObject.name == name)
                return i;
        }
        return -1;
    }
} 