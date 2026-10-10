namespace CaveCurse.Core;

public static class FloorGenerator
{
    public static FloorMap Generate(Random rng)
    {
        var map = new FloorMap();
        for (var y = 0; y < FloorMap.Size; y++)
        {
            for (var x = 0; x < FloorMap.Size; x++)
                map.Cells[x, y] = new FloorCell { X = x, Y = y };
        }

        map.SpawnEdge = rng.Next(4);
        var along = rng.Next(FloorMap.Size);
        (map.SpawnX, map.SpawnY) = map.SpawnEdge switch
        {
            0 => (along, 0),
            1 => (FloorMap.Size - 1, along),
            2 => (along, FloorMap.Size - 1),
            _ => (0, along),
        };

        map.Half = rng.Next(2) == 0 ? HalfMode.Opposite : HalfMode.Diagonal;
        var gatePool = GateCandidates(map).ToList();
        if (gatePool.Count == 0)
        {
            map.Half = HalfMode.Opposite;
            gatePool = GateCandidates(map).ToList();
        }

        var gate = gatePool[rng.Next(gatePool.Count)];
        map.GateX = gate.X;
        map.GateY = gate.Y;

        foreach (var cell in map.Cells)
        {
            if (cell.X == map.SpawnX && cell.Y == map.SpawnY)
                cell.Kind = CellKind.Start;
            else if (cell.X == map.GateX && cell.Y == map.GateY)
                cell.Kind = CellKind.Gate;
            else
            {
                var roll = rng.Next(100);
                cell.Kind = roll < 58 ? CellKind.Combat : roll < 79 ? CellKind.Shop : CellKind.Event;
                if (cell.Kind == CellKind.Shop)
                    StockShop(cell, rng);
            }
        }

        map.Spawn.Visited = true;

        for (var y = 0; y < FloorMap.Size; y++)
        {
            for (var x = 0; x < FloorMap.Size; x++)
            {
                if (x + 1 < FloorMap.Size && rng.NextDouble() < 0.42)
                    map.AddEdge(x, y, x + 1, y);
                if (y + 1 < FloorMap.Size && rng.NextDouble() < 0.42)
                    map.AddEdge(x, y, x, y + 1);
            }
        }

        if (!map.Reachable(map.SpawnX, map.SpawnY, map.GateX, map.GateY))
            Carve(map, rng);

        FixGateDegree(map, rng, rng.Next(2, 4));
        return map;
    }

    public static bool InChosenHalf(FloorMap map, int x, int y)
    {
        if (map.Half == HalfMode.Opposite)
            return InOppositeHalf(map.SpawnEdge, x, y);
        var spawnSign = map.SpawnX - map.SpawnY;
        if (spawnSign == 0)
            spawnSign = map.SpawnX + map.SpawnY >= FloorMap.Size ? 1 : -1;
        var cellSign = x - y;
        return cellSign != 0 && Math.Sign(cellSign) != Math.Sign(spawnSign);
    }

    public static void StockShop(FloorCell cell, Random rng)
    {
        cell.Shop.Clear();
        var pool = ShopCatalog.All.ToList();
        var count = 3;
        for (var i = 0; i < count && pool.Count > 0; i++)
        {
            var pick = pool[rng.Next(pool.Count)];
            pool.Remove(pick);
            cell.Shop.Add(pick.Clone());
        }
    }

    public static void RefreshShop(FloorCell cell, Random rng)
    {
        var unsold = cell.Shop.Where(item => !item.Sold).ToList();
        foreach (var item in unsold)
            cell.Shop.Remove(item);
        var owned = cell.Shop.Select(item => item.Id).ToHashSet();
        var pool = ShopCatalog.All.Where(item => !owned.Contains(item.Id)).ToList();
        for (var i = 0; i < unsold.Count && pool.Count > 0; i++)
        {
            var pick = pool[rng.Next(pool.Count)];
            pool.Remove(pick);
            cell.Shop.Add(pick.Clone());
        }
    }

    private static IEnumerable<(int X, int Y)> GateCandidates(FloorMap map)
    {
        for (var y = 0; y < FloorMap.Size; y++)
        {
            for (var x = 0; x < FloorMap.Size; x++)
            {
                if (x == map.SpawnX && y == map.SpawnY)
                    continue;
                if (InChosenHalf(map, x, y))
                    yield return (x, y);
            }
        }
    }

    private static bool InOppositeHalf(int edge, int x, int y)
    {
        var mid = FloorMap.Size / 2;
        return edge switch
        {
            0 => y >= mid,
            1 => x < mid,
            2 => y < mid,
            _ => x >= mid,
        };
    }

    private static void Carve(FloorMap map, Random rng)
    {
        var x = map.SpawnX;
        var y = map.SpawnY;
        while (x != map.GateX || y != map.GateY)
        {
            var canX = x != map.GateX;
            var canY = y != map.GateY;
            var stepX = canX && (!canY || rng.Next(2) == 0);
            if (stepX)
            {
                var nx = x + Math.Sign(map.GateX - x);
                map.AddEdge(x, y, nx, y);
                x = nx;
            }
            else
            {
                var ny = y + Math.Sign(map.GateY - y);
                map.AddEdge(x, y, x, ny);
                y = ny;
            }
        }
    }

    private static void FixGateDegree(FloorMap map, Random rng, int target)
    {
        var options = FloorMap.Orthogonal(map.GateX, map.GateY)
            .Where(p => map.InBounds(p.X, p.Y))
            .ToList();

        while (map.Degree(map.GateX, map.GateY) < target)
        {
            var missing = options.Where(p => !map.HasEdge(map.GateX, map.GateY, p.X, p.Y)).ToList();
            if (missing.Count == 0)
                break;
            var pick = missing[rng.Next(missing.Count)];
            map.AddEdge(map.GateX, map.GateY, pick.X, pick.Y);
        }

        var guard = 0;
        while (map.Degree(map.GateX, map.GateY) > target && guard++ < 8)
        {
            var linked = map.Neighbors(map.GateX, map.GateY).OrderBy(_ => rng.Next()).ToList();
            var removed = false;
            foreach (var link in linked)
            {
                map.RemoveEdge(map.GateX, map.GateY, link.X, link.Y);
                if (map.Reachable(map.SpawnX, map.SpawnY, map.GateX, map.GateY))
                {
                    removed = true;
                    break;
                }
                map.AddEdge(map.GateX, map.GateY, link.X, link.Y);
            }
            if (!removed)
                break;
        }
    }
}
