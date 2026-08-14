using Platformer.Core.Time;
using Platformer.Desktop;
using Raylib_cs;

const int VirtualWidth = 320;
const int VirtualHeight = 180;
const int Scale = 4;

Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
Raylib.InitWindow(VirtualWidth * Scale, VirtualHeight * Scale, "Platformer");
Raylib.SetTargetFPS(60);

var clock = new FixedStepClock();
var input = new RaylibInputSource();
var ticks = 0L;

while (!Raylib.WindowShouldClose())
{
    input.Poll();

    var steps = clock.Advance(Raylib.GetFrameTime());
    for (var i = 0; i < steps; i++)
    {
        ticks++;
    }

    Raylib.BeginDrawing();
    Raylib.ClearBackground(new Color(13, 15, 20, 255));
    Raylib.DrawText("walking skeleton", 24, 24, 40, new Color(94, 234, 212, 255));
    Raylib.DrawText($"ticks {ticks}", 24, 80, 24, Color.Gray);
    Raylib.DrawText($"input {input.Held}", 24, 112, 24, Color.Gray);
    Raylib.EndDrawing();
}

Raylib.CloseWindow();
