using CaveCurse.Core;
using Xunit;

namespace CaveCurse.Core.Tests;

public class FocusSliceTests
{
    [Fact]
    public void Focus_DucksMusicAndOpensTheFilterInATenth()
    {
        var bed = new FocusFilter();
        Assert.False(bed.Filtering);
        Assert.Equal(1f, bed.Music);
        Assert.Equal(FocusBed.OpenHz, bed.Cutoff);

        bed.Leave();
        bed.Tick(1f);
        Assert.False(bed.Filtering);
        Assert.Equal(1f, bed.Music);

        bed.Enter();
        Assert.True(bed.Filtering);
        Assert.Equal(0.4f, bed.Music);
        Assert.Equal(1200f, bed.Cutoff);
        Assert.Equal(0.4f, FocusBed.MusicDuringStop);
        Assert.Equal(1200f, FocusBed.LowpassHz);
        Assert.Equal(0.1f, FocusBed.ReleaseSeconds);

        bed.Tick(0.05f);
        Assert.Equal(0.4f, bed.Music);
        Assert.Equal(1200f, bed.Cutoff);

        bed.Leave();
        bed.Tick(0.05f);
        Assert.True(bed.Filtering);
        Assert.Equal(0.7f, bed.Music, 3);
        Assert.Equal(10600f, bed.Cutoff, 1);

        bed.Leave();
        bed.Tick(0.05f);
        Assert.False(bed.Filtering);
        Assert.Equal(1f, bed.Music, 3);
        Assert.Equal(FocusBed.OpenHz, bed.Cutoff, 1);

        bed.Enter();
        bed.Leave();
        bed.Tick(0.04f);
        bed.Enter();
        Assert.Equal(0.4f, bed.Music);
        Assert.Equal(1200f, bed.Cutoff);
        Assert.True(bed.Filtering);
    }

    [Fact]
    public void Hits_StepByOneSemitoneAndStopAtEight()
    {
        Assert.Equal(0.06f, HitCadence.GapSeconds);
        Assert.Equal(8, HitCadence.MaxSemitones);
        Assert.Equal(4f, HitCadence.FinisherBoostDb);
        Assert.Equal(1f, HitCadence.Pitch(0), 5);
        Assert.Equal(MathF.Pow(2f, 1f / 12f), HitCadence.Pitch(1), 5);
        Assert.Equal(MathF.Pow(2f, 8f / 12f), HitCadence.Pitch(8), 5);
        Assert.Equal(HitCadence.Pitch(8), HitCadence.Pitch(9), 5);
        Assert.True(HitCadence.Pitch(2) > HitCadence.Pitch(1));
        Assert.True(HitCadence.IsFinisher(0, 1));
        Assert.False(HitCadence.IsFinisher(1, 3));
        Assert.True(HitCadence.IsFinisher(2, 3));
        Assert.False(HitCadence.IsFinisher(0, 0));
    }

    [Fact]
    public void Marks_AreShortCrossingScratchesThatBlink()
    {
        Assert.Equal(1f, SlashMarks.LineWidth);
        Assert.Equal(0.3f, SlashMarks.FlashPeriod);
        for (var i = 0; i < 6; i++)
        {
            var stroke = SlashMarks.Stroke(i, 1 + i % 3);
            Assert.InRange(stroke.Length, SlashMarks.LengthMin, SlashMarks.LengthMax);
            Assert.Equal(8f + i % 5, stroke.Length);
            var sign = i % 2 == 0 ? 1f : -1f;
            Assert.Equal(sign * (28f + i * 14f), stroke.AngleDegrees);
        }

        var first = SlashMarks.Stroke(0, 2);
        var second = SlashMarks.Stroke(1, 2);
        Assert.True(MathF.Sign(first.AngleDegrees) != MathF.Sign(second.AngleDegrees));
        Assert.NotEqual(MathF.Abs(first.AngleDegrees), MathF.Abs(second.AngleDegrees));

        Assert.Equal(new Rgb(214, 214, 210), SlashMarks.Ink(1));
        Assert.Equal(new Rgb(150, 220, 214), SlashMarks.Ink(2));
        Assert.Equal(new Rgb(90, 255, 246), SlashMarks.Ink(3));
        Assert.False(SlashMarks.WhiteEdge(1));
        Assert.False(SlashMarks.WhiteEdge(2));
        Assert.True(SlashMarks.WhiteEdge(3));

        Assert.True(SlashMarks.Flashing(0f));
        Assert.True(SlashMarks.Flashing(0.07f));
        Assert.False(SlashMarks.Flashing(0.08f));
        Assert.True(SlashMarks.Flashing(0.3f));
        Assert.False(SlashMarks.Flashing(0.39f));

        Assert.Equal(3, SlashMarks.BurstCount(0));
        Assert.Equal(4, SlashMarks.BurstCount(1));
        Assert.Equal(3, SlashMarks.BurstCount(2));
        Assert.InRange(SlashMarks.BurstCount(9), SlashMarks.BurstMin, SlashMarks.BurstMax);
    }

    [Fact]
    public void SwingCues_FollowTheSwordTier()
    {
        Assert.Equal(SfxCue.Swing1, CueBank.SwingFor(1));
        Assert.Equal(SfxCue.Swing2, CueBank.SwingFor(2));
        Assert.Equal(SfxCue.Swing3, CueBank.SwingFor(3));
        Assert.Equal(SfxCue.Swing1, CueBank.SwingFor(0));
        Assert.Equal("swing1", CueBank.Prefix(SfxCue.Swing1));
        Assert.Equal("swing2", CueBank.Prefix(SfxCue.Swing2));
        Assert.Equal("swing3", CueBank.Prefix(SfxCue.Swing3));
        Assert.Equal("hitheavy", CueBank.Prefix(SfxCue.HitHeavy));
        Assert.Equal("swing", CueBank.Prefix(SfxCue.Swing));
        Assert.False(CueBank.LocksPitch(SfxCue.HitHeavy));
        Assert.False(CueBank.LocksPitch(SfxCue.Swing3));
        Assert.True(CueBank.LocksPitch(SfxCue.Ascend));
    }
}
