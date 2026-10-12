namespace CaveCurse.Core;

public static class CombatFeel
{
    public const float HitStopSeconds = 0.08f;
    public const float ClearHoldSeconds = 0.42f;
    public const float DeathSeconds = 0.34f;
    public const float HitFlashSeconds = 0.16f;
    public const float HitPunchSeconds = 0.18f;
    public const float NumberSeconds = 0.7f;
}

public sealed class ClearBeat
{
    public float Left { get; private set; }
    public bool Running { get; private set; }
    public bool DoorsOpen { get; private set; }
    public bool Done { get; private set; }

    public void NotifyLiving(int living)
    {
        if (living > 0 || Running || Done)
            return;
        Running = true;
        DoorsOpen = true;
        Left = CombatFeel.ClearHoldSeconds;
    }

    public void Tick(float dt)
    {
        if (!Running)
            return;
        if (dt < 0f)
            dt = 0f;
        Left -= dt;
        if (Left > 0f)
            return;
        Left = 0f;
        Running = false;
        Done = true;
    }
}
