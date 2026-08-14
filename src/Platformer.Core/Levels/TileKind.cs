namespace Platformer.Core.Levels;

/// <summary>
/// What occupies a single cell of a <see cref="TileGrid"/>.
/// </summary>
/// <remarks>
/// Collision never switches on this enum; it asks
/// <see cref="TileKindExtensions.IsSolid"/> instead. Adding a kind is therefore
/// a one-line change here plus one line in that mapping, and nothing in the
/// simulation has to be revisited.
/// </remarks>
public enum TileKind
{
    /// <summary>Open air. Nothing blocks movement through this cell.</summary>
    Empty = 0,

    /// <summary>Impassable terrain. Blocks movement on every axis.</summary>
    Solid = 1,
}
