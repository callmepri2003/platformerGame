using Platformer.Core.Input;
using Platformer.Core.Tests.Harness;
using Platformer.Core.Time;

namespace Platformer.Core.Tests;

public sealed class SimulationHarnessTests
{
    [Fact]
    public void For_DefaultsToTheGamesFixedStepRate()
    {
        var harness = SimulationHarness.For(new PointMassSimulation());

        Assert.Equal(FixedStepClock.FixedDelta, harness.FixedDelta);
        Assert.Equal(0, harness.StepCount);
        Assert.Equal(0f, harness.ElapsedSeconds);
    }

    [Fact]
    public void Advance_TakesExactlyTheRequestedNumberOfSteps()
    {
        var harness = SimulationHarness.For(new PointMassSimulation());

        harness.Advance(12);

        Assert.Equal(12, harness.StepCount);
        Assert.Equal(12, harness.Simulation.Steps);
    }

    [Fact]
    public void Advance_DefaultsToASingleStep()
    {
        var harness = SimulationHarness.For(new PointMassSimulation());

        harness.Advance().Advance();

        Assert.Equal(2, harness.StepCount);
    }

    [Fact]
    public void Advance_KeepsHoldingWhateverInputIsPending()
    {
        var harness = SimulationHarness.For(new PointMassSimulation());

        harness.Input.Press(InputCommand.Right);
        harness.Advance(5);

        Assert.All(harness.Trace, frame => Assert.Equal(InputCommand.Right, frame.Held));
        Assert.True(harness.Simulation.X > 0f);
    }

    [Fact]
    public void Advance_NegativeSteps_Throws()
    {
        var harness = SimulationHarness.For(new PointMassSimulation());

        Assert.Throws<ArgumentOutOfRangeException>(() => harness.Advance(-1));
    }

    [Fact]
    public void ElapsedSeconds_IsStepCountTimesTheFixedDelta()
    {
        var harness = SimulationHarness.For(new PointMassSimulation());

        harness.Advance(60);

        Assert.Equal(60 * FixedStepClock.FixedDelta, harness.ElapsedSeconds, 6);
    }

    [Fact]
    public void Run_PlaysEveryStepOfTheScript()
    {
        var harness = SimulationHarness.For(new PointMassSimulation());
        var script = InputScript.Create().Hold(InputCommand.Right, 30).Tap(InputCommand.Jump).Wait(9);

        harness.Run(script);

        Assert.Equal(script.Length, harness.StepCount);
        Assert.Equal(script.Length, harness.Trace.Count);
    }

    [Fact]
    public void Run_TraceRecordsTheScriptsHeldMasksInOrder()
    {
        var harness = SimulationHarness.For(new PointMassSimulation());
        var script = InputScript.Create().Hold(InputCommand.Right, 3).Idle(2);

        harness.Run(script);

        Assert.Equal(script.Frames, harness.Trace.Select(frame => frame.Held));
        Assert.Equal(Enumerable.Range(0, script.Length), harness.Trace.Select(frame => frame.StepIndex));
    }

    [Fact]
    public void Run_HeldCommandIsEdgeTriggeredExactlyOnceInTheTrace()
    {
        var harness = SimulationHarness.For(new PointMassSimulation());

        harness.Run(InputScript.Create().Hold(InputCommand.Jump, 10));

        var jumpEdges = harness.Trace.Count(f => (f.Pressed & InputCommand.Jump) != InputCommand.None);
        Assert.Equal(1, jumpEdges);
        Assert.Equal(0, harness.Trace[0].StepIndex);
        Assert.Equal(InputCommand.Jump, harness.Trace[0].Pressed);
        Assert.Equal(1, harness.Simulation.JumpsStarted);
    }

    [Fact]
    public void Run_TappingJumpTwiceOnTheGroundStartsTwoJumps()
    {
        var harness = SimulationHarness.For(new PointMassSimulation());
        var script = InputScript.Create()
            .Tap(InputCommand.Jump)
            .Wait(60)
            .Tap(InputCommand.Jump)
            .Wait(60);

        harness.Run(script);

        Assert.Equal(2, harness.Simulation.JumpsStarted);
    }

    [Fact]
    public void Run_WithASampler_ReturnsOneObservationPerStep()
    {
        var harness = SimulationHarness.For(new PointMassSimulation());
        var script = InputScript.Create().Hold(InputCommand.Right, 10);

        var positions = harness.Run(script, sim => sim.X);

        Assert.Equal(10, positions.Count);
        Assert.True(positions[9] > positions[0]);
    }

    [Fact]
    public void Run_NullScript_Throws()
    {
        var harness = SimulationHarness.For(new PointMassSimulation());

        Assert.Throws<ArgumentNullException>(() => harness.Run(null!));
        Assert.Throws<ArgumentNullException>(() => harness.Run(null!, sim => sim.X));
        Assert.Throws<ArgumentNullException>(() => harness.Run(InputScript.Create(), (Func<PointMassSimulation, float>)null!));
    }

    [Fact]
    public void RunUntil_ReturnsTheNumberOfStepsTheConditionNeeded()
    {
        var harness = SimulationHarness.For(new PointMassSimulation());

        var steps = harness.RunUntil(sim => sim.Steps >= 7, maxSteps: 100, "seven steps to elapse");

        Assert.Equal(7, steps);
        Assert.Equal(7, harness.StepCount);
    }

    [Fact]
    public void RunUntil_ConditionAlreadyTrue_TakesNoSteps()
    {
        var harness = SimulationHarness.For(new PointMassSimulation());

        var steps = harness.RunUntil(sim => sim.IsGrounded, maxSteps: 100, "the player to be grounded");

        Assert.Equal(0, steps);
        Assert.Equal(0, harness.StepCount);
    }

    [Fact]
    public void RunUntil_ConditionNeverHolds_ThrowsRatherThanLoopingForever()
    {
        var harness = SimulationHarness.For(new PointMassSimulation());

        var error = Assert.Throws<SimulationTimeoutException>(
            () => harness.RunUntil(sim => sim.X > 1_000_000f, maxSteps: 50, "the player to leave the map"));

        Assert.Contains("the player to leave the map", error.Message, StringComparison.Ordinal);
        Assert.Contains("50", error.Message, StringComparison.Ordinal);
        Assert.Equal(50, harness.StepCount);
    }

    [Fact]
    public void RunUntil_WithoutADescription_StillExplainsTheCap()
    {
        var harness = SimulationHarness.For(new PointMassSimulation());

        var error = Assert.Throws<SimulationTimeoutException>(
            () => harness.RunUntil(_ => false, maxSteps: 3));

        Assert.Contains("the condition", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RunUntil_ZeroCapAndUnmetCondition_ThrowsWithoutStepping()
    {
        var harness = SimulationHarness.For(new PointMassSimulation());

        Assert.Throws<SimulationTimeoutException>(() => harness.RunUntil(_ => false, maxSteps: 0));
        Assert.Equal(0, harness.StepCount);
    }

    [Fact]
    public void RunUntil_NegativeCap_Throws()
    {
        var harness = SimulationHarness.For(new PointMassSimulation());

        Assert.Throws<ArgumentOutOfRangeException>(() => harness.RunUntil(_ => true, maxSteps: -1));
    }

    [Fact]
    public void RunUntil_NullPredicate_Throws()
    {
        var harness = SimulationHarness.For(new PointMassSimulation());

        Assert.Throws<ArgumentNullException>(() => harness.RunUntil(null!, maxSteps: 1));
    }

    [Fact]
    public void RunUntil_HonoursHeldInputWhileWaiting()
    {
        var harness = SimulationHarness.For(new PointMassSimulation());

        harness.Input.Press(InputCommand.Jump);
        var rising = harness.RunUntil(sim => !sim.IsGrounded, maxSteps: 10, "the player to leave the ground");

        Assert.Equal(1, rising);
        harness.RunUntil(sim => sim.IsGrounded, maxSteps: 600, "the player to land again");
        Assert.True(harness.StepCount > 1);
    }

    [Fact]
    public void RunWhile_StopsAsSoonAsTheConditionFails()
    {
        var harness = SimulationHarness.For(new PointMassSimulation());
        harness.Input.Press(InputCommand.Jump);
        harness.Advance();

        var airborne = harness.RunWhile(sim => !sim.IsGrounded, maxSteps: 600, "the player to land");

        Assert.True(airborne > 1);
        Assert.True(harness.Simulation.IsGrounded);
    }

    [Fact]
    public void RunWhile_NullPredicate_Throws()
    {
        var harness = SimulationHarness.For(new PointMassSimulation());

        Assert.Throws<ArgumentNullException>(() => harness.RunWhile(null!, maxSteps: 1));
    }

    [Fact]
    public void For_WithAnAdapter_DrivesASimulationThatDoesNotImplementIFixedStepSimulation()
    {
        var world = new TickOnlyWorld();
        var harness = SimulationHarness.For(
            world,
            static (w, input, dt) => w.Tick(input.Held, input.Pressed, dt));

        harness.Run(InputScript.Create().Hold(InputCommand.Right, 2).Tap(InputCommand.Jump));

        Assert.Equal(3, world.Ticks);
        Assert.Equal(InputCommand.Right | InputCommand.Jump, world.LastHeld);
        Assert.Equal(InputCommand.Jump, world.LastPressed);
        Assert.Equal(3 * FixedStepClock.FixedDelta, world.Seconds, 6);
    }

    [Fact]
    public void For_WithAnAdapter_AcceptsACustomStepLength()
    {
        var world = new TickOnlyWorld();
        var harness = SimulationHarness.For(
            world,
            static (w, input, dt) => w.Tick(input.Held, input.Pressed, dt),
            fixedDelta: 1f / 120f);

        harness.Advance(4);

        Assert.Equal(1f / 120f, harness.FixedDelta);
        Assert.Equal(4f / 120f, world.Seconds, 6);
    }

    [Fact]
    public void Constructor_NullStepAdapter_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new SimulationHarness<TickOnlyWorld>(new TickOnlyWorld(), null!));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-0.016f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void Constructor_NonPositiveOrNonFiniteStep_Throws(float delta)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SimulationHarness<TickOnlyWorld>(
                new TickOnlyWorld(),
                static (w, input, dt) => w.Tick(input.Held, input.Pressed, dt),
                delta));
    }

    [Fact]
    public void Harness_RunsHeadless_WithNoRenderingOrWindowingDependency()
    {
        var referenced = typeof(SimulationHarness<>).Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain(
            referenced,
            name => name.Contains("Raylib", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Platformer.Desktop", StringComparison.OrdinalIgnoreCase));
    }
}
