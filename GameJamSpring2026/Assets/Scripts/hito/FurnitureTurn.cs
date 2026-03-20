using UnityEngine;

public enum FurnitureType
{
    Single, // 1マス家具 だんぼーる
    Double  // 2マス家具 ソファ・ベッド
}

public enum FurnitureDirection
{
    Up,
    Right,
    Down,
    Left
}

public class FurnitureTurn : MonoBehaviour
{
    [Header("家具の形")]
    [SerializeField] private FurnitureType furnitureType = FurnitureType.Single;

    [Header("家具の向き")]
    [SerializeField] private FurnitureDirection direction = FurnitureDirection.Up;

    [Header("見た目回転")]
    [SerializeField] private float visualRotationOffsetZ;

    [Header("家具の基準マス座標")]
    [SerializeField] private Vector2Int gridPosition;

    private StageGrid cachedStageGrid;

    private void Awake()
    {
        ApplyVisualRotation();
    }

    public FurnitureType Type => furnitureType;
    public FurnitureDirection Direction => direction;
    public Vector2Int GridPosition => gridPosition;

    public void SetGridPosition(Vector2Int pos)
    {
        gridPosition = pos;
    }

    public void SetFurnitureType(FurnitureType type)
    {
        furnitureType = type;
    }

    public void SetDirection(FurnitureDirection dir)
    {
        direction = dir;
        ApplyVisualRotation();
    }

    public void SetStageGrid(StageGrid stageGrid)
    {
        cachedStageGrid = stageGrid;
    }

    public Vector2Int[] GetOccupiedCells()
    {
        return GetOccupiedCells(direction);
    }

    public Vector2Int[] GetOccupiedCells(FurnitureDirection dir)
    {
        switch (furnitureType)
        {
            case FurnitureType.Single:
                return new Vector2Int[]
                {
                    gridPosition
                };

            case FurnitureType.Double:
                switch (dir)
                {
                    case FurnitureDirection.Up:
                    case FurnitureDirection.Down:
                        return new Vector2Int[]
                        {
                            gridPosition,
                            gridPosition + new Vector2Int(0, 1)
                        };

                    case FurnitureDirection.Right:
                    case FurnitureDirection.Left:
                        return new Vector2Int[]
                        {
                            gridPosition,
                            gridPosition + new Vector2Int(1, 0)
                        };
                }
                break;
        }

        return new Vector2Int[] { gridPosition };
    }

    public bool TryRotateRight(StageGrid stageGrid)
    {
        FurnitureDirection nextDir = GetNextRightDirection(direction);

        if (CanRotate(stageGrid, nextDir))
        {
            direction = nextDir;
            ApplyVisualRotation();
            return true;
        }

        return false;
    }

    public bool TryRotateLeft(StageGrid stageGrid)
    {
        FurnitureDirection nextDir = GetNextLeftDirection(direction);

        if (CanRotate(stageGrid, nextDir))
        {
            direction = nextDir;
            ApplyVisualRotation();
            return true;
        }

        return false;
    }

    private bool CanRotate(StageGrid stageGrid, FurnitureDirection nextDir)
    {
        Vector2Int[] nextCells = GetOccupiedCells(nextDir);

        foreach (var cell in nextCells)
        {
            if (!stageGrid.IsInside(cell))
                return false;

            if (stageGrid.IsBlocked(cell, this))
                return false;
        }

        return true;
    }

    private FurnitureDirection GetNextRightDirection(FurnitureDirection current)
    {
        switch (current)
        {
            case FurnitureDirection.Up: return FurnitureDirection.Right;
            case FurnitureDirection.Right: return FurnitureDirection.Down;
            case FurnitureDirection.Down: return FurnitureDirection.Left;
            case FurnitureDirection.Left: return FurnitureDirection.Up;
        }

        return current;
    }

    private FurnitureDirection GetNextLeftDirection(FurnitureDirection current)
    {
        switch (current)
        {
            case FurnitureDirection.Up: return FurnitureDirection.Left;
            case FurnitureDirection.Left: return FurnitureDirection.Down;
            case FurnitureDirection.Down: return FurnitureDirection.Right;
            case FurnitureDirection.Right: return FurnitureDirection.Up;
        }

        return current;
    }

    private void ApplyVisualRotation()
    {
        float zRotation = visualRotationOffsetZ;

        switch (direction)
        {
            case FurnitureDirection.Up:
                zRotation += 0f;
                break;
            case FurnitureDirection.Right:
                zRotation += -90f;
                break;
            case FurnitureDirection.Down:
                zRotation += 180f;
                break;
            case FurnitureDirection.Left:
                zRotation += 90f;
                break;
        }

        transform.rotation = Quaternion.Euler(0f, 0f, zRotation);
    }

    private void OnDestroy()
    {
        if (cachedStageGrid != null)
        {
            cachedStageGrid.UnregisterFurniture(this);
        }
    }
}