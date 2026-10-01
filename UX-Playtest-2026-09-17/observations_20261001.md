# Campaign UX playtest — 2026-10-01

## Session

- Played the Windows player build, **Beta 1.37.0**, full-screen at **3440 × 1440** on the large display.
- Resumed an existing campaign from the Stage 1 cleared screen, selected **Hyphal Economy** as the carry-over adaptation, and played **Stage 2** from Round 1 through its Round 17 victory. This was a two-player match. The human finished **1st, 58 living cells to 48**; the result awarded +1 Moldiness, bringing progress to **6/12 toward Level 4**.
- Used the normal game UI throughout. The existing profile had already seen some tutorials, so absence of a coachmark here is **not** evidence that its first-time behavior is fixed. This run did not include a campaign loss, a new-campaign setup, or a second mycovariant draft.
- This is a retest record. [observations.md](observations.md) remains the canonical remediation log and retains its implementation statuses.

## Highest-priority findings

### R01 — The HUD advertises a draft after the match's known end (old 023/024, P1)

The Round 15 draft did occur in this match, unlike the previous short match that ended before its first draft. Immediately afterward, the sidebar advertised **`Mycovariant Draft: Round 20 (in 5)`** while also showing **`Endgame in 3 rounds`**. In Round 17 it still said **`Round 20 (in 3)`** under **`Final round!`**. The match ended after Round 17. Once the countdown starts, suppress or reschedule any draft the game knows cannot happen. This is the clearest remaining trust issue in the match HUD.

### R02 — Hover inspection still blocks one-time targeting (old 018, P1)

In the Round 15 draft I chose **Jetting Mycelium III**. After choosing a source cell, the banner correctly said to aim and left-click to confirm; the reticle made targeting mode visible. The cell inspector opened over the board east of my source (about x1810–2253 at this resolution). My first click in that direction, around x1939, landed on the inspector and did nothing. I had to click beyond it, around x2333, to finish the action. Keep nearby cell inspection available if desired, but make its panel nonblocking during targeting or place it where it cannot cover valid aiming space. The purple eligible-cell fill also remains visually dominant, though less jarring than the original magenta placeholder.

### R03 — Mutation-tree layout jumps during navigation (old 004/006, P2/P3)

Opening and closing the tree repeatedly exposed a briefly shifted, partly blank frame with a dark band and board content peeking through. The settled tree also moved its detail pane from one side to the other based on the selected node, shifting the navigation grid and search position. Purchase labels (`BUY`, `UPGRADE`, `ACTIVATE`) and the separate **`Bank Points & End Turn`** action are much clearer now, but this remaining motion makes a large tree harder to scan.

### R04 — Results panel still exposes gameplay UI (old 024, P2)

The **Stage 2 Cleared** panel left a narrow strip of the right-side gameplay HUD visible. It also had a large unused lower region beneath two score rows. The results themselves are clearer: the human row now has a **YOU** badge and accent, the win is named, and the Moldiness gain is quantified. The progress card says **`6/12 to Level 4`**, but does not explicitly connect that to the next reward or say that six more are needed.

### R05 — The final spending cue ignores the final round (old 019, P2)

Round 17 prominently said **`Final round!`**, but the left action still said **`Spend 10 Points!`** with no final-action context. A short sublabel could connect that spend to the last growth/decay cycle and living-cell rank.

### R06 — Carried power remains icon-only at stage start (old 023, P2)

The Stage 1 carry-over picker now shows three named cards with their full effects and an intelligible selected state. After I selected Hyphal Economy, Stage 2 began with three adaptation icons in the left rail, but no brief named summary of the inherited build. I had to infer which effects were active from prior memory or hover exploration.

## New observations

- **N01 — Development control appears in the campaign handoff (P3).** The **Adaptation Secured** screen displayed **`Development Testing: Off`** near **Continue Campaign**. In a player build this reads like an internal setting interrupting a reward/continue moment. Hide it from normal campaign flow or explain its player-facing purpose.
- **N02 — A results tooltip initially covered table headings (P3).** The pointer remained where I had clicked a final mutation. When the result appeared, a tooltip explaining living-cell ranking opened across the score table's header row and hid several column names until the pointer moved. Delay that tooltip on screen transition or position it below the headers.

## Recheck of the 2026-09-17 observations

| ID | This run | Evidence / remaining work |
| --- | --- | --- |
| 001 | Previously rejected | Campaign tooltip still repeats nearby explanatory copy; the accepted treatment is unchanged. |
| 002 | Partly addressed | **Stage 2** and next-reward level wording were clear. Reward icons on the campaign hub remain unlabeled, as intentionally excluded from the earlier fix. |
| 003 | Still relevant | The opening HUD still presents percentage tiles, symbols, adaptations, timeline, scoreboard, and two logs at once, without a compact goal/sequence statement. Icon-only symbols/adaptations remain. Two-player Stage 2 is less crowded than the original five-player start. |
| 004 | Partly addressed | Buyable cards have action labels and a hand cursor; banking and pace controls are clear. The dependency grid, changing detail-pane side, and faint search/pin affordances still slow scanning. |
| 005 | Partly addressed / not fully retested | Pace is clearly labeled at the top of the right sidebar, and the ambiguous skip-music icon is absent from the gameplay HUD. I did not open the pause-menu music tooltip to verify track titles or its placement. |
| 006 | Partly addressed | Pace's location and label are clear. Detail-pane side changes and the partly blank transition remain. Coachmark timing was not retested on this previously used profile. |
| 007 | Not assessed | No clear Apical Yield bonus-banner event was captured for this observation. |
| 008 | Partly addressed | The resting inspector was dark and legible, and showed **`Human (You)`** for a player-owned cell. Rapid hover-to-hover retargeting could not be judged from this interaction. Its panel still interferes with targeting (018). |
| 009 | Addressed in the controls | **`Bank Points & End Turn`** states the consequence; a banking coachmark appeared later. No hidden-cost ambiguity arose while using it. |
| 010 | Not assessed | The Scout Your Rivals first-time coachmark was not visible on this profile, so its dragging and benefit copy were not tested. |
| 011 | Partly addressed | Round 15 offered three readable cards with explicit **Choose** strips. The cards still have large empty lower areas; the draft-intro overlap was not testable on this profile. The earlier truncation claim remains rejected. |
| 012 | Rejection supported | Playing a standalone player build showed no Unity editor chrome. |
| 013 | Addressed | Mutation details use **`Leads to`**. |
| 014 | Addressed in the draft seen | Cards showed the **Passive/One-time** distinction and a consistent comparison layout. This run did not reach a second draft. |
| 015 | Addressed in the draft seen | Round 15 card copy was readable at a consistent size, with explicit **Choose** affordances. The later multi-player pick-order case was not retested. |
| 016 | Not assessed | Endgame coachmark behavior was not visible on the existing profile. The compact countdown itself was prominent. |
| 017 | Not assessed | No second draft occurred; held coachmarks and tier-comparison highlighting could not be retested. |
| 018 | Still needs work | Reticle and aiming instructions help, but inspector collision is reproducible with Jetting Mycelium III; the eligible-cell overlay is still heavy. Tutorial stacking was not retested. |
| 019 | Still needs work | **Final round!** appears beside the unchanged **Spend 10 Points!** cue. |
| 020 | Partly addressed / loss not retested | Victory results now give the human row a **YOU** badge and accent. The loss explanation, carry-over button wording, and loss-specific Moldiness copy need a loss run. |
| 021 | Addressed in this run | Adaptation choices are named, show their full effects, and do not rely on overlapping hover tooltips. The confirm button's selected/unselected states were intelligible. |
| 022 | Not assessed | Resuming a cleared stage proceeded to the next stage rather than opening a new-campaign setup. Carry-over preview and difficulty explanation there remain untested. |
| 023 | Still needs work | No named inherited-build summary at Stage 2 opening. The advertised draft timing also remains absolute even when the endgame countdown makes the next draft impossible. |
| 024 | Still needs work | The first draft happened this time, but the **Round 20** draft promise persisted through a Round 17 finish. Results still expose the HUD edge and leave the next Moldiness reward relation implicit. |

## Practical next pass

Fix the impossible draft promise and targeting obstruction first. A fresh-profile run should then check coachmark layering/retirement (010, 011, 016–018), and a deliberate loss run should verify the remaining 020 changes. The next-campaign setup (022) needs a separate transition through a loss/carry-over path.
