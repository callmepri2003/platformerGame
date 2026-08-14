namespace Platformer.Core.Input;

/// <summary>
/// Intent-level input, deliberately decoupled from any key or gamepad binding.
/// The simulation consumes only these flags, which keeps
/// <c>Platformer.Core</c> free of rendering and device dependencies and lets
/// tests drive the game without a window.
/// </summary>
[Flags]
public enum InputCommand
{
    None = 0,
    Left = 1 << 0,
    Right = 1 << 1,
    Jump = 1 << 2,
    Dash = 1 << 3,
}
