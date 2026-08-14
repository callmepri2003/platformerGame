using Platformer.Core.Input;

namespace Platformer.Core.Tests.Harness;

/// <summary>
/// A deliberately tiny, fully deterministic stand-in for the real simulation,
/// used to exercise the harness before <c>Platformer.Core</c> has a simulation
/// entry point of its own. It is a point mass on a flat floor at
/// <c>Y == 0</c>: it accelerates horizontally while a direction is held and
/// jumps on the <em>edge</em> of <see cref="InputCommand.Jump"/> while grounded.
/// </summary>
/// <remarks>
/// <para>
/// <b>+Y is down, matching the rest of the project.</b> Tile row 0 is the top
/// row (see <c>Platformer.Core.Levels.TileCoord</c>) and the collider in
/// <c>Platformer.Core.Physics</c> is written to the same convention, so gravity
/// here is <em>positive</em> and a jump impulse is <em>negative</em>. The floor
/// is at <c>Y == 0</c> and the body occupies <c>Y &lt;= 0</c> above it.
/// </para>
/// <para>
/// The harness has no vertical convention of its own — it would work equally
/// well with either — but this file is the most complete worked example of "a
/// simulation" in the repository, so it deliberately teaches the project's
/// convention rather than an arbitrary local one.
/// </para>
/// <para>
/// Nothing here reads a clock or a random number, so replaying the same input
/// always produces the same floats. Delete it once the real simulation lands;
/// the harness itself does not depend on it.
/// </para>
/// </remarks>
internal sealed class PointMassSimulation : IFixedStepSimulation
{
    /// <summary>Positive because +Y is down.</summary>
    private const float Gravity = 900f;
    private const float Acceleration = 1200f;
    private const float Friction = 800f;
    private const float MaxSpeed = 180f;
    /// <summary>Negative because a jump travels towards smaller Y.</summary>
    private const float JumpSpeed = -320f;

    /// <summary>Horizontal position in world units.</summary>
    public float X { get; private set; }

    /// <summary>
    /// Vertical position in world units, increasing downwards. The floor is
    /// zero, so a body in the air has a negative Y.
    /// </summary>
    public float Y { get; private set; }

    /// <summary>Horizontal velocity in world units per second.</summary>
    public float VelocityX { get; private set; }

    /// <summary>Vertical velocity in world units per second.</summary>
    public float VelocityY { get; private set; }

    /// <summary>Whether the point mass is resting on the floor.</summary>
    public bool IsGrounded { get; private set; } = true;

    /// <summary>How many steps this instance has been advanced.</summary>
    public int Steps { get; private set; }

    /// <summary>
    /// How many jumps started. Counts edges, so holding jump for many steps
    /// still counts as one.
    /// </summary>
    public int JumpsStarted { get; private set; }

    /// <inheritdoc />
    public void Advance(IInputSource input, float deltaSeconds)
    {
        ArgumentNullException.ThrowIfNull(input);

        Steps++;

        var move = 0f;
        if ((input.Held & InputCommand.Left) != InputCommand.None)
        {
            move -= 1f;
        }

        if ((input.Held & InputCommand.Right) != InputCommand.None)
        {
            move += 1f;
        }

        if (move != 0f)
        {
            VelocityX = Math.Clamp(VelocityX + (move * Acceleration * deltaSeconds), -MaxSpeed, MaxSpeed);
        }
        else
        {
            var drop = Friction * deltaSeconds;
            VelocityX = MathF.Abs(VelocityX) <= drop
                ? 0f
                : VelocityX - (MathF.Sign(VelocityX) * drop);
        }

        if ((input.Pressed & InputCommand.Jump) != InputCommand.None && IsGrounded)
        {
            VelocityY = JumpSpeed;
            IsGrounded = false;
            JumpsStarted++;
        }

        VelocityY += Gravity * deltaSeconds;
        X += VelocityX * deltaSeconds;
        Y += VelocityY * deltaSeconds;

        // +Y is down, so falling increases Y and the floor is reached from above.
        if (Y >= 0f)
        {
            Y = 0f;
            VelocityY = 0f;
            IsGrounded = true;
        }
    }
}

/// <summary>
/// A simulation shaped <em>differently</em> from <see cref="IFixedStepSimulation"/>:
/// it takes raw command flags rather than an input source, and its method is
/// called <c>Tick</c>. It exists to prove the harness can drive whatever entry
/// point Dev A settles on via a one-line adapter.
/// </summary>
internal sealed class TickOnlyWorld
{
    /// <summary>Commands held on the most recent tick.</summary>
    public InputCommand LastHeld { get; private set; }

    /// <summary>Commands that became active on the most recent tick.</summary>
    public InputCommand LastPressed { get; private set; }

    /// <summary>Number of ticks taken.</summary>
    public int Ticks { get; private set; }

    /// <summary>Simulated seconds accumulated across ticks.</summary>
    public float Seconds { get; private set; }

    /// <summary>Advances the world one tick.</summary>
    /// <param name="held">Commands held this tick.</param>
    /// <param name="pressed">Commands that became active this tick.</param>
    /// <param name="deltaSeconds">Length of the tick in seconds.</param>
    public void Tick(InputCommand held, InputCommand pressed, float deltaSeconds)
    {
        LastHeld = held;
        LastPressed = pressed;
        Ticks++;
        Seconds += deltaSeconds;
    }
}
