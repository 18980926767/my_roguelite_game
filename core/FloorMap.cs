namespace CaveCurse.Core;

public sealed class ShopOffer
{
    public required string Id { get; init; }
    public required string Name { get; set; }
    public required string Detail { get; set; }
    public int Price { get; set; }
    public bool Sold { get; set; }
}

public sealed class FloorCell
{
    public int X { get; init; }
    public int Y { get; init; }
    public CellKind Kind { get; set; }
    public bool Visited { get; set; }
    public bool CombatCleared { get; set; }
    public bool EventUsed { get; set; }
    public List<ShopOffer> Shop { get; } = new();
}

public sealed class FloorMap
{
    public const int Size = 10;
    public const int RefreshCost = 18;

    public FloorCell[,] Cells { get; } = new FloorCell[Size, Size];
    public HashSet<(int X, int Y, int X2, int Y2)> Edges { get; } = new();
    public int SpawnX { get; set; }
    public int SpawnY { get; set; }
    public int GateX { get; set; }
    public int GateY { get; set; }
    public int SpawnEdge { get; set; }
    public HalfMode Half { get; set; }

    public FloorCell Spawn => Cells[SpawnX, SpawnY];
    public FloorCell Gate => Cells[GateX, GateY];

    public FloorCell At(int x, int y) => Cells[x, y];

    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Size && y < Size;

    public static bool OnPerimeter(int x, int y) =>
        x == 0 || y == 0 || x == Size - 1 || y == Size - 1;

    public bool HasEdge(int x, int y, int x2, int y2)
    {
        var (a, b, c, d) = Norm(x, y, x2, y2);
        return Edges.Contains((a, b, c, d));
    }

    public void AddEdge(int x, int y, int x2, int y2)
    {
        var (a, b, c, d) = Norm(x, y, x2, y2);
        Edges.Add((a, b, c, d));
    }

    public bool RemoveEdge(int x, int y, int x2, int y2)
    {
        var (a, b, c, d) = Norm(x, y, x2, y2);
        return Edges.Remove((a, b, c, d));
    }

    public IEnumerable<(int X, int Y)> Neighbors(int x, int y)
    {
        foreach (var (nx, ny) in Orthogonal(x, y))
        {
            if (HasEdge(x, y, nx, ny))
                yield return (nx, ny);
        }
    }

    public int Degree(int x, int y) => Neighbors(x, y).Count();

    public int StepsFromGate(int x, int y)
    {
        var dist = new Dictionary<(int, int), int> { [(GateX, GateY)] = 0 };
        var queue = new Queue<(int, int)>();
        queue.Enqueue((GateX, GateY));
        while (queue.Count > 0)
        {
            var (cx, cy) = queue.Dequeue();
            foreach (var next in Neighbors(cx, cy))
            {
                if (dist.ContainsKey(next))
                    continue;
                dist[next] = dist[(cx, cy)] + 1;
                queue.Enqueue(next);
            }
        }
        if (dist.TryGetValue((x, y), out var steps))
            return steps;
        return Math.Abs(x - GateX) + Math.Abs(y - GateY);
    }

    public float Threat(int x, int y)
    {
        var span = Math.Max(1, StepsFromGate(SpawnX, SpawnY));
        var away = StepsFromGate(x, y) / (float)span;
        return Math.Clamp(1f - away, 0f, 1f);
    }

    public bool Reachable(int x0, int y0, int x1, int y1)
    {
        var seen = new HashSet<(int, int)>();
        var queue = new Queue<(int, int)>();
        queue.Enqueue((x0, y0));
        seen.Add((x0, y0));
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            if (x == x1 && y == y1)
                return true;
            foreach (var next in Neighbors(x, y))
            {
                if (seen.Add(next))
                    queue.Enqueue(next);
            }
        }
        return false;
    }

    public static IEnumerable<(int X, int Y)> Orthogonal(int x, int y)
    {
        yield return (x, y - 1);
        yield return (x + 1, y);
        yield return (x, y + 1);
        yield return (x - 1, y);
    }

    public (int X, int Y)? Step(int x, int y, Dir dir)
    {
        var (nx, ny) = dir switch
        {
            Dir.North => (x, y - 1),
            Dir.East => (x + 1, y),
            Dir.South => (x, y + 1),
            _ => (x - 1, y),
        };
        if (!InBounds(nx, ny) || !HasEdge(x, y, nx, ny))
            return null;
        return (nx, ny);
    }

    public static string KindLabel(CellKind kind) => kind switch
    {
        CellKind.Start => "出生",
        CellKind.Combat => "战斗",
        CellKind.Shop => "商店",
        CellKind.Event => "事件",
        CellKind.Gate => "守门",
        _ => "?",
    };

    private static (int, int, int, int) Norm(int x, int y, int x2, int y2)
    {
        if (x > x2 || (x == x2 && y > y2))
            return (x2, y2, x, y);
        return (x, y, x2, y2);
    }
}
