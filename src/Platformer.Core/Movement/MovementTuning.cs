namespace Platformer.Core.Movement;

/// <summary>
/// Every number that decides how the player feels to control.
/// </summary>
/// <remarks>
/// <para>
/// One type, deliberately. Movement feel is the sum of these values rather than
/// any one of them, so they have to be readable together and changeable
/// together. Scattering them as literals through the update is how a tweak to
/// friction silently costs 8% of jump height.
/// </para>
/// <para>
/// Values are init-only with defaults, so a test can vary one of them with
/// <c>Tuning with { GroundFriction = 0f }</c> without editing the defaults every
/// other test depends on, and later issues can add fields without touching the
/// call sites that already exist.
/// </para>
/// <para>
/// <b>These numbers are provisional.</b> They were chosen against the targets
/// stated on each one, not by playing the game — nothing renders yet, so nobody
/// has felt them. They are arithmetic that satisfies an intention. Expect them
/// to move once the game is visible.
/// </para>
/// </remarks>
public sealed record MovementTuning
{
    /// <summary>The tuning the game ships with.</summary>
    public static MovementTuning Default { get; } = new();

    /// <summary>
    /// Fastest the player runs, in world units per second.
    /// </summary>
    /// <remarks>
    /// 120 u/s crosses the 320-unit-wide test level in about 2.7 seconds:
    /// brisk enough to feel responsive, slow enough that a 20-tile level is not
    /// over in a blink.
    /// </remarks>
    public float MaxSpeed { get; init; } = 120f;

    /// <summary>
    /// Ground acceleration while a direction is held, in world units per second
    /// squared.
    /// </summary>
    /// <remarks>
    /// Targets the issue's "roughly 0.1s to top speed":
    /// <see cref="MaxSpeed"/> / 1200 = 0.1s exactly.
    /// </remarks>
    public float GroundAcceleration { get; init; } = 1200f;

    /// <summary>
    /// Ground deceleration with no direction held, in world units per second
    /// squared.
    /// </summary>
    /// <remarks>
    /// Targets "stopping slightly less" than the 0.1s to top speed:
    /// <see cref="MaxSpeed"/> / 1500 = 0.08s. Stopping faster than starting is
    /// what makes the player feel planted rather than skating.
    /// </remarks>
    public float GroundFriction { get; init; } = 1500f;

    /// <summary>
    /// Ground deceleration while holding the direction opposite to travel, in
    /// world units per second squared.
    /// </summary>
    /// <remarks>
    /// Twice <see cref="GroundFriction"/>, which is what makes a turn feel
    /// sharp: reversing at top speed takes 0.04s to reach a standstill against
    /// friction's 0.08s, so changing your mind beats stopping and starting
    /// again. The issue requires this to exceed the neutral friction and it
    /// does, by design rather than by accident.
    /// </remarks>
    public float GroundTurnAcceleration { get; init; } = 3000f;

    /// <summary>
    /// Airborne acceleration while a direction is held, in world units per
    /// second squared.
    /// </summary>
    /// <remarks>
    /// Half of <see cref="GroundAcceleration"/>. Air control that matches the
    /// ground makes a jump feel like a hover; removing it entirely makes a
    /// mistimed jump unrecoverable and reads as unfair. Half is the
    /// conventional compromise and is the value most likely to move once the
    /// game can be played.
    /// </remarks>
    public float AirAcceleration { get; init; } = 600f;

    /// <summary>
    /// Airborne deceleration with no direction held, in world units per second
    /// squared.
    /// </summary>
    /// <remarks>
    /// Much weaker than <see cref="GroundFriction"/>, so a jump carries its
    /// momentum instead of stalling in mid-air. This is the number that decides
    /// how far a running jump travels.
    /// </remarks>
    public float AirFriction { get; init; } = 200f;

    /// <summary>
    /// Airborne deceleration while holding the direction opposite to travel, in
    /// world units per second squared.
    /// </summary>
    /// <remarks>
    /// Half of <see cref="GroundTurnAcceleration"/>, keeping air control
    /// uniformly weaker than ground control rather than making mid-air
    /// direction changes a special case.
    /// </remarks>
    public float AirTurnAcceleration { get; init; } = 1500f;

    /// <summary>
    /// Downward acceleration, in world units per second squared.
    /// </summary>
    /// <remarks>
    /// <b>Placeholder, owned by the jump issue.</b> Horizontal movement needs
    /// gravity to exist at all: being grounded is reported by the collider when
    /// downward motion is stopped, so with no gravity the player is never
    /// grounded, ground friction never applies, and nothing can fall into the
    /// pit. This is therefore the simplest gravity that works — one constant,
    /// no rising/falling asymmetry and no terminal velocity, both of which
    /// belong to the variable-height jump and will replace this.
    /// </remarks>
    public float Gravity { get; init; } = 1000f;

    /// <summary>
    /// How far below the bottom of the level the death plane sits, measured in
    /// tiles.
    /// </summary>
    /// <remarks>
    /// Expressed in tiles and applied against the level's own height, so it
    /// follows any level rather than being a coordinate that happens to suit
    /// one map. Four tiles is far enough that the player is unambiguously gone
    /// — well past any geometry and off the bottom of the screen — rather than
    /// being snatched back while still arguably in play.
    /// </remarks>
    public float DeathPlaneMarginTiles { get; init; } = 4f;
}
