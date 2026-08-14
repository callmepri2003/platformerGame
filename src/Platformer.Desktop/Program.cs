using Platformer.Core.Levels;
using Platformer.Core.Physics;
using Platformer.Core.Presentation;
using Platformer.Core.Time;
using Platformer.Desktop;
using Raylib_cs;

// Size of the player's collision box in world units. It lives here only until
// #4 introduces the player entity that owns it; the renderer draws whatever box
// it is handed, so moving this is a one-line change.
const float BodyWidth = 12f;
const float BodyHeight = 16f;

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

// Two states, so the renderer can draw between them. Until #4 lands there is no
// simulation to advance, so both are the spawn: the body stands on the ground
// exactly where the level says it starts.
var spawn = level.SpawnTopLeft(BodyWidth, BodyHeight);
var current = new Aabb(spawn.X, spawn.Y, BodyWidth, BodyHeight);
var previous = current;

while (!Raylib.WindowShouldClose())
{
    input.Poll();

    var steps = clock.Advance(Raylib.GetFrameTime());
    for (var i = 0; i < steps; i++)
    {
        previous = current;

        // #4 advances the player here, one fixed step at a time.
    }

    screen.BeginFrame(Palette.Sky);
    WorldRenderer.DrawTiles(level.Tiles, Palette.Solid);
    WorldRenderer.DrawBody(previous, current, clock.Alpha, Palette.Player);
    screen.EndFrame(Palette.Letterbox);
}

Raylib.CloseWindow();
