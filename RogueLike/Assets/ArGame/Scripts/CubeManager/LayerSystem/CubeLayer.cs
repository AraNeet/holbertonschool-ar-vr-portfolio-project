using UnityEngine;

public class CubeLayer
{
    public GameObject LayerObject { get; private set; }
    public Vector3 OriginalPosition { get; private set; }
    public Vector3 PeelDirection { get; set; }
    public Coroutine ActiveCoroutine { get; set; }
    
    public CubeLayer(GameObject layerObject)
    {
        LayerObject = layerObject;
        OriginalPosition = layerObject.transform.localPosition;
        PeelDirection = Vector3.zero;
    }
} 