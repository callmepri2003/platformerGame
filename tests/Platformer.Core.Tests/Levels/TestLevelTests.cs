using Platformer.Core.Levels;

namespace Platformer.Core.Tests.Levels;

/// <summary>
/// The shipped test level is a deliverable, not a fixture: #3's collision work
/// and #9's end-to-end scenario are both judged against it. These tests assert
/// the level still contains the features those issues need, so that editing the
/// map cannot quietly remove the thing a downstream test relies on.
/// </summary>
public sealed class TestLevelTests
{
    private const float Tile = AsciiLevelLoader.DefaultTileSize;

    private static Level Load() => AsciiLevelLoader.LoadEmbedded(AsciiLevelLoader.TestLevelName);

    /// <summary>Top edge of the tile at <paramref name="row"/>, in world units.</summary>
    private static float SurfaceOf(int row) => row * Tile;

    /// <summary>A tile you could stand on: solid, with open air directly above.</summary>
    private static bool IsStandable(TileGrid grid, int x, int y) =>
        grid.IsSolid(x, y) && !grid.IsSolid(x, y - 1);

    [Fact]
    public void TestLevel_IsEmbeddedAndParses()
    {
        var level = Load();

        Assert.Equal("test-level.txt", level.Name);
        Assert.Equal(20, level.Tiles.Width);
        Assert.Equal(11, level.Tiles.Height);
        Assert.Equal(Tile, level.Tiles.TileSize);
    }

    [Fact]
    public void TestLevel_FitsTheVirtualResolutionSoItIsPlayableBeforeTheCameraExists()
    {
        // #7 renders 320x180 with no camera; #10 (camera) is the sprint's
        // stretch goal and may be cut. The whole level fits on one screen so
        // that cutting the camera cannot make the level unplayable.
        var level = Load();

        Assert.True(level.Tiles.WorldWidth <= 320f, $"level is {level.Tiles.WorldWidth} units wide");
        Assert.True(level.Tiles.WorldHeight <= 180f, $"level is {level.Tiles.WorldHeight} units tall");
    }

    [Fact]
    public void TestLevel_SpawnsThePlayerStandingOnSolidGround()
    {
        var level = Load();
        var grid = level.Tiles;

        var spawnTile = grid.WorldToTile(level.PlayerSpawn.X, level.PlayerSpawn.Y - (Tile * 0.5f));

        Assert.False(grid.IsSolid(spawnTile));
        Assert.True(grid.IsSolid(spawnTile.X, spawnTile.Y + 1));
        Assert.Equal(SurfaceOf(spawnTile.Y + 1), level.PlayerSpawn.Y);

        // Head room: the player must not start wedged under a ceiling.
        Assert.False(grid.IsSolid(spawnTile.X, spawnTile.Y - 1));
    }

    [Fact]
    public void TestLevel_HasFlatGroundLongEnoughToReachTopSpeed()
    {
        var level = Load();
        var grid = level.Tiles;
        var spawnTile = grid.WorldToTile(level.PlayerSpawn.X, level.PlayerSpawn.Y - (Tile * 0.5f));
        var floorRow = spawnTile.Y + 1;

        var run = 0;
        for (var x = 0; x < grid.Width; x++)
        {
            run = IsStandable(grid, x, floorRow) ? run + 1 : 0;
            if (run >= 6)
            {
                break;
            }
        }

        // #4 targets top speed in ~0.1s at ~110 u/s; six tiles is 96 units of
        // runway, comfortably enough to accelerate and then stop.
        Assert.True(run >= 6, "the level needs at least six contiguous tiles of flat ground");
    }

    [Fact]
    public void TestLevel_HasARaisedPlatformExactlyTwoTilesAboveTheRunway()
    {
        var level = Load();
        var grid = level.Tiles;
        var floorRow = 9;
        var platformRow = 7;

        // Three or more tiles wide, so landing on it is not a pixel-perfect feat.
        var width = 0;
        for (var x = 0; x < grid.Width; x++)
        {
            if (IsStandable(grid, x, platformRow))
            {
                width++;
            }
        }

        Assert.True(width >= 3, $"the raised platform is only {width} tiles wide");
        Assert.Equal(2 * Tile, SurfaceOf(floorRow) - SurfaceOf(platformRow));
    }

    [Fact]
    public void TestLevel_HasAWallOnTheRunwayThatTheRunningPlayerMeetsHeadOn()
    {
        var level = Load();
        var grid = level.Tiles;

        // The raised platform is a plateau, not an overhang, so its left face is
        // a wall at ground level: running right into it stops you, and jumping
        // puts you on top of it. This is dev-a's ambiguous-corner case (#3).
        var wallColumn = -1;
        for (var x = 1; x < grid.Width; x++)
        {
            if (grid.IsSolid(x, 8) && !grid.IsSolid(x - 1, 8) && grid.IsSolid(x, 7))
            {
                wallColumn = x;
                break;
            }
        }

        Assert.True(wallColumn > 0, "no wall face found at running height");
        Assert.False(grid.IsSolid(wallColumn - 1, 8));
        Assert.True(grid.IsSolid(wallColumn, 9), "the wall must reach the floor");
    }

    [Fact]
    public void TestLevel_HasABottomlessPitBesideAWalkableLedge()
    {
        var level = Load();
        var grid = level.Tiles;

        // Columns 1-3 have no floor at all, so falling in leaves the level
        // through the bottom — which is only a fall, and not a landing on
        // invisible ground, because out of bounds is empty (#1).
        for (var x = 1; x <= 3; x++)
        {
            // From just under the ceiling to the bottom of the level: the shaft
            // is open all the way through.
            for (var y = 1; y < grid.Height; y++)
            {
                Assert.False(grid.IsSolid(x, y), $"tile ({x},{y}) blocks the pit");
            }

            Assert.False(grid.IsSolid(x, grid.Height));
        }

        // The ledge you walk off to get there: #6's coyote-time site.
        Assert.True(IsStandable(grid, 4, 9));
        Assert.False(grid.IsSolid(3, 9));
    }

    [Fact]
    public void TestLevel_SpawnIsNotSoCloseToThePitThatStandingStillIsRisky()
    {
        var level = Load();

        var pitEdge = 4 * Tile;

        Assert.True(
            level.PlayerSpawn.X - pitEdge >= Tile,
            "the player spawns less than a tile from the pit edge");
    }

    [Fact]
    public void TestLevel_HasACeilingLowEnoughToBumpFromAStandingSurface()
    {
        var level = Load();
        var grid = level.Tiles;

        // #5 needs somewhere a rising jump is stopped by a ceiling. Two tiles of
        // clearance over the plateau leaves 16 units of head room for a 16-tall
        // player, so any real jump reaches it.
        var found = false;
        for (var x = 0; x < grid.Width && !found; x++)
        {
            for (var y = 2; y < grid.Height; y++)
            {
                if (IsStandable(grid, x, y) && grid.IsSolid(x, y - 3))
                {
                    found = true;
                    break;
                }
            }
        }

        Assert.True(found, "no standing surface has a ceiling within jumping distance");
    }

    [Fact]
    public void TestLevel_HasNoCorridorExactlyOnePlayerHigh()
    {
        var level = Load();
        var grid = level.Tiles;

        // A gap of exactly one tile is exactly the height of the player, so
        // whether it can be entered comes down to whether flush contact counts
        // as a collision. That is a genuine edge case and #3 must test it — but
        // it belongs in a purpose-built grid, not in the level every other test
        // has to walk through. Getting stuck here would block #9 end to end.
        for (var y = 2; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                if (IsStandable(grid, x, y))
                {
                    Assert.False(
                        grid.IsSolid(x, y - 2),
                        $"tile ({x},{y}) is a standing surface with only one tile of head room");
                }
            }
        }
    }

    [Fact]
    public void TestLevel_IsSealedExceptForThePit()
    {
        var level = Load();
        var grid = level.Tiles;

        // Nothing may leave the level except by falling into the pit, and that
        // is authored geometry rather than an accident of the bounds rule.
        for (var x = 0; x < grid.Width; x++)
        {
            Assert.True(grid.IsSolid(x, 0), $"the ceiling is open at column {x}");
            Assert.True(
                grid.IsSolid(x, grid.Height - 1) || (x >= 1 && x <= 3),
                $"the floor is open at column {x}, outside the pit");
        }

        for (var y = 0; y < grid.Height; y++)
        {
            Assert.True(grid.IsSolid(0, y), $"the left wall is open at row {y}");
            Assert.True(grid.IsSolid(grid.Width - 1, y), $"the right wall is open at row {y}");
        }
    }
}
