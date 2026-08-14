namespace Platformer.Core.Time;

/// <summary>
/// Accumulator that decouples simulation rate from render rate. The simulation
/// always advances in identical <see cref="FixedDelta"/> slices so that physics
/// is deterministic and reproducible in tests, regardless of frame rate.
/// </summary>
public sealed class FixedStepClock
{
    /// <summary>Simulation step size in seconds (60 Hz).</summary>
    public const float FixedDelta = 1f / 60f;

    /// <summary>
    /// Longest real frame the clock will honour. Beyond this the simulation
    /// deliberately runs slow rather than entering a death spiral where each
    /// catch-up batch takes longer than the time it is trying to reclaim.
    /// </summary>
    public const float MaxFrameTime = 0.25f;

    private float _accumulator;

    /// <summary>Unconsumed time, always in [0, FixedDelta).</summary>
    public float Accumulator => _accumulator;

    /// <summary>
    /// Fraction of a step already elapsed, for interpolating rendered positions
    /// between the last two simulation states. Always in [0, 1).
    /// </summary>
    public float Alpha => _accumulator / FixedDelta;

    /// <summary>
    /// Adds a real elapsed frame and returns how many fixed steps are now due.
    /// Negative frame times are ignored; oversized ones are clamped.
    /// </summary>
    public int Advance(float frameSeconds)
    {
        if (float.IsNaN(frameSeconds) || frameSeconds <= 0f)
        {
            return 0;
        }

        _accumulator += MathF.Min(frameSeconds, MaxFrameTime);

        var steps = 0;
        while (_accumulator >= FixedDelta)
        {
            _accumulator -= FixedDelta;
            steps++;
        }

        return steps;
    }

    /// <summary>Discards pending time, e.g. after a long pause or level load.</summary>
    public void Reset() => _accumulator = 0f;
}
