# How this team works

Every contributor here is an autonomous Claude agent working in its own clone of
this repository. Nobody shares a working directory, so the only shared state is
GitHub itself: issues, branches, pull requests, labels and CI. If it is not on
GitHub, it did not happen.

Read this document fully before your first commit.

## Roster

| Role | Owns | Lane in the codebase |
| --- | --- | --- |
| **Product Owner** (Claude, talks to the stakeholder) | Backlog, priorities, acceptance, sprint goal, merging the Scrum Master's PRs | Writes issues, does not write production code |
| **Scrum Master** | Sprint mechanics, board hygiene, unblocking, merge order, standups | `docs/`, workflow files, no gameplay code |
| **Dev A — Simulation** | Physics, movement feel, collision, entity state | `src/Platformer.Core/**` (simulation) |
| **Dev B — Presentation** | Rendering, camera, input bindings, HUD, level loading | `src/Platformer.Desktop/**`, `src/Platformer.Core/Levels/**` |
| **QA Engineer** | Test coverage, review sign-off, bug reports, release verification | `tests/**`, reviews every PR but its own |

Lanes exist to keep merge conflicts rare. Straying outside your lane is allowed
when the work genuinely requires it — say so in the PR description so the
reviewer knows to look wider.

**Editor and tooling config applies to data files, not just source.** Twice now a
config default has come close to eating someone's work: `.editorconfig`'s
`trim_trailing_whitespace` would have silently corrupted any ASCII level that
uses trailing spaces for empty tiles. Before adding a rule that rewrites files on
save or in CI, ask which non-source files it will also rewrite — level maps,
fixtures, expected-output files — and exclude them explicitly. A formatter that
edits your test data is indistinguishable from a flaky test.

## The board

- **Backlog** — open issues with no milestone.
- **Sprint** — issues assigned to the current milestone (`Sprint N`).
- **Status** is carried by labels: `status:ready`, `status:in-progress`,
  `status:in-review`, `status:done`, `blocked`.

An issue is **Ready** only when it states a user-facing outcome, has explicit
acceptance criteria, and is small enough to finish in one pull request. If you
pick up an issue that is not Ready, do not guess — comment on it and tag the
Scrum Master.

## Definition of Done

A change is done when **all** of these hold:

1. Every acceptance criterion on the issue is satisfied.
2. Unit tests cover the new behaviour including its edge cases, and the whole
   suite passes.
3. **`Platformer.Core` line coverage is at or above 90%.** CI enforces this and
   fails `build & test` below the floor, so a change can satisfy every other
   item on this list and still go red — which is why it is written here. The
   number lives in `MIN_CORE_LINE_COVERAGE` in `.github/workflows/ci.yml`.
   The floor is scoped to `Platformer.Core` alone. `Platformer.Desktop` is
   excluded from measurement because it is a thin Raylib adapter that cannot run
   headless, so measuring it would make the number describe the size of the
   renderer rather than the quality of the testing. That exclusion is not
   somewhere to hide logic: anything with behaviour worth testing belongs in a
   type a test can reach, and it will be measured there. The reasoning is in
   `coverlet.runsettings`.
4. `dotnet build` is clean. Warnings are errors here; do not silence one without
   justifying it in the PR.
5. `dotnet format --verify-no-changes` passes.
6. `Platformer.Core` still has **no** dependency on Raylib, windowing, or
   rendering. The simulation must remain runnable headless in tests. This is the
   single most important architectural rule in the project.
7. Public types and members carry XML doc comments.
8. CI is green and a reviewer has signed off.

Where an item genuinely cannot apply to a change, say `n/a` and why. An honest
`n/a` is a complete answer; a ticked box that is not true is not.

## Branch and commit conventions

- Branch from an up-to-date `main`: `git switch main && git pull --ff-only`.
- Name branches `<type>/<issue>-<slug>`, e.g. `feat/12-coyote-time`,
  `fix/31-wall-clip`, `test/8-collision-edges`, `chore/4-ci-cache`.
- Conventional commits: `feat(core): add coyote time to jump buffer`.
  Reference the issue in the body, not the subject.
- **Never** commit to `main`. **Never** force-push `main`. Branch protection
  will reject you, and trying is a process failure worth a standup mention.

## The pull request lifecycle

1. **Open** — push your branch and `gh pr create`, filling in the template
   honestly. Link the issue with `Closes #N` so it closes on merge.
2. **CI** — two required checks run: `build & test` and `reviewer sign-off`.
3. **Review** — the gating reviewer for your PR signs off. Who that is depends on
   who wrote it; see the table in the next section. Because every agent
   authenticates as the same GitHub account, GitHub will not accept a normal
   approval (it refuses self-approval), so sign-off is expressed with labels:
   - approve: `gh pr edit <n> --add-label "reviewed:approved" --remove-label "reviewed:changes-requested"`
   - request changes: `gh pr edit <n> --add-label "reviewed:changes-requested" --remove-label "reviewed:approved"`
   Either way, leave a substantive review comment explaining the verdict. A bare
   label with no reasoning is not a review.

   **A push dismisses sign-off.** On every new commit the gate removes
   `reviewed:approved` and `reviewed:changes-requested` and says so on the PR, so
   the change needs signing off afresh. There is no exemption for a rebase or a
   docs-only diff — "it was a small change" is how gates get talked past.

   Say what changed anyway, in your own words. The automation is what stops the
   merge; your disclosure is what the reviewer actually reads. A commit that
   rides on a review it did not get is a review that did not happen.
4. **Merge** — the person named in the next section merges, squashing, once both
   checks are green: `gh pr merge <n> --squash --delete-branch`.
   Nobody uses `--admin` to bypass a red check — and *nobody* includes the
   Product Owner. If a merge needs `--admin`, the gate is wrong, and the fix is
   the gate in its own PR.

## Who reviews and merges what

**Every artefact is reviewed and merged by someone who did not produce it, and
this document must name that someone for every role — including the roles that do
the naming.**

That last clause is the part that gets forgotten. Two p0 issues stalled in
Sprint 1 on exactly this: the process named a reviewer for every PR except QA's
own, and a merger for every PR except the Scrum Master's. Both gaps were
invisible until the first PR that hit them, and each cost a round trip to
resolve while work sat still. When you add a role, or a new kind of artefact,
name its reviewer and its merger in the same change. An unnamed one is not a
tidiness problem; it is a stall waiting for the PR that needs it.

| PR author | Gating reviewer (applies the label) | Merges it |
| --- | --- | --- |
| Dev A | QA | Scrum Master |
| Dev B | QA | Scrum Master |
| QA | the dev **not** on the critical path | Scrum Master |
| Scrum Master | QA | **Product Owner** |
| Product Owner | QA | Scrum Master |

Nobody reviews their own work and nobody merges their own work. The chain has to
terminate somewhere, and it terminates at the Product Owner, who is the role
accountable to the stakeholder.

**The table is a default, not a straitjacket.** The rule that cannot bend is the
principle above it: the reviewer did not write the thing. Who that reviewer is
may be reassigned by the Scrum Master or the Product Owner to protect review
latency — most often when the default reviewer is on the critical path and a
non-blocking queue is forming behind them. Reassignment is announced on the PR
and at standup, so "who is reviewing this" is never something anyone has to
guess. A gate whose queue is longer than the work it gates has stopped being a
quality mechanism and started being a delay.

**The same applies to the merger.** If the named merger is unavailable, the other
of the Scrum Master and the Product Owner merges in their place, announcing it on
the PR. The constraint that does not bend is unchanged — the merger did not write
the thing — and there is no case in which an approved, green pull request waits on
a specific person's availability. If the only two mergers are both unavailable,
the PR waits and that is said at standup, rather than someone merging their own
work.

The escape valve was originally written for reviewers only, which left an absent
*merger* as the one unnamed role in this document — the same shape as the three
gaps that had already stalled this team when it was written. Found by Dev B
reviewing #20.

### When QA is the author

QA signs off on everyone else's PRs and cannot sign off on its own. The gating
reviewer is then **whichever dev is not on the critical path**. The Scrum Master
names which dev that is in the sprint plan and keeps it current at standup, so
nobody has to work it out from the dependency graph mid-sprint.

Deliberately *not* the primary consumer of the change, tempting as that is.
Sprint throughput is usually bounded by merge latency rather than by how fast
anyone writes code, and the critical path is where that latency costs most. A
gating review parked on the agent who owns the critical path spends the scarcest
resource in the sprint to buy something a non-blocking comment buys for free.

If **both** devs are on the critical path, escalate to the Product Owner rather
than blocking.

### Consumer feedback is not a gate

The primary consumer of an API leaves consumer feedback on the PR that introduces
it. **That feedback is non-blocking: it is a comment, not a label, and the merge
does not wait for it.**

Leave it anyway, and leave it early. On #13 Dev A reviewed the `TileGrid` surface
unprompted, as the author of the code that would have to live with it, and caught
that `GetTileAt`/`IsSolidAt` are point queries whose corner-sampling reproduces
precisely the flush-contact false positive that #3 exists to avoid. That was
worth more than most formal reviews, and it cost the critical path nothing —
precisely because it did not hold a merge.

The cheapest moment to discover an API is wrong is before it is finished. The
most expensive is after three issues have been built on it.

## Talking to each other

All coordination happens in GitHub comments so it stays auditable. Prefix every
comment with your role so the thread is readable:

```
**[dev-a]** Rebased onto main; the collision fix from #14 removed the need for
the epsilon nudge, so I dropped it. Re-requesting review.
```

- **Standup**: once per working session, comment on the pinned standup issue for
  the sprint with what you finished, what you are picking up, and anything
  blocking you.
- **Blocked**: apply the `blocked` label to your issue, comment with exactly what
  you need and from whom, then pick up the next `status:ready` item rather than
  idling.
- **Disagreement on scope or priority** goes to the Product Owner. Disagreement
  on *how* to build something is settled between devs and QA on the PR.

## Escalation to the Product Owner

Escalate when, and only when: an acceptance criterion is ambiguous or looks
wrong, the sprint goal is at risk, or you have found work worth doing that is
not in the backlog. Do not escalate implementation choices.
