using System.Numerics;
using Platformer.Core.Levels;
using Platformer.Core.Physics;
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
    /// Draws a body as the rectangle it actually collides with, interpolated
    /// between the last two simulation states.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The parameters are <see cref="Aabb"/> — the very type the collider
    /// resolves against tiles — rather than a position and a separately supplied
    /// size. There is therefore no second description of the player's shape that
    /// could drift from the physical one: if the box the game draws is wrong,
    /// the box the game collides with is wrong in exactly the same way, and the
    /// bug is visible instead of hidden.
    /// </para>
    /// <para>
    /// Interpolating by <see cref="Platformer.Core.Time.FixedStepClock.Alpha"/>
    /// is what stops motion stuttering on a display running faster than the
    /// 60 Hz simulation. The simulation lands on discrete positions; the renderer
    /// shows where the body is part-way between the last two. Only the position
    /// is interpolated — a collision box does not change size between steps, and
    /// interpolating its extent would draw a shape the collider never had.
    /// </para>
    /// </remarks>
    /// <param name="previous">The body after the previous simulation step.</param>
    /// <param name="current">The body after the most recent step.</param>
    /// <param name="alpha">Fraction of a step elapsed since the last one, in [0, 1).</param>
    /// <param name="colour">Colour to fill it with.</param>
    public static void DrawBody(in Aabb previous, in Aabb current, float alpha, Color colour)
    {
        var position = Vector2.Lerp(
            new Vector2(previous.X, previous.Y),
            new Vector2(current.X, current.Y),
            alpha);

        Raylib.DrawRectangleRec(
            new Rectangle(position.X, position.Y, current.Width, current.Height),
            colour);
    }
}
