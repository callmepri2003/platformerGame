using Platformer.Core.Presentation;
using Raylib_cs;

namespace Platformer.Desktop;

/// <summary>
/// The low-resolution surface the game draws on, and the magnified blit that
/// puts it on screen.
/// </summary>
/// <remarks>
/// Everything is drawn into an off-screen texture at the virtual resolution
/// first, then that whole texture is magnified once. Drawing straight to the
/// window at a scale factor instead would let each shape round its own edges
/// independently, so a tile and the player standing on it could disagree about
/// where the same world coordinate is by a pixel. One blit of one texture cannot
/// disagree with itself.
/// </remarks>
internal sealed class VirtualScreen : IDisposable
{
    private readonly VirtualViewport _viewport;
    private readonly RenderTexture2D _target;
    private bool _disposed;

    /// <summary>Creates the surface for a virtual resolution.</summary>
    /// <param name="viewport">The virtual resolution and its layout maths.</param>
    public VirtualScreen(VirtualViewport viewport)
    {
        ArgumentNullException.ThrowIfNull(viewport);

        _viewport = viewport;
        _target = Raylib.LoadRenderTexture(viewport.Width, viewport.Height);

        // Point sampling: magnifying with any interpolation is what turns pixel
        // art into mush, and it is the default on some drivers.
        Raylib.SetTextureFilter(_target.Texture, TextureFilter.Point);
    }

    /// <summary>Starts drawing at virtual resolution.</summary>
    /// <param name="background">Colour behind the level.</param>
    public void BeginFrame(Color background)
    {
        Raylib.BeginTextureMode(_target);
        Raylib.ClearBackground(background);
    }

    /// <summary>
    /// Ends the frame and blits the virtual screen to the window, magnified by a
    /// whole number and centred, with bars filling whatever is left over.
    /// </summary>
    /// <param name="letterbox">Colour of the bars around the image.</param>
    public void EndFrame(Color letterbox)
    {
        Raylib.EndTextureMode();

        var layout = _viewport.LayoutFor(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());

        Raylib.BeginDrawing();

        // Clearing the whole window every frame is what fills the bars; without
        // it the previous frame's image stays visible outside the new one after
        // a resize.
        Raylib.ClearBackground(letterbox);

        Raylib.DrawTexturePro(
            _target.Texture,

            // Negative height flips the source: render textures are stored
            // bottom-up, and blitting one without the flip draws the game
            // upside down.
            new Rectangle(0f, 0f, _target.Texture.Width, -_target.Texture.Height),
            new Rectangle(layout.X, layout.Y, layout.Width, layout.Height),
            System.Numerics.Vector2.Zero,
            0f,
            Color.White);

        Raylib.EndDrawing();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Raylib.UnloadRenderTexture(_target);
        _disposed = true;
    }
}
