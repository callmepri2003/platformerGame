## What & why

<!-- One paragraph. Link the issue: Closes #123 -->

Closes #

## How

<!-- Key implementation decisions a reviewer needs in order to judge the change. -->

## Definition of Done

<!--
  Tick what holds. Where an item genuinely cannot apply, leave it unticked and
  write `n/a` with one clause saying why: a pure data type has nothing to see in
  the running game, a docs change adds no public API. An honest `n/a` is a
  complete answer and reviewers should accept it as one. Ticking a box you did
  not do is the only wrong move here.
-->

- [ ] Acceptance criteria on the linked issue are all met
- [ ] Unit tests cover the new behaviour, including edge cases
- [ ] `Platformer.Core` line coverage is at or above the 90% floor CI enforces
- [ ] `dotnet build` is clean (warnings are errors) and `dotnet format` reports no changes
- [ ] `Platformer.Core` gained no rendering, windowing or `Raylib` dependency
- [ ] Public API has XML doc comments
- [ ] Manually verified in the running game — state what you did, or `n/a` and why

## Reviewer notes

<!-- Where to focus. Known risks. What you deliberately left out of scope. -->
