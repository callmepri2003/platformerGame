using System.Numerics;
using System.Text;
using Platformer.Core.Levels;

namespace Platformer.Core.Tests.Levels;

public sealed class AsciiLevelLoaderTests
{
    private const string Tiny = """
        ...
        .@.
        ###
        """;

    [Fact]
    public void Parse_MapsEveryCharacterToItsKind()
    {
        var level = AsciiLevelLoader.Parse("""
            #.@
            # #
            ###
            """);

        Assert.Equal(3, level.Tiles.Width);
        Assert.Equal(3, level.Tiles.Height);
        Assert.Equal(TileKind.Solid, level.Tiles.GetTile(0, 0));
        Assert.Equal(TileKind.Empty, level.Tiles.GetTile(1, 0));
        Assert.Equal(TileKind.Empty, level.Tiles.GetTile(2, 0));
        Assert.Equal(TileKind.Empty, level.Tiles.GetTile(1, 1));
    }

    [Fact]
    public void Parse_SpawnTileIsOpenAirNotGeometry()
    {
        var level = AsciiLevelLoader.Parse(Tiny);

        Assert.False(level.Tiles.IsSolid(1, 1));
    }

    [Fact]
    public void Parse_DefaultsToTheProjectTileSize()
    {
        var level = AsciiLevelLoader.Parse(Tiny);

        Assert.Equal(AsciiLevelLoader.DefaultTileSize, level.Tiles.TileSize);
        Assert.Equal(16f, AsciiLevelLoader.DefaultTileSize);
    }

    [Fact]
    public void Parse_HonoursACustomTileSize()
    {
        var level = AsciiLevelLoader.Parse(Tiny, tileSize: 8f);

        Assert.Equal(8f, level.Tiles.TileSize);
        Assert.Equal(new Vector2(12f, 16f), level.PlayerSpawn);
    }

    [Fact]
    public void Parse_SpawnIsTheBottomCentreOfTheMarkedTile()
    {
        var level = AsciiLevelLoader.Parse(Tiny);

        // '@' is tile (1,1), which spans x [16,32) and y [16,32).
        Assert.Equal(new Vector2(24f, 32f), level.PlayerSpawn);
    }

    [Fact]
    public void Parse_SpawnRestsOnTheFloorWithoutIntersectingIt()
    {
        var level = AsciiLevelLoader.Parse(Tiny);

        var topLeft = level.SpawnTopLeft(12f, 16f);
        var feet = topLeft.Y + 16f;
        var floorTop = level.Tiles.TileToWorld(1, 2).Y;

        // Flush: the feet are exactly on the floor's top edge, so the body
        // touches the floor and overlaps nothing.
        Assert.Equal(floorTop, feet);
        Assert.False(level.Tiles.IsSolidAt(topLeft.X, topLeft.Y));
        Assert.False(level.Tiles.IsSolidAt(topLeft.X + 12f - 0.001f, feet - 0.001f));
        Assert.True(level.Tiles.IsSolidAt(topLeft.X, feet));
    }

    [Fact]
    public void SpawnTopLeft_CentresTheBodyOnTheSpawnTile()
    {
        var level = AsciiLevelLoader.Parse(Tiny);

        var topLeft = level.SpawnTopLeft(12f, 16f);

        Assert.Equal(24f - 6f, topLeft.X);
        Assert.Equal(32f - 16f, topLeft.Y);
    }

    [Fact]
    public void SpawnTopLeft_TallerBodiesStillRestOnTheSameSurface()
    {
        var level = AsciiLevelLoader.Parse(Tiny);

        Assert.Equal(32f, level.SpawnTopLeft(12f, 16f).Y + 16f);
        Assert.Equal(32f, level.SpawnTopLeft(20f, 40f).Y + 40f);
    }

    [Fact]
    public void Parse_SpacesAreEmptyTiles()
    {
        var level = AsciiLevelLoader.Parse("#  #\n#@ #\n####");

        Assert.False(level.Tiles.IsSolid(1, 0));
        Assert.False(level.Tiles.IsSolid(2, 1));
        Assert.True(level.Tiles.IsSolid(3, 1));
    }

    [Theory]
    [InlineData("...\n.@.\n###\n")]
    [InlineData("...\n.@.\n###\n\n\n")]
    [InlineData("...\r\n.@.\r\n###\r\n")]
    public void Parse_ToleratesLineEndingsAndTrailingBlankLines(string text)
    {
        var level = AsciiLevelLoader.Parse(text);

        Assert.Equal(3, level.Tiles.Height);
        Assert.True(level.Tiles.IsSolid(0, 2));
    }

    [Fact]
    public void Parse_ToleratesAByteOrderMark()
    {
        var level = AsciiLevelLoader.Parse('﻿' + Tiny);

        Assert.Equal(3, level.Tiles.Width);
        Assert.Equal(new Vector2(24f, 32f), level.PlayerSpawn);
    }

    [Fact]
    public void Parse_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => AsciiLevelLoader.Parse(null!));
    }

    [Fact]
    public void Parse_RecordsTheSourceNameOnTheLevel()
    {
        var level = AsciiLevelLoader.Parse(Tiny, "greenhouse.txt");

        Assert.Equal("greenhouse.txt", level.Name);
    }

    [Fact]
    public void Parse_RaggedShortLine_NamesTheLineAndColumnAndSaysHowToFixIt()
    {
        var error = Assert.Throws<LevelFormatException>(
            () => AsciiLevelLoader.Parse("#####\n#@#\n#####", "ragged.txt"));

        Assert.Equal(2, error.Line);
        Assert.Equal(4, error.Column);
        Assert.Equal("ragged.txt", error.SourceName);
        Assert.Contains("ragged.txt(2,4)", error.Message, StringComparison.Ordinal);
        Assert.Contains("2 character(s) too short", error.Message, StringComparison.Ordinal);
        Assert.Contains("width to 5", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_RaggedLongLine_PointsAtTheFirstSurplusCharacter()
    {
        var error = Assert.Throws<LevelFormatException>(
            () => AsciiLevelLoader.Parse("###\n#@##\n###", "ragged.txt"));

        Assert.Equal(2, error.Line);
        Assert.Equal(4, error.Column);
        Assert.Contains("1 character(s) too long", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_UnknownCharacter_NamesItAndListsWhatIsLegal()
    {
        var error = Assert.Throws<LevelFormatException>(
            () => AsciiLevelLoader.Parse("###\n#@x\n###", "typo.txt"));

        Assert.Equal(2, error.Line);
        Assert.Equal(3, error.Column);
        Assert.Contains("typo.txt(2,3)", error.Message, StringComparison.Ordinal);
        Assert.Contains("'x' (U+0078)", error.Message, StringComparison.Ordinal);
        Assert.Contains("'#' solid", error.Message, StringComparison.Ordinal);
        Assert.Contains("'@' player spawn", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_TabCharacter_IsNamedByCodepointBecauseQuotingItShowsNothing()
    {
        var error = Assert.Throws<LevelFormatException>(
            () => AsciiLevelLoader.Parse("###\n#@\t\n###"));

        Assert.Contains("U+0009", error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("''", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_NoSpawn_SaysWhatIsMissingAndHowToAddIt()
    {
        var error = Assert.Throws<LevelFormatException>(
            () => AsciiLevelLoader.Parse("...\n...\n###", "spawnless.txt"));

        Assert.Contains("no player spawn", error.Message, StringComparison.Ordinal);
        Assert.Contains("'@'", error.Message, StringComparison.Ordinal);
        Assert.Contains("spawnless.txt", error.Message, StringComparison.Ordinal);
        Assert.Equal(0, error.Line);
        Assert.Equal(0, error.Column);
    }

    [Fact]
    public void Parse_DuplicateSpawn_NamesBothPositions()
    {
        var error = Assert.Throws<LevelFormatException>(
            () => AsciiLevelLoader.Parse(".@.\n..@\n###", "twins.txt"));

        Assert.Equal(2, error.Line);
        Assert.Equal(3, error.Column);
        Assert.Contains("twins.txt(2,3)", error.Message, StringComparison.Ordinal);
        Assert.Contains("line 1, column 2", error.Message, StringComparison.Ordinal);
        Assert.Contains("exactly one", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("\n")]
    [InlineData("\n\n\n")]
    public void Parse_NothingToParse_SaysSo(string text)
    {
        var error = Assert.Throws<LevelFormatException>(() => AsciiLevelLoader.Parse(text, "blank.txt"));

        Assert.Contains("the level is empty", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_FirstLineEmpty_IsRejectedAtLineOne()
    {
        var error = Assert.Throws<LevelFormatException>(() => AsciiLevelLoader.Parse("\n#@#\n###"));

        Assert.Equal(1, error.Line);
        Assert.Equal(1, error.Column);
        Assert.Contains("first line is empty", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_BlankLineInTheMiddle_IsRaggedNotIgnored()
    {
        var error = Assert.Throws<LevelFormatException>(() => AsciiLevelLoader.Parse("#@#\n\n###"));

        Assert.Equal(2, error.Line);
        Assert.Contains("ragged line", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_InvalidTileSize_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AsciiLevelLoader.Parse(Tiny, tileSize: 0f));
    }

    [Fact]
    public void Load_ReadsUtf8FromAStream()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(Tiny));

        var level = AsciiLevelLoader.Load(stream, "stream.txt");

        Assert.Equal("stream.txt", level.Name);
        Assert.Equal(new Vector2(24f, 32f), level.PlayerSpawn);
    }

    [Fact]
    public void Load_NullStream_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => AsciiLevelLoader.Load(null!));
    }

    [Fact]
    public void LoadEmbedded_UnknownName_ListsWhatIsAvailable()
    {
        var error = Assert.Throws<LevelFormatException>(() => AsciiLevelLoader.LoadEmbedded("no-such-level"));

        Assert.Contains("no-such-level.txt", error.Message, StringComparison.Ordinal);
        Assert.Contains(AsciiLevelLoader.TestLevelName, error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void LoadEmbedded_BlankName_Throws(string? name)
    {
        Assert.ThrowsAny<ArgumentException>(() => AsciiLevelLoader.LoadEmbedded(name!));
    }

    [Fact]
    public void EmbeddedLevelNames_ContainsTheShippedTestLevel()
    {
        Assert.Contains(AsciiLevelLoader.TestLevelName, AsciiLevelLoader.EmbeddedLevelNames());
    }

    [Fact]
    public void Level_NullGrid_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new Level(null!, Vector2.Zero));
    }
}
