namespace CaveCurse.Core;

public readonly record struct Vec2(float X, float Y)
{
    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vec2 operator *(Vec2 a, float s) => new(a.X * s, a.Y * s);
    public float Length() => MathF.Sqrt(X * X + Y * Y);
}

public static class CombatMath
{
    public const float PoisonFractionPerSecond = 0.001f;
    public const float KnockbackPerStack = 48f;
    public const int CollisionDamage = 6;

    public static int BasicDamage(int strength, float ratio, int defense)
    {
        var raw = strength * ratio;
        var rate = defense <= 0f ? 0f : defense / (defense + 100f);
        if (rate > 0.85f)
            rate = 0.85f;
        var after = raw * (1f - rate);
        if (after <= 0f)
            return 0;
        if (after < 1f)
            return 1;
        return (int)MathF.Round(after);
    }

    public static int ApplyStrike(ref int shield, ref int hp, int amount)
    {
        var blocked = Math.Min(shield, amount);
        shield -= blocked;
        var rest = amount - blocked;
        hp -= rest;
        return rest;
    }

    public static int ApplyPoisonTick(int maxHp, ref float fraction, ref int hp, float rate = 1f)
    {
        fraction += maxHp * PoisonFractionPerSecond * rate;
        var whole = (int)MathF.Floor(fraction);
        if (whole <= 0)
            return 0;
        fraction -= whole;
        hp -= whole;
        return whole;
    }

    public static float KnockbackDistance(int stacks, float weight)
    {
        if (stacks <= 0)
            return 0f;
        var safeWeight = MathF.Max(0.4f, weight);
        return stacks * KnockbackPerStack / safeWeight;
    }

    public static Vec2 Push(Vec2 from, Vec2 target, float distance, Vec2 min, Vec2 max, out bool hitWall)
    {
        hitWall = false;
        var delta = target - from;
        var length = delta.Length();
        if (length < 0.001f || distance <= 0f)
            return target;
        var step = delta * (1f / length);
        var pos = target;
        var left = distance;
        while (left > 0f)
        {
            var hop = MathF.Min(4f, left);
            var next = pos + step * hop;
            if (next.X < min.X || next.Y < min.Y || next.X > max.X || next.Y > max.Y)
            {
                hitWall = true;
                pos = new Vec2(Math.Clamp(next.X, min.X, max.X), Math.Clamp(next.Y, min.Y, max.Y));
                break;
            }
            pos = next;
            left -= hop;
        }
        return pos;
    }
}
