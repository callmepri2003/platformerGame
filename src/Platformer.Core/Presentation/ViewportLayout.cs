namespace Platformer.Core.Presentation;

/// <summary>
/// Where and how large the virtual screen is drawn inside the real window,
/// in window pixels.
/// </summary>
/// <param name="Scale">
/// Whole-number magnification. Never zero, so a layout is always drawable.
/// </param>
/// <param name="X">Left edge of the scaled image within the window.</param>
/// <param name="Y">Top edge of the scaled image within the window.</param>
/// <param name="Width">Width of the scaled image.</param>
/// <param name="Height">Height of the scaled image.</param>
public readonly record struct ViewportLayout(int Scale, int X, int Y, int Width, int Height)
{
    /// <summary>Right edge of the scaled image within the window.</summary>
    public int Right => X + Width;

    /// <summary>Bottom edge of the scaled image within the window.</summary>
    public int Bottom => Y + Height;
}
