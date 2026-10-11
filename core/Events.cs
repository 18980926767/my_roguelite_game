namespace CaveCurse.Core;

public sealed class EventChoice
{
    public required string Label { get; init; }
    public required Func<RunState, Random, string> Apply { get; init; }
}

public sealed class MazeEvent
{
    public required string Title { get; init; }
    public required string Body { get; init; }
    public required EventChoice[] Choices { get; init; }
}

public static class EventTable
{
    public static MazeEvent For(FloorCell cell)
    {
        var index = Math.Abs(cell.X * 17 + cell.Y * 31) % All.Length;
        return All[index];
    }

    public static string GambleInTheCrack(RunState run, Random rng)
    {
        if (rng.Next(100) < 90)
        {
            run.Gold += 100;
            return "缝里摸出一把硬币。获得 100 金币。";
        }
        run.MaxHp = 1;
        run.Hp = 1;
        return "缝里的东西咬住了你。生命上限变成 1。";
    }

    private static readonly MazeEvent[] All =
    {
        new()
        {
            Title = "墙缝里的响动",
            Body = "砖缝里有金属碰撞的声音。伸手就能摸到，但里面黑得什么都看不见。你当初举过的那种赌法，只是这里的一个选项。",
            Choices = new[]
            {
                new EventChoice { Label = "伸手摸进去", Apply = GambleInTheCrack },
                new EventChoice { Label = "只捡缝口的一枚硬币", Apply = (run, _) => TakeLooseCoin(run) },
                new EventChoice { Label = "离开", Apply = (_, _) => "你没有把手伸进去。" },
            },
        },
        new()
        {
            Title = "靠墙坐着的人",
            Body = "一个人靠着墙，还活着，但站不起来。他说自己也是被推进来的，口袋里有钱，换你扶他喝口水。",
            Choices = new[]
            {
                new EventChoice { Label = "把水留给他，分走他一半的钱", Apply = ShareWithStranger },
                new EventChoice { Label = "趁他没力气，搜走口袋", Apply = RobStranger },
                new EventChoice { Label = "只帮他包扎，什么都不拿", Apply = BandageStranger },
            },
        },
        new()
        {
            Title = "滴水的石像",
            Body = "石像的嘴里在滴水，地上积了一小洼。水很清，但石像的眼睛是后刻上去的。",
            Choices = new[]
            {
                new EventChoice { Label = "喝一口", Apply = DrinkStatue },
                new EventChoice { Label = "把水抹在剑上", Apply = AnointSword },
                new EventChoice { Label = "离开", Apply = (_, _) => "你绕开了那洼水。" },
            },
        },
        new()
        {
            Title = "写着你名字的纸条",
            Body = "地上有一张纸条，字是你的名字。后面还有一行很淡的箭头，指向墙里并不存在的门。",
            Choices = new[]
            {
                new EventChoice { Label = "把路线记下来", Apply = MemorizeRoute },
                new EventChoice { Label = "顺着箭头敲墙", Apply = FollowArrow },
                new EventChoice { Label = "烧掉纸条", Apply = BurnNote },
            },
        },
        new()
        {
            Title = "锁着的铁箱",
            Body = "箱子没有钥匙孔，只有一条撬得动的缝。旁边用粉笔写着一个价：二十五枚硬币。",
            Choices = new[]
            {
                new EventChoice { Label = "硬撬，会受伤", Apply = PryChest },
                new EventChoice { Label = "付 25 金币打开", Apply = PayChest },
                new EventChoice { Label = "离开", Apply = (_, _) => "箱子还锁着。你走了。" },
            },
        },
        new()
        {
            Title = "没有标签的药瓶",
            Body = "玻璃瓶滚到你脚边，塞子还在。闻起来像药，也像铁锈。没有字说明该喝还是该倒掉。",
            Choices = new[]
            {
                new EventChoice { Label = "喝下去", Apply = DrinkUnlabeled },
                new EventChoice { Label = "倒在剑刃上", Apply = PourOnSword },
                new EventChoice { Label = "扔掉", Apply = (_, _) => "瓶子碎在墙角。什么都没发生。" },
            },
        },
    };

    private static string TakeLooseCoin(RunState run)
    {
        run.Gold += 15;
        return "你只拿走看得到的那一枚。获得 15 金币。";
    }

    private static string ShareWithStranger(RunState run, Random _)
    {
        var paid = Math.Max(0, run.Gold / 2);
        run.Gold -= paid;
        run.Attack += 2;
        run.Hp = Math.Min(run.MaxHp, run.Hp + 12);
        return $"你留下了 {paid} 金币。他告诉你怎么把剑握稳。力量 +2，生命回复 12。";
    }

    private static string RobStranger(RunState run, Random _)
    {
        run.Gold += 40;
        run.Hp = Math.Max(1, run.Hp - 14);
        return "口袋里有 40 金币。他抓伤了你，生命 -14。";
    }

    private static string BandageStranger(RunState run, Random _)
    {
        run.Defense += 2;
        run.Hp = Math.Min(run.MaxHp, run.Hp + 8);
        return "你没有拿他的东西。他把一块护臂塞给你。防御 +2，生命回复 8。";
    }

    private static string DrinkStatue(RunState run, Random rng)
    {
        if (rng.Next(100) < 55)
        {
            run.MaxMana += 10;
            run.Mana = Math.Min(run.MaxMana, run.Mana + 20);
            return "水是凉的。魔力上限 +10，魔力回复 20。";
        }
        run.Hp = Math.Max(1, run.Hp - 18);
        return "水里有一股锈味。生命 -18。";
    }

    private static string AnointSword(RunState run, Random _)
    {
        var sword = run.OrdinarySword;
        sword.PoisonSeconds += 3f;
        return "水在剑刃上结成一层薄膜。这把普通剑的毒延长 3 秒。";
    }

    private static string MemorizeRoute(RunState run, Random _)
    {
        run.MoveSpeed += RunState.MoveSpeedPerPoint * 8f;
        return "你记住了哪里该停、哪里该跑。速度 +8。";
    }

    private static string FollowArrow(RunState run, Random rng)
    {
        if (rng.Next(2) == 0)
        {
            run.Gold += 45;
            return "墙砖松了。后面有一小袋钱。获得 45 金币。";
        }
        run.Hp = Math.Max(1, run.Hp - 22);
        return "砖后是空的，碎石砸在你肩上。生命 -22。";
    }

    private static string BurnNote(RunState run, Random _)
    {
        run.MaxHp += 8;
        run.Hp += 8;
        return "火光只亮了一下。名字没有了。生命上限 +8。";
    }

    private static string PryChest(RunState run, Random _)
    {
        run.Hp = Math.Max(1, run.Hp - 12);
        if (run.Backpack.All(weapon => weapon.Id != "rusty-knife"))
        {
            run.Backpack.Add(new WeaponState
            {
                Id = "rusty-knife",
                Name = "生锈的短刀",
                BasicMultiplier = 0.7f,
                KnockbackStacks = 1,
            });
            return "盖子刮破了手。生命 -12。箱子里是一把生锈的短刀，已经放进背包。";
        }
        run.Gold += 30;
        return "盖子刮破了手。生命 -12。箱子里是 30 金币。";
    }

    private static string PayChest(RunState run, Random _)
    {
        if (run.Gold < 25)
            return "你没有 25 金币。箱子没有开。";
        run.Gold -= 25;
        run.Shield += 20;
        return "硬币落进去，锁自己开了。里面是一面薄盾。护盾 +20。";
    }

    private static string DrinkUnlabeled(RunState run, Random rng)
    {
        if (rng.Next(100) < 50)
        {
            run.Hp = Math.Min(run.MaxHp, run.Hp + 30);
            return "喉咙先苦后热。生命回复 30。";
        }
        run.Hp = Math.Max(1, run.Hp - 18);
        return "你跪了一下。生命 -18。";
    }

    private static string PourOnSword(RunState run, Random rng)
    {
        var sword = run.OrdinarySword;
        if (rng.Next(2) == 0)
        {
            sword.BasicMultiplier += 0.08f;
            return "药液吃进缺口。普通剑的普攻系数 +0.08。";
        }
        sword.FreezeSeconds += 0.6f;
        return "剑刃结了一层白霜。普通剑的冻结延长 0.6 秒。";
    }
}
