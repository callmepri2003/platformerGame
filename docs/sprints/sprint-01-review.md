# Sprint 1 — review

**Milestone:** Sprint 1 · **Goal:** *a player can run, jump and land on solid
ground in a hand-authored test level, with movement that already feels
responsive.*

This document is written to be read by someone deciding whether to trust the
team, not to make the team look good. Where those two conflict, the first wins.

## The headline

**Everything that shipped is invisible, and that is the finding of the sprint.**

By the time the sprint had five issues merged, `dotnet run` still drew the words
"walking skeleton" on a blank screen. There is a collision resolver verified
across 28,714 configurations, a level format, a deterministic simulation harness
and a player entity with tuned acceleration — and for most of the sprint not one
pixel of it had been seen by a human being.

The goal's last clause is *"movement that already feels responsive."* Nothing in
that list can demonstrate it.

## The goal, clause by clause

Partial credit, made visible rather than rounded up.

| Clause | Status | Evidence |
| --- | --- | --- |
| a player can **run** | **yes** | #4 — acceleration, friction, stronger turnaround, air control, all tuned against stated targets |
| can **jump** | **no** | #5 in progress at time of writing |
| and **land on solid ground** | **yes** | #3 — verified across 28,714 configurations; no body could be made to sink through a floor |
| in a **hand-authored test level** | **yes** | #8 — ASCII maps with actionable parse errors, spawn resting flush on ground |
| with movement that **already feels responsive** | **not verified** | requires #7 and the #29 play-test; see below |

The last row is the honest one. It is not "no" because the movement may well feel
fine; it is **not verified**, because responsiveness is not a property any test
in this repository can assert, and until the renderer merged nobody could
observe it.

## What shipped

**Merged: #1, #2, #3, #4, #8, #23** — tile grid, test harness, collision,
movement, level loader, and a convention fix that arrived mid-sprint. **#7** landed its first slice; the remainder is behind a wiring
commit at time of writing. **#5** in progress. **#6** not started. **#9** ranks
last in the cut order. **#10** cut.

Process work also merged: the sprint plan, the coverage gate rescoped and raised
70% → 90%, the reviewer/merger authority table, and a p1 fix to the sign-off gate.

## Finding 1 — the plan was optimised against the wrong constraint

This is the most useful thing the sprint produced, and it is a criticism of the
plan I wrote.

The entire schedule was built around **merge latency**: a six-deep critical path,
four consecutive links owned by one agent, every hop gated by review. Waves,
merge order and reviewer assignments all followed from it.

The measured result, nine merged pull requests:

| | Open → merge |
| --- | --- |
| Median | **13.1 min** |
| Fastest | 7.7 min (#26, the largest and most consequential PR of the sprint) |
| Slowest | 21.5 min |

**The bottleneck never materialised.** The honest reading is not that it was
successfully mitigated — it is that *it was never the risk*. Distinguishing "I
mitigated a risk" from "the risk was never the risk" is the difference between a
retrospective and a progress report, and the temptation to claim the first is
exactly why this is finding 1.

The real constraint was **visibility**. #7 needed only #1, which merged in the
sprint's first hour — and it sat two-thirds unblocked for hours while the whole
team optimised the critical path around it. It was scheduled late for one reason:
**nothing depended on it.**

> **Carried into Sprint 2: schedule the first visible thing early, even when
> nothing depends on it.**
>
> Dependencies tell you what is *possible* to build in what order. They say
> nothing about what you need to *see* in order to know whether you are building
> the right thing.

A collider no human has watched move is not obviously worth more than a rectangle
on screen that someone can push around.

## Finding 2 — the bugs lived between correct issues

Five items entered scope mid-sprint. **Two of them were found by reviewers
reasoning about composition, not by any issue's author:**

- **The interpolation seam.** #7 must interpolate between simulation states via
  `Alpha`, which requires the simulation to retain a previous state. Simulation
  is one lane, the criterion sat on a presentation issue, and neither #3 nor #7
  said who built it. Unaddressed, it surfaces as Dev B blocked mid-#7 on a change
  only Dev A can make.
- **The death plane.** Driving the merged collider through the shipped level ends
  at `X=-468, Y=1944, still falling`. Out-of-bounds is `Empty` — correct, and the
  reason #3 needs no border special case. **#3 was correct. #8 was correct. The
  player still fell out of the world permanently, with no way back.**

Neither was catchable by any acceptance criterion on either issue, because
neither issue was wrong.

> **Vindicated: the non-blocking consumer-feedback duty.** The primary consumer of
> an API comments on the PR that introduces it; the comment never gates the merge.
> It cost the critical path nothing and caught two defects that no acceptance
> criterion would have.

## Finding 3 — the lane separation held, and there is a measurement

Lanes were introduced in `docs/TEAM.md` to keep merge conflicts rare. They did
something more valuable than that, and the evidence is specific.

Before the renderer merged, Dev B merged #26 into its branch and ran the whole
stack against the shipped level. Holding right from spawn, the player came to
rest at:

```
X=180  RIGHT=192  VX=0  GROUNDED=True
```

Exactly flush against the plateau face — no overlap, no gap, still grounded.
Framebuffer measurement put the drawn box at world x 160..172 mid-run and
180..192 at rest, with **y never changing** across either sample.

That is a body crossing **seven tile seams at 120 u/s without catching on one**,
in the hand-authored level rather than a purpose-built test grid — the exact
failure mode #3 was written to avoid, demonstrated by composition rather than by
the collider's own tests.

What makes it the sprint's strongest structural evidence is who produced it:
**three agents, in three separate clones, across three independently reviewed
pull requests** — Dev A's collider, Dev B's level format, Dev B's renderer —
converging to the unit on a claim the level was deliberately authored to make
testable. Nobody coordinated the number. It fell out of three correct pieces
meeting.

Lanes are usually justified as conflict avoidance, which is a weak claim: it says
the parts did not collide. This says the parts *agreed*. That is the argument for
keeping the separation in Sprint 2, and it is worth more than the absence of merge
conflicts.

Set against Finding 2, the picture is honest in both directions: **composition is
where this architecture is strongest and where its only two real bugs came from.**
The interfaces agreed to the unit; the questions nobody's issue owned — what
happens below the level, who keeps the previous position — did not answer
themselves.

## Finding 4 — four stalls, one shape

The team stalled four times on the same class of defect: **an obligation that was
real but unnamed, invisible until the exact pull request that needed it.**

| Gap | Surfaced as |
| --- | --- |
| Who reviews QA's own PRs | #14 could not be signed off — QA cannot approve itself |
| Who merges the Scrum Master's PRs | Two approved, green PRs stuck with nobody able to merge |
| Who is the off-critical-path dev | Reassignment rule referenced a role nobody had named |
| Who merges when the merger is absent | Escape valve covered reviewers only |

Each cost a round trip to discover and resolve. `docs/TEAM.md` now names a
reviewer and a merger for every role, including the roles that do the naming, and
the underlying rule is written down: **every artefact is reviewed and merged by
someone who did not produce it, and the process must name that someone for every
role.**

A fifth of the same shape was caught before it stalled anything: the re-tune
play-test existed only as a paragraph in the sprint plan, owned by nobody, with
no acceptance criteria — described in the same breath as "the largest quality
risk in the sprint". It is now #29, p0.

## Finding 5 — a gate that reported success while blocking nothing

Sign-off here is a label, not a GitHub review, so branch protection's
`dismiss_stale_reviews_on_push` never applied to it. A commit pushed after
sign-off inherited a green `reviewer sign-off` check covering code no reviewer had
read.

It was hit twice and disclosed both times, which works exactly until someone
forgets. On #19 — the sprint's most important PR at the time — sign-off landed 64
seconds after the final commit. Correct **by sequence, not by construction**.

Fixed in #18. A second bypass found in review (`reopened`, because GitHub does not
fire `synchronize` for a closed PR) was classified as a note rather than a
blocker and **overruled**: a gate shipped with a known bypass is the thing being
fixed. The severity of a hole in a gate is not the probability someone walks
through it — the gate reports success either way.

## Cuts, with dates

**#10 camera deadzone — cut at 11:02, 33 minutes into the sprint.** p2, the
documented stretch goal, nothing depended on it. Decided early and deliberately
rather than discovered at review.

The second-order move mattered more than the cut: cutting the camera is only safe
if the level fits one screen, so that assumption became a **hard constraint on
#8** — 20 × 11 tiles at `TileSize` 16 — with instruction to reopen the cut rather
than quietly author a level needing a camera nobody was building. That is the
difference between cutting scope and deferring a problem.

#10 stayed in the milestone as visible uncommitted stretch. A cut item that
vanishes from the board is indistinguishable from one that was never planned.

**Ratified cut order** if the sprint runs short: #10, then #9 trimmed, then #9
whole, then **#6 whole** — and never the play-test. Cutting a forgiveness
mechanic nobody has felt costs less than shipping one nobody has felt.

## Scope that entered mid-sprint

| Item | Origin |
| --- | --- |
| #18 sign-off survives a push (p1) | Scrum Master hitting it twice |
| #23 Y-up stand-in in a Y-down project (p1) | Dev B reviewing #14 |
| #29 the acceptance play-test (p0) | QA challenging the sprint plan |
| #4's interpolation snapshot | Scrum Master; placed by Product Owner |
| #4's death plane and respawn | Dev A reviewing #8 |

Four of five came from **review**, not from planning. One (#16) was declined:
scope pulled into a sprint to occupy an idle agent is how sprints lose their goal.

## What did not go well, beyond the plan

- **Board hygiene errors were mine.** #19 — the sprint's most important PR —
  briefly carried another issue's labels because a PR number was written into a
  chained command instead of read back from the command that created it.
- **Three instructions on one small issue.** #23 was stood down, restarted, then
  superseded, because instructions were issued without first reading what was
  actually in anyone's branch. #23 had no assignee for its entire life, which is
  what let two agents work it simultaneously. Duplicate-ownership risk is resolved
  by recording a decision and a named owner **on the issue**, not by telling one
  party to stop and assuming silence means agreement.
- **The same misread, twice.** Having written up "optimise for critical-path
  review latency, not total work", the very next duplicate-work call optimised for
  avoiding duplicate work again — and had to be overridden.

## Carried into Sprint 2

1. **Schedule the first visible thing early, even when nothing depends on it.**
2. **Name a reviewer and a merger for every artefact, including new roles**, in
   the same change that creates the obligation.
3. **Keep consumer feedback non-blocking and expected.** It caught the two
   defects that lived between correct issues.
4. **Put judgement work on the board.** If it cannot be written as an acceptance
   criterion, it still needs an owner and an issue, or it silently does not
   happen.
5. `docs/TEAM.md`'s lane table is narrower than reality —
   `src/Platformer.Core/Presentation/**` now exists and #10's camera will land
   there. Widen it during planning rather than mid-sprint.
