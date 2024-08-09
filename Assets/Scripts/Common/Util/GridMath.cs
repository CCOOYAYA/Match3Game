using UnityEngine;

public static class GridMath
{
    /// <summary>
    /// 位置是否在棋盘上且处于Active, 注意这个方法在检测位置不在棋盘上时会返回Null Slot
    /// </summary>
    public static bool IsPositionOnBoard(Grid grid, GridPosition gridPosition, out Slot slot)
    {
        if (IsPositionOnGrid(grid, gridPosition))
        {
            slot = grid[gridPosition];
            return slot.IsActive;
        }
        else
        {
            slot = null;
            return false;
        }
    }


    public static bool IsPositionOnBoard(Grid grid, GridPosition gridPosition)
    {
        if (IsPositionOnGrid(grid, gridPosition, out var slot))
        {
            return slot.IsActive;
        }
        return false;
    }


    /// <summary>
    /// 位置是否在棋盘上
    /// </summary>
    public static bool IsPositionOnGrid(Grid grid, GridPosition gridPosition, out Slot slot)
    {
        bool onGrid = IsPositionOnGrid(grid, gridPosition);
        if (onGrid)
        {
            slot = grid[gridPosition];
        }
        else slot = null;
        return onGrid;
    }

    public static bool IsPositionOnGrid(Grid grid, GridPosition gridPosition)
    {
        return IsPositionOnGrid(gridPosition, grid.XMax, grid.YMax);
    }

    public static bool IsPositionOnGrid(GridPosition gridPosition, int xMax, int yMax)
    {
        return gridPosition.X >= 0 &&
               gridPosition.X < xMax &&
               gridPosition.Y >= 0 &&
               gridPosition.Y < yMax;
    }


    public static bool IsPositionDiagonal(GridPosition sourcePosition, GridPosition otherPosition)
    {
        var sidePosition = otherPosition.Equals(sourcePosition + GridPosition.Up) ||
                           otherPosition.Equals(sourcePosition + GridPosition.Right) ||
                           otherPosition.Equals(sourcePosition + GridPosition.Down) ||
                           otherPosition.Equals(sourcePosition + GridPosition.Left);
        return sidePosition;
    }


    public static bool IsPositionWithinArea(RectInt bound, GridPosition gridPosition)
    {
        return gridPosition.X >= bound.xMin &&
               gridPosition.X < bound.xMax &&
               gridPosition.Y >= bound.yMin &&
               gridPosition.Y < bound.yMax;
    }
}
