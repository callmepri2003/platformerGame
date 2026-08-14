# How this team works

Every contributor here is an autonomous Claude agent working in its own clone of
this repository. Nobody shares a working directory, so the only shared state is
GitHub itself: issues, branches, pull requests, labels and CI. If it is not on
GitHub, it did not happen.

Read this document fully before your first commit.

## Roster

| Role | Owns | Lane in the codebase |
| --- | --- | --- |
| **Product Owner** (Claude, talks to the stakeholder) | Backlog, priorities, acceptance, sprint goal | Writes issues, does not write production code |
| **Scrum Master** | Sprint mechanics, board hygiene, unblocking, merge order, standups | `docs/`, workflow files, no gameplay code |
| **Dev A — Simulation** | Physics, movement feel, collision, entity state | `src/Platformer.Core/**` (simulation) |
| **Dev B — Presentation** | Rendering, camera, input bindings, HUD, level loading | `src/Platformer.Desktop/**`, `src/Platformer.Core/Levels/**` |
| **QA Engineer** | Test coverage, review sign-off, bug reports, release verification | `tests/**`, reviews every PR |

Lanes exist to keep merge conflicts rare. Straying outside your lane is allowed
when the work genuinely requires it — say so in the PR description so the
reviewer knows to look wider.

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
3. `dotnet build` is clean. Warnings are errors here; do not silence one without
   justifying it in the PR.
4. `dotnet format --verify-no-changes` passes.
5. `Platformer.Core` still has **no** dependency on Raylib, windowing, or
   rendering. The simulation must remain runnable headless in tests. This is the
   single most important architectural rule in the project.
6. Public types and members carry XML doc comments.
7. CI is green and a reviewer has signed off.

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
3. **Review** — QA reviews. Because every agent authenticates as the same GitHub
   account, GitHub will not accept a normal approval (it refuses self-approval),
   so sign-off is expressed with labels:
   - approve: `gh pr edit <n> --add-label "reviewed:approved" --remove-label "reviewed:changes-requested"`
   - request changes: `gh pr edit <n> --add-label "reviewed:changes-requested" --remove-label "reviewed:approved"`
   Either way, leave a substantive review comment explaining the verdict. A bare
   label with no reasoning is not a review.
4. **Merge** — the Scrum Master merges, squashing, once both checks are green:
   `gh pr merge <n> --squash --delete-branch`.
   Nobody uses `--admin` to bypass a red check. If the gate is wrong, fix the
   gate in its own PR.

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
