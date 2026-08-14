using Platformer.Core.Time;

namespace Platformer.Core.Tests;

public sealed class FixedStepClockTests
{
    [Fact]
    public void Advance_OneFixedDelta_RunsExactlyOneStep()
    {
        var clock = new FixedStepClock();

        Assert.Equal(1, clock.Advance(FixedStepClock.FixedDelta));
    }

    [Fact]
    public void Advance_CarriesRemainderIntoAccumulator()
    {
        var clock = new FixedStepClock();

        var steps = clock.Advance(FixedStepClock.FixedDelta * 1.5f);

        Assert.Equal(1, steps);
        Assert.Equal(FixedStepClock.FixedDelta * 0.5f, clock.Accumulator, 5);
    }

    [Fact]
    public void Advance_HugeFrame_ClampsSoTheSimulationCannotSpiral()
    {
        var clock = new FixedStepClock();

        var steps = clock.Advance(10f);

        Assert.True(steps <= (int)(FixedStepClock.MaxFrameTime / FixedStepClock.FixedDelta) + 1);
    }

    [Theory]
    [InlineData(-5f)]
    [InlineData(0f)]
    [InlineData(float.NaN)]
    public void Advance_NonPositiveOrNaN_IsIgnored(float frame)
    {
        var clock = new FixedStepClock();

        Assert.Equal(0, clock.Advance(frame));
        Assert.Equal(0f, clock.Accumulator);
    }

    [Fact]
    public void Alpha_ReportsFractionOfAStepElapsed()
    {
        var clock = new FixedStepClock();

        clock.Advance(FixedStepClock.FixedDelta * 0.5f);

        Assert.Equal(0.5f, clock.Alpha, 5);
    }

    [Fact]
    public void Reset_DiscardsPendingTime()
    {
        var clock = new FixedStepClock();
        clock.Advance(FixedStepClock.FixedDelta * 0.75f);

        clock.Reset();

        Assert.Equal(0f, clock.Accumulator);
    }
}
