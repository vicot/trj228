using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class Grid : IEnumerable<Cell>
{
    private readonly int _width;
    private readonly int _height;
    private Cell[,] _grid;
    private Spawn _spawner;

    public Grid(int width, int height)
    {
        _width   = width;
        _height  = height;
        _spawner = new();
        _grid    = new Cell[width, height];
        for (var x = 0; x < _grid.GetLength(0); x++)
        {
            for (var y = 0; y < _grid.GetLength(1); y++)
            {
                _grid[x, y] = new((x, y), _spawner.Create(false));
            }
        }

        DestoryGems(new List<GridPos>());
        while (FallGems(false)) DestoryGems(new List<GridPos>());
        foreach (var cell in _grid)
        {
            cell.SetInitialised();
        }
    }

    public Cell this[int x, int y] => this[(x, y)];

    public Cell this[GridPos pos] =>
        pos.X >= 0 && pos.Y >= 0 && pos.X < _width && pos.Y < _height ? _grid[pos.X, pos.Y] : null;


    // 0,0 bottom left
    public bool FallGems(bool isOverheat)
    {
        bool changed = false;

        for (var x = 0; x < _grid.GetLength(0); x++)
        for (var y = 1; y < _grid.GetLength(1); y++)
        {
            var cell  = this[x, y];
            var cellB = this[x, y - 1];
            var cellA = this[x, y + 1];

            if (cell.Color != GemColor.Empty && cellB.Color == GemColor.Empty)
            {
                cellB.Replace(cell);
                cell.Empty(false);
                changed = true;
                cellB.NotifyFall(cell.Position);
            }

            if (cell.Color == GemColor.Empty && cellA == null)
            {
                cell.Replace(_spawner.Create(isOverheat));
                cell.NotifyFall(cell.Position + (0, 1));
                if (!isOverheat) changed = true;
            }
        }

        return changed;
    }

    public bool SwapGems(GridPos from, GridPos to)
    {
        var a = this[from];
        var b = this[to];
        if (a == null || b == null) return false;

        if (a.Color == GemColor.Empty || b.Color == GemColor.Empty) return false;
        
        a.Swap(b);
        if (IsValidMove(from) || IsValidMove(to) || a.Color == GemColor.Spanner || b.Color == GemColor.Spanner ||
            a.Color == GemColor.Timer || b.Color == GemColor.Timer)
        {
            a.NotifySlide(b.Position);
            b.NotifySlide(a.Position);
            
            if (a.Color == GemColor.Spanner) a.NotifySpanner();
            if (b.Color == GemColor.Spanner) b.NotifySpanner();
            if (a.Color == GemColor.Timer) a.NotifyTimer();
            if (b.Color == GemColor.Timer) b.NotifyTimer();
            return true;
        }

        a.Swap(b); //unswap
        return false;
    }

    private bool IsValidMove(GridPos at)
    {
        for (int dx = -2; dx <= 2; dx++)
        {
            var a = this[at + (dx, 0)];
            var b = this[at + (dx + 1, 0)];
            var c = this[at + (dx + 2, 0)];

            if (a == null || b == null || c == null) continue;
            if (a.Color == GemColor.Empty || b.Color == GemColor.Empty || c.Color == GemColor.Empty) continue;

            if (a.Color == b.Color && b.Color == c.Color) return true;
        }

        for (int dy = -2; dy <= 2; dy++)
        {
            var a = this[at + (0, dy)];
            var b = this[at + (0, dy + 1)];
            var c = this[at + (0, dy + 2)];

            if (a == null || b == null || c == null) continue;
            if (a.Color == GemColor.Empty || b.Color == GemColor.Empty || c.Color == GemColor.Empty) continue;

            if (a.Color == b.Color && b.Color == c.Color) return true;
        }

        return false;
    }

    public bool AnyMoveValid()
    {
        for (var x = 0; x < _grid.GetLength(0); x++)
        for (var y = 0; y < _grid.GetLength(1); y++)
        {
            if (IsValidMove((x, y))) return true;
        }

        return false;
    }

    public int DestoryGems(List<GridPos> upgradePositions)
    {
        int  count   = 0;
        bool changed = false;

        List<Cell> allCellsToClear = new();

        for (var x = 0; x < _grid.GetLength(0); x++)
        for (var y = 0; y < _grid.GetLength(1); y++)
        {
            if (!IsValidMove((x, y))) continue;
            var cell = this[x, y];
            if (cell.Color == GemColor.Empty) continue;
            List<Cell> cellsToClear = new();


            GridPos at = (x, y);

            int hcount = 0;
            int vcount = 0;

            if (!cell.GetClearFlag(ClearFlag.Horizontal))
            {
                hcount = 1;
                List<Cell> tempList = new();
                tempList.Add(cell);
                for (int i = 1; i < _width; i++)
                {
                    var next = this[at + (i, 0)];
                    if (next == null) break;
                    if (next.Color != cell.Color) break;
                    //still same color
                    hcount++;
                    tempList.Add(next);
                }

                if (hcount >= 3)
                {
                    foreach (var c in tempList)
                    {
                        c.SetClearFlag(ClearFlag.Horizontal);
                        cellsToClear.Add(c);
                    }
                }
            }

            if (!cell.GetClearFlag(ClearFlag.Vertical))
            {
                vcount = 1;
                List<Cell> tempList = new();
                tempList.Add(cell);
                for (int i = 1; i < _height; i++)
                {
                    var next = this[at + (0, i)];
                    if (next == null) break;
                    if (next.Color != cell.Color) break;
                    //still same color
                    vcount++;
                    tempList.Add(next);
                }

                if (vcount >= 3)
                {
                    foreach (var c in tempList)
                    {
                        c.SetClearFlag(ClearFlag.Vertical);
                        cellsToClear.Add(c);
                    }
                }
            }

            GemColor upgrade = GemColor.Empty;

            if (hcount >= 5 || vcount >= 5) upgrade      = GemColor.Timer;
            else if (hcount >= 4 || vcount >= 4) upgrade = GemColor.Spanner;
            bool upgradeFound = false;

            count += cellsToClear.Count;
            allCellsToClear.AddRange(cellsToClear);

            foreach (var cellX in cellsToClear)
            {
                changed = true;
                var hasUpgradePos = upgradePositions.Any(c => c == cellX.Position);

                if (hasUpgradePos)
                {
                    upgradeFound = true;
                }

                if (hasUpgradePos && upgrade != GemColor.Empty && upgradeFound)
                    cellX.Upgrade(_spawner.CreateUpgrade(upgrade));
            }

            if (!upgradeFound && upgrade != GemColor.Empty)
                cellsToClear.First().Upgrade(_spawner.CreateUpgrade(upgrade));
        }
        
        foreach (var cell in allCellsToClear)
        {
            if (cell.Color is GemColor.Spanner or GemColor.Timer) continue;
            cell.Empty(true);

        }


        return count;
    }


    public IEnumerator<Cell> GetEnumerator()
    {
        for (var index0 = 0; index0 < _grid.GetLength(0); index0++)
        for (var index1 = 0; index1 < _grid.GetLength(1); index1++)
        {
            var cell = _grid[index0, index1];
            yield return cell;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class Spawn
{
    public Gem Create(bool isOverheat)
    {
        if (isOverheat)
            return Gem.Empty;

        var colors = new List<GemColor>
        {
            GemColor.Blue,
            GemColor.Green,
            GemColor.Yellow,
            GemColor.Red,
            GemColor.Purple
        };
        var color = colors[Random.Range(0, colors.Count)];

        return new Gem(color);
    }

    public Gem CreateUpgrade(GemColor upgrade)
    {
        return new Gem(upgrade);
    }
}

public class Cell
{
    public readonly Queue<CellEvent> Events = new();
    private bool Initialised = false;

    public GridPos Position { get; }
    private ClearFlag _clearFlag = ClearFlag.None;

    public Gem Stone { get; private set; }

    public GemColor Color => Stone.Color;

    public Cell(GridPos position, Gem gem)
    {
        Position = position;
        Stone    = gem;
    }

    public void Swap(Cell other) => (Stone, other.Stone) = (other.Stone, Stone);

    public void Replace(Cell src) => Replace(src.Stone);

    public void Replace(Gem gem) => Stone = gem;

    public void SetClearFlag(ClearFlag flag) => _clearFlag |= flag;

    public bool GetClearFlag(ClearFlag flag) => _clearFlag.HasFlag(flag);

    public void SetInitialised() => Initialised = true;

    public void Empty(bool notify = true)
    {
        _clearFlag = ClearFlag.None;
        if (Stone.Color == GemColor.Empty) return;
        Stone = Gem.Empty;

        if (Initialised == false || !notify) return;
        Events.Enqueue(new(CellEventType.Destroy));
    }

    public void NotifySlide(GridPos destination)
    {
        if (Initialised == false) return;
        Events.Enqueue(new(CellEventType.Slide, destination));
    }

    public void NotifyFall(GridPos destination)
    {
        if (Initialised == false) return;
        Events.Enqueue(new(CellEventType.Fall, destination));
    }

    public void NotifySpanner()
    {
        if (Initialised == false) return;
        Events.Enqueue(new(CellEventType.ExecPowerSpanner));
    }

    public void NotifyTimer()
    {
        if (Initialised == false) return;
        Events.Enqueue(new(CellEventType.ExecPowerTimer));
    }

    public void Upgrade(Gem upgrade) => Stone = upgrade;
}

public class CellEvent
{
    public CellEvent(CellEventType eventType, GridPos target = default)
    {
        EventType = eventType;
        Target    = target;
    }

    public CellEventType EventType { get; }
    public GridPos Target { get; }
}

public enum CellEventType
{
    Slide,
    Fall,
    Destroy,
    ExecPowerSpanner,
    ExecPowerTimer
}

public class Gem
{
    public GemColor Color { get; }

    public Gem(GemColor color)
    {
        Color = color;
    }

    public static Gem Empty => new Gem(GemColor.Empty);
}

[Flags]
public enum ClearFlag
{
    None = 0b00,
    Horizontal = 0b01,
    Vertical = 0b10
}

public enum GemColor
{
    Empty,
    Blue,
    Green,
    Yellow,
    Red,
    Purple,
    Bomb,
    Spanner,
    Timer
}