using CaveCurse.Core;
using Xunit;

namespace CaveCurse.Core.Tests;

public class SwordImbueTests
{
    [Fact]
    public void Defaults_RankAtRoom3_ThenRoom6Or7()
    {
        // 每间战斗房按 2 次技能估算。第 3 间结束进第 2 档，第 6 间结束进第 3 档。
        // 某一间少放一次，第三档就会落到第 7 间。
        Assert.Equal(2, SwordImbue.SkillsPerRoom);
        Assert.Equal(3, SwordImbue.Tier2Room);
        Assert.Equal(6, SwordImbue.Tier3Room);
        Assert.Equal(SwordImbue.SkillsPerRoom * SwordImbue.Tier2Room, SwordImbue.Tier2At);
        Assert.Equal(SwordImbue.SkillsPerRoom * SwordImbue.Tier3Room, SwordImbue.Tier3At);
        Assert.Equal(6, SwordImbue.Tier2At);
        Assert.Equal(12, SwordImbue.Tier3At);
    }

    [Fact]
    public void EachSkillUse_AddsOne_AndRanksOnlyOnTheThreshold()
    {
        var sword = RunState.CreateNew().OrdinarySword;
        Assert.Equal(0, sword.Stain);
        Assert.Equal(1, SwordImbue.Tier(sword.Stain));

        for (var i = 1; i < SwordImbue.Tier2At; i++)
        {
            var step = SwordImbue.UseSkill(sword);
            Assert.Equal(i, sword.Stain);
            Assert.Equal(1, step.Tier);
            Assert.False(step.RankedUp);
        }

        var second = SwordImbue.UseSkill(sword);
        Assert.Equal(2, second.Tier);
        Assert.True(second.RankedUp);
        Assert.Equal(SwordImbue.RankText, "剑在回应你");

        while (sword.Stain < SwordImbue.Tier3At - 1)
        {
            var step = SwordImbue.UseSkill(sword);
            Assert.Equal(2, step.Tier);
            Assert.False(step.RankedUp);
        }

        var third = SwordImbue.UseSkill(sword);
        Assert.Equal(3, third.Tier);
        Assert.True(third.RankedUp);

        var stay = SwordImbue.UseSkill(sword);
        Assert.Equal(SwordImbue.Tier3At + 1, stay.Stain);
        Assert.Equal(3, stay.Tier);
        Assert.False(stay.RankedUp);

        var copy = sword.Clone();
        Assert.Equal(sword.Stain, copy.Stain);
    }

    [Fact]
    public void Damage_RisesALittle_FollowUpStaysSmaller()
    {
        var main = CombatMath.BasicDamage(10, 0.5f, 10);
        Assert.Equal(main, SwordImbue.WithBonus(main, 1));
        Assert.Equal(main + SwordImbue.Tier2DamageBonus, SwordImbue.WithBonus(main, 2));
        Assert.Equal(main + SwordImbue.Tier3DamageBonus, SwordImbue.WithBonus(main, 3));
        Assert.InRange(SwordImbue.Tier2DamageBonus, 1, 2);
        Assert.InRange(SwordImbue.Tier3DamageBonus, SwordImbue.Tier2DamageBonus + 1, 3);
        Assert.Equal(0, SwordImbue.WithBonus(0, 3));

        var follow = CombatMath.BasicDamage(10, SwordImbue.FollowUpRatio(0.5f), 10);
        Assert.InRange(follow, 1, main - 1);
        Assert.False(SwordImbue.FollowsUp(1));
        Assert.False(SwordImbue.FollowsUp(2));
        Assert.True(SwordImbue.FollowsUp(3));
        Assert.Equal(0, SwordImbue.ExtraTrailLayers(1));
        Assert.Equal(1, SwordImbue.ExtraTrailLayers(2));
        Assert.Equal(1, SwordImbue.ExtraTrailLayers(3));
    }

    [Fact]
    public void Hud_ShowsTierAndProgressTowardTheNext()
    {
        Assert.Equal("第1档 0/6 [------]", SwordImbue.HudLine(0));
        Assert.Equal("第1档 3/6 [###---]", SwordImbue.HudLine(3));
        Assert.Equal("第2档 6/12 [------]", SwordImbue.HudLine(SwordImbue.Tier2At));
        Assert.Equal("第2档 9/12 [###---]", SwordImbue.HudLine(9));
        Assert.Equal("第3档 [######] 已到顶", SwordImbue.HudLine(SwordImbue.Tier3At));
        Assert.Equal("第3档 [######] 已到顶", SwordImbue.HudLine(SwordImbue.Tier3At + 4));
    }

    [Fact]
    public void Blade_GoesFromSteelToCyanToCyanWhite()
    {
        var steel = SwordImbue.Blade(1);
        var cyan = SwordImbue.Blade(2);
        var hot = SwordImbue.Blade(3);
        Assert.True(Math.Abs(steel.R - steel.G) < 40);
        Assert.True(Math.Abs(steel.G - steel.B) < 40);
        Assert.True(steel.R < 200);
        Assert.True(cyan.G > cyan.R + 80 && cyan.B > cyan.R + 80);
        Assert.True(hot.R > 220 && hot.G >= hot.R && hot.B >= hot.R);
        Assert.True(hot.R + hot.G + hot.B > cyan.R + cyan.G + cyan.B);
        Assert.False(SwordImbue.Glows(1));
        Assert.False(SwordImbue.Glows(2));
        Assert.True(SwordImbue.Glows(3));
        Assert.NotEqual(steel, cyan);
        Assert.NotEqual(cyan, hot);
    }
}
