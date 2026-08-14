namespace Platformer.Core.Physics;

/// <summary>
/// An axis-aligned bounding box in world units, given by its minimum corner and
/// its size.
/// </summary>
/// <remarks>
/// <para>
/// The origin is the top-left corner and Y increases downwards, matching
/// <see cref="Levels.TileGrid"/> and the renderer. <see cref="Top"/> is
/// therefore numerically smaller than <see cref="Bottom"/>.
/// </para>
/// <para>
/// Edges are treated as half-open by everything that consumes this type: a box
/// occupies <c>[Left, Right)</c> horizontally and <c>[Top, Bottom)</c>
/// vertically. Two boxes that merely touch do not overlap. That rule is what
/// lets a body come to rest exactly flush against a surface without being
/// reported as colliding on the following step — see <see cref="Overlaps"/>.
/// </para>
/// </remarks>
/// <param name="X">World X of the left edge.</param>
/// <param name="Y">World Y of the top edge.</param>
/// <param name="Width">Extent along X; expected to be positive and finite.</param>
/// <param name="Height">Extent along Y; expected to be positive and finite.</param>
public readonly record struct Aabb(float X, float Y, float Width, float Height)
{
    /// <summary>World X of the left edge. Inclusive.</summary>
    public float Left => X;

    /// <summary>World X of the right edge. Exclusive.</summary>
    public float Right => X + Width;

    /// <summary>World Y of the top edge. Inclusive.</summary>
    public float Top => Y;

    /// <summary>World Y of the bottom edge. Exclusive.</summary>
    public float Bottom => Y + Height;

    /// <summary>
    /// Whether this box and <paramref name="other"/> share any area.
    /// </summary>
    /// <remarks>
    /// Strict on all four edges, so boxes that touch exactly are <b>not</b>
    /// overlapping. A body standing on a floor has <c>Bottom</c> equal to the
    /// floor's top edge and is deliberately not in contact by this test; being
    /// grounded is reported by the collision resolver instead, from the fact
    /// that downward motion was stopped. Loosening this to a non-strict
    /// comparison reintroduces the flush-contact flicker it exists to prevent.
    /// </remarks>
    /// <param name="other">Box to test against.</param>
    /// <returns><see langword="true"/> when the boxes genuinely intersect.</returns>
    public bool Overlaps(in Aabb other) =>
        Left < other.Right && Right > other.Left &&
        Top < other.Bottom && Bottom > other.Top;
}
