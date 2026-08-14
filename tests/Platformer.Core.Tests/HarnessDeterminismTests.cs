using Platformer.Core.Input;
using Platformer.Core.Tests.Harness;

namespace Platformer.Core.Tests;

/// <summary>
/// The harness is only useful if a scenario replayed later reproduces the same
/// numbers. These tests run the same script twice and compare the raw bits, so
/// any wall-clock read, unseeded random or hash-ordered iteration that creeps
/// into the harness fails here rather than as an intermittently red build.
/// </summary>
public sealed class HarnessDeterminismTests
{
    private static InputScript Scenario() =>
        InputScript.Create()
            .Hold(InputCommand.Right, 30)
            .Tap(InputCommand.Jump)
            .Wait(25)
            .Release(InputCommand.Right)
            .Press(InputCommand.Left)
            .Wait(20)
            .Tap(InputCommand.Jump, 4)
            .Wait(40)
            .Idle(15);

    private static byte[] RunScenario()
    {
        var harness = SimulationHarness.For(new PointMassSimulation());

        var samples = harness.Run(
            Scenario(),
            static sim => (sim.X, sim.Y, sim.VelocityX, sim.VelocityY, sim.IsGrounded));

        var bytes = new byte[samples.Count * ((4 * sizeof(int)) + 1)];
        var offset = 0;
        foreach (var (x, y, velocityX, velocityY, grounded) in samples)
        {
            foreach (var value in new[] { x, y, velocityX, velocityY })
            {
                BitConverter.TryWriteBytes(
                    bytes.AsSpan(offset),
                    BitConverter.SingleToInt32Bits(value));
                offset += sizeof(int);
            }

            bytes[offset++] = grounded ? (byte)1 : (byte)0;
        }

        return bytes;
    }

    [Fact]
    public void SameScript_RunTwice_ProducesByteIdenticalSimulationState()
    {
        var first = RunScenario();
        var second = RunScenario();

        Assert.NotEmpty(first);
        Assert.Equal(first, second);
    }

    [Fact]
    public void SameScript_RunTwice_ProducesAnIdenticalInputTrace()
    {
        var first = SimulationHarness.For(new PointMassSimulation());
        var second = SimulationHarness.For(new PointMassSimulation());

        first.Run(Scenario());
        second.Run(Scenario());

        Assert.Equal(first.Trace, second.Trace);
        Assert.Equal(first.StepCount, second.StepCount);
        Assert.Equal(first.ElapsedSeconds, second.ElapsedSeconds);
    }

    [Fact]
    public void SameScript_ReplayedManyTimes_NeverDrifts()
    {
        var reference = RunScenario();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            Assert.Equal(reference, RunScenario());
        }
    }

    [Fact]
    public void SplittingAScriptAcrossRunCalls_MatchesRunningItInOneGo()
    {
        var whole = SimulationHarness.For(new PointMassSimulation());
        var split = SimulationHarness.For(new PointMassSimulation());

        whole.Run(InputScript.Create().Hold(InputCommand.Right, 20).Tap(InputCommand.Jump).Wait(20));
        split.Run(InputScript.Create().Hold(InputCommand.Right, 20));
        split.Run(InputScript.Create().Press(InputCommand.Right).Tap(InputCommand.Jump).Wait(20));

        Assert.Equal(whole.Trace, split.Trace);
        Assert.Equal(
            BitConverter.SingleToInt32Bits(whole.Simulation.X),
            BitConverter.SingleToInt32Bits(split.Simulation.X));
        Assert.Equal(
            BitConverter.SingleToInt32Bits(whole.Simulation.Y),
            BitConverter.SingleToInt32Bits(split.Simulation.Y));
    }

    [Fact]
    public void ScriptedRun_AndHandDrivenRun_AgreeStepForStep()
    {
        var scripted = SimulationHarness.For(new PointMassSimulation());
        var manual = SimulationHarness.For(new PointMassSimulation());

        scripted.Run(InputScript.Create().Hold(InputCommand.Right, 10).Tap(InputCommand.Jump).Wait(10));

        manual.Input.Press(InputCommand.Right);
        manual.Advance(10);
        manual.Input.Press(InputCommand.Jump);
        manual.Advance();
        manual.Input.Release(InputCommand.Jump);
        manual.Advance(10);

        Assert.Equal(scripted.Trace, manual.Trace);
        Assert.Equal(
            BitConverter.SingleToInt32Bits(scripted.Simulation.X),
            BitConverter.SingleToInt32Bits(manual.Simulation.X));
    }
}
