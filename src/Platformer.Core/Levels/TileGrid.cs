using System.Numerics;

namespace Platformer.Core.Levels;

/// <summary>
/// A rectangular, uniformly sized grid of tiles: the shared idea of "what the
/// level is" that collision, rendering and level loading all read from.
/// </summary>
/// <remarks>
/// <para>
/// This is a data structure and nothing more. It answers what is at a
/// coordinate; it does not resolve movement, draw anything, or know where
/// levels come from.
/// </para>
/// <para>
/// The grid occupies the world rectangle from (0, 0) to
/// (<see cref="WorldWidth"/>, <see cref="WorldHeight"/>), with Y increasing
/// downwards so that tile row 0 is the top row.
/// </para>
/// </remarks>
public sealed class TileGrid
{
    /// <summary>
    /// The kind reported for any coordinate outside the grid.
    /// </summary>
    /// <remarks>
    /// Outside the level is <see cref="TileKind.Empty"/>, not solid, and the
    /// choice is deliberate. A player who runs off the edge of the map should
    /// fall into open space and be caught by an explicit out-of-bounds rule
    /// (respawn, death plane), because that is a state the game can see and
    /// handle. Were the outside solid, the player would instead stop dead in
    /// mid-air on a wall that the renderer never draws and no level author ever
    /// placed — physics and picture would silently disagree, which is the one
    /// class of bug that is hardest to diagnose from a video. Levels that must
    /// not be escapable say so by authoring real walls, which show up in the
    /// level file and on screen. It also keeps collision uniform: the border of
    /// the grid is not a special case, so a body straddling it is resolved by
    /// exactly the same code as one in the middle.
    /// </remarks>
    public const TileKind OutOfBoundsKind = TileKind.Empty;

    private readonly TileKind[] _tiles;

    /// <summary>Creates a grid filled with <see cref="TileKind.Empty"/>.</summary>
    /// <param name="width">Number of tile columns; must be positive.</param>
    /// <param name="height">Number of tile rows; must be positive.</param>
    /// <param name="tileSize">Edge length of one tile in world units; must be positive and finite.</param>
    public TileGrid(int width, int height, float tileSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tileSize);

        if (!float.IsFinite(tileSize))
        {
            throw new ArgumentOutOfRangeException(nameof(tileSize), tileSize, "Tile size must be finite.");
        }

        Width = width;
        Height = height;
        TileSize = tileSize;
        _tiles = new TileKind[width * height];
    }

    /// <summary>Creates a grid from existing tiles laid out row-major, top row first.</summary>
    /// <param name="width">Number of tile columns; must be positive.</param>
    /// <param name="height">Number of tile rows; must be positive.</param>
    /// <param name="tileSize">Edge length of one tile in world units; must be positive and finite.</param>
    /// <param name="tiles">Exactly <paramref name="width"/> * <paramref name="height"/> tiles, copied into the grid.</param>
    public TileGrid(int width, int height, float tileSize, ReadOnlySpan<TileKind> tiles)
        : this(width, height, tileSize)
    {
        if (tiles.Length != _tiles.Length)
        {
            throw new ArgumentException(
                $"Expected {_tiles.Length} tiles for a {width}x{height} grid but got {tiles.Length}.",
                nameof(tiles));
        }

        tiles.CopyTo(_tiles);
    }

    /// <summary>Number of tile columns.</summary>
    public int Width { get; }

    /// <summary>Number of tile rows.</summary>
    public int Height { get; }

    /// <summary>Edge length of a single tile in world units.</summary>
    public float TileSize { get; }

    /// <summary>Width of the whole grid in world units.</summary>
    public float WorldWidth => Width * TileSize;

    /// <summary>Height of the whole grid in world units.</summary>
    public float WorldHeight => Height * TileSize;

    /// <summary>Whether a tile coordinate names a cell that actually exists.</summary>
    /// <param name="tileX">Column index.</param>
    /// <param name="tileY">Row index.</param>
    /// <returns><see langword="true"/> when the coordinate is inside the grid.</returns>
    public bool InBounds(int tileX, int tileY) =>
        (uint)tileX < (uint)Width && (uint)tileY < (uint)Height;

    /// <summary>Whether a tile coordinate names a cell that actually exists.</summary>
    /// <param name="coord">Coordinate to test.</param>
    /// <returns><see langword="true"/> when the coordinate is inside the grid.</returns>
    public bool InBounds(TileCoord coord) => InBounds(coord.X, coord.Y);

    /// <summary>
    /// Reads a tile. Total: any coordinate is legal, and coordinates outside the
    /// grid report <see cref="OutOfBoundsKind"/> rather than throwing.
    /// </summary>
    /// <param name="tileX">Column index.</param>
    /// <param name="tileY">Row index.</param>
    /// <returns>The tile there, or <see cref="OutOfBoundsKind"/> outside the grid.</returns>
    public TileKind GetTile(int tileX, int tileY) =>
        InBounds(tileX, tileY) ? _tiles[(tileY * Width) + tileX] : OutOfBoundsKind;

    /// <summary>
    /// Reads a tile. Total: any coordinate is legal, and coordinates outside the
    /// grid report <see cref="OutOfBoundsKind"/> rather than throwing.
    /// </summary>
    /// <param name="coord">Coordinate to read.</param>
    /// <returns>The tile there, or <see cref="OutOfBoundsKind"/> outside the grid.</returns>
    public TileKind GetTile(TileCoord coord) => GetTile(coord.X, coord.Y);

    /// <summary>
    /// Reads the tile covering a world position. Total, like
    /// <see cref="GetTile(int, int)"/>.
    /// </summary>
    /// <param name="worldX">World X in world units.</param>
    /// <param name="worldY">World Y in world units.</param>
    /// <returns>The tile there, or <see cref="OutOfBoundsKind"/> outside the grid.</returns>
    public TileKind GetTileAt(float worldX, float worldY) =>
        GetTile(WorldToTileX(worldX), WorldToTileY(worldY));

    /// <summary>
    /// Reads the tile covering a world position. Total, like
    /// <see cref="GetTile(int, int)"/>.
    /// </summary>
    /// <param name="worldPosition">Position in world units.</param>
    /// <returns>The tile there, or <see cref="OutOfBoundsKind"/> outside the grid.</returns>
    public TileKind GetTileAt(Vector2 worldPosition) =>
        GetTileAt(worldPosition.X, worldPosition.Y);

    /// <summary>Whether the tile at a coordinate blocks movement.</summary>
    /// <param name="tileX">Column index.</param>
    /// <param name="tileY">Row index.</param>
    /// <returns><see langword="true"/> when that tile obstructs a moving body.</returns>
    public bool IsSolid(int tileX, int tileY) => GetTile(tileX, tileY).IsSolid();

    /// <summary>Whether the tile at a coordinate blocks movement.</summary>
    /// <param name="coord">Coordinate to test.</param>
    /// <returns><see langword="true"/> when that tile obstructs a moving body.</returns>
    public bool IsSolid(TileCoord coord) => GetTile(coord).IsSolid();

    /// <summary>Whether the tile covering a world position blocks movement.</summary>
    /// <param name="worldX">World X in world units.</param>
    /// <param name="worldY">World Y in world units.</param>
    /// <returns><see langword="true"/> when that tile obstructs a moving body.</returns>
    public bool IsSolidAt(float worldX, float worldY) => GetTileAt(worldX, worldY).IsSolid();

    /// <summary>Whether the tile covering a world position blocks movement.</summary>
    /// <param name="worldPosition">Position in world units.</param>
    /// <returns><see langword="true"/> when that tile obstructs a moving body.</returns>
    public bool IsSolidAt(Vector2 worldPosition) => GetTileAt(worldPosition).IsSolid();

    /// <summary>
    /// Writes a tile. Unlike reading, writing outside the grid throws: a read
    /// off the edge is a normal consequence of a body moving around, but a write
    /// off the edge is always a bug in the caller and should surface loudly.
    /// </summary>
    /// <param name="tileX">Column index; must be inside the grid.</param>
    /// <param name="tileY">Row index; must be inside the grid.</param>
    /// <param name="kind">Kind to store.</param>
    public void SetTile(int tileX, int tileY, TileKind kind)
    {
        if (!InBounds(tileX, tileY))
        {
            throw new ArgumentOutOfRangeException(
                nameof(tileX),
                $"({tileX}, {tileY}) is outside a {Width}x{Height} grid.");
        }

        _tiles[(tileY * Width) + tileX] = kind;
    }

    /// <summary>
    /// Writes a tile. Unlike reading, writing outside the grid throws: a read
    /// off the edge is a normal consequence of a body moving around, but a write
    /// off the edge is always a bug in the caller and should surface loudly.
    /// </summary>
    /// <param name="coord">Coordinate to write; must be inside the grid.</param>
    /// <param name="kind">Kind to store.</param>
    public void SetTile(TileCoord coord, TileKind kind) => SetTile(coord.X, coord.Y, kind);

    /// <summary>Replaces every tile in the grid with a single kind.</summary>
    /// <param name="kind">Kind to store everywhere.</param>
    public void Fill(TileKind kind) => Array.Fill(_tiles, kind);

    /// <summary>
    /// Column containing a world X.
    /// </summary>
    /// <remarks>
    /// Floors rather than casting. A cast truncates towards zero, so world X in
    /// (-tileSize, 0) would land in column 0 alongside the first real column and
    /// every negative coordinate would be shifted one tile to the right. Since
    /// the grid must answer for positions off its left and top edges, that error
    /// would be live from the first frame a body leaves the map.
    /// </remarks>
    /// <param name="worldX">World X in world units.</param>
    /// <returns>The column index, which may be negative or beyond <see cref="Width"/>.</returns>
    public int WorldToTileX(float worldX) => (int)MathF.Floor(worldX / TileSize);

    /// <summary>
    /// Row containing a world Y.
    /// </summary>
    /// <remarks>Floors rather than casting, for the reason given on <see cref="WorldToTileX"/>.</remarks>
    /// <param name="worldY">World Y in world units.</param>
    /// <returns>The row index, which may be negative or beyond <see cref="Height"/>.</returns>
    public int WorldToTileY(float worldY) => (int)MathF.Floor(worldY / TileSize);

    /// <summary>Tile coordinate containing a world position.</summary>
    /// <remarks>
    /// Defined for every finite input, including positions outside the grid;
    /// infinities map far outside it. The result is not clamped, so callers that
    /// need a valid cell should check <see cref="InBounds(TileCoord)"/>.
    /// </remarks>
    /// <param name="worldX">World X in world units.</param>
    /// <param name="worldY">World Y in world units.</param>
    /// <returns>The containing tile coordinate, in bounds or not.</returns>
    public TileCoord WorldToTile(float worldX, float worldY) =>
        new(WorldToTileX(worldX), WorldToTileY(worldY));

    /// <summary>Tile coordinate containing a world position.</summary>
    /// <param name="worldPosition">Position in world units.</param>
    /// <returns>The containing tile coordinate, in bounds or not.</returns>
    public TileCoord WorldToTile(Vector2 worldPosition) =>
        WorldToTile(worldPosition.X, worldPosition.Y);

    /// <summary>
    /// Top-left corner of a tile in world units. This is the inverse of
    /// <see cref="WorldToTile(float, float)"/>: feeding the corner back in
    /// returns the same coordinate.
    /// </summary>
    /// <param name="tileX">Column index; need not be in bounds.</param>
    /// <param name="tileY">Row index; need not be in bounds.</param>
    /// <returns>The tile's minimum corner in world units.</returns>
    public Vector2 TileToWorld(int tileX, int tileY) => new(tileX * TileSize, tileY * TileSize);

    /// <summary>
    /// Top-left corner of a tile in world units. This is the inverse of
    /// <see cref="WorldToTile(float, float)"/>: feeding the corner back in
    /// returns the same coordinate.
    /// </summary>
    /// <param name="coord">Coordinate to convert; need not be in bounds.</param>
    /// <returns>The tile's minimum corner in world units.</returns>
    public Vector2 TileToWorld(TileCoord coord) => TileToWorld(coord.X, coord.Y);

    /// <summary>Centre of a tile in world units.</summary>
    /// <param name="tileX">Column index; need not be in bounds.</param>
    /// <param name="tileY">Row index; need not be in bounds.</param>
    /// <returns>The tile's centre in world units.</returns>
    public Vector2 TileCenterToWorld(int tileX, int tileY) =>
        new((tileX + 0.5f) * TileSize, (tileY + 0.5f) * TileSize);

    /// <summary>Centre of a tile in world units.</summary>
    /// <param name="coord">Coordinate to convert; need not be in bounds.</param>
    /// <returns>The tile's centre in world units.</returns>
    public Vector2 TileCenterToWorld(TileCoord coord) => TileCenterToWorld(coord.X, coord.Y);
}
