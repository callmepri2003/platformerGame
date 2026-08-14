namespace Platformer.Core.Presentation;

/// <summary>
/// Works out how a fixed low-resolution image is placed inside a window of any
/// size: how far to magnify it, and where to put it.
/// </summary>
/// <remarks>
/// <para>
/// The game draws at one small resolution and is magnified to fit. Magnifying by
/// a whole number is the whole point: a fractional scale spreads some source
/// pixels over more screen pixels than others, so a tile edge that is crisp in
/// one place shimmers in another, and the art stops being pixel art. Whatever is
/// left over after a whole-number fit becomes even bars around the image —
/// letterboxing — rather than being taken up by stretching, which would change
/// the aspect ratio and distort every sprite in the game.
/// </para>
/// <para>
/// This type is deliberately free of any rendering dependency: it is arithmetic
/// over sizes, so it can be tested without opening a window. The renderer in
/// <c>Platformer.Desktop</c> asks it where to blit and does no layout maths of
/// its own.
/// </para>
/// </remarks>
public sealed class VirtualViewport
{
    /// <summary>Virtual width the game is authored and drawn at.</summary>
    public const int DefaultWidth = 320;

    /// <summary>Virtual height the game is authored and drawn at.</summary>
    public const int DefaultHeight = 180;

    /// <summary>Creates a viewport for a virtual resolution.</summary>
    /// <param name="width">Virtual width in pixels; must be positive.</param>
    /// <param name="height">Virtual height in pixels; must be positive.</param>
    public VirtualViewport(int width = DefaultWidth, int height = DefaultHeight)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        Width = width;
        Height = height;
    }

    /// <summary>Virtual width in pixels.</summary>
    public int Width { get; }

    /// <summary>Virtual height in pixels.</summary>
    public int Height { get; }

    /// <summary>
    /// Largest whole-number magnification at which the virtual screen still fits
    /// inside a window, and where to place it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Total by design: any window size produces a layout rather than an error,
    /// because window size is not something the game controls. A window smaller
    /// than the virtual resolution cannot fit it at any whole scale, so the scale
    /// floors at one and the image is centred, overflowing equally on each side
    /// — the edges are lost rather than the picture being squashed. The
    /// alternative, scaling below one, is the fractional case this type exists to
    /// avoid. A zero or negative size, which is what a minimised window reports,
    /// is treated the same way, so a minimise cannot crash a frame.
    /// </para>
    /// <para>
    /// Both offsets are whole pixels, and any odd remainder is left on the right
    /// or bottom bar rather than being split, since half a pixel of offset is the
    /// blurring this is all here to prevent.
    /// </para>
    /// </remarks>
    /// <param name="windowWidth">Window width in pixels.</param>
    /// <param name="windowHeight">Window height in pixels.</param>
    /// <returns>Where to draw the virtual screen, in window pixels.</returns>
    public ViewportLayout LayoutFor(int windowWidth, int windowHeight)
    {
        var scale = Math.Min(windowWidth / Width, windowHeight / Height);
        if (scale < 1)
        {
            scale = 1;
        }

        var scaledWidth = Width * scale;
        var scaledHeight = Height * scale;

        return new ViewportLayout(
            scale,
            (windowWidth - scaledWidth) / 2,
            (windowHeight - scaledHeight) / 2,
            scaledWidth,
            scaledHeight);
    }
}
