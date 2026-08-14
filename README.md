# Platformer

A precision 2D platformer in C# — tight, responsive movement of the Celeste and
Super Meat Boy school: coyote time, jump buffering, variable jump height, and a
dash. Built with .NET 9 and [Raylib-cs](https://github.com/chrisdill/raylib-cs).

It is also an experiment: every line is written by a team of autonomous Claude
agents running proper Scrum — sprints, a groomed backlog, pull requests, code
review sign-off and enforced CI/CD. See [docs/TEAM.md](docs/TEAM.md) for how the
team operates.

## Layout

```
src/Platformer.Core/      Simulation. Pure C#, no rendering, no windowing.
src/Platformer.Desktop/   Raylib front-end: window, rendering, input bindings.
tests/Platformer.Core.Tests/  xUnit tests over the simulation.
```

`Platformer.Core` deliberately knows nothing about how it is drawn. The
simulation runs headless, so the entire game is testable in CI without a display
and the renderer can be replaced without touching gameplay.

## Running it

```sh
dotnet run --project src/Platformer.Desktop
```

## Developing

```sh
dotnet build                       # warnings are errors
dotnet test                        # xUnit suite
dotnet format                      # apply formatting
dotnet format --verify-no-changes  # what CI checks

# Platformer.Core coverage, the number CI gates at 90%
dotnet test --settings coverlet.runsettings --collect:"XPlat Code Coverage"
```

Coverage is measured over `Platformer.Core` only. `Platformer.Desktop` is a thin
Raylib adapter that cannot run headless, so measuring it would describe the size
of the renderer rather than the quality of the testing — see
`coverlet.runsettings`.

## Controls

| Action | Keys |
| --- | --- |
| Move | `←` `→` / `A` `D` |
| Jump | `Space` / `Z` |
| Dash | `Shift` / `X` |
