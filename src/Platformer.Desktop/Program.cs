using Platformer.Core.Levels;
using Platformer.Core.Movement;
using Platformer.Core.Presentation;
using Platformer.Core.Time;
using Platformer.Desktop;
using Raylib_cs;

var viewport = new VirtualViewport();
var level = AsciiLevelLoader.LoadEmbedded(AsciiLevelLoader.TestLevelName);

Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
Raylib.InitWindow(viewport.Width * 4, viewport.Height * 4, "Platformer");

// Below the virtual resolution the image can only be cropped, so stop the window
// there rather than letting someone shrink the game out of view.
Raylib.SetWindowMinSize(viewport.Width, viewport.Height);
Raylib.SetTargetFPS(60);

using var screen = new VirtualScreen(viewport);

var clock = new FixedStepClock();
var input = new RaylibInputSource();

var player = new PlayerBody(level);

while (!Raylib.WindowShouldClose())
{
    input.Poll();

    // Every step is the same length regardless of how long the frame took, so
    // the simulation behaves identically on any machine; the renderer absorbs
    // the difference through the clock's alpha rather than the physics doing it.
    var steps = clock.Advance(Raylib.GetFrameTime());
    for (var i = 0; i < steps; i++)
    {
        player.Advance(input.Held, FixedStepClock.FixedDelta);
    }

    screen.BeginFrame(Palette.Sky);
    WorldRenderer.DrawTiles(level.Tiles, Palette.Solid);
    WorldRenderer.DrawBody(player, clock.Alpha, Palette.Player);
    screen.EndFrame(Palette.Letterbox);
}

Raylib.CloseWindow();
