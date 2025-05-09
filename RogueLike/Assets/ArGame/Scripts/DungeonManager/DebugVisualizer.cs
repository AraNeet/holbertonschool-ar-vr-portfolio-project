using System.Collections.Generic;
using UnityEngine;

public class DebugVisualizer
{
    // Debug visualization colors
    private readonly Color roomColor = new Color(0.2f, 0.7f, 0.3f, 0.5f);     // Green for rooms
    private readonly Color corridorColor = new Color(0.3f, 0.3f, 0.9f, 0.5f); // Blue for corridors
    private readonly Color verticalCorridorColor = new Color(0.5f, 0.3f, 0.9f, 0.5f); // Purple for vertical corridors
    private readonly Color barrierColor = new Color(1.0f, 0.2f, 0.2f, 0.6f);  // Red for collision barriers
    
    private int gridSize;
    private float cellSize;
    private bool[,,] occupiedCells;
    private Transform parentTransform;
    private Material lineRendererMaterial;
    private float lineWidth;
    private Transform debugContainer;
    private bool debugVisualsCreated = false;

    public DebugVisualizer(Transform parentTransform, int gridSize, float cellSize, bool[,,] occupiedCells, Material lineRendererMaterial, float lineWidth)
    {
        this.parentTransform = parentTransform;
        this.gridSize = gridSize;
        this.cellSize = cellSize;
        this.occupiedCells = occupiedCells;
        this.lineRendererMaterial = lineRendererMaterial;
        this.lineWidth = lineWidth;
    }

    public void UpdateOccupiedCells(bool[,,] occupiedCells)
    {
        this.occupiedCells = occupiedCells;
    }

    public bool IsDebugVisualsCreated()
    {
        return debugVisualsCreated;
    }

    public void CreateDebugVisuals()
    {
        // Clear any existing debug visuals
        ClearDebugVisuals();

        // Create container for debug visualizations
        debugContainer = new GameObject("DebugVisuals").transform;
        debugContainer.SetParent(parentTransform);
        debugContainer.localPosition = Vector3.zero;

        // Draw grid outline
        CreateGridOutline();

        // Draw axes
        CreateAxes();

        // Draw cell outlines
        CreateCellVisuals();
        
        // Visualize the rooms and corridors with different colors
        VisualizeRoomsAndCorridors();
        
        // Visualize collision barriers
        VisualizeCollisionBarriers();

        debugVisualsCreated = true;
    }

    public void ClearDebugVisuals()
    {
        // Find and destroy any existing debug container
        Transform existingDebugContainer = parentTransform.Find("DebugVisuals");
        if (existingDebugContainer != null)
        {
            Object.Destroy(existingDebugContainer.gameObject);
        }

        debugVisualsCreated = false;
    }

    private void CreateGridOutline()
    {
        // Create the grid outline using LineRenderer
        GameObject gridOutline = new GameObject("GridOutline");
        gridOutline.transform.SetParent(debugContainer);
        gridOutline.transform.localPosition = Vector3.zero;

        LineRenderer lineRenderer = gridOutline.AddComponent<LineRenderer>();
        SetupLineRenderer(lineRenderer, Color.yellow, lineWidth * 2);

        // Calculate the grid corners
        float halfSize = gridSize * cellSize / 2;
        Vector3[] corners = new Vector3[8];
        corners[0] = new Vector3(-halfSize, -halfSize, -halfSize);
        corners[1] = new Vector3(halfSize, -halfSize, -halfSize);
        corners[2] = new Vector3(halfSize, -halfSize, halfSize);
        corners[3] = new Vector3(-halfSize, -halfSize, halfSize);
        corners[4] = new Vector3(-halfSize, halfSize, -halfSize);
        corners[5] = new Vector3(halfSize, halfSize, -halfSize);
        corners[6] = new Vector3(halfSize, halfSize, halfSize);
        corners[7] = new Vector3(-halfSize, halfSize, halfSize);

        // Draw the grid cube
        lineRenderer.positionCount = 16;
        // Bottom face
        lineRenderer.SetPosition(0, parentTransform.TransformPoint(corners[0]));
        lineRenderer.SetPosition(1, parentTransform.TransformPoint(corners[1]));
        lineRenderer.SetPosition(2, parentTransform.TransformPoint(corners[2]));
        lineRenderer.SetPosition(3, parentTransform.TransformPoint(corners[3]));
        lineRenderer.SetPosition(4, parentTransform.TransformPoint(corners[0]));
        // Connect to top face
        lineRenderer.SetPosition(5, parentTransform.TransformPoint(corners[4]));
        // Top face
        lineRenderer.SetPosition(6, parentTransform.TransformPoint(corners[5]));
        lineRenderer.SetPosition(7, parentTransform.TransformPoint(corners[6]));
        lineRenderer.SetPosition(8, parentTransform.TransformPoint(corners[7]));
        lineRenderer.SetPosition(9, parentTransform.TransformPoint(corners[4]));
        // Connect remaining edges
        lineRenderer.SetPosition(10, parentTransform.TransformPoint(corners[5]));
        lineRenderer.SetPosition(11, parentTransform.TransformPoint(corners[1]));
        lineRenderer.SetPosition(12, parentTransform.TransformPoint(corners[2]));
        lineRenderer.SetPosition(13, parentTransform.TransformPoint(corners[6]));
        lineRenderer.SetPosition(14, parentTransform.TransformPoint(corners[7]));
        lineRenderer.SetPosition(15, parentTransform.TransformPoint(corners[3]));
    }

    private void CreateAxes()
    {
        // Create axes for orientation
        float axisLength = gridSize * cellSize;
        
        // X Axis (Red)
        CreateAxisLine("XAxis", Vector3.zero, Vector3.right * axisLength, Color.red);
        
        // Y Axis (Green)
        CreateAxisLine("YAxis", Vector3.zero, Vector3.up * axisLength, Color.green);
        
        // Z Axis (Blue)
        CreateAxisLine("ZAxis", Vector3.zero, Vector3.forward * axisLength, Color.blue);
    }

    private void CreateAxisLine(string name, Vector3 start, Vector3 end, Color color)
    {
        GameObject axis = new GameObject(name);
        axis.transform.SetParent(debugContainer);
        axis.transform.localPosition = Vector3.zero;

        LineRenderer lineRenderer = axis.AddComponent<LineRenderer>();
        SetupLineRenderer(lineRenderer, color, lineWidth);

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, parentTransform.TransformPoint(start));
        lineRenderer.SetPosition(1, parentTransform.TransformPoint(end));
    }

    private void CreateCellVisuals()
    {
        if (occupiedCells == null)
            return;

        // Create container for cells
        GameObject cellsContainer = new GameObject("Cells");
        cellsContainer.transform.SetParent(debugContainer);
        cellsContainer.transform.localPosition = Vector3.zero;

        // Draw individual cells
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    Vector3 cellCenter = new Vector3(
                        (x - gridSize / 2) * cellSize,
                        (y - gridSize / 2) * cellSize,
                        (z - gridSize / 2) * cellSize
                    );

                    // Only draw cells that are occupied to avoid clutter
                    if (occupiedCells[x, y, z])
                    {
                        // Use default red color for cell outlines - content type will be shown with filled cubes
                        CreateCellCube(cellsContainer.transform, cellCenter, Color.red, x, y, z);
                    }
                }
            }
        }
    }
    
    private void VisualizeRoomsAndCorridors()
    {
        // Create container for room/corridor visualizations
        GameObject typesContainer = new GameObject("RoomsAndCorridors");
        typesContainer.transform.SetParent(debugContainer);
        typesContainer.transform.localPosition = Vector3.zero;
        
        // Find all room and corridor GameObjects
        GameObject[] rooms = GameObject.FindGameObjectsWithTag("Room");
        GameObject[] corridors = GameObject.FindGameObjectsWithTag("Corridor");
        GameObject[] verticalCorridors = GameObject.FindGameObjectsWithTag("VerticalCorridor");
        
        // Create transparent cubes for rooms (green)
        foreach (GameObject room in rooms)
        {
            CreateTransparentCube(typesContainer.transform, room.transform.position, room.transform.localScale * 0.95f, roomColor);
        }
        
        // Create transparent cubes for corridors (blue)
        foreach (GameObject corridor in corridors)
        {
            CreateTransparentCube(typesContainer.transform, corridor.transform.position, corridor.transform.localScale * 0.95f, corridorColor);
        }
        
        // Create transparent cubes for vertical corridors (purple)
        foreach (GameObject corridor in verticalCorridors)
        {
            CreateTransparentCube(typesContainer.transform, corridor.transform.position, corridor.transform.localScale * 0.95f, verticalCorridorColor);
        }
    }
    
    private void VisualizeCollisionBarriers()
    {
        // Create container for barrier visualizations
        GameObject barriersContainer = new GameObject("CollisionBarriers");
        barriersContainer.transform.SetParent(debugContainer);
        barriersContainer.transform.localPosition = Vector3.zero;
        
        // Find all room GameObjects
        GameObject[] roomObjects = GameObject.FindGameObjectsWithTag("Room");
        
        foreach (GameObject roomObj in roomObjects)
        {
            Transform barriersTransform = roomObj.transform.Find("Barriers");
            if (barriersTransform != null)
            {
                foreach (Transform barrier in barriersTransform)
                {
                    // Get the barrier's world position and scale
                    Vector3 worldPosition = barrier.position;
                    Vector3 worldScale = barrier.lossyScale;
                    
                    // Create a transparent visual representation
                    CreateTransparentCube(barriersContainer.transform, worldPosition, worldScale, barrierColor);
                }
            }
        }
    }
    
    private void CreateTransparentCube(Transform parent, Vector3 position, Vector3 scale, Color color)
    {
        // Create a cube for visualization
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(parent);
        cube.transform.position = position;
        cube.transform.localScale = scale;
        
        // Make it transparent
        Renderer renderer = cube.GetComponent<Renderer>();
        Material transparentMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        transparentMaterial.SetFloat("_Surface", 1); // 0 = opaque, 1 = transparent
        transparentMaterial.SetFloat("_Blend", 0);   // 0 = alpha blend
        transparentMaterial.SetFloat("_Metallic", 0.0f);
        transparentMaterial.SetFloat("_Smoothness", 0.5f);
        transparentMaterial.renderQueue = 3000; // Transparent queue
        transparentMaterial.color = color;
        renderer.material = transparentMaterial;
        
        // Disable the collider so it doesn't interfere with gameplay
        Collider collider = cube.GetComponent<Collider>();
        if (collider != null)
        {
            Object.Destroy(collider);
        }
    }

    private void CreateCellCube(Transform parent, Vector3 center, Color color, int x, int y, int z)
    {
        string cellName = $"Cell_{x}_{y}_{z}";
        GameObject cell = new GameObject(cellName);
        cell.transform.SetParent(parent);
        cell.transform.localPosition = Vector3.zero;

        LineRenderer lineRenderer = cell.AddComponent<LineRenderer>();
        SetupLineRenderer(lineRenderer, color, lineWidth * 0.8f);

        // Calculate the cell corners
        float halfCell = cellSize * 0.45f; // Slightly smaller than the actual cell
        Vector3[] corners = new Vector3[8];
        corners[0] = center + new Vector3(-halfCell, -halfCell, -halfCell);
        corners[1] = center + new Vector3(halfCell, -halfCell, -halfCell);
        corners[2] = center + new Vector3(halfCell, -halfCell, halfCell);
        corners[3] = center + new Vector3(-halfCell, -halfCell, halfCell);
        corners[4] = center + new Vector3(-halfCell, halfCell, -halfCell);
        corners[5] = center + new Vector3(halfCell, halfCell, -halfCell);
        corners[6] = center + new Vector3(halfCell, halfCell, halfCell);
        corners[7] = center + new Vector3(-halfCell, halfCell, halfCell);

        // Draw the cell cube
        lineRenderer.positionCount = 16;
        // Bottom face
        lineRenderer.SetPosition(0, parentTransform.TransformPoint(corners[0]));
        lineRenderer.SetPosition(1, parentTransform.TransformPoint(corners[1]));
        lineRenderer.SetPosition(2, parentTransform.TransformPoint(corners[2]));
        lineRenderer.SetPosition(3, parentTransform.TransformPoint(corners[3]));
        lineRenderer.SetPosition(4, parentTransform.TransformPoint(corners[0]));
        // Connect to top face
        lineRenderer.SetPosition(5, parentTransform.TransformPoint(corners[4]));
        // Top face
        lineRenderer.SetPosition(6, parentTransform.TransformPoint(corners[5]));
        lineRenderer.SetPosition(7, parentTransform.TransformPoint(corners[6]));
        lineRenderer.SetPosition(8, parentTransform.TransformPoint(corners[7]));
        lineRenderer.SetPosition(9, parentTransform.TransformPoint(corners[4]));
        // Connect remaining edges
        lineRenderer.SetPosition(10, parentTransform.TransformPoint(corners[5]));
        lineRenderer.SetPosition(11, parentTransform.TransformPoint(corners[1]));
        lineRenderer.SetPosition(12, parentTransform.TransformPoint(corners[2]));
        lineRenderer.SetPosition(13, parentTransform.TransformPoint(corners[6]));
        lineRenderer.SetPosition(14, parentTransform.TransformPoint(corners[7]));
        lineRenderer.SetPosition(15, parentTransform.TransformPoint(corners[3]));
    }

    private void SetupLineRenderer(LineRenderer lineRenderer, Color color, float width)
    {
        // Configure the LineRenderer
        lineRenderer.material = lineRendererMaterial != null ? lineRendererMaterial : new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        lineRenderer.useWorldSpace = true;
    }

    public void DrawDebugGrid()
    {
        // This is for OnDrawGizmos
#if UNITY_EDITOR
        // Calculate world offset from center
        Vector3 offset = new Vector3(
            -gridSize * cellSize / 2,
            -gridSize * cellSize / 2,
            -gridSize * cellSize / 2
        );
        
        // Draw the overall grid bounds with a thicker line
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(parentTransform.position, Vector3.one * gridSize * cellSize);
        
        // Draw grid axes for better orientation
        DrawGridAxes();
        
        // Draw occupied cells
        if (occupiedCells != null)
        {
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    for (int z = 0; z < gridSize; z++)
                    {
                        if (occupiedCells[x, y, z])
                        {
                            Vector3 cellPos = new Vector3(
                                offset.x + (x + 0.5f) * cellSize,
                                offset.y + (y + 0.5f) * cellSize,
                                offset.z + (z + 0.5f) * cellSize
                            );
                            
                            Vector3 worldPos = parentTransform.TransformPoint(cellPos);
                            
                            // Draw a small wire cube for each occupied cell
                            Gizmos.color = Color.red;
                            Gizmos.DrawWireCube(worldPos, Vector3.one * cellSize * 0.9f);
                            
                            // Optionally, draw coordinates for better debugging
                            DrawDebugCoordinates(worldPos, new Vector3Int(x, y, z));
                        }
                    }
                }
            }
        }
#endif
    }
    
    private void DrawGridAxes()
    {
        float axisLength = gridSize * cellSize;
        Vector3 origin = parentTransform.position;
        
        // X Axis (Red)
        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin, origin + parentTransform.right * axisLength);
        
        // Y Axis (Green)
        Gizmos.color = Color.green;
        Gizmos.DrawLine(origin, origin + parentTransform.up * axisLength);
        
        // Z Axis (Blue)
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(origin, origin + parentTransform.forward * axisLength);
    }
    
    private void DrawDebugCoordinates(Vector3 position, Vector3Int coords)
    {
#if UNITY_EDITOR
        // Optionally draw coordinate text next to each cell
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(position, $"({coords.x},{coords.y},{coords.z})");
#endif
    }
} 