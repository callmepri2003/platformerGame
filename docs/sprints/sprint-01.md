# Sprint 1 — plan

**Milestone:** [Sprint 1](https://github.com/callmepri2003/platformerGame/milestone/1) · due 2026-08-21
**Standup thread:** #11
**Issues in scope:** #1–#10 (ten). #11 is the standup thread, not work.

## Sprint goal

A player can run, jump and land on solid ground in a hand-authored test level,
with movement that already feels responsive.

Read literally, that sentence names five issues: #1 (there is a level),
#8 (it is hand-authored), #3 (landing on solid ground works), #4 + #5 (running
and jumping). Those five are the goal. #2 makes them testable, #7 makes them
visible, #6 makes them feel good, #9 locks the feel in, #10 is scenery. That
distinction drives the cut list at the bottom.

## Dependency graph

```mermaid
graph LR
  I1["#1 TileGrid<br/>Dev B · p0"] --> I3["#3 AABB collision<br/>Dev A · p0"]
  I1 --> I8["#8 ASCII level loader<br/>Dev B · p1"]
  I1 --> I7["#7 Render grid + player<br/>Dev B · p1"]
  I3 --> I4["#4 Run: accel + friction<br/>Dev A · p1"]
  I3 --> I5["#5 Gravity + variable jump<br/>Dev A · p1"]
  I4 -.player entity + prev state.-> I7
  I4 -.tuning type.-> I5
  I5 --> I6["#6 Coyote + jump buffer<br/>Dev A · p1"]
  I7 --> I10["#10 Camera deadzone<br/>Dev B · p2"]
  I4 --> I9["#9 Feel characterisation<br/>QA · p2"]
  I5 --> I9
  I6 --> I9
  I8 --e2e test level--> I9
  I2["#2 Sim harness + fake input<br/>QA · p0"] -.used by.-> I4
  I2 -.used by.-> I5
  I2 -.used by.-> I6
  I2 -.used by.-> I9
```

Solid arrows are hard dependencies. Dotted arrows are softer couplings. Four of
these edges were not stated on the issues when the sprint opened — I added them
because the acceptance criteria imply them, and the Product Owner has since
confirmed the omission and amended #9:

| Edge | Why it exists | Consequence |
| --- | --- | --- |
| **#4** → #7 | #7 requires the player be drawn "as a rectangle matching its actual collision box" and interpolated between two simulation states via `Alpha`. I first read this as a dependency on #3; it is not. #3 is a pure static collider with no entity. The player entity and its previous-state snapshot are born in **#4**, where the Product Owner placed the interpolation AC. | #7 is scheduled after **#4**, which is one merge later than the original plan assumed. |
| #4 → #5 | #4's AC mandates a single named tuning type and says "the next issue and QA will both need to reference them". #5 and #6 extend that type; building it twice guarantees a conflict in `Platformer.Core`. | Dev A does #4 before #5 even though both only hard-depend on #3. |
| #8 → #9 | #9's last AC is an end-to-end run in "the real test level" — that level ships in #8. **Confirmed by the Product Owner and now written onto #9.** | #8 is scheduled ahead of #7 so it cannot become the thing that strands #9. |
| #2 → #4/#5/#6/#9 | Not a build dependency, but every one of those issues asserts behaviour over stepped time. Without the harness each dev hand-rolls a stepping loop and the suites diverge. | #2 runs in Wave 0 alongside #1, not later. |

**Critical path:** #1 → #3 → #4 → #5 → #6 → #9. Six issues deep, four of them on
one agent, every hop gated by a squash merge behind two required checks. This
path, not any individual issue, is what the sprint lives or dies on.

## Wave ordering

Waves are gated on **merge to `main`**, not on "someone has it working locally".
A dependent issue starts when its blocker is merged.

### Wave 0 — now

| Who | Issue | Note |
| --- | --- | --- |
| Dev B | #1 TileGrid data model | Highest-leverage item in the sprint: three issues wait on it. Keep it a plain data structure — no parsing, no drawing, no collision. |
| QA | #2 Deterministic harness + fake input | Startable today: `IInputSource`, `InputCommand` and `FixedStepClock` already exist on `main`. Define the driver against the simulation interface you need and say so on the PR. |
| Dev A | *(blocked — see below)* | Not idle: publishes the collision contract, then builds #3 against Dev B's pushed branch. |

Dev B, in the first standup comment, must state the **out-of-bounds decision**
from #1 (is outside the grid solid or empty?) and the exact `TileGrid` surface —
signatures for read-by-tile, read-by-world-position, and the world↔tile
conversions. That decision is not Dev B's alone to make quietly: it determines
whether a player who walks off the map falls forever or hits an invisible wall,
and Dev A has to write the collision code that lives with it.

### Wave 1 — on #1 merged

| Who | Issue | Unblocked by |
| --- | --- | --- |
| Dev A | #3 AABB collision resolution | #1 |
| Dev B | #8 ASCII level loader + test level | #1 |
| QA | finish #2, then review #1 and #3 ahead of its own work | — |

### Wave 2 — on #3 merged

| Who | Issue | Unblocked by |
| --- | --- | --- |
| Dev A | #4 run: acceleration and friction, and the shared tuning type | #3 |
| Dev B | #7 render grid + player at 320×180 — **start the unblocked two-thirds now** | #1 merged; player box and interpolation need #4 |
| QA | review #3 and #8; start scaffolding #9 against #4's tuning constants | — |

### Wave 3 — on #4, #5 merged

| Who | Issue | Unblocked by |
| --- | --- | --- |
| Dev A | #5 gravity + variable jump, then #6 coyote time + buffering | #3, #4 (tuning type), #5 |
| Dev B | ~~#10 camera deadzone~~ — **cut**, see below | #7 |
| QA | #9 feel characterisation + `docs/movement-feel.md` | #4, #5, #6, #8 |

**Merge order I will enforce:** #1 → #2 → #3 → #8 → #4 → #5 → #7 → #6 → #9 → #10.

Only part of that order is load-bearing. **#4 → #5 → #6 is hard**: all three edit
the same movement types in `Platformer.Core`, they are one agent's work, and they
will be merged strictly in sequence rather than raced. **#1 → #2 is not**: QA
merged the two branches locally and found no conflicts and a green combined
suite, so neither should ever sit waiting on the other. Where the order is
merely preference, whichever is green first goes first — idle time on this
critical path is the thing the plan is trying to buy back.

## Assignments

| Agent | Lane | Issues, in the order they should be picked up |
| --- | --- | --- |
| **Dev A — Simulation** | `src/Platformer.Core/**` (gameplay) | #3 → #4 → #5 → #6 |
| **Dev B — Presentation** | `src/Platformer.Desktop/**`, `src/Platformer.Core/Levels/**` | #1 → #8 → #7 → #10 |
| **QA** | `tests/**`, review on every PR | #2 → #9, plus sign-off on all nine other PRs |
| **Scrum Master** | `docs/`, workflows | This plan, board hygiene, merge order, unblocking |
| **Product Owner** | backlog | Acceptance, the two decisions at the bottom of this doc |

#1 is `feat(core)` but lands in `Platformer.Core/Levels` and is labelled
`area:presentation` — that is Dev B's lane per `docs/TEAM.md`, and it is correct.
Dev A reading `TileGrid` from collision code is a normal cross-lane read, not a
lane violation.

### Who gates QA's own PRs

`docs/TEAM.md` requires the Scrum Master to name, here, whichever dev is **not**
on the critical path, because that dev is the gating reviewer for QA's PRs. QA
cannot sign off on its own work, and parking that gate on the critical-path agent
would spend the one resource this sprint is short of.

**Currently: Dev B.** The critical path is #3 → #4 → #5 → #6, all Dev A. This
holds for the whole sprint unless #7 or #8 slips far enough to put Dev B on the
path too, in which case it goes to the Product Owner rather than blocking.

Consumer feedback is a separate, non-blocking duty and is not affected: the
primary consumer of an API comments on the PR that introduces it, and the merge
never waits for that comment.

## Dev A is blocked at sprint start. Honestly.

Dev A owns four of the ten issues, all four are on the critical path, and every
one of them is behind #3, which is behind #1, which is Dev B's. At the moment the
sprint opens, the agent carrying the most goal-critical work has nothing it can
legally merge. That is a planning defect in the issue graph, not a personal one,
and pretending otherwise at the first standup would be worthless.

What I am doing about it, in order of how much it actually buys:

1. **#1 is deliberately tiny and reviewed out of band.** It is a plain data
   structure with no parsing, no drawing and no collision. QA reviews #1 the
   moment it opens, ahead of continuing #2. Shrinking the blocker is worth more
   than any amount of parallel busywork downstream.
2. **Dev A branches off Dev B's branch, not `main`.** Dev B pushes
   `feat/1-tile-grid` as soon as the type compiles, even if the tests are not
   finished. Dev A branches `feat/3-aabb-collision` from it, builds against the
   real API, and rebases onto `main` after #1 squash-merges. Dev A does **not**
   open the #3 PR until #1 is merged — an early PR would carry Dev B's commits in
   its diff and make review a mess. This is an explicit, sanctioned exception to
   "branch from an up-to-date `main`"; it is written down here so it does not
   look like a process breach on the PR.
3. **The contract is agreed before the code exists.** Dev B posts the `TileGrid`
   surface and the out-of-bounds decision on #11 in the first session. Dev A
   reviews it as a consumer *before* #1 is finished, which is the cheapest moment
   to find out the API is wrong.
4. **Dev A's first deliverable is the adversarial spec for #3**, written while
   waiting: the seam-catching case, the flush-against-a-wall case, the
   tunnelling speed threshold. Those are pure test intentions and need no
   `TileGrid` instance to design.

If #1 has not merged by the end of the second working session, I escalate to the
Product Owner rather than letting Dev A keep waiting.

## Biggest risk to the sprint goal

**The critical path is six issues deep, four consecutive links are owned by one
agent, and every link costs a full review-and-CI round trip. Throughput is bounded
by merge latency, not by how fast anyone writes code.**

Concretely: #1 → #3 → #4 → #5 → #6 is five sequential merges before QA can even
start #9, and #9 is the issue that proves the movement feels right. One slow
review anywhere on that chain moves everything behind it. There is no parallelism
available to spend on it, because #4, #5 and #6 all edit the same movement types
and all belong to Dev A.

Mitigations, all of which are things I do rather than things I hope for:

- **Review latency is treated as the scarce resource.** QA reviews any PR on the
  critical path (#1, #3, #4, #5, #6) before starting or resuming its own issue.
  A critical-path PR sitting unreviewed at the end of a session is a standup item
  and a `blocked` label, not a silent wait.
- **I merge the moment both checks are green.** No batching, no waiting for a
  tidy moment. Every hour a green PR sits unmerged is an hour Dev A is idle.
- **Small PRs on the chain.** If #3 grows past collision resolution — if
  tunnelling prevention turns into a swept-AABB rewrite — Dev A documents the
  tunnelling speed threshold on the PR (the issue explicitly allows this) and
  ships. Perfect collision that lands after the sprint is worth zero.
- **Dev B never waits on Dev A.** #8 needs only #1, so Dev B always has merged
  work available even while the simulation chain is moving.
- **QA's spare capacity is protected, not filled.** Once #2 lands, QA's remaining
  sprint issue is #9, which is both the last thing cut and blocked behind #4, #5,
  #6 and #8. That idleness is not a gap to plug with pulled-in scope — it *is*
  the mitigation for the bottleneck. A reviewer with nothing else on means #3 and
  #8 get reviewed the moment they open instead of queuing behind the reviewer's
  own build work. #13 went open-to-merge in about twenty minutes precisely
  because someone was free to look at it. Scope pulled into a sprint to occupy an
  idle agent is how sprints lose their goal, and "we had capacity" is the weakest
  reason to expand one.

Second-order note on the coverage gate, corrected by QA during review and worth
recording accurately. CI enforces a line-coverage floor, and #7 and #10 add
Raylib-facing code in `Platformer.Desktop` that cannot be unit tested headlessly.
**This is not a live risk today.** `coverlet.collector` instruments only the
assemblies actually loaded during a test run, and nothing loads
`Platformer.Desktop` — the sole test project references `Platformer.Core` alone.
QA ran the coverage step and confirmed the report contains exactly one package,
`Platformer.Core`, at 100%. So #7 cannot drag the number down, and nobody should
plan around it as a threat to the sprint. What *is* worth fixing is that the scoping is currently incidental rather than
stated: it holds because of which project references which, and it would change
silently the day someone adds a `Platformer.Desktop.Tests` project. PR #15
(`chore/12-process-gaps`) makes it explicit — measurement scoped to
`Platformer.Core`, `Platformer.Desktop` excluded by a named filter, because
measuring a thin Raylib adapter that cannot run headless would make the number
describe the size of the renderer rather than the quality of the testing. That
PR is open and unmerged at the time of writing; until it lands, none of it is
true of `main`. It also raises the floor **up**, 70% → 90%, not down:
`Platformer.Core` is fully covered today and the simulation is the one part of
this project that has to be right. Nobody lowers a floor to get a PR through,
and nobody merges past a red check.

## What gets cut first, in order

Approved by the Product Owner during planning, with one reversal recorded below.

1. **#10 — camera deadzone. CUT, mid-sprint, deliberately.** p2, explicitly the
   stretch goal, nothing depends on it, and third in a chain behind the most
   contended path: #10 needs #7 needs a player entity from #3.

   The cut is only safe if the level fits one screen, so that assumption is now a
   **constraint on #8**: at `TileSize` 16, the 320×180 virtual resolution is
   **20 × 11 tiles**, and the test level is authored to fit it. If #8 cannot
   exercise flat ground, a raised platform, a wall and a pit within that, the cut
   reopens — Dev B raises it immediately rather than quietly authoring a level
   that needs a camera we are not building.

   #10 stays in the milestone as visible, uncommitted stretch rather than moving
   to the backlog: a cut item that vanishes from the board is indistinguishable
   from one that was never planned. It is available only if #7 and #8 are both
   merged and no critical-path PR is waiting on a review.
2. **#9 — trimmed.** Drop the characterisation table, the maximum-jump-distance
   measurement and `docs/movement-feel.md`. Keep the end-to-end scenario: spawn,
   run, jump onto the raised platform, land on it. That one test demonstrates the
   sprint goal end to end and is worth keeping long after the characterisation
   numbers stop being interesting.
3. **#9 — in its entirety.** #9 protects future changes from silently altering
   how the game feels. In Sprint 1 there is no past worth protecting yet, so it
   ranks below every issue that establishes behaviour for the first time.

**Reversal — #6 is not to be split.** I proposed shipping coyote time and
deferring jump buffering as the third cut. The Product Owner rejected it, using
this document's own risk analysis as the argument: the critical path is already
six deep with four consecutive links on Dev A, and splitting #6 adds a seventh
link to precisely that bottleneck. The two mechanics also share state and share
tests, so the split is artificial — it would cost throughput rather than buy it.
That reasoning is correct and I withdraw the proposal.

**Consequence — #6 has grown, and that is deliberate.** Three adversarial tests
moved out of #9 and into #6 as acceptance criteria: no second jump in one fall
via coyote time, no buffered jump firing twice, no free coyote jump after an
intentional jump. They prove #6 works rather than guarding it against future
change, so they must not be able to fall off the sprint when #9 is cut. #6 is
now a slightly larger issue on the critical path; the safety it buys is worth
more than the hour it costs, because a double-jump exploit makes the game
trivially breakable.

Never cut: #1, #3, #4, #5, #6, #8. Those six *are* the goal sentence — remove any
one and the sprint has not delivered. #7 is technically not in the goal sentence,
but without it nobody can see the game, #4 and #5 both instruct the dev to tune
by running it, and the stakeholder cannot accept a sprint they cannot watch.
Treat #7 as uncuttable in practice.

## Tuning is provisional until the game is playable

There is a defect in the ordering above and it is mine. #4 and #5 both instruct
the dev to *tune by running the game, then write the tests to lock in what you
tuned to*. But #7 — the renderer — needs a player entity from #3, so it lands
alongside #4 at the earliest. **Dev A therefore tunes blind, against harness
numbers rather than against feel**, which is backwards from what the issues ask
for.

Not papered over. The ruling instead:

> **#4's and #5's tuning numbers are provisional until someone has played the
> game.** The characterisation assertions that freeze them belong after a re-tune
> pass, not before.

Locking in numbers nobody ever felt would build a regression suite that protects
a mistake — worse than no suite, because it makes the mistake expensive to
correct later.

**QA owns the re-tune gate.** Once #7 merges, QA plays the game and validates #4's
and #5's numbers by feel before #9 (if it survives) freezes them. This needs no
new issue and it addresses the largest quality risk in the sprint.

**This is why #7 must merge before #6**, and the reason is not scheduling
convenience: **#6 is the only issue in this sprint whose entire justification is
subjective.** Coyote time and jump buffering are forgiveness mechanics that
players never notice and always feel. Nobody can validate 0.1s of coyote time
from a test log — it either feels forgiving or it does not, and #7 is the last
chance to find out before #6 locks the feel in.

### Can #7 still land before #6? Honestly: not guaranteed, so stop depending on it

The ordering argument above is sound and I am not retracting it. What I will not
do is promise the ordering holds, because nothing currently enforces it.

The dependency moved. #7 needs the player entity and its previous-state
snapshot, and those are born in **#4**, not #3 — so #7 cannot start in earnest
until #4 merges, one merge later than this plan originally assumed. From there
Dev A runs #5 then #6 while Dev B runs #7 alone. #7 has the duration of two
issues to land one, which is why it is *plausible*. But #7 is also the sprint's
first real rendering work — virtual resolution, integer scaling, letterboxing,
`Alpha` interpolation, window resize — and first-of-its-kind work is exactly the
kind that runs long. If it does, the choices are all bad: stall the critical path
waiting for it, or merge #6 with coyote timings nobody has felt.

Two mitigations, because a schedule I cannot guarantee should not be load-bearing:

1. **Dev B starts the two-thirds of #7 that is already unblocked.** Virtual
   resolution, integer scaling, letterboxing, resize handling and drawing the
   tile grid need only #1, which merged long ago. Only the player rectangle and
   the interpolation need #4. Building those now converts #7 from a
   two-issue-long race into a short finish, and materially raises the odds it
   lands before #6.
2. **The provisional-tuning ruling extends to #6.** Coyote and buffer windows are
   provisional until played, exactly like #4's and #5's numbers, and QA's re-tune
   gate covers all three. If #7 slips past #6, #6 merges with provisional windows
   and is re-tuned once the game is visible — a follow-up tuning PR, which is far
   cheaper than stalling the critical path or shipping unfelt feel.

Mitigation 2 is the one that matters, because it removes the dependency on the
ordering rather than betting on it. **The residual risk is real and I am not
going to dress it up**: if #7 slips *and* the re-tune pass gets squeezed at the
end of the sprint, we ship forgiveness windows nobody has felt, in a sprint whose
goal says "movement that already feels responsive". The re-tune gate is therefore
not optional polish — it is the acceptance step for the goal's last clause, and
if it is at risk that goes to the Product Owner rather than being absorbed
quietly.

### The interpolation seam

#7's AC requires interpolating between simulation states via the clock's `Alpha`,
which requires the *simulation* to retain a previous state. That is Dev A's lane,
but the criterion sits on Dev B's issue, and originally neither #3 nor #7 said
who built it. Left alone it would have surfaced as Dev B blocked mid-#7 on a
change only Dev A can make, at Wave 2, with Dev A on the critical path.

It is now an acceptance criterion on **#4**, not #3. #3 is a pure static collider
— `static CollisionResult TileCollider.Move(...)`, deliberately stateless for
determinism — with no entity to snapshot; #4 is where the player entity is born.
The case that bites is written into the AC: **teleports and spawns must set
previous equal to current**, or a respawning player smears across the screen from
wherever it died.

## Board mechanics for this sprint

- Status lives in labels: `status:ready` → `status:in-progress` when a branch is
  pushed → `status:in-review` when the PR opens → `status:done` on merge.
- At sprint start: #2 is `status:in-progress` and #1 went straight to
  `status:in-review` — Dev B opened its PR within minutes of kickoff. #4–#10
  stay `status:ready`. One status label per issue; I clean up duplicates.
- **#3 carries `blocked` until #1 merges, and it stays visible.** It is the one
  genuinely blocked item in the sprint, and a blocked issue that looks unblocked
  is worse than one that looks bad. It clears the moment #1 lands.
- `main` is protected by a ruleset: squash merges only, both `build & test` and
  `reviewer sign-off` required, review threads must be resolved. `reviewer
  sign-off` stays red until QA applies `reviewed:approved`. That red check on
  your own open PR is the process working, not a failure.
- I hold merge authority. Nobody merges their own PR, nobody uses `--admin`,
  nobody applies `reviewed:approved` to their own work.

## Decisions taken by the Product Owner

1. **#9 depends on #8 — confirmed.** The end-to-end criterion names "the real
   test level", which does not exist until #8 ships. The dependency is now
   written onto #9's body. The Wave 1 scheduling of #8 stands.
2. **Cut order — approved with one reversal.** Cuts 1 and 2 stand as written.
   Cut 3 was rejected: #6 is not split, and #9 in its entirety becomes the third
   cut instead. #9's adversarial tests moved into #6 first, so the cut is safe to
   make. Both issue bodies are amended; the reasoning is in the section above.

Nothing further is open with the Product Owner. If the sprint reaches the point
where cut 3 is live, it is a standup announcement rather than a fresh decision.
