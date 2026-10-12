using CaveCurse.Core;
using Xunit;

namespace CaveCurse.Core.Tests;

public class CombatTests
{
    [Fact]
    public void BasicHit_UsesHalfStrengthThenPercentDefense()
    {
        var damage = CombatMath.BasicDamage(10, 0.5f, 10);
        Assert.Equal(5, damage);
    }

    [Fact]
    public void Strike_SpendsShieldBeforeHp()
    {
        var shield = 20;
        var hp = 40;
        CombatMath.ApplyStrike(ref shield, ref hp, 8);
        Assert.Equal(12, shield);
        Assert.Equal(40, hp);
    }

    [Fact]
    public void Poison_IgnoresShieldAndDefense()
    {
        var shield = 50;
        var hp = 1000;
        var fraction = 0f;
        var dealt = 0;
        for (var i = 0; i < 10; i++)
            dealt += CombatMath.ApplyPoisonTick(1000, ref fraction, ref hp);
        Assert.Equal(50, shield);
        Assert.Equal(10, dealt);
        Assert.Equal(990, hp);
    }

    [Fact]
    public void Knockback_IsShorterWhenHeavier()
    {
        Assert.True(CombatMath.KnockbackDistance(2, 2f) < CombatMath.KnockbackDistance(2, 1f));
    }

    [Fact]
    public void Push_StopsAtWall()
    {
        var pos = CombatMath.Push(new Vec2(0, 10), new Vec2(10, 10), 80, new Vec2(0, 0), new Vec2(20, 20), out var hit);
        Assert.True(hit);
        Assert.Equal(20, pos.X);
    }

    [Fact]
    public void RandomSkill_StacksSameStat()
    {
        var run = RunState.CreateNew();
        var rng = new Random(1);
        var seen = new HashSet<string>();
        for (var i = 0; i < 30; i++)
            seen.Add(run.ApplyRandomBonus(rng));
        Assert.Contains(seen, line => line.Contains("攻击") || line.Contains("击退") || line.Contains("生命"));
        Assert.True(run.Attack >= 10);
        Assert.True(run.OrdinarySword.BasicMultiplier >= 0.5f);
    }

    [Fact]
    public void CrackGamble_CanDropMaxHpToOne()
    {
        var found = false;
        for (var seed = 0; seed < 40 && !found; seed++)
        {
            var run = RunState.CreateNew();
            EventTable.GambleInTheCrack(run, new Random(seed));
            if (run.MaxHp == 1)
            {
                found = true;
                Assert.Equal(1, run.Hp);
            }
        }
        Assert.True(found);
    }

    [Fact]
    public void Events_AreNotAllTheSameCoinFlip()
    {
        var titles = new HashSet<string>();
        for (var y = 0; y < 6; y++)
            titles.Add(EventTable.For(new FloorCell { X = y, Y = 1 }).Title);
        Assert.True(titles.Count >= 3);
    }

    [Fact]
    public void RoomLoop_HitStaysClosed_LastKillHoldsThenOpensDoors()
    {
        Assert.InRange(CombatFeel.ClearHoldSeconds, 0.30f, 0.50f);
        Assert.InRange(CombatFeel.HitStopSeconds, 0.05f, 0.12f);
        Assert.True(CombatFeel.HitStopSeconds < CombatFeel.ClearHoldSeconds);
        Assert.True(CombatFeel.DeathSeconds < CombatFeel.ClearHoldSeconds);

        var beat = new ClearBeat();

        // Enter and fight: someone is still standing, so the exits stay shut.
        beat.NotifyLiving(2);
        beat.Tick(2f);
        Assert.False(beat.DoorsOpen);
        Assert.False(beat.Running);
        Assert.False(beat.Done);

        // A hit that is not the last kill does not open the room.
        beat.NotifyLiving(1);
        beat.Tick(CombatFeel.HitStopSeconds);
        Assert.False(beat.DoorsOpen);
        Assert.False(beat.Done);

        // Last enemy dies: doors open now, but the beat is not finished yet.
        beat.NotifyLiving(0);
        Assert.True(beat.Running);
        Assert.True(beat.DoorsOpen);
        Assert.False(beat.Done);
        Assert.InRange(beat.Left, 0.30f, 0.50f);

        beat.Tick(0.20f);
        Assert.True(beat.Running);
        Assert.False(beat.Done);

        beat.Tick(0.30f);
        Assert.True(beat.Done);
        Assert.False(beat.Running);
        Assert.True(beat.DoorsOpen);

        // The hold does not start a new wave, and it does not run twice.
        beat.NotifyLiving(0);
        beat.Tick(1f);
        Assert.True(beat.Done);
        Assert.False(beat.Running);
    }

    [Fact]
    public void Shop_SoldGoodsStayGoneAfterRefresh()
    {
        var run = RunState.CreateNew();
        run.Gold = 200;
        var cell = new FloorCell { X = 1, Y = 1, Kind = CellKind.Shop };
        FloorGenerator.StockShop(cell, new Random(3));
        var first = cell.Shop[0];
        Assert.True(ShopCatalog.TryBuy(run, cell, first.Id, out _));
        FloorGenerator.RefreshShop(cell, new Random(9));
        Assert.Contains(cell.Shop, item => item.Id == first.Id && item.Sold);
    }
}
