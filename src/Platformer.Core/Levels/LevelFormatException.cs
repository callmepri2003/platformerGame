using System.Globalization;

namespace Platformer.Core.Levels;

/// <summary>
/// Thrown when a level source cannot be parsed. Carries the position of the
/// offending character so the author can go straight to it.
/// </summary>
/// <remarks>
/// Messages are formatted as <c>source(line,column): explanation</c>, the same
/// shape compilers use, so an IDE or editor that recognises build output can
/// jump to the character that caused the failure.
/// </remarks>
public sealed class LevelFormatException : Exception
{
    /// <summary>Creates the exception with a default message.</summary>
    public LevelFormatException()
        : this("The level could not be parsed.")
    {
    }

    /// <summary>Creates the exception with an explanatory message.</summary>
    /// <param name="message">What was wrong with the level.</param>
    public LevelFormatException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and an inner cause.</summary>
    /// <param name="message">What was wrong with the level.</param>
    /// <param name="innerException">The underlying cause.</param>
    public LevelFormatException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    private LevelFormatException(string message, string? sourceName, int line, int column)
        : base(message)
    {
        SourceName = sourceName;
        Line = line;
        Column = column;
    }

    /// <summary>
    /// Name of the level source, if one was supplied. Usually a file name.
    /// </summary>
    public string? SourceName { get; }

    /// <summary>
    /// One-based line of the offending character, or zero when the problem is
    /// the level as a whole rather than one position in it.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// One-based column of the offending character, or zero when the problem is
    /// the level as a whole rather than one position in it.
    /// </summary>
    public int Column { get; }

    /// <summary>Builds an exception that points at a specific character.</summary>
    /// <param name="sourceName">Name of the level source, for the message.</param>
    /// <param name="line">One-based line of the offending character.</param>
    /// <param name="column">One-based column of the offending character.</param>
    /// <param name="problem">
    /// What is wrong and what to do about it, as a complete sentence.
    /// </param>
    /// <returns>The exception, ready to throw.</returns>
    internal static LevelFormatException At(string? sourceName, int line, int column, string problem)
    {
        var where = string.Create(
            CultureInfo.InvariantCulture,
            $"{sourceName ?? "level"}({line},{column})");

        return new LevelFormatException($"{where}: {problem}", sourceName, line, column);
    }

    /// <summary>Builds an exception about the level as a whole.</summary>
    /// <param name="sourceName">Name of the level source, for the message.</param>
    /// <param name="problem">
    /// What is wrong and what to do about it, as a complete sentence.
    /// </param>
    /// <returns>The exception, ready to throw.</returns>
    internal static LevelFormatException ForSource(string? sourceName, string problem) =>
        new($"{sourceName ?? "level"}: {problem}", sourceName, 0, 0);
}
