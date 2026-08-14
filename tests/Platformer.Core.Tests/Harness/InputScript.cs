using Platformer.Core.Input;

namespace Platformer.Core.Tests.Harness;

/// <summary>
/// Fluent builder for a fixed sequence of per-step held-command masks.
/// </summary>
/// <remarks>
/// <para>
/// A script is built from two kinds of call. <see cref="Press"/>,
/// <see cref="Release"/> and <see cref="ReleaseAll"/> change what is held but
/// emit no steps. <see cref="Wait"/> emits steps at whatever is currently held.
/// <see cref="Hold"/>, <see cref="Tap"/> and <see cref="Idle"/> are sugar over
/// those two kinds.
/// </para>
/// <para>
/// The result is an ordered list of masks — one per simulation step — held in
/// <see cref="Frames"/>. It contains no timestamps and no randomness, so
/// replaying the same script always drives the simulation identically.
/// </para>
/// <example>
/// Run right for thirty steps, then tap jump on the thirty-first while still
/// running:
/// <code>
/// var script = InputScript.Create()
///     .Hold(InputCommand.Right, 30)
///     .Tap(InputCommand.Jump)
///     .Wait(20);
///
/// // Frames[0..29]  == Right
/// // Frames[30]     == Right | Jump   (Jump reports Pressed here and nowhere else)
/// // Frames[31..50] == Right
/// </code>
/// </example>
/// </remarks>
public sealed class InputScript
{
    private readonly List<InputCommand> _frames = [];

    /// <summary>Starts an empty script with nothing held.</summary>
    /// <returns>A new, empty script.</returns>
    public static InputScript Create() => new();

    /// <summary>The held mask for each step, in order.</summary>
    public IReadOnlyList<InputCommand> Frames => _frames;

    /// <summary>Number of simulation steps this script covers.</summary>
    public int Length => _frames.Count;

    /// <summary>
    /// Commands held right now while building. Steps emitted next will use it.
    /// </summary>
    public InputCommand CurrentHeld { get; private set; }

    /// <summary>Starts holding commands. Emits no steps on its own.</summary>
    /// <param name="commands">Commands to start holding.</param>
    /// <returns>This instance, so calls can be chained.</returns>
    public InputScript Press(InputCommand commands)
    {
        CurrentHeld |= commands;
        return this;
    }

    /// <summary>Stops holding commands. Emits no steps on its own.</summary>
    /// <param name="commands">Commands to stop holding.</param>
    /// <returns>This instance, so calls can be chained.</returns>
    public InputScript Release(InputCommand commands)
    {
        CurrentHeld &= ~commands;
        return this;
    }

    /// <summary>Stops holding everything. Emits no steps on its own.</summary>
    /// <returns>This instance, so calls can be chained.</returns>
    public InputScript ReleaseAll()
    {
        CurrentHeld = InputCommand.None;
        return this;
    }

    /// <summary>Emits steps at whatever is currently held.</summary>
    /// <param name="steps">How many steps to emit. Zero is allowed.</param>
    /// <returns>This instance, so calls can be chained.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="steps"/> is negative.
    /// </exception>
    public InputScript Wait(int steps)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(steps);

        for (var i = 0; i < steps; i++)
        {
            _frames.Add(CurrentHeld);
        }

        return this;
    }

    /// <summary>
    /// Holds commands for a number of steps and keeps holding them afterwards.
    /// </summary>
    /// <param name="commands">Commands to hold.</param>
    /// <param name="steps">How many steps to emit while holding them.</param>
    /// <returns>This instance, so calls can be chained.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="steps"/> is negative.
    /// </exception>
    public InputScript Hold(InputCommand commands, int steps) => Press(commands).Wait(steps);

    /// <summary>
    /// Holds commands for a number of steps and then releases them, leaving
    /// everything else held untouched.
    /// </summary>
    /// <param name="commands">Commands to tap.</param>
    /// <param name="steps">
    /// How many steps to hold them for. One step by default, which is the
    /// shortest press the simulation can observe.
    /// </param>
    /// <returns>This instance, so calls can be chained.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="steps"/> is negative.
    /// </exception>
    public InputScript Tap(InputCommand commands, int steps = 1) =>
        Press(commands).Wait(steps).Release(commands);

    /// <summary>Releases everything and emits steps with no input at all.</summary>
    /// <param name="steps">How many empty steps to emit.</param>
    /// <returns>This instance, so calls can be chained.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="steps"/> is negative.
    /// </exception>
    public InputScript Idle(int steps) => ReleaseAll().Wait(steps);

    /// <summary>
    /// Appends another script's steps to this one. The appended steps are
    /// replayed exactly as recorded; the current held mask is then whatever the
    /// appended script finished on.
    /// </summary>
    /// <param name="other">Script whose steps are appended.</param>
    /// <returns>This instance, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is null.</exception>
    public InputScript Then(InputScript other)
    {
        ArgumentNullException.ThrowIfNull(other);

        _frames.AddRange(other._frames);
        CurrentHeld = other.CurrentHeld;
        return this;
    }
}
