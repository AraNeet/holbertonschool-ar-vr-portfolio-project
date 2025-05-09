using UnityEngine;

public class CubeSpawner
{
    private GameObject cubePrefab;
    private float cubeSize;
    
    public CubeSpawner(GameObject cubePrefab, float cubeSize)
    {
        this.cubePrefab = cubePrefab;
        this.cubeSize = cubeSize;
    }
    
    public GameObject SpawnCube(Pose pose, Transform parent = null)
    {
        // Instantiate the cube
        GameObject dungeonCube = Object.Instantiate(cubePrefab, pose.position, pose.rotation);
        
        // Set parent if provided
        if (parent != null)
        {
            dungeonCube.transform.parent = parent;
        }
        
        // Set scale
        dungeonCube.transform.localScale = Vector3.one * cubeSize;
        
        return dungeonCube;
    }
    
    public void DestroyCube(GameObject cube)
    {
        if (cube != null)
        {
            Object.Destroy(cube);
        }
    }
} 