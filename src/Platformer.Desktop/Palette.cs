using Raylib_cs;

namespace Platformer.Desktop;

/// <summary>
/// Every colour the game draws. Flat colours are correct for now: shapes and
/// motion are what this sprint is judging, and solid blocks make it obvious
/// where a collision box actually is.
/// </summary>
internal static class Palette
{
    /// <summary>Behind the level, inside the virtual screen.</summary>
    public static readonly Color Sky = new(18, 20, 28, 255);

    /// <summary>Impassable tiles.</summary>
    public static readonly Color Solid = new(86, 102, 122, 255);

    /// <summary>The player's collision box.</summary>
    public static readonly Color Player = new(94, 234, 212, 255);

    /// <summary>
    /// The bars around the virtual screen. Deliberately not
    /// <see cref="Sky"/>: the played area has to be visibly bounded, or a
    /// letterboxed window looks like a rendering bug.
    /// </summary>
    public static readonly Color Letterbox = new(8, 9, 12, 255);
}
