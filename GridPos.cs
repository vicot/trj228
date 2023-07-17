using System;
using UnityEngine;

public struct GridPos
{
    private int _x;
    private int _y;

    public GridPos(int x, int y)
    {
        _x = x;
        _y = y;
    }

    public int X => _x;
    public int Y => _y;

    public void Deconstruct(out int x, out int z)
    {
        x = _x;
        z = _y;
    }

    public override string ToString()
    {
        return $"( {_x}, {_y} )";
    }

    public bool Equals(GridPos other)
    {
        return _x == other._x && _y == other._y;
    }

    public override bool Equals(object obj)
    {
        return obj is GridPos other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_x, _y);
    }

    public static bool operator ==(GridPos left, GridPos right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(GridPos left, GridPos right)
    {
        return !left.Equals(right);
    }

    public static implicit operator (int x, int z)(GridPos pos) => (pos._x, pos._y);
    public static implicit operator GridPos((int x, int y) pos) => new(pos.x, pos.y);

    public static implicit operator Vector2Int(GridPos pos) => new(pos._x, pos._y);
    public static implicit operator GridPos(Vector2Int pos) => new(pos.x, pos.y);

    public static implicit operator Vector3(GridPos pos) => new(pos._x, pos._y, 0f);

    public static implicit operator GridPos(Vector3Int pos) => new(pos.x, pos.y);

    public static GridPos operator +(GridPos a, GridPos b) => (a.X + b.X, a.Y + b.Y);
    public static GridPos operator -(GridPos a, GridPos b) => (a.X - b.X, a.Y - b.Y);
}