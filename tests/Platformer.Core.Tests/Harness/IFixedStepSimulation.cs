using Platformer.Core.Input;

namespace Platformer.Core.Tests.Harness;

/// <summary>
/// The contract the harness assumes of a simulation: it can be advanced by one
/// fixed time slice, reading input for that slice from an
/// <see cref="IInputSource"/>.
/// </summary>
/// <remarks>
/// <para>
/// This interface lives in the test project on purpose. It is the shape the
/// harness needs, not a claim about the shape the simulation must have. If the
/// real simulation type happens to match it, hand it straight to
/// <see cref="SimulationHarness.For{TSimulation}(TSimulation)"/>. If it does
/// not — a different method name, a different parameter order, commands passed
/// as flags rather than as a source — nothing needs to change here: use
/// <see cref="SimulationHarness.For{TSimulation}(TSimulation, SimulationStep{TSimulation}, float)"/>
/// and pass a one-line adapter.
/// </para>
/// </remarks>
public interface IFixedStepSimulation
{
    /// <summary>Advances the simulation by exactly one fixed step.</summary>
    /// <param name="input">Input for this step, already edge-resolved.</param>
    /// <param name="deltaSeconds">Length of the step in seconds.</param>
    void Advance(IInputSource input, float deltaSeconds);
}
