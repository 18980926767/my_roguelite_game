namespace CaveCurse.Core;

public enum OfferKind
{
    Heal,
    Mana,
    Shield,
    Knife,
}

public sealed class ShopTemplate
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Detail { get; init; }
    public int Price { get; init; }
    public OfferKind Kind { get; init; }

    public ShopOffer Clone() => new()
    {
        Id = Id,
        Name = Name,
        Detail = Detail,
        Price = Price,
    };
}

public static class ShopCatalog
{
    public static IReadOnlyList<ShopTemplate> All { get; } = new ShopTemplate[]
    {
        new() { Id = "heal", Name = "粗布绷带", Detail = "回复 25 生命", Price = 25, Kind = OfferKind.Heal },
        new() { Id = "mana", Name = "浑浊药水", Detail = "回复 15 蓝", Price = 22, Kind = OfferKind.Mana },
        new() { Id = "shield", Name = "薄铁护符", Detail = "获得 20 点护盾", Price = 40, Kind = OfferKind.Shield },
        new() { Id = "knife", Name = "生锈的短刀", Detail = "普攻 70% 攻击力，自带 1 层击退", Price = 35, Kind = OfferKind.Knife },
    };

    public static bool TryBuy(RunState run, FloorCell cell, string offerId, out string message)
    {
        var offer = cell.Shop.FirstOrDefault(item => item.Id == offerId && !item.Sold);
        if (offer == null)
        {
            message = "没有这件货。";
            return false;
        }
        if (run.Gold < offer.Price)
        {
            message = "金币不够。";
            return false;
        }
        var template = All.First(item => item.Id == offer.Id);
        run.Gold -= offer.Price;
        offer.Sold = true;
        message = Apply(run, template);
        return true;
    }

    public static string Apply(RunState run, ShopTemplate template)
    {
        switch (template.Kind)
        {
            case OfferKind.Heal:
                run.Hp = Math.Min(run.MaxHp, run.Hp + 25);
                return "生命回复了 25。";
            case OfferKind.Mana:
                run.Mana = Math.Min(run.MaxMana, run.Mana + 15);
                return "蓝回复了 15。";
            case OfferKind.Shield:
                run.Shield += 20;
                return "套上了 20 点护盾。";
            default:
                if (run.Backpack.All(weapon => weapon.Id != "rusty-knife"))
                {
                    run.Backpack.Add(new WeaponState
                    {
                        Id = "rusty-knife",
                        Name = "生锈的短刀",
                        BasicMultiplier = 0.7f,
                        KnockbackStacks = 1,
                    });
                }
                return "短刀放进了背包。";
        }
    }
}

public static class EventTable
{
    public static string Resolve(RunState run, Random rng)
    {
        if (rng.Next(100) < 90)
        {
            run.Gold += 100;
            return "你赌赢了。获得 100 金币。";
        }
        run.MaxHp = 1;
        run.Hp = 1;
        return "你赌输了。生命上限变成 1。";
    }
}

public sealed class EnemySpec
{
    public required string Name { get; init; }
    public int MaxHp { get; init; }
    public int Defense { get; init; }
    public int Contact { get; init; }
    public int Shield { get; init; }
    public float Weight { get; init; } = 1f;
    public bool Shoots { get; init; }
    public bool Dashes { get; init; }

    public EnemySpec AtThreat(float threat)
    {
        var t = Math.Clamp(threat, 0f, 1f);
        var grow = 0.28f + 1.35f * t;
        return new EnemySpec
        {
            Name = Name,
            MaxHp = Math.Max(6, (int)MathF.Round(MaxHp * grow)),
            Defense = (int)MathF.Round(Defense * t),
            Contact = Math.Max(2, (int)MathF.Round(Contact * (0.4f + t))),
            Shield = (int)MathF.Round(Shield * t),
            Weight = Weight,
            Shoots = Shoots,
            Dashes = Dashes,
        };
    }
}

public static class EnemyCatalog
{
    public static EnemySpec Bullet { get; } = new()
    {
        Name = "吐弹的守卫",
        MaxHp = 28,
        Defense = 1,
        Contact = 5,
        Weight = 1f,
        Shoots = true,
    };

    public static EnemySpec Dasher { get; } = new()
    {
        Name = "扑来的守卫",
        MaxHp = 34,
        Defense = 1,
        Contact = 8,
        Weight = 1.3f,
        Dashes = true,
    };

    public static EnemySpec Boss { get; } = new()
    {
        Name = "守门的小 Boss",
        MaxHp = 96,
        Defense = 3,
        Contact = 10,
        Shield = 20,
        Weight = 2.2f,
        Shoots = true,
        Dashes = true,
    };
}

public static class Loot
{
    public static int GoldFor(int enemyCount) => 8 + enemyCount * 6;

    public static bool RollsMana(Random rng) => rng.Next(100) < 35;

    public const int ManaFromLoot = 10;
}
