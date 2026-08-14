using Platformer.Core.Input;

namespace Platformer.Core.Tests.Harness;

/// <summary>
/// Test double for <see cref="IInputSource"/> that a test drives by hand, one
/// simulation step at a time.
/// </summary>
/// <remarks>
/// <para>
/// The source keeps a <em>pending</em> mask that <see cref="Press"/>,
/// <see cref="Release"/>, <see cref="ReleaseAll"/> and <see cref="SetHeld"/>
/// mutate. Nothing the simulation observes changes until
/// <see cref="BeginStep()"/> commits that mask, which happens exactly once per
/// simulation step. Committing is what produces the edge:
/// <see cref="Pressed"/> is set to the commands that are held now but were not
/// held on the previous step, so a command that stays down reports
/// <see cref="Pressed"/> on its first step only.
/// </para>
/// <para>
/// <see cref="SimulationHarness{TSimulation}"/> owns the commit for you; call
/// <see cref="BeginStep()"/> directly only when testing input in isolation.
/// </para>
/// <example>
/// Tapping jump while running right:
/// <code>
/// var input = new FakeInputSource();
///
/// input.Press(InputCommand.Right).BeginStep();
/// // Held == Right, Pressed == Right
///
/// input.Press(InputCommand.Jump).BeginStep();
/// // Held == Right | Jump, Pressed == Jump   (Right is no longer an edge)
///
/// input.BeginStep();
/// // Held == Right | Jump, Pressed == None   (nothing became active)
/// </code>
/// </example>
/// </remarks>
public sealed class FakeInputSource : IInputSource
{
    /// <inheritdoc />
    public InputCommand Held { get; private set; }

    /// <inheritdoc />
    public InputCommand Pressed { get; private set; }

    /// <summary>
    /// Mask that the next <see cref="BeginStep()"/> will commit. Reading it
    /// lets a caller resume from the current hold rather than restating it.
    /// </summary>
    public InputCommand PendingHeld { get; private set; }

    /// <summary>Adds commands to the pending mask without ending the step.</summary>
    /// <param name="commands">Commands to start holding.</param>
    /// <returns>This instance, so calls can be chained.</returns>
    public FakeInputSource Press(InputCommand commands)
    {
        PendingHeld |= commands;
        return this;
    }

    /// <summary>Removes commands from the pending mask without ending the step.</summary>
    /// <param name="commands">Commands to stop holding.</param>
    /// <returns>This instance, so calls can be chained.</returns>
    public FakeInputSource Release(InputCommand commands)
    {
        PendingHeld &= ~commands;
        return this;
    }

    /// <summary>Clears every held command, leaving the committed state alone.</summary>
    /// <returns>This instance, so calls can be chained.</returns>
    public FakeInputSource ReleaseAll()
    {
        PendingHeld = InputCommand.None;
        return this;
    }

    /// <summary>Replaces the pending mask outright.</summary>
    /// <param name="commands">The exact set of commands to hold next step.</param>
    /// <returns>This instance, so calls can be chained.</returns>
    public FakeInputSource SetHeld(InputCommand commands)
    {
        PendingHeld = commands;
        return this;
    }

    /// <summary>
    /// Commits the pending mask as the input for one simulation step and
    /// recomputes the <see cref="Pressed"/> edge against the previous step.
    /// </summary>
    public void BeginStep()
    {
        Pressed = PendingHeld & ~Held;
        Held = PendingHeld;
    }

    /// <summary>
    /// Sets the pending mask and commits it in one call. Equivalent to
    /// <see cref="SetHeld"/> followed by <see cref="BeginStep()"/>.
    /// </summary>
    /// <param name="held">The exact set of commands held during this step.</param>
    public void BeginStep(InputCommand held)
    {
        SetHeld(held);
        BeginStep();
    }

    /// <summary>
    /// Returns the source to its initial state. The next command to be held
    /// after a reset reports as an edge again.
    /// </summary>
    public void Reset()
    {
        PendingHeld = InputCommand.None;
        Held = InputCommand.None;
        Pressed = InputCommand.None;
    }
}
