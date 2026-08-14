using System.Numerics;
using Platformer.Core.Levels;

namespace Platformer.Core.Physics;

/// <summary>
/// Moves an axis-aligned body through a <see cref="TileGrid"/> and stops it at
/// solid tiles.
/// </summary>
/// <remarks>
/// <para>
/// Pure and stateless: identical inputs always produce bit-identical outputs, so
/// the simulation stays reproducible in tests and across runs.
/// </para>
/// <para>
/// <b>Axes are resolved separately.</b> X is applied and resolved first, then Y
/// is applied and resolved against the already-corrected X. Resolving both at
/// once forces the resolver to decide which way to push a body out from the
/// overlap alone, and the usual answer — displace along whichever axis is
/// penetrated least — is ambiguous at exactly the places it matters. Run into a
/// wall whose top edge sits a hair below your feet and the vertical penetration
/// is marginally shallower, so the body is popped up on top of the wall it ran
/// into. Separating the axes removes the question: in the X pass the only legal
/// displacement is opposite the X motion, and in the Y pass opposite the Y
/// motion. The outcome follows from where the body was going rather than from
/// which of two overlaps happened to round smaller.
/// </para>
/// <para>
/// X runs first so that the Y pass always operates on a horizontally legal box.
/// Running into a wall and then falling therefore reads as a slide down the wall
/// face rather than a snag on its corner. Either order works; this one is fixed
/// and documented so the behaviour cannot drift.
/// </para>
/// <para>
/// <b>There is no epsilon.</b> Each pass performs one move and at most one
/// resolution — there is no iterate-until-settled loop, so there is nothing to
/// converge and no tolerance to tune. Contact is stable because the overlap test
/// is strict (see <see cref="Aabb.Overlaps"/>) and resolution snaps the body
/// exactly onto the blocking face. A resting body is flush, which is not
/// colliding, and every step it re-attempts a small downward move, is caught,
/// and is snapped back to the identical float.
/// </para>
/// <para>
/// <b>Tunnelling is solved</b> for axis-aligned motion. Each pass scans the
/// whole span between the body's old and new position, nearest tile first, so
/// there is no speed at which a single-tile-thick wall becomes passable. Two
/// limits remain, both conservative: because the axes resolve in sequence the
/// swept region is an L rather than the true swept polygon, and each pass tests
/// a bounding span rather than an exact one. At extreme speed either can stop a
/// body slightly early against a tile a perfect sweep would have missed. Both
/// fail towards stopping, never towards falling through the world.
/// </para>
/// </remarks>
public static class TileCollider
{
    /// <summary>
    /// Tile indices are clamped to this magnitude before use.
    /// </summary>
    /// <remarks>
    /// Converting a float that lies outside <see cref="int"/>'s range is
    /// unspecified in an unchecked context, and the scan bounds adjust indices
    /// by one. Clamping well inside the range makes an absurd (but finite)
    /// velocity produce an empty scan instead of an overflowed one.
    /// </remarks>
    private const int TileIndexLimit = 1 << 29;

    /// <summary>
    /// Advances <paramref name="box"/> by <paramref name="velocity"/> for one
    /// step and resolves it against the solid tiles of <paramref name="grid"/>.
    /// </summary>
    /// <param name="box">Body to move. Must be finite, with positive extents.</param>
    /// <param name="velocity">World units per second. Must be finite.</param>
    /// <param name="dt">Step length in seconds. Must be finite and not negative.</param>
    /// <param name="grid">Level to collide against.</param>
    /// <returns>The resolved box, the corrected velocity, and what was hit.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="grid"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Any input is non-finite, <paramref name="dt"/> is negative, or the box has
    /// a non-positive extent. These are rejected rather than absorbed because a
    /// non-finite position propagates silently and irreversibly: once a body's
    /// coordinates are NaN every later frame is too, and the symptom ("the
    /// player vanished") appears arbitrarily far from the cause.
    /// </exception>
    public static CollisionResult Move(in Aabb box, Vector2 velocity, float dt, TileGrid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ValidateBox(box);
        ValidateStep(velocity, dt);

        var contacts = TileContacts.None;
        var resolvedVelocity = velocity;
        var deltaX = velocity.X * dt;
        var deltaY = velocity.Y * dt;

        var resolved = box with { X = ResolveX(grid, box, deltaX, out var stoppedX) };

        if (stoppedX)
        {
            contacts |= deltaX > 0f ? TileContacts.WallRight : TileContacts.WallLeft;
            resolvedVelocity.X = 0f;
        }

        resolved = resolved with { Y = ResolveY(grid, resolved, deltaY, out var stoppedY) };

        if (stoppedY)
        {
            contacts |= deltaY > 0f ? TileContacts.Ground : TileContacts.Ceiling;
            resolvedVelocity.Y = 0f;
        }

        return new CollisionResult(resolved, resolvedVelocity, contacts);
    }

    /// <summary>
    /// Horizontal pass: moves the box along X and stops it at the first solid
    /// tile in the direction of travel.
    /// </summary>
    private static float ResolveX(TileGrid grid, in Aabb box, float delta, out bool stopped)
    {
        stopped = false;

        if (delta == 0f)
        {
            return box.X;
        }

        var size = grid.TileSize;
        var destination = box.X + delta;

        // Rows come from the vertical extent, which this pass does not change.
        // Ceil(Bottom) - 1 rather than Floor(Bottom) is what excludes the row
        // starting exactly at the body's feet. That is the whole reason a box
        // sliding along a flat floor does not catch on the seam between two
        // floor tiles: the floor is never a candidate for a horizontal stop,
        // so there is no boundary for it to snag on.
        var rowFrom = Math.Max(FloorTile(box.Top, size), 0);
        var rowTo = Math.Min(CeilTile(box.Bottom, size) - 1, grid.Height - 1);

        if (delta > 0f)
        {
            // Start at the first column the body is not already inside, so a
            // body that begins overlapping geometry walks out of it instead of
            // being flung. Scanning nearest-first means the first solid column
            // found is the one that actually blocks.
            var from = Math.Max(CeilTile(box.Right, size), 0);
            var to = Math.Min(CeilTile(destination + box.Width, size) - 1, grid.Width - 1);

            for (var column = from; column <= to; column++)
            {
                if (AnySolidInColumn(grid, column, rowFrom, rowTo))
                {
                    stopped = true;
                    return (column * size) - box.Width;
                }
            }
        }
        else
        {
            var from = Math.Min(FloorTile(box.Left, size) - 1, grid.Width - 1);
            var to = Math.Max(FloorTile(destination, size), 0);

            for (var column = from; column >= to; column--)
            {
                if (AnySolidInColumn(grid, column, rowFrom, rowTo))
                {
                    stopped = true;
                    return (column + 1) * size;
                }
            }
        }

        return destination;
    }

    /// <summary>
    /// Vertical pass: moves the box along Y and stops it at the first solid tile
    /// in the direction of travel. Y increases downwards, so a positive delta is
    /// a fall and lands on <see cref="TileContacts.Ground"/>.
    /// </summary>
    private static float ResolveY(TileGrid grid, in Aabb box, float delta, out bool stopped)
    {
        stopped = false;

        if (delta == 0f)
        {
            return box.Y;
        }

        var size = grid.TileSize;
        var destination = box.Y + delta;

        var columnFrom = Math.Max(FloorTile(box.Left, size), 0);
        var columnTo = Math.Min(CeilTile(box.Right, size) - 1, grid.Width - 1);

        if (delta > 0f)
        {
            var from = Math.Max(CeilTile(box.Bottom, size), 0);
            var to = Math.Min(CeilTile(destination + box.Height, size) - 1, grid.Height - 1);

            for (var row = from; row <= to; row++)
            {
                if (AnySolidInRow(grid, row, columnFrom, columnTo))
                {
                    stopped = true;

                    // Snap exactly onto the tile's top face -- never offset by a
                    // small separation. Because the overlap test is strict,
                    // flush is not colliding, so the body settles on one float
                    // and returns to it bit-for-bit on every subsequent step.
                    return (row * size) - box.Height;
                }
            }
        }
        else
        {
            var from = Math.Min(FloorTile(box.Top, size) - 1, grid.Height - 1);
            var to = Math.Max(FloorTile(destination, size), 0);

            for (var row = from; row >= to; row--)
            {
                if (AnySolidInRow(grid, row, columnFrom, columnTo))
                {
                    stopped = true;
                    return (row + 1) * size;
                }
            }
        }

        return destination;
    }

    /// <summary>Whether any tile in a column slice blocks movement.</summary>
    private static bool AnySolidInColumn(TileGrid grid, int column, int rowFrom, int rowTo)
    {
        for (var row = rowFrom; row <= rowTo; row++)
        {
            if (grid.IsSolid(column, row))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether any tile in a row slice blocks movement.</summary>
    private static bool AnySolidInRow(TileGrid grid, int row, int columnFrom, int columnTo)
    {
        for (var column = columnFrom; column <= columnTo; column++)
        {
            if (grid.IsSolid(column, row))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Index of the tile containing a world coordinate.</summary>
    private static int FloorTile(float world, float tileSize) =>
        ClampIndex(MathF.Floor(world / tileSize));

    /// <summary>
    /// Exclusive upper tile index for a world coordinate: the index of the first
    /// tile that starts at or after it. Subtract one to get the last tile a span
    /// ending there actually covers, which correctly excludes a tile the span
    /// only touches.
    /// </summary>
    private static int CeilTile(float world, float tileSize) =>
        ClampIndex(MathF.Ceiling(world / tileSize));

    /// <summary>Narrows a tile index to a range where +/-1 cannot overflow.</summary>
    private static int ClampIndex(float index)
    {
        if (index <= -TileIndexLimit)
        {
            return -TileIndexLimit;
        }

        return index >= TileIndexLimit ? TileIndexLimit : (int)index;
    }

    private static void ValidateBox(in Aabb box)
    {
        if (!float.IsFinite(box.X) || !float.IsFinite(box.Y))
        {
            throw new ArgumentOutOfRangeException(nameof(box), box, "Box position must be finite.");
        }

        if (!float.IsFinite(box.Width) || box.Width <= 0f ||
            !float.IsFinite(box.Height) || box.Height <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(box), box, "Box extents must be positive and finite.");
        }
    }

    private static void ValidateStep(Vector2 velocity, float dt)
    {
        if (!float.IsFinite(velocity.X) || !float.IsFinite(velocity.Y))
        {
            throw new ArgumentOutOfRangeException(nameof(velocity), velocity, "Velocity must be finite.");
        }

        if (!float.IsFinite(dt) || dt < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(dt), dt, "Step must be finite and not negative.");
        }
    }
}
