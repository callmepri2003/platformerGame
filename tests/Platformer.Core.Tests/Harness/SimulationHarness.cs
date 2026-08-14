using System.Globalization;
using Platformer.Core.Input;
using Platformer.Core.Time;

namespace Platformer.Core.Tests.Harness;

/// <summary>
/// Entry points for building a <see cref="SimulationHarness{TSimulation}"/>.
/// </summary>
public static class SimulationHarness
{
    /// <summary>
    /// Wraps a simulation that already matches <see cref="IFixedStepSimulation"/>.
    /// </summary>
    /// <typeparam name="TSimulation">The simulation type being driven.</typeparam>
    /// <param name="simulation">The simulation to drive.</param>
    /// <returns>A harness bound to <paramref name="simulation"/>.</returns>
    public static SimulationHarness<TSimulation> For<TSimulation>(TSimulation simulation)
        where TSimulation : IFixedStepSimulation =>
        new(simulation, static (sim, input, dt) => sim.Advance(input, dt));

    /// <summary>
    /// Wraps a simulation of any shape by supplying the one-step adapter.
    /// </summary>
    /// <typeparam name="TSimulation">The simulation type being driven.</typeparam>
    /// <param name="simulation">The simulation to drive.</param>
    /// <param name="step">How to advance <paramref name="simulation"/> one step.</param>
    /// <param name="fixedDelta">
    /// Step length in seconds. Defaults to the simulation rate the game runs at.
    /// </param>
    /// <returns>A harness bound to <paramref name="simulation"/>.</returns>
    public static SimulationHarness<TSimulation> For<TSimulation>(
        TSimulation simulation,
        SimulationStep<TSimulation> step,
        float fixedDelta = FixedStepClock.FixedDelta) =>
        new(simulation, step, fixedDelta);
}

/// <summary>
/// Drives a simulation a fixed number of headless steps, or until a condition
/// holds, feeding it scripted input.
/// </summary>
/// <typeparam name="TSimulation">The simulation type being driven.</typeparam>
/// <remarks>
/// <para>
/// Every step is the same length — <see cref="FixedDelta"/>, taken from
/// <see cref="FixedStepClock.FixedDelta"/> — and comes from a counter, never
/// from the wall clock. There is no timer, no thread and no window here, so a
/// scenario replayed twice produces identical numbers on any machine.
/// </para>
/// <para>
/// The harness owns a <see cref="FakeInputSource"/> and commits it exactly once
/// per step, which is what makes <see cref="Platformer.Core.Input.IInputSource.Pressed"/>
/// mean "became active on this step" inside the simulation under test.
/// </para>
/// <example>
/// Run right for thirty steps, tap jump, and assert on the apex:
/// <code>
/// var harness = SimulationHarness.For(new PlayerSimulation());
///
/// var heights = harness.Run(
///     InputScript.Create()
///         .Hold(InputCommand.Right, 30)
///         .Tap(InputCommand.Jump)
///         .Wait(40),
///     sim =&gt; sim.Player.Position.Y);
///
/// Assert.True(heights.Max() &gt; startHeight + 32f);
/// </code>
/// Or step until something happens, with a cap that fails loudly:
/// <code>
/// harness.Input.Press(InputCommand.Right);
/// var steps = harness.RunUntil(sim =&gt; sim.Player.IsGrounded, maxSteps: 600, "the player lands");
/// </code>
/// If a simulation is not shaped like <see cref="IFixedStepSimulation"/>, bind it
/// with an adapter instead — no harness change is needed:
/// <code>
/// var harness = SimulationHarness.For(world, (w, input, dt) =&gt; w.Tick(input.Held, input.Pressed, dt));
/// </code>
/// </example>
/// </remarks>
public sealed class SimulationHarness<TSimulation>
{
    private readonly SimulationStep<TSimulation> _step;
    private readonly List<InputFrame> _trace = [];

    /// <summary>Creates a harness around a simulation and its step adapter.</summary>
    /// <param name="simulation">The simulation to drive.</param>
    /// <param name="step">How to advance <paramref name="simulation"/> one step.</param>
    /// <param name="fixedDelta">
    /// Step length in seconds. Defaults to the simulation rate the game runs at.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="step"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="fixedDelta"/> is not a positive, finite number.
    /// </exception>
    public SimulationHarness(
        TSimulation simulation,
        SimulationStep<TSimulation> step,
        float fixedDelta = FixedStepClock.FixedDelta)
    {
        ArgumentNullException.ThrowIfNull(step);

        if (!float.IsFinite(fixedDelta) || fixedDelta <= 0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fixedDelta),
                fixedDelta,
                "A fixed step must be a positive, finite number of seconds.");
        }

        Simulation = simulation;
        _step = step;
        FixedDelta = fixedDelta;
    }

    /// <summary>The simulation being driven.</summary>
    public TSimulation Simulation { get; }

    /// <summary>The input the simulation reads. Mutate it to change what is held.</summary>
    public FakeInputSource Input { get; } = new();

    /// <summary>Length of every step, in seconds.</summary>
    public float FixedDelta { get; }

    /// <summary>How many steps have been taken since the harness was created.</summary>
    public int StepCount { get; private set; }

    /// <summary>
    /// Simulated time elapsed, in seconds. Derived from
    /// <see cref="StepCount"/> by multiplication rather than accumulated, so it
    /// carries no rounding drift.
    /// </summary>
    public float ElapsedSeconds => StepCount * FixedDelta;

    /// <summary>Input observed on every step so far, in order.</summary>
    public IReadOnlyList<InputFrame> Trace => _trace;

    /// <summary>
    /// Advances the simulation, holding whatever <see cref="Input"/> currently
    /// has pending. Useful for "keep holding right for another 10 steps".
    /// </summary>
    /// <param name="steps">How many steps to take. Zero is allowed.</param>
    /// <returns>This instance, so calls can be chained.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="steps"/> is negative.
    /// </exception>
    public SimulationHarness<TSimulation> Advance(int steps = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(steps);

        for (var i = 0; i < steps; i++)
        {
            StepOnce(Input.PendingHeld);
        }

        return this;
    }

    /// <summary>Plays every step of a script.</summary>
    /// <param name="script">The scripted input to replay.</param>
    /// <returns>This instance, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="script"/> is null.</exception>
    public SimulationHarness<TSimulation> Run(InputScript script)
    {
        ArgumentNullException.ThrowIfNull(script);

        var frames = script.Frames;
        for (var i = 0; i < frames.Count; i++)
        {
            StepOnce(frames[i]);
        }

        return this;
    }

    /// <summary>
    /// Plays every step of a script, taking one observation after each step.
    /// The returned sequence is the evidence a determinism test compares.
    /// </summary>
    /// <typeparam name="TSample">Type of the per-step observation.</typeparam>
    /// <param name="script">The scripted input to replay.</param>
    /// <param name="sample">Reads the value of interest out of the simulation.</param>
    /// <returns>One sample per step, in order.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="script"/> or <paramref name="sample"/> is null.
    /// </exception>
    public IReadOnlyList<TSample> Run<TSample>(InputScript script, Func<TSimulation, TSample> sample)
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(sample);

        var frames = script.Frames;
        var samples = new List<TSample>(frames.Count);
        for (var i = 0; i < frames.Count; i++)
        {
            StepOnce(frames[i]);
            samples.Add(sample(Simulation));
        }

        return samples;
    }

    /// <summary>
    /// Steps — holding whatever <see cref="Input"/> currently has pending —
    /// until a condition holds. The condition is checked before the first step,
    /// so an already-satisfied condition costs no steps.
    /// </summary>
    /// <param name="predicate">The condition being waited for.</param>
    /// <param name="maxSteps">
    /// Cap on the number of steps. Reaching it is a test failure, not a silent
    /// stop.
    /// </param>
    /// <param name="description">
    /// What is being waited for, phrased to complete "waiting for ...". Used in
    /// the failure message.
    /// </param>
    /// <returns>How many steps were needed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="maxSteps"/> is negative.
    /// </exception>
    /// <exception cref="SimulationTimeoutException">
    /// The condition never held within <paramref name="maxSteps"/> steps.
    /// </exception>
    public int RunUntil(Func<TSimulation, bool> predicate, int maxSteps, string? description = null)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentOutOfRangeException.ThrowIfNegative(maxSteps);

        for (var taken = 0; taken <= maxSteps; taken++)
        {
            if (predicate(Simulation))
            {
                return taken;
            }

            if (taken == maxSteps)
            {
                break;
            }

            StepOnce(Input.PendingHeld);
        }

        var what = description ?? "the condition";
        var seconds = maxSteps * FixedDelta;
        throw new SimulationTimeoutException(string.Create(
            CultureInfo.InvariantCulture,
            $"Gave up waiting for {what} after {maxSteps} steps ({seconds:0.###}s of simulated time; {StepCount} steps taken in total). Either the simulation cannot reach that state or the cap is too low."));
    }

    /// <summary>
    /// Steps while a condition holds, stopping as soon as it stops holding.
    /// </summary>
    /// <param name="predicate">The condition to keep stepping under.</param>
    /// <param name="maxSteps">
    /// Cap on the number of steps. Reaching it is a test failure, not a silent
    /// stop.
    /// </param>
    /// <param name="description">
    /// What is being waited for, phrased to complete "waiting for ...". Used in
    /// the failure message.
    /// </param>
    /// <returns>How many steps were needed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
    /// <exception cref="SimulationTimeoutException">
    /// The condition still held after <paramref name="maxSteps"/> steps.
    /// </exception>
    public int RunWhile(Func<TSimulation, bool> predicate, int maxSteps, string? description = null)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return RunUntil(sim => !predicate(sim), maxSteps, description);
    }

    private void StepOnce(InputCommand held)
    {
        Input.BeginStep(held);
        _trace.Add(new InputFrame(StepCount, Input.Held, Input.Pressed));
        StepCount++;
        _step(Simulation, Input, FixedDelta);
    }
}
