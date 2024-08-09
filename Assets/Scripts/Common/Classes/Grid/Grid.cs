using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class Grid : IGrid, IDisposable, IEnumerable<Slot>, IEnumerable<GridPosition>
{
    private int _xMax;
    private int _yMax;
    private Slot[,] _slotArray;

    public int XMax => _xMax;
    public int YMax => _yMax;
    public Slot this[GridPosition gridPosition]
    {
        get => _slotArray[gridPosition.Y, gridPosition.X];
        set => _slotArray[gridPosition.Y, gridPosition.X] = value;
    }

    public Slot this[int x, int y]
    {
        get => _slotArray[y, x];
        set => _slotArray[y, x] = value;
    }

    public void SetGrid(Slot[,] slotArray)
    {
        if (_slotArray != null)
        {
            throw new InvalidOperationException("Grid has already been created");
        }

        _xMax = slotArray.GetLength(1);
        _yMax = slotArray.GetLength(0);
        _slotArray = slotArray;
    }



    public IEnumerator<Slot> GetEnumerator()
    {
        EnsureSlotArrayIsNotNull();

        foreach (Slot slot in _slotArray)
        {
            yield return slot;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    IEnumerator<GridPosition> IEnumerable<GridPosition>.GetEnumerator()
    {
        EnsureSlotArrayIsNotNull();

        foreach (Slot slot in _slotArray)
        {
            yield return slot.GridPosition;
        }
    }


    public bool IsPositionOnGrid(GridPosition gridPosition)
    {
        EnsureSlotArrayIsNotNull();

        return GridMath.IsPositionOnGrid(this, gridPosition);
    }

    public bool IsPositionOnBoard(GridPosition gridPosition)
    {
        return IsPositionOnGrid(gridPosition) && _slotArray[gridPosition.X, gridPosition.Y].IsActive;
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureSlotArrayIsNotNull()
    {
        if (_slotArray == null)
        {
            throw new InvalidOperationException("Grid slots are not created.");
        }
    }



    public void Dispose()
    {
        if (_slotArray == null) { return; }

        Array.Clear(_slotArray, 0, _slotArray.Length);
        ResetState();
    }



    public void ResetState()
    {
        _xMax = 0;
        _yMax = 0;
        _slotArray = null;
    }
}
