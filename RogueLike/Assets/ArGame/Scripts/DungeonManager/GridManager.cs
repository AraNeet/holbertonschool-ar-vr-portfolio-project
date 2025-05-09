using UnityEngine;

public class GridManager
{
    private int gridSize;
    private float cellSize;
    private bool[,,] occupiedCells;

    public GridManager(int gridSize, float cellSize)
    {
        this.gridSize = gridSize;
        this.cellSize = cellSize;
        InitializeGrid();
    }

    public void InitializeGrid()
    {
        occupiedCells = new bool[gridSize, gridSize, gridSize];
    }

    public bool IsPositionOccupied(Vector3Int pos)
    {
        if (!IsValidPosition(pos)) return true;
        return occupiedCells[pos.x, pos.y, pos.z];
    }

    public void SetPositionOccupied(Vector3Int pos, bool occupied = true)
    {
        if (IsValidPosition(pos))
        {
            occupiedCells[pos.x, pos.y, pos.z] = occupied;
        }
    }

    public bool IsValidPosition(Vector3Int pos)
    {
        return pos.x >= 0 && pos.x < gridSize &&
               pos.y >= 0 && pos.y < gridSize &&
               pos.z >= 0 && pos.z < gridSize;
    }

    public bool HasAdjacentEmptySpace(Vector3Int pos)
    {
        // Check if there's space around this position
        Vector3Int[] directions = new Vector3Int[]
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1)
        };

        foreach (var dir in directions)
        {
            Vector3Int checkPos = pos + dir;

            if (IsValidPosition(checkPos) && !occupiedCells[checkPos.x, checkPos.y, checkPos.z])
            {
                return true;
            }
        }

        return false;
    }

    public Vector3 GridToWorldPosition(Vector3Int gridPos)
    {
        return new Vector3(
            (gridPos.x - gridSize / 2) * cellSize,
            (gridPos.y - gridSize / 2) * cellSize,
            (gridPos.z - gridSize / 2) * cellSize
        );
    }

    public int GetGridSize()
    {
        return gridSize;
    }

    public float GetCellSize()
    {
        return cellSize;
    }

    public bool[,,] GetOccupiedCells()
    {
        return occupiedCells;
    }
} 