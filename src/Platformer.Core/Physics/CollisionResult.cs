using System.Numerics;

namespace Platformer.Core.Physics;

/// <summary>
/// Outcome of moving a body one step through the tile grid.
/// </summary>
/// <remarks>
/// <see cref="Velocity"/> is returned already corrected: the resolver zeroes the
/// component of any axis it stopped, because it is the only thing that knows an
/// axis was blocked. Callers should adopt it rather than keeping their own copy,
/// otherwise a body held against a wall silently accumulates speed that is
/// released the moment it turns around.
/// </remarks>
/// <param name="Box">Where the body ended up, already resolved out of geometry.</param>
/// <param name="Velocity">Velocity after zeroing any blocked axis.</param>
/// <param name="Contacts">What was hit on this step.</param>
public readonly record struct CollisionResult(Aabb Box, Vector2 Velocity, TileContacts Contacts)
{
    /// <summary>Whether downward motion was stopped this step.</summary>
    public bool IsGrounded => (Contacts & TileContacts.Ground) != 0;

    /// <summary>Whether upward motion was stopped this step.</summary>
    public bool HitCeiling => (Contacts & TileContacts.Ceiling) != 0;

    /// <summary>Whether horizontal motion was stopped this step, on either side.</summary>
    public bool HitWall => (Contacts & (TileContacts.WallLeft | TileContacts.WallRight)) != 0;
}
