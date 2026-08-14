using System.Numerics;
using Platformer.Core.Levels;

namespace Platformer.Core.Tests.Levels;

public sealed class TileGridTests
{
    private const float TileSize = 16f;

    /// <summary>
    /// 4x3 grid whose bottom row is solid, with a single solid block at (1, 1),
    /// so tests can distinguish "empty inside" from "outside".
    /// </summary>
    private static TileGrid MakeGrid()
    {
        var grid = new TileGrid(4, 3, TileSize);
        grid.SetTile(1, 1, TileKind.Solid);
        for (var x = 0; x < grid.Width; x++)
        {
            grid.SetTile(x, 2, TileKind.Solid);
        }

        return grid;
    }

    [Fact]
    public void Constructor_ExposesDimensionsAndTileSize()
    {
        var grid = new TileGrid(4, 3, TileSize);

        Assert.Equal(4, grid.Width);
        Assert.Equal(3, grid.Height);
        Assert.Equal(TileSize, grid.TileSize);
        Assert.Equal(64f, grid.WorldWidth);
        Assert.Equal(48f, grid.WorldHeight);
    }

    [Fact]
    public void Constructor_StartsEmpty()
    {
        var grid = new TileGrid(2, 2, TileSize);

        Assert.All(
            new[] { (0, 0), (1, 0), (0, 1), (1, 1) },
            cell => Assert.Equal(TileKind.Empty, grid.GetTile(cell.Item1, cell.Item2)));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    public void Constructor_NonPositiveDimensions_Throws(int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TileGrid(width, height, TileSize));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-8f)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NaN)]
    public void Constructor_InvalidTileSize_Throws(float tileSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TileGrid(2, 2, tileSize));
    }

    [Fact]
    public void Constructor_FromTiles_ReadsRowMajorTopRowFirst()
    {
        TileKind[] tiles =
        [
            TileKind.Empty, TileKind.Empty,
            TileKind.Solid, TileKind.Empty,
        ];

        var grid = new TileGrid(2, 2, TileSize, tiles);

        Assert.Equal(TileKind.Empty, grid.GetTile(0, 0));
        Assert.Equal(TileKind.Solid, grid.GetTile(0, 1));
        Assert.Equal(TileKind.Empty, grid.GetTile(1, 1));
    }

    [Fact]
    public void Constructor_FromTiles_WrongLength_Throws()
    {
        TileKind[] tiles = [TileKind.Empty, TileKind.Solid, TileKind.Empty];

        Assert.Throws<ArgumentException>(() => new TileGrid(2, 2, TileSize, tiles));
    }

    [Fact]
    public void Constructor_FromTiles_CopiesSoLaterEditsDoNotLeakIn()
    {
        var tiles = new TileKind[4];
        var grid = new TileGrid(2, 2, TileSize, tiles);

        tiles[0] = TileKind.Solid;

        Assert.Equal(TileKind.Empty, grid.GetTile(0, 0));
    }

    [Fact]
    public void GetTile_ReadsWhatWasWritten()
    {
        var grid = MakeGrid();

        Assert.Equal(TileKind.Solid, grid.GetTile(1, 1));
        Assert.Equal(TileKind.Empty, grid.GetTile(0, 1));
        Assert.Equal(TileKind.Solid, grid.GetTile(new TileCoord(3, 2)));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(4, 0)]
    [InlineData(0, 3)]
    [InlineData(int.MinValue, int.MinValue)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public void GetTile_OutOfBounds_IsTotalAndReportsTheDocumentedKind(int tileX, int tileY)
    {
        var grid = MakeGrid();

        Assert.False(grid.InBounds(tileX, tileY));
        Assert.Equal(TileGrid.OutOfBoundsKind, grid.GetTile(tileX, tileY));
    }

    [Fact]
    public void GetTile_OutOfBounds_IsEmptySoFallingOffTheMapIsAFallNotAWall()
    {
        var grid = MakeGrid();

        // Directly below the solid floor row, and off the left edge.
        Assert.False(grid.IsSolid(0, grid.Height));
        Assert.False(grid.IsSolid(-1, 2));
        Assert.False(grid.IsSolidAt(-1f, 40f));
        Assert.False(grid.IsSolidAt(0f, grid.WorldHeight + 1f));
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(3, 2, true)]
    [InlineData(4, 2, false)]
    [InlineData(3, 3, false)]
    [InlineData(-1, -1, false)]
    public void InBounds_MatchesTheGridRectangle(int tileX, int tileY, bool expected)
    {
        var grid = MakeGrid();

        Assert.Equal(expected, grid.InBounds(tileX, tileY));
        Assert.Equal(expected, grid.InBounds(new TileCoord(tileX, tileY)));
    }

    [Fact]
    public void SetTile_OutOfBounds_ThrowsBecauseAStrayWriteIsAlwaysACallerBug()
    {
        var grid = MakeGrid();

        Assert.Throws<ArgumentOutOfRangeException>(() => grid.SetTile(4, 0, TileKind.Solid));
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.SetTile(new TileCoord(0, -1), TileKind.Solid));
    }

    [Fact]
    public void Fill_ReplacesEveryTile()
    {
        var grid = MakeGrid();

        grid.Fill(TileKind.Solid);

        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                Assert.True(grid.IsSolid(x, y));
            }
        }
    }

    [Fact]
    public void IsSolid_ByCoordAndWorldPosition_Agree()
    {
        var grid = MakeGrid();

        Assert.True(grid.IsSolid(new TileCoord(1, 1)));
        Assert.True(grid.IsSolidAt(20f, 20f));
        Assert.True(grid.IsSolidAt(new Vector2(20f, 20f)));
        Assert.False(grid.IsSolidAt(new Vector2(4f, 4f)));
    }

    [Theory]
    [InlineData(0f, 0)]
    [InlineData(15.9f, 0)]
    [InlineData(16f, 1)]
    [InlineData(-0.0001f, -1)]
    [InlineData(-1f, -1)]
    [InlineData(-15.9f, -1)]
    [InlineData(-16f, -1)]
    [InlineData(-16.1f, -2)]
    [InlineData(-32f, -2)]
    public void WorldToTile_FloorsInsteadOfTruncating(float world, int expected)
    {
        var grid = MakeGrid();

        Assert.Equal(expected, grid.WorldToTileX(world));
        Assert.Equal(expected, grid.WorldToTileY(world));
    }

    [Fact]
    public void WorldToTile_NegativeWorld_DisagreesWithANaiveCast()
    {
        var grid = MakeGrid();

        // (int)(-1f / 16f) truncates towards zero and yields column 0, which
        // would put a body just off the left edge inside the first column.
        Assert.Equal(0, (int)(-1f / TileSize));
        Assert.Equal(-1, grid.WorldToTileX(-1f));
    }

    [Fact]
    public void GetTileAt_NegativeWorldPosition_IsOutOfBoundsNotColumnZero()
    {
        var grid = MakeGrid();
        grid.SetTile(0, 0, TileKind.Solid);

        Assert.Equal(TileKind.Empty, grid.GetTileAt(-1f, 1f));
        Assert.Equal(TileKind.Empty, grid.GetTileAt(new Vector2(1f, -1f)));
        Assert.Equal(TileKind.Solid, grid.GetTileAt(1f, 1f));
    }

    [Theory]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void GetTileAt_NonFiniteWorldPosition_StillDoesNotThrow(float world)
    {
        var grid = MakeGrid();

        Assert.Equal(TileGrid.OutOfBoundsKind, grid.GetTileAt(world, world));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(3, 2)]
    [InlineData(-1, -1)]
    [InlineData(-7, 12)]
    [InlineData(1000, -1000)]
    public void TileToWorld_ThenBack_RoundTripsIncludingNegatives(int tileX, int tileY)
    {
        var grid = MakeGrid();
        var coord = new TileCoord(tileX, tileY);

        var corner = grid.TileToWorld(coord);
        var center = grid.TileCenterToWorld(coord);

        Assert.Equal(coord, grid.WorldToTile(corner));
        Assert.Equal(coord, grid.WorldToTile(center));
    }

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(31.5f, 47.9f)]
    [InlineData(-0.5f, -33.25f)]
    [InlineData(-64f, 64f)]
    public void WorldToTile_ThenBack_SnapsToTheContainingTileCorner(float worldX, float worldY)
    {
        var grid = MakeGrid();
        var world = new Vector2(worldX, worldY);

        var corner = grid.TileToWorld(grid.WorldToTile(world));

        Assert.True(corner.X <= world.X && world.X < corner.X + grid.TileSize);
        Assert.True(corner.Y <= world.Y && world.Y < corner.Y + grid.TileSize);
    }

    [Fact]
    public void TileToWorld_PlacesTheOriginTileAtTheWorldOrigin()
    {
        var grid = MakeGrid();

        Assert.Equal(Vector2.Zero, grid.TileToWorld(0, 0));
        Assert.Equal(new Vector2(8f, 8f), grid.TileCenterToWorld(0, 0));
        Assert.Equal(new Vector2(16f, 32f), grid.TileToWorld(1, 2));
    }
}
