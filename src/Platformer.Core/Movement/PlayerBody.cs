using System.Numerics;
using Platformer.Core.Input;
using Platformer.Core.Levels;
using Platformer.Core.Physics;

namespace Platformer.Core.Movement;

/// <summary>
/// The player: a box in a level, with the velocity and contact state that
/// movement is built from.
/// </summary>
/// <remarks>
/// <para>
/// This is the first entity in the simulation and the thing later movement work
/// hangs off. It owns its position, its velocity and what it is touching;
/// resolving that movement against geometry belongs to
/// <see cref="TileCollider"/>, and the numbers belong to
/// <see cref="MovementTuning"/>.
/// </para>
/// <para>
/// Headless and deterministic. It reads <see cref="InputCommand"/> rather than
/// any device, and advances only when <see cref="Advance"/> is called, so a
/// test drives it with no window and no clock.
/// </para>
/// </remarks>
public sealed class PlayerBody
{
    /// <summary>Width of the player's collision box in world units.</summary>
    /// <remarks>
    /// Narrower than a tile so the player fits through a single-tile gap
    /// without pixel-perfect alignment.
    /// </remarks>
    public const float DefaultWidth = 12f;

    /// <summary>Height of the player's collision box in world units.</summary>
    /// <remarks>Exactly one tile, so a one-tile corridor is passable.</remarks>
    public const float DefaultHeight = 16f;

    /// <summary>Places a player at the level's spawn point.</summary>
    /// <param name="level">The level to move around in.</param>
    /// <param name="tuning">Feel values. Defaults to <see cref="MovementTuning.Default"/>.</param>
    /// <param name="width">Collision box width in world units.</param>
    /// <param name="height">Collision box height in world units.</param>
    /// <exception cref="ArgumentNullException"><paramref name="level"/> is null.</exception>
    public PlayerBody(
        Level level,
        MovementTuning? tuning = null,
        float width = DefaultWidth,
        float height = DefaultHeight)
    {
        ArgumentNullException.ThrowIfNull(level);

        Level = level;
        Tuning = tuning ?? MovementTuning.Default;
        Width = width;
        Height = height;

        Respawn();
    }

    /// <summary>The level this player moves around in.</summary>
    public Level Level { get; }

    /// <summary>The values deciding how this player feels to control.</summary>
    public MovementTuning Tuning { get; }

    /// <summary>Width of the collision box in world units.</summary>
    public float Width { get; }

    /// <summary>Height of the collision box in world units.</summary>
    public float Height { get; }

    /// <summary>Where the player is now, as a collision box.</summary>
    public Aabb Bounds { get; private set; }

    /// <summary>Top-left corner of the player in world units.</summary>
    public Vector2 Position => new(Bounds.X, Bounds.Y);

    /// <summary>
    /// Where the player was at the start of the previous fixed step.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exists so that a renderer can interpolate. The simulation advances
    /// at a fixed 60 Hz while the screen refreshes at whatever rate it likes,
    /// so drawing the simulation's raw position makes motion stutter on any
    /// faster display. A renderer draws between this and <see cref="Position"/>
    /// using the clock's alpha — see <see cref="InterpolatedPosition"/>.
    /// </para>
    /// <para>
    /// It is deliberately part of the public surface rather than an internal
    /// field a renderer happens to be able to reach, because a renderer is
    /// required to use it.
    /// </para>
    /// <para>
    /// <see cref="Teleport"/> sets it equal to the current position. Any
    /// movement that is not travel — a spawn, a respawn — must do the same, or
    /// the renderer will smoothly interpolate across the jump and the player
    /// will visibly smear from where they died to where they reappeared.
    /// </para>
    /// </remarks>
    public Vector2 PreviousPosition { get; private set; }

    /// <summary>Current velocity in world units per second.</summary>
    public Vector2 Velocity { get; private set; }

    /// <summary>What the player was stopped by on the most recent step.</summary>
    public TileContacts Contacts { get; private set; }

    /// <summary>Whether the player is standing on something.</summary>
    public bool IsGrounded => (Contacts & TileContacts.Ground) != 0;

    /// <summary>
    /// World Y at or past which the player is considered to have fallen out of
    /// the level.
    /// </summary>
    /// <remarks>
    /// Derived from the level's own height plus
    /// <see cref="MovementTuning.DeathPlaneMarginTiles"/>, so it follows
    /// whatever level is loaded instead of being a coordinate that suits one
    /// map. Out of bounds is empty, so below the level there is no geometry at
    /// all — not even the side walls — and without this the player falls
    /// forever.
    /// </remarks>
    public float DeathPlaneY =>
        Level.Tiles.WorldHeight + (Tuning.DeathPlaneMarginTiles * Level.Tiles.TileSize);

    /// <summary>
    /// Position to draw at, blended between the previous and current steps.
    /// </summary>
    /// <param name="alpha">
    /// Fraction of a fixed step already elapsed, from
    /// <see cref="Time.FixedStepClock.Alpha"/>. Values in [0, 1].
    /// </param>
    /// <returns>The interpolated top-left corner in world units.</returns>
    public Vector2 InterpolatedPosition(float alpha) =>
        Vector2.Lerp(PreviousPosition, Position, alpha);

    /// <summary>
    /// Advances the player by one fixed step: applies input, then resolves the
    /// movement against the level.
    /// </summary>
    /// <param name="held">Directions held during this step.</param>
    /// <param name="deltaSeconds">Length of the step in seconds.</param>
    public void Advance(InputCommand held, float deltaSeconds)
    {
        // Snapshot before anything moves, so the renderer always has the two
        // ends of exactly one step to blend between.
        PreviousPosition = Position;

        var velocity = new Vector2(
            HorizontalVelocity(held, deltaSeconds),
            Velocity.Y + (Tuning.Gravity * deltaSeconds));

        var result = TileCollider.Move(Bounds, velocity, deltaSeconds, Level.Tiles);

        Bounds = result.Box;
        Velocity = result.Velocity;
        Contacts = result.Contacts;

        if (Bounds.Top >= DeathPlaneY)
        {
            Respawn();
        }
    }

    /// <summary>
    /// Moves the player somewhere without travelling there.
    /// </summary>
    /// <remarks>
    /// Velocity, contacts and the interpolation snapshot are all reset, because
    /// none of them describe the new position: carrying velocity across a
    /// teleport drags the player onward from where they arrive, stale contacts
    /// claim ground that may not be there, and a stale snapshot makes the
    /// renderer draw the journey.
    /// </remarks>
    /// <param name="topLeft">Where to put the player's top-left corner.</param>
    public void Teleport(Vector2 topLeft)
    {
        Bounds = new Aabb(topLeft.X, topLeft.Y, Width, Height);
        PreviousPosition = topLeft;
        Velocity = Vector2.Zero;
        Contacts = TileContacts.None;
    }

    /// <summary>Returns the player to the level's spawn point.</summary>
    /// <remarks>
    /// A death plane and nothing more: the player reappears at the start,
    /// upright and able to move. There is no life to lose and no death to
    /// animate.
    /// </remarks>
    public void Respawn() => Teleport(Level.SpawnTopLeft(Width, Height));

    /// <summary>
    /// Horizontal velocity after one step of acceleration, friction or
    /// turnaround.
    /// </summary>
    private float HorizontalVelocity(InputCommand held, float deltaSeconds)
    {
        var direction = 0;
        if ((held & InputCommand.Left) != InputCommand.None)
        {
            direction--;
        }

        if ((held & InputCommand.Right) != InputCommand.None)
        {
            direction++;
        }

        var speed = Velocity.X;
        var grounded = IsGrounded;

        // Holding both directions cancels to zero and is treated as holding
        // nothing, which decelerates to a standstill rather than fighting
        // between the two and jittering.
        if (direction == 0)
        {
            var friction = (grounded ? Tuning.GroundFriction : Tuning.AirFriction) * deltaSeconds;

            // Stop exactly at zero. Subtracting past it would leave the player
            // creeping backwards a fraction of a unit every step.
            return MathF.Abs(speed) <= friction ? 0f : speed - (MathF.Sign(speed) * friction);
        }

        // Turning around is a separate, stronger rate than accelerating from a
        // standstill: it is what makes a direction change feel deliberate
        // rather than mushy.
        var turning = speed != 0f && MathF.Sign(speed) != direction;
        var acceleration = grounded
            ? (turning ? Tuning.GroundTurnAcceleration : Tuning.GroundAcceleration)
            : (turning ? Tuning.AirTurnAcceleration : Tuning.AirAcceleration);

        return Math.Clamp(
            speed + (direction * acceleration * deltaSeconds),
            -Tuning.MaxSpeed,
            Tuning.MaxSpeed);
    }
}
