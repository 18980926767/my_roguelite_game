namespace CaveCurse.Core;

public readonly record struct ImbueStep(int Stain, int Tier, bool RankedUp);

/// <summary>
/// 普通剑的浸染。数值只放在这里，方便改节奏。
///
/// 估算：普通战斗房有 2 个敌人。开局力量 10、普攻系数 0.5，一刀大约 5 点。
/// 早期敌人大约 8～12 点生命，清一间房大约 4 刀。每次时停只有 1 点行动点，
/// 蓄力大约 8 秒，技能冷却 6 秒，下一轮技能已经转好。想让剑升级的人会在砍人之间插技能，
/// 所以按每间房 <see cref="SkillsPerRoom"/> 次技能估算。
///
/// 第 <see cref="Tier2Room"/> 间结束时累计 <see cref="Tier2At"/> 次，进入第 2 档。
/// 第 <see cref="Tier3Room"/> 间结束时累计 <see cref="Tier3At"/> 次，进入第 3 档。
/// 某一间少放一次，第三档就会落到第 7 间。
/// 开局魔力 50、每次技能 8 点，正好放 6 次升到第 2 档。再放 6 次才到第 3 档，
/// 需要商店的浑浊药水（22 金币，回复 15 蓝）补一两次；一间战斗大约掉 20 金币。
/// </summary>
public static class SwordImbue
{
    public const int SkillsPerRoom = 2;
    public const int Tier2Room = 3;
    public const int Tier3Room = 6;
    public const int Tier2At = SkillsPerRoom * Tier2Room;
    public const int Tier3At = SkillsPerRoom * Tier3Room;

    public const int Tier2DamageBonus = 1;
    public const int Tier3DamageBonus = 2;
    public const float FollowUpScale = 0.4f;
    public const int AfterimageLayers = 1;
    public const string RankText = "剑在回应你";
    public const float RankTextSeconds = 1.2f;
    public const float RankFlashSeconds = 0.18f;

    public static int Tier(int stain)
    {
        if (stain >= Tier3At)
            return 3;
        if (stain >= Tier2At)
            return 2;
        return 1;
    }

    public static ImbueStep UseSkill(WeaponState sword)
    {
        var step = AfterUse(sword.Stain);
        sword.Stain = step.Stain;
        return step;
    }

    public static ImbueStep AfterUse(int stain)
    {
        if (stain < 0)
            stain = 0;
        var before = Tier(stain);
        var next = stain + 1;
        var after = Tier(next);
        return new ImbueStep(next, after, after > before);
    }

    public static int BonusFor(int tier)
    {
        if (tier >= 3)
            return Tier3DamageBonus;
        if (tier == 2)
            return Tier2DamageBonus;
        return 0;
    }

    /// <summary>
    /// 加在防御结算之后，避免 +1 被百分比减伤四舍五入吃掉。
    /// </summary>
    public static int WithBonus(int damage, int tier)
    {
        if (damage <= 0 || tier <= 1)
            return damage;
        return damage + BonusFor(tier);
    }

    public static float FollowUpRatio(float weaponRatio) => weaponRatio * FollowUpScale;

    public static bool FollowsUp(int tier) => tier >= 3;

    public static int ExtraTrailLayers(int tier) => tier >= 2 ? AfterimageLayers : 0;

    public static Rgb Blade(int tier)
    {
        if (tier >= 3)
            return new Rgb(236, 255, 252);
        if (tier == 2)
            return new Rgb(24, 196, 214);
        return new Rgb(154, 156, 168);
    }

    public static Rgb Glow(int tier) => tier >= 3 ? new Rgb(120, 255, 248) : Blade(tier);

    public static bool Glows(int tier) => tier >= 3;

    public static string HudLine(int stain)
    {
        var tier = Tier(stain);
        if (tier >= 3)
            return $"第{tier}档 {Bar(stain)} 已到顶";
        return $"第{tier}档 {stain}/{NextAt(tier)} {Bar(stain)}";
    }

    public static int NextAt(int tier) => tier <= 1 ? Tier2At : Tier3At;

    public static string Bar(int stain, int cells = 6)
    {
        if (cells < 1)
            cells = 1;
        if (Tier(stain) >= 3)
            return "[" + new string('#', cells) + "]";
        var tier = Tier(stain);
        var start = tier <= 1 ? 0 : Tier2At;
        var next = NextAt(tier);
        var span = Math.Max(1, next - start);
        var filled = (int)MathF.Round((stain - start) * cells / (float)span);
        if (filled < 0)
            filled = 0;
        if (filled > cells)
            filled = cells;
        return "[" + new string('#', filled) + new string('-', cells - filled) + "]";
    }
}
