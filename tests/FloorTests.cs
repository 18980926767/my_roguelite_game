using CaveCurse.Core;
using Xunit;

namespace CaveCurse.Core.Tests;

public class FloorTests
{
    [Fact]
    public void ManySeeds_MatchPlacementRules()
    {
        for (var seed = 0; seed < 80; seed++)
        {
            var map = FloorGenerator.Generate(new Random(seed));
            Assert.Equal(FloorMap.Size, map.Cells.GetLength(0));
            Assert.True(FloorMap.OnPerimeter(map.SpawnX, map.SpawnY), $"seed {seed} spawn");
            Assert.True(FloorGenerator.InChosenHalf(map, map.GateX, map.GateY), $"seed {seed} half");
            Assert.False(map.SpawnX == map.GateX && map.SpawnY == map.GateY);
            var degree = map.Degree(map.GateX, map.GateY);
            Assert.InRange(degree, 2, 3);
            Assert.True(map.Reachable(map.SpawnX, map.SpawnY, map.GateX, map.GateY), $"seed {seed} path");
            Assert.Equal(CellKind.Gate, map.Gate.Kind);
            Assert.True(map.Threat(map.SpawnX, map.SpawnY) < map.Threat(map.GateX, map.GateY));
            var nearStart = EnemyCatalog.Bullet.AtThreat(0f).MaxHp;
            var nearGate = EnemyCatalog.Bullet.AtThreat(1f).MaxHp;
            Assert.True(nearStart < nearGate);
            Assert.Equal(CellKind.Start, map.Spawn.Kind);
            Assert.True(map.Spawn.Visited);
        }
    }
}
