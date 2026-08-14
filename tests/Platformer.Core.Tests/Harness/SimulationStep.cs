using Platformer.Core.Input;

namespace Platformer.Core.Tests.Harness;

/// <summary>
/// Adapts any simulation type to the harness by describing how to advance it a
/// single fixed step.
/// </summary>
/// <typeparam name="TSimulation">The simulation type being driven.</typeparam>
/// <param name="simulation">The simulation instance to advance.</param>
/// <param name="input">Input for this step, already edge-resolved.</param>
/// <param name="deltaSeconds">Length of the step in seconds.</param>
public delegate void SimulationStep<in TSimulation>(
    TSimulation simulation,
    IInputSource input,
    float deltaSeconds);
