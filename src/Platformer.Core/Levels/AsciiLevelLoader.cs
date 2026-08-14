using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace Platformer.Core.Levels;

/// <summary>
/// Reads levels from plain-text maps, one character per tile.
/// </summary>
/// <remarks>
/// <para>
/// Text was chosen so that a level is authored in any editor and, more
/// importantly, so that a change to a level is legible in a pull request diff:
/// moving a platform shows up as the platform moving.
/// </para>
/// <para>
/// The format is deliberately unforgiving. Every line must be the same length
/// and every character must be one the format knows, because the alternative —
/// quietly padding short lines, or ignoring stray characters — turns a typo
/// into a level that loads and is subtly wrong. Errors name the line and column
/// so the author can go straight to the character at fault.
/// </para>
/// <example>
/// A three-by-three level with the player standing on the floor:
/// <code>
/// var level = AsciiLevelLoader.Parse(
///     """
///     ...
///     .@.
///     ###
///     """);
///
/// // level.Tiles.IsSolid(1, 2) == true
/// // level.PlayerSpawn        == (24, 32)   feet on the floor, centred on the tile
/// </code>
/// </example>
/// </remarks>
public static class AsciiLevelLoader
{
    /// <summary>
    /// Tile size, in world units, that every level in this project is authored
    /// against.
    /// </summary>
    /// <remarks>
    /// The value is part of the level format, not a per-level setting: art,
    /// movement tuning and the collision sweep are all sized against it, so
    /// changing it changes how the game plays everywhere.
    /// </remarks>
    public const float DefaultTileSize = 16f;

    /// <summary>Character for an impassable tile.</summary>
    public const char Solid = '#';

    /// <summary>Character for open air.</summary>
    public const char Empty = '.';

    /// <summary>
    /// Also accepted for open air. <see cref="Empty"/> is preferred in files
    /// that ship, because most editors strip trailing spaces on save and would
    /// silently shorten any line that ends in one.
    /// </summary>
    public const char EmptySpace = ' ';

    /// <summary>Character marking where the player starts. Exactly one per level.</summary>
    public const char Spawn = '@';

    /// <summary>Name of the hand-authored level that ships with the game.</summary>
    public const string TestLevelName = "test-level";

    private const string ResourcePrefix = "Platformer.Core.Levels.";

    /// <summary>Parses a level from its text.</summary>
    /// <param name="text">The level map. Lines may end with LF or CRLF.</param>
    /// <param name="sourceName">Name used in error messages, usually a file name.</param>
    /// <param name="tileSize">Tile size in world units.</param>
    /// <returns>The parsed level.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="LevelFormatException">The text is not a valid level.</exception>
    public static Level Parse(
        string text,
        string? sourceName = null,
        float tileSize = DefaultTileSize)
    {
        ArgumentNullException.ThrowIfNull(text);

        var lines = SplitLines(text);
        if (lines.Count == 0)
        {
            throw LevelFormatException.ForSource(
                sourceName,
                "the level is empty. A level needs at least one line of tiles.");
        }

        var width = lines[0].Length;
        if (width == 0)
        {
            throw LevelFormatException.At(
                sourceName,
                1,
                1,
                "the first line is empty. A level needs at least one tile on every line.");
        }

        var height = lines.Count;
        var tiles = new TileKind[width * height];
        var spawn = default(TileCoord);
        var spawnLine = 0;
        var spawnColumn = 0;
        var spawnsFound = 0;

        for (var y = 0; y < height; y++)
        {
            var line = lines[y];
            if (line.Length != width)
            {
                throw RaggedLine(sourceName, y, line.Length, width);
            }

            for (var x = 0; x < width; x++)
            {
                var character = line[x];
                switch (character)
                {
                    case Solid:
                        tiles[(y * width) + x] = TileKind.Solid;
                        break;

                    case Empty:
                    case EmptySpace:
                        tiles[(y * width) + x] = TileKind.Empty;
                        break;

                    case Spawn:
                        if (spawnsFound > 0)
                        {
                            throw DuplicateSpawn(sourceName, y, x, spawnLine, spawnColumn);
                        }

                        // The marker occupies open air; the player stands in it.
                        tiles[(y * width) + x] = TileKind.Empty;
                        spawn = new TileCoord(x, y);
                        spawnLine = y + 1;
                        spawnColumn = x + 1;
                        spawnsFound++;
                        break;

                    default:
                        throw UnknownCharacter(sourceName, y, x, character);
                }
            }
        }

        if (spawnsFound == 0)
        {
            throw LevelFormatException.ForSource(
                sourceName,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"there is no player spawn. Mark where the player starts with a single '{Spawn}'."));
        }

        var grid = new TileGrid(width, height, tileSize, tiles);

        // Feet on the bottom edge of the spawn tile, centred across it.
        var spawnPosition = new Vector2(
            (spawn.X + 0.5f) * tileSize,
            (spawn.Y + 1) * tileSize);

        return new Level(grid, spawnPosition, sourceName);
    }

    /// <summary>Parses a level from a stream of UTF-8 text.</summary>
    /// <param name="stream">Stream positioned at the start of the level text.</param>
    /// <param name="sourceName">Name used in error messages, usually a file name.</param>
    /// <param name="tileSize">Tile size in world units.</param>
    /// <returns>The parsed level.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
    /// <exception cref="LevelFormatException">The text is not a valid level.</exception>
    public static Level Load(
        Stream stream,
        string? sourceName = null,
        float tileSize = DefaultTileSize)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return Parse(reader.ReadToEnd(), sourceName, tileSize);
    }

    /// <summary>
    /// Loads a level embedded in this assembly.
    /// </summary>
    /// <remarks>
    /// Levels are embedded rather than copied beside the executable so that a
    /// published build cannot lose them and so tests read exactly the bytes the
    /// game ships.
    /// </remarks>
    /// <param name="name">Level name without a path or extension, e.g. <c>test-level</c>.</param>
    /// <param name="tileSize">Tile size in world units.</param>
    /// <returns>The parsed level.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is null or blank.</exception>
    /// <exception cref="LevelFormatException">
    /// No level of that name is embedded, or its text is not a valid level.
    /// </exception>
    public static Level LoadEmbedded(string name, float tileSize = DefaultTileSize)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var fileName = $"{name}.txt";
        var assembly = typeof(AsciiLevelLoader).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourcePrefix + fileName);

        if (stream is null)
        {
            throw LevelFormatException.ForSource(
                fileName,
                $"no level by that name is embedded in {assembly.GetName().Name}. Embedded levels are: {DescribeEmbeddedLevels(assembly)}.");
        }

        return Load(stream, fileName, tileSize);
    }

    /// <summary>Names of every level embedded in this assembly, in order.</summary>
    /// <returns>Level names as accepted by <see cref="LoadEmbedded"/>.</returns>
    public static IReadOnlyList<string> EmbeddedLevelNames() =>
        EmbeddedLevelNames(typeof(AsciiLevelLoader).Assembly);

    private static string[] EmbeddedLevelNames(Assembly assembly) =>
        assembly.GetManifestResourceNames()
            .Where(resource =>
                resource.StartsWith(ResourcePrefix, StringComparison.Ordinal)
                && resource.EndsWith(".txt", StringComparison.Ordinal))
            .Select(resource => resource[ResourcePrefix.Length..^".txt".Length])
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static string DescribeEmbeddedLevels(Assembly assembly)
    {
        var names = EmbeddedLevelNames(assembly);
        return names.Length == 0 ? "(none)" : string.Join(", ", names);
    }

    private static List<string> SplitLines(string text)
    {
        // A byte-order mark would otherwise be read as a tile in the top-left
        // corner, which is a baffling error to receive.
        var lines = new List<string>();
        foreach (var line in text.TrimStart('\uFEFF').Split('\n'))
        {
            lines.Add(line.TrimEnd('\r'));
        }

        // Trailing blank lines are what a text editor adds, not level content.
        while (lines.Count > 0 && lines[^1].Length == 0)
        {
            lines.RemoveAt(lines.Count - 1);
        }

        return lines;
    }

    private static LevelFormatException RaggedLine(string? sourceName, int lineIndex, int actual, int expected)
    {
        // Point at where the line stops matching: the first missing character
        // if it is short, the first surplus one if it is long.
        var column = (actual < expected ? actual : expected) + 1;
        var comparison = actual < expected
            ? $"is {expected - actual} character(s) too short"
            : $"is {actual - expected} character(s) too long";

        return LevelFormatException.At(
            sourceName,
            lineIndex + 1,
            column,
            string.Create(
                CultureInfo.InvariantCulture,
                $"ragged line: it {comparison}. Line 1 sets the level width to {expected} but this line has {actual}. Every line must be the same length; pad short lines with '{Empty}'."));
    }

    private static LevelFormatException DuplicateSpawn(
        string? sourceName,
        int lineIndex,
        int columnIndex,
        int firstLine,
        int firstColumn)
    {
        return LevelFormatException.At(
            sourceName,
            lineIndex + 1,
            columnIndex + 1,
            string.Create(
                CultureInfo.InvariantCulture,
                $"a second player spawn: '{Spawn}' already appears at line {firstLine}, column {firstColumn}. A level must contain exactly one."));
    }

    private static LevelFormatException UnknownCharacter(
        string? sourceName,
        int lineIndex,
        int columnIndex,
        char character)
    {
        return LevelFormatException.At(
            sourceName,
            lineIndex + 1,
            columnIndex + 1,
            string.Create(
                CultureInfo.InvariantCulture,
                $"unknown character {Describe(character)}. A level may only contain '{Solid}' solid, '{Empty}' or a space for empty, and '{Spawn}' player spawn."));
    }

    private static string Describe(char character)
    {
        var codepoint = string.Create(CultureInfo.InvariantCulture, $"U+{(int)character:X4}");

        // Printing a tab or a control character between quotes shows nothing at
        // all, so those are named by codepoint only.
        return char.IsControl(character) || char.IsWhiteSpace(character)
            ? codepoint
            : $"'{character}' ({codepoint})";
    }
}
