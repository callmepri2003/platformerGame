using System.Numerics;

namespace Platformer.Core.Levels;

/// <summary>
/// A parsed level: the tiles plus the places the game needs to know about
/// before it can start, which for now is only where the player begins.
/// </summary>
public sealed class Level
{
    /// <summary>Creates a level.</summary>
    /// <param name="tiles">The tile grid. Must not be null.</param>
    /// <param name="playerSpawn">
    /// Where the player's feet start, in world units. See
    /// <see cref="PlayerSpawn"/> for why this is a foot position rather than a
    /// corner.
    /// </param>
    /// <param name="name">Name of the source the level came from, for diagnostics.</param>
    public Level(TileGrid tiles, Vector2 playerSpawn, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(tiles);

        Tiles = tiles;
        PlayerSpawn = playerSpawn;
        Name = name;
    }

    /// <summary>The level geometry.</summary>
    public TileGrid Tiles { get; }

    /// <summary>
    /// Where the player's feet start: the bottom-centre of the spawn tile, in
    /// world units.
    /// </summary>
    /// <remarks>
    /// Deliberately a foot position and not a bounding-box corner, because the
    /// level does not know how big the player is and should not have to. The
    /// spawn tile itself is empty and its bottom edge is the surface the player
    /// rests on, so a body of any height placed with
    /// <see cref="SpawnTopLeft"/> sits exactly flush on that surface — touching
    /// it, never overlapping it. Spawning a body one pixel inside the floor is
    /// how a level starts the player embedded in geometry, and the collision
    /// rules treat a pre-existing overlap differently from a fresh one, so the
    /// bug would show up later as a mysterious first-frame push.
    /// </remarks>
    public Vector2 PlayerSpawn { get; }

    /// <summary>Name of the source this level came from, when one was given.</summary>
    public string? Name { get; }

    /// <summary>
    /// Top-left corner for a body of the given size standing at
    /// <see cref="PlayerSpawn"/>: horizontally centred on the spawn tile and
    /// resting exactly on its bottom edge.
    /// </summary>
    /// <param name="width">Width of the body in world units.</param>
    /// <param name="height">Height of the body in world units.</param>
    /// <returns>The body's top-left corner in world units.</returns>
    public Vector2 SpawnTopLeft(float width, float height) =>
        new(PlayerSpawn.X - (width * 0.5f), PlayerSpawn.Y - height);
}
