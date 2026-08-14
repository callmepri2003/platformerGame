using Platformer.Core.Levels;
using Platformer.Core.Movement;
using Raylib_cs;

namespace Platformer.Desktop;

/// <summary>
/// Draws the world at virtual resolution. World units are virtual pixels, so
/// there is no transform between the two: what the simulation calls position
/// 88.0 is the pixel the player is drawn at.
/// </summary>
internal static class WorldRenderer
{
    /// <summary>Draws every solid tile in a grid, and nothing for empty ones.</summary>
    /// <param name="grid">The level geometry.</param>
    /// <param name="colour">Colour for solid tiles.</param>
    public static void DrawTiles(TileGrid grid, Color colour)
    {
        ArgumentNullException.ThrowIfNull(grid);

        var size = (int)grid.TileSize;

        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                if (!grid.IsSolid(x, y))
                {
                    continue;
                }

                var corner = grid.TileToWorld(x, y);
                Raylib.DrawRectangle((int)corner.X, (int)corner.Y, size, size, colour);
            }
        }
    }

    /// <summary>
    /// Draws the player as the rectangle it actually collides with, at the
    /// position it occupies part-way through the current simulation step.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The size comes from <see cref="PlayerBody.Bounds"/> — the box
    /// <see cref="Platformer.Core.Physics.TileCollider"/> resolves against tiles
    /// — so there is no second description of the player's shape that could
    /// drift from the physical one. If the drawn box is wrong, the collided box
    /// is wrong identically, and the bug is visible instead of hidden.
    /// </para>
    /// <para>
    /// The position comes from <see cref="PlayerBody.InterpolatedPosition"/>
    /// rather than from a blend performed here. The simulation advances at a
    /// fixed 60 Hz while the screen refreshes at whatever rate it likes, so
    /// drawing raw simulation positions stutters — but the correct blend is not
    /// simply "lerp the last two positions". A respawn moves the player without
    /// travelling, and interpolating across that would smear them across the
    /// level. The body knows which of its movements were travel; the renderer
    /// does not, and must not have to.
    /// </para>
    /// </remarks>
    /// <param name="player">The player to draw.</param>
    /// <param name="alpha">
    /// Fraction of a fixed step already elapsed, from
    /// <see cref="Platformer.Core.Time.FixedStepClock.Alpha"/>.
    /// </param>
    /// <param name="colour">Colour to fill the box with.</param>
    public static void DrawBody(PlayerBody player, float alpha, Color colour)
    {
        ArgumentNullException.ThrowIfNull(player);

        // Ask the body; do not lerp here. It is the only thing that knows which
        // of its movements were travel: a respawn moves the player without
        // travelling, and a blend written at this line would smear them across
        // the level on every death. The bug appears only when the player dies,
        // which is why this line is worth leaving alone.
        var position = player.InterpolatedPosition(alpha);

        Raylib.DrawRectangleRec(
            new Rectangle(position.X, position.Y, player.Bounds.Width, player.Bounds.Height),
            colour);
    }
}
