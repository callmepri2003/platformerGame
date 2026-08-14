namespace Platformer.Core.Tests.Harness;

/// <summary>
/// Thrown when <see cref="SimulationHarness{TSimulation}.RunUntil"/> reaches
/// its step cap without the condition ever holding. The harness fails this way
/// rather than looping forever so a broken simulation shows up as a red test in
/// seconds instead of a hung CI job.
/// </summary>
public sealed class SimulationTimeoutException : Exception
{
    /// <summary>Creates the exception with a default message.</summary>
    public SimulationTimeoutException()
        : base("The simulation did not reach the expected condition within the step cap.")
    {
    }

    /// <summary>Creates the exception with an explanatory message.</summary>
    /// <param name="message">Description of what was awaited and for how long.</param>
    public SimulationTimeoutException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and an inner cause.</summary>
    /// <param name="message">Description of what was awaited and for how long.</param>
    /// <param name="innerException">The underlying cause.</param>
    public SimulationTimeoutException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
