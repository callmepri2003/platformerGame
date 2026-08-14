namespace Platformer.Core.Physics;

/// <summary>
/// What stopped a body during a single call to <see cref="TileCollider.Move"/>.
/// </summary>
/// <remarks>
/// These describe <b>this step only</b>. The resolver is a pure function with no
/// memory, so it cannot report transitions such as "just left the ground".
/// Mechanics that need an edge — coyote time, jump buffering, landing effects —
/// keep the previous step's flags themselves and compare.
/// </remarks>
[Flags]
public enum TileContacts
{
    /// <summary>Nothing was hit.</summary>
    None = 0,

    /// <summary>
    /// Downward motion was stopped: the body is standing on something.
    /// </summary>
    /// <remarks>
    /// This means "was stopped moving down this step", not "there is floor
    /// beneath me". It is produced by the resolution itself rather than by a
    /// separate probe, which is what makes it free of any tolerance constant. A
    /// caller that moves with no downward velocity at all will not see it, so
    /// gravity must be applied every step — which it is.
    /// </remarks>
    Ground = 1 << 0,

    /// <summary>Upward motion was stopped: the body hit a ceiling.</summary>
    Ceiling = 1 << 1,

    /// <summary>Leftward motion was stopped: there is a wall to the left.</summary>
    WallLeft = 1 << 2,

    /// <summary>Rightward motion was stopped: there is a wall to the right.</summary>
    WallRight = 1 << 3,
}
