namespace Platformer.Core.Input;

/// <summary>
/// Supplies the commands held during the current simulation step. Implemented
/// by the desktop front-end against real devices, and by fakes in tests.
/// </summary>
public interface IInputSource
{
    /// <summary>Commands held down this step.</summary>
    InputCommand Held { get; }

    /// <summary>Commands that became active on this step only (edge-triggered).</summary>
    InputCommand Pressed { get; }
}
