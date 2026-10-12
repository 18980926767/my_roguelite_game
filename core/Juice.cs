namespace CaveCurse.Core;

public readonly record struct Rgb(byte R, byte G, byte B)
{
    public int Key => (R << 16) | (G << 8) | B;
}

/// <summary>
/// About sixteen cave colors taken from Kerrie Lake's Resurrect 64.
/// Dark tones are the body of the set. White and the two yellows are the
/// hit spark and the death ring, which the effect list calls out by name.
/// </summary>
public static class Palette
{
    public static readonly string[] Hex =
    {
        "2e222f", "3e3546", "625565", "313638",
        "4c3e24", "676633", "6e2727", "9e4539",
        "7a3045", "4d65b4", "165a4c", "45293f",
        "ffffff", "f9c22b", "fbb954", "e6904e",
    };

    public static readonly Rgb Void = Parse("2e222f");
    public static readonly Rgb Ink = Parse("313638");
    public static readonly Rgb Earth = Parse("4c3e24");
    public static readonly Rgb DimYellow = Parse("676633");
    public static readonly Rgb Rust = Parse("9e4539");
    public static readonly Rgb White = Parse("ffffff");
    public static readonly Rgb BrightYellow = Parse("f9c22b");
    public static readonly Rgb PaleYellow = Parse("fbb954");
    public static readonly Rgb Amber = Parse("e6904e");

    public static IReadOnlyList<Rgb> Cave { get; } = Hex.Select(Parse).ToArray();

    public static Rgb Parse(string hex)
    {
        hex = hex.Trim().TrimStart('#');
        return new Rgb(
            Convert.ToByte(hex[..2], 16),
            Convert.ToByte(hex[2..4], 16),
            Convert.ToByte(hex[4..6], 16));
    }

    public static Rgb Snap(byte r, byte g, byte b)
    {
        var best = Cave[0];
        var bestDist = int.MaxValue;
        foreach (var color in Cave)
        {
            var dr = r - color.R;
            var dg = g - color.G;
            var db = b - color.B;
            var dist = dr * dr + dg * dg + db * db;
            if (dist >= bestDist)
                continue;
            bestDist = dist;
            best = color;
        }
        return best;
    }

    /// <summary>
    /// Picks the enemy's main color. Near-black outlines and near-white
    /// highlights are ignored, then the most common snapped palette color wins.
    /// </summary>
    public static Rgb Main(IEnumerable<(byte R, byte G, byte B, byte A)> pixels)
    {
        var counts = new Dictionary<int, int>();
        Rgb? seen = null;
        foreach (var pixel in pixels)
        {
            if (pixel.A < 128)
                continue;
            var max = Math.Max(pixel.R, Math.Max(pixel.G, pixel.B));
            var min = Math.Min(pixel.R, Math.Min(pixel.G, pixel.B));
            if (max < 48 || min > 230)
                continue;
            var snapped = Snap(pixel.R, pixel.G, pixel.B);
            counts.TryGetValue(snapped.Key, out var count);
            counts[snapped.Key] = count + 1;
            seen = snapped;
        }
        if (counts.Count == 0)
            return seen ?? Rust;
        var winner = counts.MaxBy(pair => pair.Value).Key;
        return Cave.First(color => color.Key == winner);
    }
}

public static class HitBurst
{
    public const int CountMin = 6;
    public const int CountMax = 10;
    public const float LifeMin = 0.2f;
    public const float LifeMax = 0.3f;
    public const float SpeedMin = 80f;
    public const float SpeedMax = 140f;
    public const float SpreadDegrees = 45f;
    public const float Gravity = 520f;
    public const float DrawScale = 3f;

    public static float LifeRandomness => 1f - LifeMin / LifeMax;

    public static int Count(Random rng) => rng.Next(CountMin, CountMax + 1);
}

public struct DebrisMote
{
    public float X;
    public float Y;
    public float Vx;
    public float Vy;
    public float FlightLeft;
    public float Rest;
    public bool Grounded;
    public int Size;

    public readonly bool Alive => !Grounded || Rest < DeathBurst.RestSeconds;
}

public static class DeathBurst
{
    public const int CountMin = 12;
    public const int CountMax = 16;
    public const float LifeMin = 0.4f;
    public const float LifeMax = 0.6f;
    public const float RestSeconds = 1f;
    public const float Gravity = 900f;

    public static DebrisMote[] Spawn(Random rng, float x, float y, float groundY)
    {
        var count = rng.Next(CountMin, CountMax + 1);
        var motes = new DebrisMote[count];
        for (var i = 0; i < count; i++)
        {
            var angle = (float)(rng.NextDouble() * Math.PI * 2.0);
            var speed = 60f + (float)rng.NextDouble() * 80f;
            motes[i] = new DebrisMote
            {
                X = x,
                Y = y,
                Vx = MathF.Cos(angle) * speed,
                Vy = MathF.Sin(angle) * speed - 110f,
                FlightLeft = LifeMin + (float)rng.NextDouble() * (LifeMax - LifeMin),
                Size = rng.Next(2) == 0 ? 2 : 3,
            };
            DebrisMotion.Tick(ref motes[i], 0f, groundY);
        }
        return motes;
    }
}

public static class DebrisMotion
{
    public static void Tick(ref DebrisMote mote, float dt, float groundY)
    {
        if (!mote.Alive)
            return;
        if (dt < 0f)
            dt = 0f;
        if (mote.Grounded)
        {
            mote.Rest += dt;
            mote.Vx = 0f;
            mote.Vy = 0f;
            return;
        }

        mote.Vy += DeathBurst.Gravity * dt;
        mote.X += mote.Vx * dt;
        mote.Y += mote.Vy * dt;
        mote.FlightLeft -= dt;
        var landed = mote.Y >= groundY || mote.FlightLeft <= 0f;
        if (!landed)
            return;
        if (groundY > mote.Y)
            mote.Y = groundY;
        else if (mote.Y > groundY)
            mote.Y = groundY;
        mote.Grounded = true;
        mote.Vx = 0f;
        mote.Vy = 0f;
    }
}

public static class ShockRing
{
    public const float Seconds = 0.25f;
    public const int Steps = 4;
    public static readonly float[] Diameters = { 8f, 16f, 24f, 32f };

    public static int Step(float age)
    {
        if (age < 0f || age >= Seconds)
            return -1;
        var index = (int)(age / Seconds * Steps);
        if (index >= Steps)
            index = Steps - 1;
        return index;
    }

    public static bool Dark(int step) => step >= Steps - 2;

    public static Rgb Color(int step)
    {
        if (step < 0)
            return Palette.Earth;
        if (!Dark(step))
            return Palette.PaleYellow;
        return step == Steps - 1 ? Palette.Earth : Palette.DimYellow;
    }
}

public static class WhiteFlash
{
    public const float Alpha = 0.20f;
    public const int Frames = 1;
}

public enum SfxCue
{
    Swing,
    Hit,
    Death,
    Hurt,
    Clear,
    Door,
    Pickup,
}

public static class SfxPitch
{
    public const float Min = 0.95f;
    public const float Max = 1.05f;

    public static float Next(Random rng)
    {
        var step = rng.Next(0, 101) / 100f;
        return Min + step * (Max - Min);
    }
}

public static class CueBank
{
    public const int MinVariants = 2;
    public const int MaxVariants = 3;

    public static string Prefix(SfxCue cue) => cue switch
    {
        SfxCue.Swing => "swing",
        SfxCue.Hit => "hit",
        SfxCue.Death => "death",
        SfxCue.Hurt => "hurt",
        SfxCue.Clear => "clear",
        SfxCue.Door => "door",
        SfxCue.Pickup => "pickup",
        _ => "hit",
    };

    public static int Variant(Random rng, int count)
    {
        if (count <= 1)
            return 0;
        return rng.Next(count);
    }
}

public sealed class MusicMix
{
    public const float CombatSweepSeconds = 0.3f;
    public const float ClearFadeOut = 1f;

    public float Drums { get; private set; }
    public bool AmbientOn { get; private set; } = true;
    private float _target;
    private float _span = ClearFadeOut;
    private float _entryDelay;

    public void EnterCombat()
    {
        AmbientOn = true;
        Drums = 0f;
        _target = 0f;
        _span = 0f;
        _entryDelay = CombatSweepSeconds;
    }

    public void LeaveCombat()
    {
        AmbientOn = true;
        _entryDelay = 0f;
        _target = 0f;
        _span = ClearFadeOut;
    }

    public void ResetExplore()
    {
        AmbientOn = true;
        _entryDelay = 0f;
        _target = 0f;
        Drums = 0f;
        _span = ClearFadeOut;
    }

    public void Tick(float dt)
    {
        AmbientOn = true;
        if (dt < 0f)
            dt = 0f;
        if (_entryDelay > 0f)
        {
            _entryDelay -= dt;
            if (_entryDelay > 0.001f)
                return;
            Drums = 1f;
            _target = 1f;
            _span = 0f;
            return;
        }
        var rate = _span <= 0.0001f ? 1f : dt / _span;
        if (Drums < _target)
            Drums = MathF.Min(_target, Drums + rate);
        else if (Drums > _target)
            Drums = MathF.Max(_target, Drums - rate);
    }
}

public sealed class VolumeKnob
{
    public const float Step = 0.1f;

    public float Music { get; private set; } = 0.8f;
    public float Sfx { get; private set; } = 0.8f;

    public void NudgeMusic(int direction) => Music = Clamp(Music + direction * Step);

    public void NudgeSfx(int direction) => Sfx = Clamp(Sfx + direction * Step);

    public static float Clamp(float value)
    {
        if (value < 0f)
            return 0f;
        if (value > 1f)
            return 1f;
        return value;
    }
}
