# Fungus-Toast Worklog

This file is the lightweight continuity anchor for OpenClaw-assisted Fungus-Toast work.

## Modus Operandi

Use the following minimal workflow to preserve working memory across sessions:

1. **Session anchor**
   - At the start of a Fungus-Toast session, explicitly say to work in:
     - `/home/jakejgordon/Fungus-Toast`
   - Also name the current thread of work when helpful.

2. **Canonical task list**
   - Use this file as the canonical in-repo task list and handoff for active Fungus-Toast work.
   - At the start of a new task, check `Pending Tasks` here and ask whether one of those should be completed first.

3. **Durable project record**
   - Keep durable project context in the repo only when it is still actively useful.
   - Do not keep transient simulation findings or stale task history here once they stop helping current decisions.

4. **End-of-session checkpoint**
   - Put end-of-session checkpoints in OpenClaw daily memory (`memory/YYYY-MM-DD.md`).
   - Only keep partial-progress resume notes here when they are needed to continue an unfinished task.

## Current Notes

- This file should stay concise and current.
- Detailed balance or simulation findings should live in the most relevant project docs, while this file tracks the active thread, pending tasks, and the next handoff.
- `docs/WORKLOG.md` is the canonical in-repo task and handoff file for active Fungus-Toast work.
- When starting a new Fungus-Toast task, first check the pending tasks here and ask whether one of them should be completed before starting something new.

## Active Thread

- **Repo:** `/home/jakejgordon/Fungus-Toast`
- **Current focus:** campaign moldiness meta-progression. The current backend prototype exists, but the design needs to shift from unlocking ordinary existing adaptations toward explicit locked content gated by moldiness level.
- **How to update this section:** whenever we pivot, replace this with the current active thread in one or two lines

## Current Plan

1. Define the moldiness progression model around explicit locked content and `MoldinessUnlockLevel`, not ordinary existing adaptations.
2. Replace the temporary auto-apply behavior with a real post-victory moldiness unlock choice UI.
3. Decide how moldiness unlock flow should chain with normal adaptation rewards, then build and smoke-test the combined campaign progression flow.
4. Tune reward pacing, unlock pacing, and the initial locked-content catalog once the full loop is playable.

## Pending Tasks

1. Introduce a first-class concept for locked content that can be gated by moldiness progression.
2. Add a `MoldinessUnlockLevel` concept so content can become eligible only after reaching the required moldiness tier.
3. Decide which systems should participate in moldiness gating first: Adaptations only, or Adaptations plus Mutations/Mycovariants.
4. Replace the temporary auto-apply moldiness behavior with a real player-facing moldiness unlock choice step.
5. Add player-facing UI copy for moldiness progress, threshold crossings, unlock level, and newly available content.
6. Verify campaign save/resume behavior when moldiness progress or unlock choice state is pending.
7. Smoke-test a full campaign victory flow covering: no moldiness event, unlock threshold crossed, normal adaptation reward, and chained reward states.
8. Create the first real locked-content catalog instead of reusing ordinary existing adaptations as pseudo-unlocks.
9. Tune moldiness reward pacing and threshold pacing after the end-to-end flow is playable.

## Current Handoff

- `WORKLOG.md` is intentionally present-tense only. Historical task logs and stale tuning notes have been removed.
- Moldiness progression foundation is committed in `90b81d3` and the first unlock-plumbing prototype is committed in `32b2b44`.
- Current moldiness prototype in repo:
  - level clears award persistent moldiness using rewards `1,1,2,2,3,3,4,4,5,5,6,6,7,7,8`
  - threshold tiers are `6,9,12,15,18,21,24,27,30,34`, then continue with `+4` growth beyond the table
  - overflow carries over and multiple unlock thresholds can trigger from one award
  - a temporary unlock/offer plumbing path exists in campaign state/controller code
  - the endgame service currently contains a temporary shortcut that auto-applies a pending moldiness unlock before the normal adaptation draft
- Design correction from Jake:
  - ordinary existing adaptations should not be treated as permanent moldiness unlocks
  - moldiness should instead gate explicit locked content
  - content availability should be controlled by `MoldinessUnlockLevel`
  - the system should be extensible enough to gate Adaptations, and later possibly Mutations, Mycovariants, or other reward types
- Immediate next implementation target: revise the prototype to match the corrected design, then build the real moldiness unlock UI flow on top of that model.
- **Learned:** Campaign11 is at least survivable in short samples, but Campaign12-14 remain a real blocker. Even after removing duplicate elite anchors and substituting mostly medium/hard curated molds that obey the earliest-introduction guidance, the safe proxy still went `0/10` on every tested Campaign12-14 lineup.
- **Evidence:** Baseline short screen results were Campaign11 `20.0% (2/10), avg living 1101.4, avg dead 1039.1`; Campaign12 `0.0% (0/10), avg living 1196.8, avg dead 982.2`; Campaign13 `0.0% (0/10), avg living 1191.4, avg dead 1213.4`; Campaign14 `0.0% (0/10), avg living 1368.1, avg dead 1227.9`. Additional candidate screens are recorded in `docs/campaign-modernization-2026-03-27.md`, and every Campaign12-14 candidate there also stayed at `0/10`.
- **Open questions:** Is the blocker the proxy itself, fixed-slot late-board variance, or an implicit target-band mismatch where `0-5%` is actually acceptable for current late campaign?
- **Next steps:** Before more preset churn, either (1) review/upgrade the safe player-proxy for late-game testing, or (2) explicitly decide acceptable proxy bands for Campaign12-14 and then rerun the best late candidates at a larger sample size.

### 2026-03-27 (early/medium pool continuation)
- **Focus:** Keep filling the curated early/medium pools with the campaign-safe molds that existed in `AIRoster` but were still underused in authored presets.
- **Changed:** Added `CMP_Reclaim_Scavenger_Easy` and `CMP_Surge_Pulsar_Easy` to the `Campaign1` pool asset, screened both in direct 15x15 safe-proxy duels, screened `CMP_Growth_Pressure_Medium` as a `Campaign5` replacement candidate, screened `CMP_Bloom_FortifyMimic_Medium` on the `Campaign8` board, and then changed `Campaign8` back from `CMP_Bloom_FortifyMimic_Medium` to `CMP_Bloom_BeaconRegression_Medium` after confirmation.
- **Learned:** `CMP_Reclaim_Scavenger_Easy` is a legitimate early easy mold and `CMP_Surge_Pulsar_Easy` is softer but still safe for the `Campaign1` duel pool. `CMP_Growth_Pressure_Medium` is too sharp for `Campaign5`, and `CMP_Bloom_FortifyMimic_Medium` is not ready for first-introduction duty at `Campaign8` because it collapses the safe proxy there.
- **Evidence:** Direct 15x15 duel vs `CMP_Reclaim_Scavenger_Easy` -> `55.0% (11/20), avg living 93.3, avg dead 44.6`; direct 15x15 duel vs `CMP_Surge_Pulsar_Easy` -> `70.0% (14/20), avg living 91.3, avg dead 24.8`; `Campaign5` swap using `CMP_Growth_Pressure_Medium` -> `0.0% (0/20), avg living 225.7, avg dead 188.9`; authored `Campaign8` with `CMP_Bloom_FortifyMimic_Medium` -> `0.0% (0/20), avg living 727.5, avg dead 606.1`; restored `Campaign8` with `CMP_Bloom_BeaconRegression_Medium` -> `10.0% (2/20), avg living 696.7, avg dead 583.2`; current authored `Campaign9` with `CMP_Bloom_FortifyMimic_Medium` remained survivable at `10.0% (2/20), avg living 837.5, avg dead 762.4` so no `Campaign9` rewrite was taken in this pass.
- **Open questions:** `Campaign1` now has a healthier easy pool on paper, but the safe harness only resolves one pool member per seed; if we want stronger confidence in the whole duel pool, the next step is a small multi-seed sweep rather than one more one-off duel. On the medium side, `CMP_Bloom_FortifyMimic_Medium` probably belongs later than `Campaign8`, and `CMP_Growth_Pressure_Medium` likely needs a Campaign9+ or hard-preview slot instead of first-medium duty.
- **Next steps:** If continuing, do a short multi-seed `Campaign1` pool sweep, then characterize whether `CMP_Bloom_FortifyMimic_Medium` or `CMP_Growth_Pressure_Medium` fit better at `Campaign9`, `Campaign10`, or only as later hard-transition molds.
- **Campaign10 conservative retune (2026-03-28):** Re-screened the exact authored `120x120 6 AI` board with the safe proxy and found the then-current authored lineup (`AI11`, `CMP_Economy_KillReclaim_Medium`, `CMP_Bloom_CreepingNecro_Medium`, `CMP_Bloom_BeaconRegression_Medium`, `CMP_Control_AnabolicFirst_Hard`, `CMP_Economy_LateSpike_Hard`) had fallen to `0.0%` (`0/20`) on seed `20260328` and only `4.0%` (`2/50`) on confirmation seed `20260329`. A conservative single-slot softening — `CMP_Control_AnabolicFirst_Hard -> CMP_Control_AnabolicRebirth_Medium` — improved the screen to `20.0%` (`4/20`) and held at `16.0%` (`8/50`) on confirmation, so `Campaign10` now uses `AI11`, `CMP_Economy_KillReclaim_Medium`, `CMP_Bloom_CreepingNecro_Medium`, `CMP_Bloom_BeaconRegression_Medium`, `CMP_Control_AnabolicRebirth_Medium`, `CMP_Economy_LateSpike_Hard`.
- **Learned:** For the `120x120` seven-player `Campaign10` field, two hard curated anchors were no longer buying a better curve than the late `Campaign9` medium stack; keeping `CMP_Economy_LateSpike_Hard` as the lone hard spike preserved the level's identity while avoiding the `CMP_Control_AnabolicFirst_Hard` pile-on.
- **Campaign11 authored-board check (2026-03-28):** Ran the next exact level after the Campaign10 retune without changing assets first. On the authored `130x130 7 AI` board (`AI1`, `AI2`, `AI3`, `AI7`, `AI8`, `AI9`, `AI11`) with the safe proxy and seed `20260328`, the 20-game run landed at `10.0% (2/20)`, avg living `1032.2`, avg dead `978.0`. Practical read: this is harsher than the newly softened `Campaign10` (`16.0%` on its 50-game confirmation) but still plausible as the first real elite-preview field, so no conservative swap was taken yet.
- **Campaign12 authored-board check (2026-03-28):** Rechecked the exact authored `140x140 7 AI` board (`AI1`, `AI2`, `AI3`, `AI10`, `AI7`, `AI8`, `AI9`) before trying any swap. The 20-game seed-`20260328` screen landed at `10.0% (2/20)`, avg living `1121.8`, avg dead `1063.0`, and the 50-game confirmation with seed `20260329` held exactly at `10.0% (5/50)`, avg living `1156.6`, avg dead `1104.9`. Practical read: the earlier `0/10` short screen was too pessimistic; Campaign12 is currently survivable enough to hold as-authored, so no conservative swap was taken.
- **Campaign13 authored-board check (2026-03-28):** Rechecked the exact authored `150x150 7 AI` board (`AI1`, `AI2`, `AI3`, `AI10`, `AI1`, `AI7`, `AI8`) before trying any swap. The 20-game seed-`20260328` screen landed at `15.0% (3/20)`, avg living `1300.2`, avg dead `1177.4`, and the 50-game confirmation with seed `20260329` held at `10.0% (5/50)`, avg living `1269.4`, avg dead `1240.6`. Practical read: the earlier `0/10` short screen was again too pessimistic; despite the duplicate `AI1` anchor, authored Campaign13 is currently survivable enough to hold as-is, so no conservative swap was taken.
- **Campaign14 authored-board check (2026-03-28):** Rechecked the exact authored `160x160 7 AI` board (`AI1`, `AI2`, `AI3`, `AI10`, `AI1`, `AI2`, `AI7`) with the safe proxy before touching assets. The 20-game seed-`20260328` screen landed at `10.0% (2/20)`, avg living `1537.9`, avg dead `1291.9`, but the 50-game confirmation with seed `20260329` dropped to `4.0% (2/50)`, avg living `1440.5`, avg dead `1213.7`, which is back in the practical blocker band.
- **Campaign14 conservative swap probe (2026-03-28):** Tested the least-surprising softening on the same `160x160` board by replacing one duplicate elite anchor with a second medium support: `AI1, AI2, AI3, AI10, AI1, AI8, AI7`. The 20-game seed-`20260328` screen only managed `5.0% (1/20)`, avg living `1439.9`, avg dead `1399.0`, so that duplicate-`AI2` -> `AI8` swap does **not** look like the fix. Practical read: keep authored Campaign14 for the moment, but it is the first late board in this pass that still looks meaningfully suspect and should be the next target for any additional conservative softening.

### 2026-03-27 (Campaign1 pool integrity pass)
- **Focus:** Audit the full `Campaign1` pooled duel roster instead of trusting a single resolved seed.
- **Changed:** Fixed a parser bug in `scripts/run_campaign_balance.py` where pooled board-preset parsing could accidentally treat the Unity YAML header (`--- !u!114 ...`) as a fake AI pool entry. Also replaced stale `Growth/Resilience` in `FungusToast.Unity/Assets/Configs/Board Presets/15x15 1 AI.asset` with the actual campaign-safe alias `CMP_TierCap_GrowthResilience_Easy`.
- **Learned:** `Campaign1` had two quiet integrity problems: one in the harness and one in the asset. The old pool entry `Growth/Resilience` is not present in the `Campaign` strategy set at all, so the authored duel pool was carrying a dead ID; the intended curated alias is softer and valid.
- **Evidence:** Direct 50-game no-nutrient duel screens with safe proxy `TST_CampaignPlayer_SafeBaseline`: `AI12` -> `68.0% (34/50), avg living 90.2, avg dead 28.0`; `CMP_TierCap_GrowthResilience_Easy` -> `74.0% (37/50), avg living 100.0, avg dead 30.8`.
- **Open questions:** The rest of the `Campaign1` pool now looks sane on paper, but a true multi-seed per-member authored-level sweep would still be the cleanest way to measure how often each pool member appears and whether any one candidate produces outlier variance.
- **Next steps:** If continuing this lane, run a scripted multi-seed `Campaign1` harness sweep now that pooled parsing is fixed, then decide whether `AI12` should stay in the duel pool or give way to another softer curated easy mold.

### 2026-03-27 (early/medium bridge follow-up)
- **Focus:** Re-check whether underused medium molds could safely deepen `Campaign6-7` instead of leaving that band thin.
- **Changed:** Ran fresh safe-proxy screens on the authored `Campaign6` and `Campaign7` boards using `CMP_Bloom_AnabolicRegression_Medium` and `CMP_Surge_GrowthTempo_Medium` as possible bridge inserts. No preset changes were taken because the substitutions did not actually improve the curve.
- **Learned:** The current `Campaign6` lineup is still the safer bridge. Replacing `AI9` there with `CMP_Bloom_AnabolicRegression_Medium` or `CMP_Surge_GrowthTempo_Medium` pushed the proxy down from the current `25.0%` result to `20.0%` and `15.0%` in 20-game screens. A one-off 20-game `Campaign7` screen briefly made `CMP_Bloom_AnabolicRegression_Medium` look softer than the current easy-filler version, but a 50-game confirmation erased that signal: both the current authored `Campaign7` lineup and the anabolic-regression swap finished at `6.0%` proxy wins.
- **Evidence:** `Campaign5` current authored lineup recheck -> `30.0% (6/20), avg living 257.1, avg dead 192.6`; `Campaign6` current -> `25.0% (5/20), avg living 657.5, avg dead 431.2`; `Campaign6` with `CMP_Bloom_AnabolicRegression_Medium` -> `20.0% (4/20), avg living 644.8, avg dead 441.2`; `Campaign6` with `CMP_Surge_GrowthTempo_Medium` -> `15.0% (3/20), avg living 632.0, avg dead 472.4`; `Campaign7` current short screen -> `0.0% (0/20), avg living 633.8, avg dead 430.5`; `Campaign7` with `CMP_Bloom_AnabolicRegression_Medium` short screen -> `10.0% (2/20), avg living 597.0, avg dead 425.1`; 50-game confirmations on the exact `Campaign7` board: current authored lineup -> `6.0% (3/50), avg living 669.4, avg dead 434.5`; anabolic-regression swap -> `6.0% (3/50), avg living 598.4, avg dead 437.1`.
- **Open questions:** The real gap is no longer a clearly superior fixed-lineup swap; it is authored *variety*. If we want more depth in this band without making it harsher, the next likely move is to pool `Campaign5-7` from already-screened safe mediums/easy bridges instead of promoting sharper mediums downward.
- **Next steps:** Characterize pool-friendly `Campaign5-7` candidates across multiple seeds, then decide whether those levels should join the pooled path rather than stay as brittle fixed lineups.

### 2026-03-27 (AI4/AI5 bridge audit)
- **Focus:** Audit the last two major roster unknowns (`AI4`, `AI5`) on the actual `Campaign5-7` boards instead of guessing where they belong.
- **Changed:** Ran 20-game safe-proxy authored-board screens on `Campaign5`, `Campaign6`, and `Campaign7`, swapping `AI4` or `AI5` into the current lineups as possible variety inserts. No preset changes were taken.
- **Learned:** `AI4` and `AI5` do **not** solve the `Campaign5-7` variety problem. `AI4` is at best a side-grade on `Campaign5-6` and still leaves `Campaign7` in the same harsh ~`5%` band. `AI5` is worse: it becomes the dominant winner on the `Campaign6` board and is still not a safe `Campaign7` filler.
- **Evidence:** With seed `20260327` and `TST_CampaignPlayer_SafeBaseline` as the proxy: `Campaign5 + AI4` -> `25.0% (5/20), avg living 246.3, avg dead 172.8`; `Campaign5 + AI5` -> `30.0% (6/20), avg living 238.6, avg dead 165.5`; `Campaign6 + AI4` -> `25.0% (5/20), avg living 657.5, avg dead 431.2`; `Campaign6 + AI5` -> `25.0% (5/20), avg living 615.0, avg dead 472.4`; `Campaign7 + AI4` -> `5.0% (1/20), avg living 646.0, avg dead 393.4`; `Campaign7 + AI5` -> `5.0% (1/20), avg living 639.5, avg dead 428.9`.
- **Open questions:** If `Campaign5-7` should gain more lineup variety without getting harder, the next candidates should come from pooled combinations of the already-screened easy/medium molds, not from `AI4/AI5`.
- **Next steps:** Either prototype actual `Campaign5-7` pools from the safe medium/easy bridge roster, or leave those levels fixed for now and move on to defining explicit target success bands so late-campaign tuning has firmer acceptance criteria.

### 2026-03-27 (Campaign5-7 pooled bridge prototype)
- **Focus:** Test whether the next practical move for `Campaign5-7` is simple poolification using already-screened easy/medium molds, instead of swapping in sharper mediums or legacy unknowns.
- **Changed:** Ran 10-game safe-proxy screens on exact authored boards while treating each fixed lineup as a near-pool: add one extra easy bridge mold, then enumerate every omit-one resolved subset. Tested `Campaign5 + CMP_Reclaim_Scavenger_Easy`, `Campaign6 + CMP_TierCap_GrowthResilience_Easy`, and two `Campaign7` variants (`+ CMP_Reclaim_Scavenger_Easy`, `+ CMP_Surge_Pulsar_Easy`). No preset changes were taken.
- **Learned:** Light poolification looks acceptable for `Campaign5-6`, but it does **not** fix `Campaign7`.
  - `Campaign5` pooled with Scavenger stayed in a plausible short-screen band: `10.0-30.0%` proxy wins across the six resolved subsets (average `18.3%`).
  - `Campaign6` pooled with Growth/Resilience also stayed plausible: `20.0-30.0%` across the five resolved subsets (average `24.0%`).
  - `Campaign7` remained harsh no matter which extra easy bridge was added. With Scavenger the seven resolved subsets averaged only `4.3%`; with Pulsar they averaged only `5.7%`, and both pools still produced multiple `0.0% (0/10)` results.
- **Evidence:** Representative survivors: `Campaign5` omitting `CMP_Control_AnabolicRebirth_Medium` -> `30.0% (3/10), avg living 262.1, avg dead 195.8`; `Campaign6` omitting `CMP_Bloom_CreepingNecro_Medium` -> `30.0% (3/10), avg living 663.8, avg dead 505.7`; `Campaign7 + Scavenger` omitting `CMP_Surge_BeaconTempo_Medium` -> `10.0% (1/10), avg living 668.0, avg dead 442.7`; `Campaign7 + Pulsar` omitting `AI9` -> `10.0% (1/10), avg living 651.4, avg dead 458.2`.
- **Open questions:** For `Campaign5-6`, is the small extra variety worth turning those levels into true pools, or is the current fixed authored version already good enough? For `Campaign7`, what structural softening should come first: drop one medium entirely, replace a sharper medium with an easier bridge, or accept the current ~`5%` band as intended?
- **Next steps:** If staying in this lane, do a focused `Campaign7` softening pass rather than more add-one-easy pool experiments. `Campaign5-6` can be revisited later as low-risk poolification candidates.

### 2026-03-21 (4-player assumption / 3-player start)
- **Focus:** Record the 4-player symmetry assumption and move on to 3-player tuning.
- **Changed:** Added a symmetric 4-player fast-path reference for square boards and documented the assumption that 4-player square placement is geometrically even enough to treat as solved unless future evidence says otherwise.
- **Selected 4-player layout:** P0 `(128,128)`, P1 `(32,128)`, P2 `(32,32)`, P3 `(128,32)`.
- **Open questions:** 3-player placement still appears asymmetric under geometry and needs real simulation-driven tuning.
- **Next steps:** Run the first clean 3-player baseline and compare candidate alternatives if the current auto-selected layout is biased.

### 2026-03-21 (3-player decision)
- **Focus:** Finish 3-player starting-position selection.
- **Changed:** Accepted candidate 7 as the selected 3-player layout and added it to the precomputed fast-path.
- **Learned:** The previous 3-player auto-selected layout was clearly biased. A wider 3-player ring performed much better under clean validation.
- **Evidence:** Previous auto-selected layout produced `38,28,34` (range `10`). Selected candidate 7 produced `33,35,32` (range `3`) on `160x160` with same AI in all slots, fixed positions, nutrients off, and mycovariants off.
- **Selected layout:** P0 `(141,80)`, P1 `(50,133)`, P2 `(50,27)`.
- **Open questions:** Player counts below 3 remain untreated, but may be low priority depending on actual game usage.
- **Next steps:** Commit/push the selected 3-player layout and decide whether any smaller player counts need explicit treatment.

### 2026-03-21 (2-player fast-path confirmation)
- **Focus:** Close the loop on startup placement coverage.
- **Changed:** Added a symmetric 2-player precomputed layout and documented the complete fast-path behavior.
- **Selected 2-player layout:** P0 `(128,80)`, P1 `(32,80)` on the `160x160` reference board.
- **Resulting startup behavior:**
  - 1 player uses direct center placement
  - 2-8 players now use precomputed/scaled layouts
  - only other player counts fall back to search
- **Next steps:** No further starting-position search is needed for the common square-board player counts unless future gameplay evidence suggests a new imbalance.

### 2026-03-27 (campaign levels 5-7 retune)
- **Focus:** Continue mid-campaign lineup tuning for Campaign5-Campaign7 using the existing `scripts/run_campaign_balance.py` / safe-player-proxy workflow.
- **Changed:**
  - `Campaign5` (`50x50 5 AI`): replaced `AI6` with `AI4`.
  - `Campaign6` (`75x75 4 AI`): replaced `AI11` with `AI4`.
  - `Campaign7` (`90x90 6 AI`): replaced the back half `AI11, AI8, AI6` with `AI5, AI4, AI12` after longer validation showed the lighter swap was still too soft.
- **Learned:**
  - The documented medium-campaign candidates like `Grow>Kill>Reclaim(Econ)` and `Creeping>Necrosporulation` are not currently available in the simulation's `Campaign` strategy set, so the campaign-balance harness can only validate the legacy `AI1`-`AI13`/training roster unless those newer molds are added to `RawCampaignStrategies`.
  - Leaving `AI6` in Campaign5/7 created misleading results because it is still a training-tier mold, not a stable mid-campaign opponent.
  - `AI13` was too spiky for this band: it reliably crushed Campaign7 validation and turned the level into an early wall rather than a cleaner ramp.
- **Evidence (50-game validation, safe proxy = `TST_CampaignPlayer_SafeBaseline`):**
  - `Campaign5` (`AI7, AI8, AI9, AI4, AI12`): proxy `12.0%` (`6/50`), avg alive `248.5`, avg dead `166.9`.
  - `Campaign6` (`AI7, AI8, AI9, AI4`): proxy `22.0%` (`11/50`), avg alive `648.8`, avg dead `442.4`.
  - `Campaign7` strongest validated legal candidate (`AI7, AI8, AI9, AI5, AI4, AI12`): proxy `8.0%` (`4/50`), avg alive `641.4`, avg dead `409.6`.
- **Open questions:** If Campaign7 should stay closer to a 10-15% proxy band without leaning on legacy `AI12`, the next step is probably to add the newer curated medium molds into `RawCampaignStrategies` so the harness can validate the intended roster, not just the legacy IDs.
- **Next steps:** Re-run the campaign-balance harness after any further strategy-set expansion, especially for Campaign7 where the best currently legal roster is serviceable but still skewed toward `AI12`.

### 2026-03-27 (Campaign5-6 asset poolification)
- **Focus:** Convert the low-risk mid-campaign bridge candidates from prototype pool math into real authored board-preset pools and validate them on the safe proxy.
- **Changed:** `Campaign5` (`50x50 5 AI.asset`) now resolves 5 opponents from a 6-entry curated pool by adding `CMP_Reclaim_Scavenger_Easy`; `Campaign6` (`75x75 4 AI.asset`) now resolves 4 opponents from a 5-entry curated pool by adding `CMP_TierCap_GrowthResilience_Easy`. `Campaign7` was intentionally left fixed.
- **Learned:** The prototype held up on the actual pooled assets. Over two 20-game seeds each, both `Campaign5` and `Campaign6` landed at the same aggregate safe-proxy band: `25.0%` won (`10/40`). Campaign5 stayed flat at `25%` across both seeds, while Campaign6 moved from `20%` to `30%`, which is acceptable short-run spread for now but worth a larger recheck later.
- **Evidence:** `python3 scripts/run_campaign_balance.py --level 5 --games 20 --seed 20260327`; `--level 5 --games 20 --seed 20260328`; `--level 6 --games 20 --seed 20260327`; `--level 6 --games 20 --seed 20260328`. Aggregated safe-proxy results: `Campaign5` -> `25.0% (10/40), avg living 247.9, avg dead 175.3`; `Campaign6` -> `25.0% (10/40), avg living 678.9, avg dead 464.3`.
- **Open questions:** The next pass should probably run 50-100 game confirmations on pooled `Campaign5-6` and then revisit whether `Campaign7` needs an actual softer lineup rather than more pool-only variety.
- **Next steps:** Keep `Campaign7` fixed, treat pooled `Campaign5-6` as the current authored baseline, and expand validation rather than churning those two boards again immediately.

### 2026-04-05
- **Focus:** Close out the early/mid campaign bridge by fixing Campaign4 authoring drift, tuning Campaign5 onto the agreed proxy band, and tightening simulation-reporting discipline.
- **Changed:** Corrected `Campaign4` so the `40x40 4 AI` board now uses the three early training opponents together; retuned `Campaign5` through a mix of board-pool and roster-level changes (`TST_Training_ResilientMycelium` in the board pool instead of `CMP_Surge_BeaconTempo_Medium`, `CMP_TierCap_GrowthResilience_Easy` capped at Tier2, `CMP_Control_AnabolicRebirth_Medium` shifted to `MinorEconomy`, `CMP_Reclaim_Scavenger_Easy` front-loaded with `MycelialBloom` level 5); updated `FungusToast.Core/docs/SIMULATION_HELPER.md` to require artifact-backed balance summaries instead of live-console-only reporting.
- **Learned:** Campaign5 was the real early/mid cliff; once softened, it landed inside the agreed `35-55%` proxy band. Campaign6 should still be treated as a leave-as-is checkpoint for now, and Campaign7 is the next likely difficulty problem if this thread continues.
- **Evidence:** `Campaign4` validation on the corrected three-training-opponent lineup landed at `76.0% (38/50)` in one 50-game screen. Final accepted `Campaign5` artifact-backed result on seed family `20260405/20260410` was `44.0% (22/50)` for `TST_CampaignPlayer_SafeBaseline`, with standard summary fields emitted via `post_simulation_player_summary.csv`. Commit/push: `1584742` (`Tune Campaign4-5 balance and harden sim reporting`).
- **Open questions:** Is `Campaign4`'s current forgiving result acceptable as-is, or should the early-band target table eventually be revisited for levels below Campaign5? For the next tuning pass, does Campaign7 need a structural softening or just a better pool/roster composition pass?
- **Next steps:** Run an artifact-backed `Campaign7` validation pass and judge it against the agreed `15-35%` proxy band.

## Session Checkpoint Template

### YYYY-MM-DD
- **Focus:**
- **Changed:**
- **Learned:**
- **Open questions:**
- **Next steps:**
