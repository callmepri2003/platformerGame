using Platformer.Core.Input;

namespace Platformer.Core.Tests.Harness;

/// <summary>
/// What the simulation saw on one step. Recorded by
/// <see cref="SimulationHarness{TSimulation}.Trace"/> so a failing test can
/// show the input that produced the failure.
/// </summary>
/// <param name="StepIndex">Zero-based index of the step.</param>
/// <param name="Held">Commands held during the step.</param>
/// <param name="Pressed">Commands that became active on this step only.</param>
public readonly record struct InputFrame(int StepIndex, InputCommand Held, InputCommand Pressed);
