namespace CaveCurse.Core;

public sealed class WeaponState
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public float BasicMultiplier { get; set; }
    public int KnockbackStacks { get; set; }
    public float PoisonSeconds { get; set; }
    public float FreezeSeconds { get; set; }
    public int Stain { get; set; }
    public bool IsOrdinarySword { get; init; }

    public WeaponState Clone() => new()
    {
        Id = Id,
        Name = Name,
        BasicMultiplier = BasicMultiplier,
        KnockbackStacks = KnockbackStacks,
        PoisonSeconds = PoisonSeconds,
        FreezeSeconds = FreezeSeconds,
        Stain = Stain,
        IsOrdinarySword = IsOrdinarySword,
    };
}

public sealed class RunState
{
    public const int SlotCount = 2;
    public const int SkillManaCost = 8;
    public const float SkillCooldownSeconds = 6f;
    public const float MoveSpeedPerPoint = 3f;

    public int Hp { get; set; } = 100;
    public int MaxHp { get; set; } = 100;
    public int Mana { get; set; } = 50;
    public int MaxMana { get; set; } = 50;
    public int Attack { get; set; } = 10;
    public int Defense { get; set; } = 10;
    public float MoveSpeed { get; set; } = 300f;
    public int ActionPoints { get; set; } = 1;
    public int Gold { get; set; }
    public int Shield { get; set; }
    public float SkillCooldown { get; set; }
    public bool Won { get; set; }
    public bool Dead { get; set; }

    public List<WeaponState> Backpack { get; } = new();
    public WeaponState?[] Slots { get; } = new WeaponState?[SlotCount];
    public int ActiveSlot { get; set; }

    public WeaponState? ActiveWeapon => Slots[ActiveSlot];

    public static RunState CreateNew()
    {
        var run = new RunState();
        var sword = new WeaponState
        {
            Id = "ordinary-sword",
            Name = "一把非常普通的剑",
            BasicMultiplier = 0.5f,
            IsOrdinarySword = true,
        };
        run.Backpack.Add(sword);
        run.Slots[0] = sword;
        run.ActiveSlot = 0;
        return run;
    }

    public WeaponState OrdinarySword =>
        Backpack.First(weapon => weapon.IsOrdinarySword);

    public bool Equip(WeaponState weapon, int slot)
    {
        if (slot < 0 || slot >= SlotCount)
            return false;
        if (!Backpack.Contains(weapon))
            return false;
        for (var i = 0; i < Slots.Length; i++)
        {
            if (i != slot && ReferenceEquals(Slots[i], weapon))
                Slots[i] = null;
        }
        Slots[slot] = weapon;
        return true;
    }

    public string ApplyRandomBonus(Random rng)
    {
        if (rng.Next(2) == 0)
            return ApplyToPerson(rng);
        return ApplyToSword(rng);
    }

    private string ApplyToPerson(Random rng)
    {
        switch (rng.Next(5))
        {
            case 0:
                MaxHp += 4;
                Hp += 4;
                return "生命上限 +4";
            case 1:
                MaxMana += 4;
                Mana = Math.Min(MaxMana, Mana + 4);
                return "蓝量上限 +4";
            case 2:
                MoveSpeed += 12f;
                return "移速 +12";
            case 3:
                Attack += 1;
                return "攻击力 +1";
            default:
                Defense += 1;
                return "防御力 +1";
        }
    }

    private string ApplyToSword(Random rng)
    {
        var sword = OrdinarySword;
        switch (rng.Next(4))
        {
            case 0:
                sword.BasicMultiplier += 0.05f;
                return "普通剑的普攻倍率 +0.05";
            case 1:
                sword.KnockbackStacks += 1;
                return "普通剑的击退 +1 层";
            case 2:
                sword.PoisonSeconds += 3f;
                return "普通剑的毒延长 3 秒";
            default:
                sword.FreezeSeconds += 0.6f;
                return "普通剑的冻结延长 0.6 秒";
        }
    }
}
