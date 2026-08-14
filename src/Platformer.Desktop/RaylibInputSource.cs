using Platformer.Core.Input;
using Raylib_cs;

namespace Platformer.Desktop;

/// <summary>
/// Translates physical keys into intent-level <see cref="InputCommand"/> flags.
/// This is the only place in the codebase that knows about specific keys.
/// </summary>
public sealed class RaylibInputSource : IInputSource
{
    private static readonly (KeyboardKey Key, InputCommand Command)[] Bindings =
    [
        (KeyboardKey.Left, InputCommand.Left),
        (KeyboardKey.A, InputCommand.Left),
        (KeyboardKey.Right, InputCommand.Right),
        (KeyboardKey.D, InputCommand.Right),
        (KeyboardKey.Space, InputCommand.Jump),
        (KeyboardKey.Z, InputCommand.Jump),
        (KeyboardKey.LeftShift, InputCommand.Dash),
        (KeyboardKey.X, InputCommand.Dash),
    ];

    public InputCommand Held { get; private set; }

    public InputCommand Pressed { get; private set; }

    /// <summary>Polls devices once per rendered frame, before simulation steps.</summary>
    public void Poll()
    {
        var held = InputCommand.None;
        var pressed = InputCommand.None;

        foreach (var (key, command) in Bindings)
        {
            if (Raylib.IsKeyDown(key))
            {
                held |= command;
            }

            if (Raylib.IsKeyPressed(key))
            {
                pressed |= command;
            }
        }

        Held = held;
        Pressed = pressed;
    }
}
