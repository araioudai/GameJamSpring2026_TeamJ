using System.Collections.Generic;
using UnityEngine;

public class StageGrid : MonoBehaviour
{
    [SerializeField] private int width = 10;
    [SerializeField] private int height = 10;

    private int[,] mapData;

    private readonly List<FurnitureTurn> furnitureList = new List<FurnitureTurn>();

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
        if (!IsInside(pos))
            return true;

        return mapData[pos.y, pos.x] == 1;
    }

    public bool IsGoal(Vector2Int pos)
    {
        if (!IsInside(pos))
            return false;

        return mapData[pos.y, pos.x] == 2;
    }

    public bool IsBlocked(Vector2Int pos, FurnitureTurn self)
    {
        if (IsWall(pos))
            return true;

        if (IsOccupiedByOtherFurniture(pos, self))
            return true;

        return false;
    }

    public void RegisterFurniture(FurnitureTurn furniture)
    {
        if (furniture == null)
            return;

        if (!furnitureList.Contains(furniture))
        {
            furnitureList.Add(furniture);
        }
    }

    public void UnregisterFurniture(FurnitureTurn furniture)
    {
        if (furniture == null)
            return;

        furnitureList.Remove(furniture);
    }

    private bool IsOccupiedByOtherFurniture(Vector2Int pos, FurnitureTurn self)
    {
        for (int i = furnitureList.Count - 1; i >= 0; i--)
        {
            FurnitureTurn furniture = furnitureList[i];

            if (furniture == null)
            {
                furnitureList.RemoveAt(i);
                continue;
            }

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