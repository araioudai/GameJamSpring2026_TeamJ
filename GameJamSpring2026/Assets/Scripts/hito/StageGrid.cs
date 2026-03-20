using UnityEngine;

public class StageGrid : MonoBehaviour
{
    [SerializeField] private int width = 10;
    [SerializeField] private int height = 10;

    // 0 = 床
    // 1 = 壁
    // 2 = ゴール
    private int[,] mapData;

    private void Awake()
    {
        mapData = new int[height, width];
    }

    public void SetMapData(int[,] data)
    {
        mapData = data;
        height = data.GetLength(0);
        width = data.GetLength(1);
    }

    public bool IsInside(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
    }

    public bool IsWall(Vector2Int pos)
    {
        return mapData[pos.y, pos.x] == 1;
    }

    public bool IsBlocked(Vector2Int pos, FurnitureTurn self)
    {
        if (IsWall(pos))
            return true;

        if (IsOccupiedByOtherFurniture(pos, self))
            return true;

        return false;
    }

    private bool IsOccupiedByOtherFurniture(Vector2Int pos, FurnitureTurn self)
    {
        FurnitureTurn[] allFurniture = FindObjectsByType<FurnitureTurn>(FindObjectsSortMode.None);

        foreach (var furniture in allFurniture)
        {
            if (furniture == self)
                continue;

            Vector2Int[] cells = furniture.GetOccupiedCells();

            foreach (var cell in cells)
            {
                if (cell == pos)
                    return true;
            }
        }

        return false;
    }
}