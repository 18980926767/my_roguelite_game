using CaveCurse.Core;
using Xunit;

namespace CaveCurse.Core.Tests;

public class JuiceTests
{
    [Fact]
    public void Palette_IsSixteenResurrectCaveColors()
    {
        Assert.Equal(16, Palette.Cave.Count);
        Assert.Equal(Palette.Cave.Count, Palette.Cave.Select(color => color.Key).Distinct().Count());
        Assert.Contains(Palette.Cave, color => color == Palette.White);
        Assert.Contains(Palette.Cave, color => color == Palette.BrightYellow);
        Assert.Contains(Palette.Cave, color => color == Palette.PaleYellow);
        var dark = Palette.Cave.Count(color => Luma(color) < 120f);
        Assert.InRange(dark, 10, 14);
    }

    [Fact]
    public void MainColor_SkipsOutlineAndHighlight()
    {
        var pixels = new List<(byte, byte, byte, byte)>();
        for (var i = 0; i < 40; i++)
            pixels.Add((20, 18, 22, 255));
        for (var i = 0; i < 12; i++)
            pixels.Add((250, 248, 244, 255));
        for (var i = 0; i < 18; i++)
            pixels.Add((218, 78, 56, 255));
        Assert.Equal(Palette.Rust, Palette.Main(pixels));
    }

    [Fact]
    public void HitBurst_StaysInsideTheArtList()
    {
        var rng = new Random(4);
        for (var i = 0; i < 200; i++)
            Assert.InRange(HitBurst.Count(rng), HitBurst.CountMin, HitBurst.CountMax);
        Assert.InRange(HitBurst.LifeMin, 0.2f, 0.3f);
        Assert.InRange(HitBurst.LifeMax, 0.2f, 0.3f);
        Assert.True(HitBurst.LifeMin < HitBurst.LifeMax);
        Assert.InRange(HitBurst.SpeedMin, 80f, 140f);
        Assert.InRange(HitBurst.SpeedMax, 80f, 140f);
        Assert.Equal(45f, HitBurst.SpreadDegrees);
        Assert.True(HitBurst.Gravity > 0f);
        Assert.InRange(HitBurst.LifeRandomness, 0.2f, 0.5f);
        Assert.Equal(Palette.White, Palette.Snap(255, 255, 255));
        Assert.Equal(Palette.BrightYellow, Palette.Snap(249, 194, 43));
    }

    [Fact]
    public void Debris_ScattersThenRestsAboutOneSecond()
    {
        var rng = new Random(9);
        var motes = DeathBurst.Spawn(rng, 0f, 0f, 28f);
        Assert.InRange(motes.Length, DeathBurst.CountMin, DeathBurst.CountMax);
        Assert.All(motes, mote =>
        {
            Assert.InRange(mote.FlightLeft, DeathBurst.LifeMin, DeathBurst.LifeMax);
            Assert.True(mote.Size is 2 or 3);
            Assert.False(mote.Grounded);
        });

        var mote = new DebrisMote
        {
            X = 10f,
            Y = 0f,
            Vx = 40f,
            Vy = -80f,
            FlightLeft = 0.5f,
            Size = 2,
        };
        var guard = 0;
        while (!mote.Grounded && guard < 120)
        {
            DebrisMotion.Tick(ref mote, 1f / 60f, 36f);
            guard++;
        }
        Assert.True(mote.Grounded);
        Assert.Equal(36f, mote.Y);
        var landedX = mote.X;
        DebrisMotion.Tick(ref mote, 0.99f, 36f);
        Assert.Equal(landedX, mote.X);
        Assert.Equal(0f, mote.Vx);
        Assert.True(mote.Alive);
        DebrisMotion.Tick(ref mote, 0.02f, 36f);
        Assert.False(mote.Alive);
        Assert.InRange(mote.Rest, 1f, 1.05f);
    }

    [Fact]
    public void Shockwave_StepsFromEightToThirtyTwoThenGoesDark()
    {
        Assert.Equal(4, ShockRing.Steps);
        Assert.Equal(new[] { 8f, 16f, 24f, 32f }, ShockRing.Diameters);
        Assert.Equal(0.25f, ShockRing.Seconds);
        Assert.Equal(0, ShockRing.Step(0f));
        Assert.Equal(8f, ShockRing.Diameters[ShockRing.Step(0f)]);
        Assert.Equal(3, ShockRing.Step(0.24f));
        Assert.Equal(32f, ShockRing.Diameters[ShockRing.Step(0.24f)]);
        Assert.Equal(-1, ShockRing.Step(0.25f));
        Assert.False(ShockRing.Dark(0));
        Assert.False(ShockRing.Dark(1));
        Assert.True(ShockRing.Dark(2));
        Assert.True(ShockRing.Dark(3));
        Assert.Equal(Palette.PaleYellow, ShockRing.Color(0));
        Assert.Equal(Palette.DimYellow, ShockRing.Color(2));
        Assert.Equal(Palette.Earth, ShockRing.Color(3));
        Assert.Equal(0.20f, WhiteFlash.Alpha);
        Assert.Equal(1, WhiteFlash.Frames);
    }

    [Fact]
    public void Pitch_StaysBetweenTheTwoLimits()
    {
        var rng = new Random(3);
        var seenMin = false;
        var seenMax = false;
        for (var i = 0; i < 4000; i++)
        {
            var pitch = SfxPitch.Next(rng);
            Assert.InRange(pitch, SfxPitch.Min, SfxPitch.Max);
            if (pitch <= SfxPitch.Min + 0.0001f)
                seenMin = true;
            if (pitch >= SfxPitch.Max - 0.0001f)
                seenMax = true;
        }
        Assert.True(seenMin);
        Assert.True(seenMax);
        Assert.InRange(CueBank.Variant(rng, 3), 0, 2);
    }

    [Fact]
    public void Music_FadesDrumsInOnCombatAndOutAfterClear()
    {
        var mix = new MusicMix();
        Assert.True(mix.AmbientOn);
        Assert.Equal(0f, mix.Drums);

        mix.EnterCombat();
        mix.Tick(0.25f);
        Assert.Equal(0.5f, mix.Drums, 3);
        mix.Tick(0.25f);
        Assert.Equal(1f, mix.Drums, 3);
        Assert.True(mix.AmbientOn);
        Assert.Equal(0.5f, MusicMix.CombatFadeIn);

        mix.LeaveCombat();
        mix.Tick(0.5f);
        Assert.Equal(0.5f, mix.Drums, 3);
        mix.Tick(0.5f);
        Assert.Equal(0f, mix.Drums, 3);
        Assert.True(mix.AmbientOn);
        Assert.Equal(1f, MusicMix.ClearFadeOut);

        mix.EnterCombat();
        mix.Tick(0.5f);
        mix.ResetExplore();
        Assert.Equal(0f, mix.Drums);
        Assert.True(mix.AmbientOn);
    }

    [Fact]
    public void Volume_ClampsAndStepsByTenths()
    {
        var knob = new VolumeKnob();
        Assert.Equal(0.8f, knob.Music);
        Assert.Equal(0.8f, knob.Sfx);
        knob.NudgeMusic(1);
        knob.NudgeMusic(1);
        knob.NudgeMusic(1);
        Assert.Equal(1f, knob.Music);
        knob.NudgeSfx(-20);
        Assert.Equal(0f, knob.Sfx);
        Assert.Equal(0f, VolumeKnob.Clamp(-1f));
        Assert.Equal(1f, VolumeKnob.Clamp(2f));
    }

    [Fact]
    public void GeneratedAudio_HasVariantsAndATwoLayerLoop()
    {
        var root = RepoRoot();
        foreach (SfxCue cue in Enum.GetValues<SfxCue>())
        {
            var prefix = CueBank.Prefix(cue);
            var files = Directory.GetFiles(Path.Combine(root, "audio", "sfx"), prefix + "-*.wav")
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            Assert.InRange(files.Length, CueBank.MinVariants, CueBank.MaxVariants);
            foreach (var file in files)
            {
                var wav = WavFile.Read(file);
                Assert.True(wav.Peak > 1000, file);
                Assert.Equal(44100, wav.Rate);
                Assert.False(wav.Stereo);
            }
        }

        var deaths = Directory.GetFiles(Path.Combine(root, "audio", "sfx"), "death-*.wav");
        Assert.All(deaths, file => Assert.InRange(WavFile.Read(file).Seconds, 0.26, 0.36));

        var clears = Directory.GetFiles(Path.Combine(root, "audio", "sfx"), "clear-*.wav");
        Assert.All(clears, file => Assert.True(WavFile.Read(file).Seconds >= 0.35));

        var ambient = WavFile.Read(Path.Combine(root, "audio", "music", "ambient.wav"));
        var drums = WavFile.Read(Path.Combine(root, "audio", "music", "drums.wav"));
        Assert.Equal(ambient.Samples, drums.Samples);
        Assert.Equal(846720, ambient.Samples);
        Assert.InRange(ambient.Seconds, 19.1f, 19.3f);
        Assert.True(ambient.Peak > 1000);
        Assert.True(drums.Peak > 1000);
        Assert.True(File.Exists(Path.Combine(root, "tools", "gen_audio.py")));

        var ascends = Directory.GetFiles(Path.Combine(root, "audio", "sfx"), "ascend-*.wav");
        Assert.InRange(ascends.Length, CueBank.MinVariants, CueBank.MaxVariants);
        Assert.All(ascends, file => Assert.InRange(WavFile.Read(file).Seconds, 0.78f, 0.82f));
        Assert.True(CueBank.LocksPitch(SfxCue.Ascend));
        Assert.False(CueBank.LocksPitch(SfxCue.Clear));
    }

    private static float Luma(Rgb color) =>
        0.2126f * color.R + 0.7152f * color.G + 0.0722f * color.B;

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "project.godot")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("找不到工程根目录。");
    }

    private readonly record struct WavFile(int Rate, int Samples, bool Stereo, int Peak)
    {
        public float Seconds => Samples / (float)Rate;

        public static WavFile Read(string path)
        {
            using var stream = File.OpenRead(path);
            using var reader = new BinaryReader(stream);
            Assert.Equal("RIFF", new string(reader.ReadChars(4)));
            reader.ReadInt32();
            Assert.Equal("WAVE", new string(reader.ReadChars(4)));
            short channels = 1;
            var rate = 44100;
            short bits = 16;
            byte[] data = Array.Empty<byte>();
            while (stream.Position + 8 <= stream.Length)
            {
                var id = new string(reader.ReadChars(4));
                var size = reader.ReadInt32();
                if (id == "fmt ")
                {
                    reader.ReadInt16();
                    channels = reader.ReadInt16();
                    rate = reader.ReadInt32();
                    reader.ReadInt32();
                    reader.ReadInt16();
                    bits = reader.ReadInt16();
                    if (size > 16)
                        reader.ReadBytes(size - 16);
                }
                else if (id == "data")
                {
                    data = reader.ReadBytes(size);
                    break;
                }
                else
                {
                    reader.ReadBytes(size);
                }
            }
            Assert.Equal(16, bits);
            var samples = data.Length / 2 / channels;
            var peak = 0;
            for (var i = 0; i + 1 < data.Length; i += 2)
            {
                var sample = Math.Abs((int)BitConverter.ToInt16(data, i));
                if (sample > peak)
                    peak = sample;
            }
            return new WavFile(rate, samples, channels == 2, peak);
        }
    }
}
