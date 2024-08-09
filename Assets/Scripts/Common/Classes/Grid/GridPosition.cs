using System;
using System.Runtime.CompilerServices;

[Serializable]
public struct GridPosition : IEquatable<GridPosition>
{
    public int X;
    public int Y;
    public GridPosition(int x, int y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Shorthand for writing GridPosition(0, -1)
    /// </summary>
    public static GridPosition Up { get; } = new GridPosition(0, -1);

    /// <summary>
    /// Shorthand for writing GridPosition(0, 1)
    /// </summary>
    public static GridPosition Down { get; } = new GridPosition(0, 1);

    /// <summary>
    /// Shorthand for writing GridPosition(-1, 0)
    /// </summary>
    public static GridPosition Left { get; } = new GridPosition(-1, 0);

    /// <summary>
    /// Shorthand for writing GridPosition(1, 0)
    /// </summary>
    public static GridPosition Right { get; } = new GridPosition(1, 0);

    /// <summary>
    /// Shorthand for writing GridPosition(0, 0)
    /// </summary>
    public static GridPosition Zero { get; } = new GridPosition(0, 0);

    /// <summary>
    /// Shorthand for writing GridPosition(-1, -1)
    /// </summary>
    public static GridPosition UpLeft { get; } = Up + Left;

    /// <summary>
    /// Shorthand for writing GridPosition(1, -1)
    /// </summary>
    public static GridPosition UpRight { get; } = Up + Right;

    /// <summary>
    /// Shorthand for writing GridPosition(1, 1)
    /// </summary>
    public static GridPosition DownRight { get; } = Down + Right;

    /// <summary>
    /// Shorthand for writing GridPosition(-1, 1)
    /// </summary>
    public static GridPosition DownLeft { get; } = Down + Left;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GridPosition operator +(GridPosition a, GridPosition b)
    {
        return new GridPosition(a.X + b.X, a.Y + b.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GridPosition operator -(GridPosition a, GridPosition b)
    {
        return new GridPosition(a.X - b.X, a.Y - b.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GridPosition operator *(GridPosition a, int b)
    {
        return new GridPosition(a.X * b, a.Y * b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GridPosition operator *(int b, GridPosition a)
    {
        return new GridPosition(a.X * b, a.Y * b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(GridPosition a, GridPosition b)
    {
        return a.X == b.X && a.Y == b.Y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(GridPosition a, GridPosition b)
    {
        return a.X != b.X || a.Y != b.Y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(GridPosition other)
    {
        return X == other.X && Y == other.Y;
    }

    public override bool Equals(object obj)
    {
        return obj is GridPosition other && Equals(other);
    }

    public override int GetHashCode()
    {
        return X.GetHashCode() ^ (Y.GetHashCode() << 2);
    }

    public override string ToString()
    {
        return $"({X}, {Y})";
    }
}