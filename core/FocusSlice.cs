namespace CaveCurse.Core;

/// <summary>
/// Time-stop bed. Music drops and the effect bus dulls, then both ease back.
/// </summary>
public static class FocusBed
{
    public const float MusicDuringStop = 0.4f;
    public const float LowpassHz = 1200f;
    public const float OpenHz = 20000f;
    public const float ReleaseSeconds = 0.1f;
}

public sealed class FocusFilter
{
    public float Music { get; private set; } = 1f;
    public float Cutoff { get; private set; } = FocusBed.OpenHz;
    public bool Filtering { get; private set; }

    private bool _releasing;
    private float _releaseAge;
    private float _fromMusic = 1f;
    private float _fromCutoff = FocusBed.OpenHz;

    public void Enter()
    {
        Filtering = true;
        _releasing = false;
        _releaseAge = 0f;
        Music = FocusBed.MusicDuringStop;
        Cutoff = FocusBed.LowpassHz;
    }

    public void Leave()
    {
        if (!Filtering || _releasing)
            return;
        _releasing = true;
        _releaseAge = 0f;
        _fromMusic = Music;
        _fromCutoff = Cutoff;
    }

    public void Tick(float dt)
    {
        if (!_releasing)
            return;
        if (dt < 0f)
            dt = 0f;
        _releaseAge += dt;
        var span = FocusBed.ReleaseSeconds;
        var t = span <= 0f ? 1f : MathF.Min(1f, _releaseAge / span);
        Music = _fromMusic + (1f - _fromMusic) * t;
        Cutoff = _fromCutoff + (FocusBed.OpenHz - _fromCutoff) * t;
        if (t < 1f)
            return;
        Filtering = false;
        _releasing = false;
        _releaseAge = 0f;
        Music = 1f;
        Cutoff = FocusBed.OpenHz;
    }
}

/// <summary>
/// Resolved swings land as a short rising row. The last connected hit is heavier.
/// </summary>
public static class HitCadence
{
    public const float GapSeconds = 0.06f;
    public const int MaxSemitones = 8;
    public const float FinisherBoostDb = 4f;

    public static float Pitch(int index)
    {
        if (index < 0)
            index = 0;
        var steps = Math.Min(index, MaxSemitones);
        return MathF.Pow(2f, steps / 12f);
    }

    public static bool IsFinisher(int index, int count) => count > 0 && index == count - 1;
}

public readonly record struct SlashStroke(float AngleDegrees, float Length, int Tier);

/// <summary>
/// Scratches left on a foe during time-stop. Several cuts cross; none of them are numbered.
/// </summary>
public static class SlashMarks
{
    public const float LengthMin = 8f;
    public const float LengthMax = 12f;
    public const float FlashPeriod = 0.3f;
    public const float FlashDuty = 0.08f;
    public const float LineWidth = 1f;
    public const int BurstMin = 3;
    public const int BurstMax = 4;

    public static Rgb Ink(int tier) => tier switch
    {
        >= 3 => new Rgb(90, 255, 246),
        2 => new Rgb(150, 220, 214),
        _ => new Rgb(214, 214, 210),
    };

    public static bool WhiteEdge(int tier) => tier >= 3;

    public static SlashStroke Stroke(int index, int tier)
    {
        var step = Math.Abs(index);
        var sign = step % 2 == 0 ? 1f : -1f;
        var angle = sign * (28f + step * 14f);
        var span = (int)(LengthMax - LengthMin) + 1;
        var length = LengthMin + step % span;
        var clamped = tier < 1 ? 1 : tier > 3 ? 3 : tier;
        return new SlashStroke(angle, length, clamped);
    }

    public static bool Flashing(float age)
    {
        if (age < 0f)
            age = 0f;
        var turns = MathF.Floor(age / FlashPeriod);
        var phase = age - turns * FlashPeriod;
        if (phase < 0f || phase >= FlashPeriod - 0.0008f)
            phase = 0f;
        return phase < FlashDuty;
    }

    public static int BurstCount(int index) =>
        BurstMin + Math.Abs(index) % (BurstMax - BurstMin + 1);
}
