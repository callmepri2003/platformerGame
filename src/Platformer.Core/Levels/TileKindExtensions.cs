namespace Platformer.Core.Levels;

/// <summary>
/// The single place that decides which tile kinds obstruct movement.
/// </summary>
/// <remarks>
/// Keeping this mapping out of the collision code is what lets new kinds (ice,
/// hazards, one-way platforms) be introduced without editing the resolver: the
/// resolver only ever asks whether a tile is solid.
/// </remarks>
public static class TileKindExtensions
{
    /// <summary>Whether a moving body is blocked by this kind of tile.</summary>
    /// <param name="kind">The tile kind to classify.</param>
    /// <returns><see langword="true"/> for obstructing tiles.</returns>
    public static bool IsSolid(this TileKind kind) => kind == TileKind.Solid;
}
