using CaveCurse.Core;
using Godot;

namespace CaveCurse;

public static class PixelTex
{
    private static readonly Dictionary<ulong, Color> Mains = new();
    private static Texture2D? _square2;
    private static Texture2D? _square3;
    private static Gradient? _spark;

    public static Texture2D Square2 => _square2 ??= Square(2);
    public static Texture2D Square3 => _square3 ??= Square(3);

    public static Gradient SparkRamp()
    {
        if (_spark != null)
            return _spark;
        var ramp = new Gradient();
        ramp.SetColor(0, ColorOf(Palette.White));
        ramp.SetColor(1, ColorOf(Palette.BrightYellow));
        _spark = ramp;
        return ramp;
    }

    public static Color ColorOf(Rgb color) =>
        new(color.R / 255f, color.G / 255f, color.B / 255f, 1f);

    public static Color MainOf(Texture2D tex)
    {
        var id = tex.GetInstanceId();
        if (Mains.TryGetValue(id, out var cached))
            return cached;
        var image = tex.GetImage();
        var pixels = new List<(byte R, byte G, byte B, byte A)>();
        if (image != null)
        {
            for (var y = 0; y < image.GetHeight(); y++)
            {
                for (var x = 0; x < image.GetWidth(); x++)
                {
                    var pixel = image.GetPixel(x, y);
                    pixels.Add((
                        (byte)Mathf.RoundToInt(pixel.R * 255f),
                        (byte)Mathf.RoundToInt(pixel.G * 255f),
                        (byte)Mathf.RoundToInt(pixel.B * 255f),
                        (byte)Mathf.RoundToInt(pixel.A * 255f)));
                }
            }
        }
        var color = ColorOf(Palette.Main(pixels));
        Mains[id] = color;
        return color;
    }

    private static Texture2D Square(int size)
    {
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        image.Fill(Colors.White);
        return ImageTexture.CreateFromImage(image);
    }
}

public sealed partial class FxHost : Node2D
{
    private sealed class Chunk
    {
        public DebrisMote Body;
        public float Ground;
        public Color Color;
    }

    private sealed class Ring
    {
        public float X;
        public float Y;
        public float Age;
    }

    private readonly List<Chunk> _chunks = new();
    private readonly List<Ring> _rings = new();

    public bool Busy => _chunks.Count > 0 || _rings.Count > 0;

    public override void _Ready()
    {
        TextureFilter = TextureFilterEnum.Nearest;
    }

    public void Hit(Vector2 at, Vector2 direction, Random rng, int amount = -1)
    {
        if (direction.LengthSquared() < 0.0001f)
            direction = Vector2.Right;
        direction = direction.Normalized();
        var burst = new CpuParticles2D
        {
            Position = new Vector2(Mathf.Round(at.X), Mathf.Round(at.Y)),
            Amount = amount > 0 ? amount : HitBurst.Count(rng),
            Lifetime = HitBurst.LifeMax,
            LifetimeRandomness = HitBurst.LifeRandomness,
            OneShot = true,
            Explosiveness = 1f,
            Randomness = 0f,
            Direction = direction,
            Spread = HitBurst.SpreadDegrees,
            Gravity = new Vector2(0f, HitBurst.Gravity),
            InitialVelocityMin = HitBurst.SpeedMin,
            InitialVelocityMax = HitBurst.SpeedMax,
            ScaleAmountMin = HitBurst.DrawScale,
            ScaleAmountMax = HitBurst.DrawScale,
            Color = Colors.White,
            ColorRamp = PixelTex.SparkRamp(),
            Texture = PixelTex.Square2,
            TextureFilter = TextureFilterEnum.Nearest,
            LocalCoords = true,
            Emitting = false,
        };
        AddChild(burst);
        burst.Emitting = true;
        burst.Restart();
        burst.Finished += () =>
        {
            if (IsInstanceValid(burst))
                burst.QueueFree();
        };
    }

    public void Death(Vector2 at, float groundY, Color color, Random rng)
    {
        foreach (var mote in DeathBurst.Spawn(rng, at.X, at.Y, groundY))
        {
            _chunks.Add(new Chunk
            {
                Body = mote,
                Ground = groundY,
                Color = color,
            });
        }
        _rings.Add(new Ring { X = at.X, Y = at.Y });
        QueueRedraw();
    }

    public void Tick(float dt)
    {
        for (var i = _chunks.Count - 1; i >= 0; i--)
        {
            var chunk = _chunks[i];
            var body = chunk.Body;
            DebrisMotion.Tick(ref body, dt, chunk.Ground);
            chunk.Body = body;
            if (!body.Alive)
                _chunks.RemoveAt(i);
        }
        for (var i = _rings.Count - 1; i >= 0; i--)
        {
            _rings[i].Age += dt;
            if (_rings[i].Age >= ShockRing.Seconds)
                _rings.RemoveAt(i);
        }
        if (Busy)
            QueueRedraw();
    }

    public override void _Draw()
    {
        foreach (var chunk in _chunks)
        {
            var body = chunk.Body;
            if (!body.Alive)
                continue;
            var tex = body.Size == 3 ? PixelTex.Square3 : PixelTex.Square2;
            var pixels = body.Size * HitBurst.DrawScale;
            var pos = new Vector2(Mathf.Round(body.X), Mathf.Round(body.Y));
            DrawTextureRect(tex, new Rect2(pos.X - pixels * 0.5f, pos.Y - pixels * 0.5f, pixels, pixels), false, chunk.Color);
        }
        foreach (var ring in _rings)
        {
            var step = ShockRing.Step(ring.Age);
            if (step < 0)
                continue;
            var radius = Mathf.RoundToInt(ShockRing.Diameters[step] * 0.5f);
            PlotCircle(Mathf.RoundToInt(ring.X), Mathf.RoundToInt(ring.Y), radius, PixelTex.ColorOf(ShockRing.Color(step)));
        }
    }

    private void PlotCircle(int cx, int cy, int radius, Color color)
    {
        if (radius < 1)
            radius = 1;
        var x = radius;
        var y = 0;
        var error = 1 - x;
        while (x >= y)
        {
            Plot(cx + x, cy + y, color);
            Plot(cx + y, cy + x, color);
            Plot(cx - y, cy + x, color);
            Plot(cx - x, cy + y, color);
            Plot(cx - x, cy - y, color);
            Plot(cx - y, cy - x, color);
            Plot(cx + y, cy - x, color);
            Plot(cx + x, cy - y, color);
            y++;
            if (error < 0)
                error += 2 * y + 1;
            else
            {
                x--;
                error += 2 * (y - x + 1);
            }
        }
    }

    private void Plot(int x, int y, Color color)
    {
        DrawRect(new Rect2(x, y, 2, 2), color);
    }
}

public sealed partial class WhiteFrame : Control
{
    public int Frames;

    public WhiteFrame()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public void Pop()
    {
        Frames = WhiteFlash.Frames;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (Frames <= 0)
            return;
        var size = Size.X < 32f ? GetViewportRect().Size : Size;
        DrawRect(new Rect2(Vector2.Zero, size), new Color(1f, 1f, 1f, WhiteFlash.Alpha));
        Frames--;
    }
}
