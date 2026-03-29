# Fungus-Toast Worklog

This file is the lightweight continuity anchor for OpenClaw-assisted Fungus-Toast work.

## Modus Operandi

Use the following minimal workflow to preserve working memory across sessions:

1. **Session anchor**
   - At the start of a Fungus-Toast session, explicitly say to work in:
     - `/home/jakejgordon/Fungus-Toast`
   - Also name the current thread of work when helpful:
     - balance sims
     - AI tuning
     - campaign difficulty
     - starting positions
     - feature work

2. **Daily continuity**
   - Use OpenClaw daily memory (`memory/YYYY-MM-DD.md`) for short session summaries:
     - what changed today
     - what was learned
     - what is next

3. **Durable project record**
   - Keep durable project context in the repo, especially in this file and other relevant docs.
   - Detailed simulation findings should be recorded in project docs with concrete evidence:
     - commands
     - seeds
     - board sizes
     - player counts
     - slot policy
     - results
     - conclusions

4. **End-of-session checkpoint**
   - End substantial work sessions with a compact checkpoint containing:
     - what changed
     - what we learned
     - open questions
     - next steps

## Current Notes

- This file should stay concise and current.
- Detailed balance or simulation findings should live in the most relevant project docs, while this file tracks the active thread and the next handoff.
- Active Fungus-Toast implementation backlog now lives in `/home/jakejgordon/.openclaw/workspace/FUNGUS_TOAST_TASKS.md` rather than repo `/docs`.
- Campaign/UI idea: enemy molds should eventually have fun thematic names shown in UI instead of generic labels like `Player 2`. Prefer names that hint at strategy theme and ideally feel slightly scientific / mold-like (e.g. names that imply rapid growth, toxin pressure, reclamation, etc.). This should support campaign readability by helping players understand what kind of opponent they are facing.
- Fresh Campaign9 safe-proxy follow-up says the current authored medium stack was slightly too sharp. On the exact `Campaign9` board (`110x110`, 6 AI, nutrients on), a 20-game seed-`20260328` screen put the current authored lineup at `0.0% (0/20), avg living 844.6, avg dead 785.4`; three conservative variants were then screened and only one survived: replacing `CMP_Bloom_AnabolicRegression_Medium` with `CMP_Bloom_BeaconRegression_Medium` improved the proxy to `10.0% (2/20), avg living 854.8, avg dead 842.7`, while swapping `AI11` to `CMP_Control_AnabolicRebirth_Medium` only reached `5.0% (1/20)` and swapping `CMP_Bloom_FortifyMimic_Medium` to `CMP_Bloom_BeaconRegression_Medium` also only reached `5.0% (1/20)`. A 50-game confirmation with seed `20260329` kept the same modest edge: current authored lineup `6.0% (3/50), avg living 803.1, avg dead 694.6`; Beacon-over-Anabolic swap `8.0% (4/50), avg living 790.1, avg dead 782.2`. `Campaign9` now uses the Beacon variant twice and drops the Anabolic regression mold from this board.
- Added three early-campaign training molds to the roster and first five campaign presets: `TST_Training_ResilientMycelium`, `TST_Training_Overextender`, and `TST_Training_ToxicTurtle`. Current early campaign pass uses them heavily in Campaign0-4 to improve readability and onboarding.
- Early/medium curated pool placement is still being filled in around that training shell. Latest evidence: `CMP_Reclaim_Scavenger_Easy`, `CMP_Surge_Pulsar_Easy`, `CMP_Defense_ResilientShell_Easy`, and `CMP_Defense_ReclaimShell_Easy` are safe to keep in the `Campaign1` duel pool, while `CMP_Growth_Pressure_Medium` is too sharp for `Campaign5` and `CMP_Bloom_FortifyMimic_Medium` overshot `Campaign8` badly enough that `Campaign8` was restored to `CMP_Bloom_BeaconRegression_Medium`. `CMP_Attrition_ToxicTurtle_Easy` failed screening and should stay out of campaign pools for now.
- Fresh `Campaign8` authored-board follow-up (100x100, 6 AI, nutrients on, safe proxy) now has both the short screen and the confirmation. The 20-game seed-`20260336` screen said the current baseline still lands at `10.0% (2/20), avg living 704.0, avg dead 578.8`, but the real bully is `CMP_Economy_KillReclaim_Medium` (`80%` of wins in that sample). Two conservative swaps were clearly worse: replacing `CMP_Bloom_BeaconRegression_Medium` with `CMP_Bloom_AnabolicRegression_Medium` kept the proxy at `10.0%` but made the new bloom mold the dominant winner (`35%`), and replacing `CMP_Surge_GrowthTempo_Medium` with `CMP_Surge_BeaconTempo_Medium` collapsed the proxy to `0.0% (0/20)`. The one apparent survivor was swapping `CMP_Economy_KillReclaim_Medium` to `CMP_Economy_TempoReclaim_Medium` in that same 20-game screen (`10.0% (2/20)`, avg living `689.8`, avg dead `624.3`, more even win spread), but the 50-game confirmation with the same authored board and seed family killed that idea: current baseline `8.0% (4/50), avg living 706.4, avg dead 578.2`; TempoReclaim swap `6.0% (3/50), avg living 690.4, avg dead 617.8`. Keep the authored `CMP_Economy_KillReclaim_Medium` lineup for now; do not promote the TempoReclaim swap.
- Fresh follow-up screens also argue against pulling later-medium molds earlier just for roster variety: on the authored `Campaign6` board (`75x75`, 4 AI), swapping in `CMP_Bloom_AnabolicRegression_Medium` or `CMP_Surge_GrowthTempo_Medium` dropped the safe proxy from the current `25.0%` band to `20.0%` and `15.0%` respectively in 20-game screens, and a 50-game confirmation on `Campaign7` showed that replacing `CMP_TierCap_GrowthResilience_Easy` with `CMP_Bloom_AnabolicRegression_Medium` merely held the proxy at the same `6.0%` win rate as the current authored lineup. For now, keep those sharper mediums at `Campaign8+` rather than using them to bulk up `Campaign6-7`.
- Converted Campaign0 and Campaign1 presets to use the new AI-pool path for Unity validation. Both still spawn 1 AI, but now draw deterministically from a small curated pool instead of a fixed lineup.
- Converted Campaign2, Campaign3, and Campaign4 from fixed lineups to curated early-tier pools as well. Current authored pools keep those levels in the training/easy band but allow real run-to-run variety instead of always serving the exact same opponent quartet.
- Campaign3 volatility reduction pass: replaced `TST_Training_Overextender` and `TST_Training_ToxicTurtle` in the `30x30 3 AI` pool with `AI12`, leaving a tighter 5-entry bridge pool (`TST_Training_ResilientMycelium`, `AI12`, `CMP_TierCap_GrowthResilience_Easy`, `CMP_Reclaim_Scavenger_Easy`, `CMP_Defense_ReclaimShell_Easy`). Multi-seed safe-proxy validation improved the observed pooled range from roughly `25-70%` in short screens to `35-60%` in 20-game runs and `38-62%` in 50-game confirmations.
- Reclassified `CMP_Surge_Pulsar` upward to Medium. Also reclassified the Bloom/fortify/mimic mold upward as `CMP_Bloom_FortifyMimic_Medium` and introduced it into the later-medium campaign band (Campaign8+), instead of trying to force it into the easy bucket.
- TODO: Investigate Mimetic Resilience presentation bug in Unity. Current core tests suggest the 20% eligibility gate is functioning, so focus next on the visual pipeline: `GrowthSource.MimeticResilience` is currently routed through the generic post-growth resistance pulse path instead of a reclaim-specific deferred-overlay sequence. Desired order: old cell fades out, new cell fades in, field pulse plays, then shield appears on the new resistant cell at the end.
- Temporary audio backlog tracker: `docs/sound-effects-checklist-temp-2026-03-29.md`. Remove it once the sound pass is complete.

## Active Thread

- **Repo:** `/home/jakejgordon/Fungus-Toast`
- **Current focus:** campaign difficulty tuning with safe-proxy validation and curated campaign AI roster promotion
- **How to update this section:** whenever we pivot, replace this with the current active thread in one or two lines

## Current Plan

1. ✅ Add simulation flags to disable nutrient patches and mycovariant drafting for fairness tests.
2. ✅ Replace temporary source edits with reusable starting-position override parameters/plumbing.
3. ✅ Keep progress tracked here so future sessions can resume cleanly.
4. ✅ Commit the new simulation/override plumbing.
5. ✅ Run a clean 6-player candidate bakeoff using explicit starting-position overrides.
6. ✅ Promote the best validated 6-player layout into the precomputed fast-path once it actually holds up.
7. ✅ Start clean 5-player validation and identify the fairest starting layout.
8. ✅ Commit the selected 5-player layout and record the validation results.
9. ✅ Document the 4-player symmetry assumption and start 3-player validation.
10. ✅ Commit the selected 3-player layout and record the validation results.
11. ⏳ Add/document the symmetric 2-player fast-path and confirm `1-8` startup placement behavior.

## Current Handoff

### 2026-03-27 (campaign AI pool support)
- **Focus:** Start backlog implementation for campaign board presets that can draw opponents from a strategy pool without tying pool size to active AI count.
- **Changed:** Added optional `BoardPreset.aiStrategyPool` + `pooledAiPlayerCount`, preserved existing fixed `aiPlayers` behavior, and wired campaign state/controller/runtime so pooled levels resolve a stable per-run/per-level lineup that persists across resume instead of reshuffling.
- **Learned:** The risky part was not the pool itself but reproducibility: game startup inferred player count from `aiPlayers.Count`, so pooled support also needed explicit active-AI count plumbing plus persisted resolved names in `CampaignState`.
- **Open questions:** Unity-side validation still needs a real editor pass on pooled level assets.
- **Next steps:** Use the new pooled authoring path in early campaign presets and validate exact resolved lineups with the safe-proxy harness.

### 2026-03-27 (early-campaign reclaim/surge pass)
- **Focus:** Add Jake's new easy campaign mold and continue early-campaign tuning with the safe proxy.
- **Changed:** Added `CMP_Reclaim_InfiltrationSurge_Easy` to the campaign roster (`Necrohyphal Infiltration` maxed first, then `Hyphal Surge`), extended `scripts/run_campaign_balance.py` so pooled campaign presets resolve deterministic AI lineups from `aiStrategyPool`, added the new mold to the `Campaign1` pool, and replaced `AI12` with it in `Campaign4` after validation.
- **Learned:** The new mold is a good *easy/training-adjacent* fit on small boards but too soft for `Campaign2`-style 20x20 multi-AI screens. The bigger problem was `Campaign4`: current authored `AI12` made the level a cliff (`5%` proxy win rate), while the new reclaim/surge mold softens it back into a plausible early-campaign band.
- **Evidence:** 20-game safe-proxy screens (`TST_CampaignPlayer_SafeBaseline`) with seed `20260327` for authored levels produced: `Campaign0 90.0% (18/20), avg living 50.8, avg dead 14.9`; `Campaign1 60.0% (12/20), avg living 69.4, avg dead 20.9` (pool resolved to `TST_Training_Overextender` for this seed); `Campaign2 65.0% (13/20), avg living 105.9, avg dead 32.5`; `Campaign3 55.0% (11/20), avg living 166.9, avg dead 113.9`; `Campaign4` old lineup with `AI12` `5.0% (1/20), avg living 172.2, avg dead 133.8`. Direct candidate screens: `15x15` duel vs `CMP_Reclaim_InfiltrationSurge_Easy` -> `85.0% (17/20), avg living 103.2, avg dead 29.2`; `20x20` with `TST_Training_ResilientMycelium + CMP_Reclaim_InfiltrationSurge_Easy` -> `95.0% (19/20), avg living 146.2, avg dead 39.5`; `Campaign4` swap replacing `AI12` with `CMP_Reclaim_InfiltrationSurge_Easy` -> `35.0% (7/20), avg living 201.7, avg dead 150.6`.
- **Open questions:** `Campaign0-3` now look broadly sane in short screens, but `Campaign4` should be rerun at 50-100 games to make sure the new softer lineup stays in-band instead of just being high-variance.
- **Next steps:** Continue converting Campaign2-4 to curated pools, then rerun Campaign0-4 on the safe-proxy harness to see whether the opening arc smooths out once those levels stop using fixed lineups.

### 2026-03-27 (Campaign2-4 poolification pass)
- **Focus:** Replace the remaining fixed early-campaign lineups (Campaign2-4) with real curated pools and rerun the first five campaign levels against the safe baseline.
- **Changed:** Switched `Campaign2` (`20x20 2 AI`) to a 4-entry easy/training pool with 2 resolved opponents, `Campaign3` (`30x30 3 AI`) to a 6-entry training/easy pool with 3 resolved opponents, and `Campaign4` (`40x40 4 AI`) to a 7-entry training/easy pool with 4 resolved opponents. This keeps the current early-tier design language (training molds plus weak curated `CMP_*` easy molds) while finally using the pooled runtime path on all of Campaign0-4.
- **Learned:** The pool conversion clearly removes the old Campaign4 brick-wall behavior, but a single-seed pooled sweep is not automatically monotone. With seed `20260327`, Campaign1 still resolved `AI12` and stayed relatively sharp (`74%` proxy win rate), while Campaign2 resolved a softer pair and also landed at `74%`. The good news is the old fixed-lineup cliff is gone: Campaign3 settled at `44%` and Campaign4 at `28%` instead of the earlier `55%` -> `5%` collapse.
- **Evidence:** `python3 scripts/run_campaign_balance.py --games 50 --seed 20260327 --level 0..4` equivalent per-level runs. Resolved lineups/results: `Campaign0` -> `AI6`: `48.0% (24/50), avg living 37.7, avg dead 14.1`; `Campaign1` -> `AI12`: `74.0% (37/50), avg living 91.7, avg dead 30.8`; `Campaign2` -> `TST_Training_Overextender + CMP_TierCap_GrowthResilience_Easy`: `74.0% (37/50), avg living 101.4, avg dead 30.5`; `Campaign3` -> `CMP_Reclaim_Scavenger_Easy + CMP_TierCap_GrowthResilience_Easy + TST_Training_ResilientMycelium`: `44.0% (22/50), avg living 199.7, avg dead 88.8`; `Campaign4` -> `TST_Training_Overextender + CMP_TierCap_GrowthResilience_Easy + CMP_Reclaim_Scavenger_Easy + TST_Training_ToxicTurtle`: `28.0% (14/50), avg living 189.5, avg dead 175.9`.
- **Open questions:** Should Campaign1 lose `AI12` now that the rest of the opening band is pooled and softer, or should we treat Campaign1/Campaign2 as acceptable variance and evaluate across multiple seeds before more asset churn?
- **Next steps:** Run a short multi-seed Campaign0-4 pooled sweep so we can judge the pool *distribution*, not just the `20260327` resolution, then decide whether to remove/replace `AI12` from the early duel band or tighten the Campaign2 pool upward slightly.

### 2026-03-27 (campaign roster expansion pass)
- **Focus:** Add the next safe weak/easy molds for Campaign1 variety and conservative named medium aliases for the first medium band.
- **Changed:** Added `CMP_Defense_ResilientShell_Easy`, `CMP_Defense_ReclaimShell_Easy`, `CMP_Surge_BeaconTempo_Medium`, `CMP_Control_AnabolicRebirth_Medium`, and `CMP_Surge_GrowthTempo_Medium` to `AIRoster` with campaign metadata. Expanded the `Campaign1` pool to include the two new weak defense molds. Replaced early-medium preset references to legacy `AI7`/`AI8`/`AI11` with their curated `CMP_*` aliases in Campaign5/6/7/8-authored boards where behavior was intended to stay conservative. Rejected a stronger draft (`CMP_Attrition_ToxicTurtle_Easy`) after proxy screening instead of folding it into the pool.
- **Learned:** The safe additions for `Campaign1` are the two defense shells, not the toxin turtle. `CMP_Defense_ResilientShell_Easy` is extremely soft in a 15x15 duel, while `CMP_Defense_ReclaimShell_Easy` is still weaker than the proxy but offers more bite. The named medium aliases behave conservatively in authored Campaign5/6 screens and stay clearly below the safe proxy even by Campaign8, which fits the "first-medium with a little spice" guidance.
- **Evidence:** Build smoke: `dotnet build FungusToast.Core/FungusToast.Core.csproj`. Direct 20-game 15x15 duel screens with seed `20260327`: `CMP_Defense_ResilientShell_Easy` -> `0.0% (0/20), avg living 65.7, avg dead 5.1`; `CMP_Defense_ReclaimShell_Easy` -> `35.0% (7/20), avg living 78.1, avg dead 12.2`; rejected `CMP_Attrition_ToxicTurtle_Easy` -> `65.0% (13/20), avg living 64.7, avg dead 16.4`. Authored 20-game campaign screens with seed `20260327`: `Campaign5` (`CMP_Surge_BeaconTempo_Medium`, `CMP_Control_AnabolicRebirth_Medium`, `AI9`, `CMP_Economy_KillReclaim_Medium`, `CMP_TierCap_GrowthResilience_Easy`) -> proxy `30.0% (6/20), avg living 272.9, avg dead 181.4`; `Campaign6` (`CMP_Surge_BeaconTempo_Medium`, `CMP_Control_AnabolicRebirth_Medium`, `AI9`, `CMP_Bloom_CreepingNecro_Medium`) -> `25.0% (5/20), avg living 665.6, avg dead 447.9`; `Campaign8` (`CMP_Control_AnabolicRebirth_Medium`, `AI9`, `CMP_Surge_GrowthTempo_Medium`, `CMP_Economy_KillReclaim_Medium`, `CMP_Bloom_CreepingNecro_Medium`, `CMP_Bloom_BeaconRegression_Medium`) -> `10.0% (2/20), avg living 694.8, avg dead 584.5`.
- **Open questions:** Campaign7 still uses the same conservative alias pair but was not rerun in this pass; if we want full authored-level evidence for every renamed board, run Campaign7 next. `CMP_Defense_ReclaimShell_Easy` is acceptable as weak variety now, but if Campaign1 needs even more softness later it is the first easy slot I'd revisit.
- **Next steps:** Keep expanding named curated aliases upward from Campaign9 while preserving Jake's guardrail that `CMP_Bloom_FortifyMimic_Medium` does not enter until Campaign9+.

### 2026-03-21
- **Focus:** Reconstruct lost OpenClaw context and preserve current Fungus-Toast thread.
- **Changed:** Established the new memory workflow; created this worklog; recovered recent project context from saved memory and recent commits.
- **Learned:** Recent work was centered on simulation-driven balance and starting-position fairness. `StartingSporeUtility` was upgraded from a fixed-radius circle to a board-aware search, then exposed analysis data for per-slot fairness inspection.
- **Recovered context:**
  - true fixed-slot simulation runs now preserve fixed starting positions
  - board-aware starting layout search landed in `StartingSporeUtility`
  - `GetStartingPositionAnalysis(width, height, playerCount)` now exposes coordinates, uncontested counts, early uncontested counts, tie counts, and favor rank
  - 160x160 / 8-player fairness improved substantially versus the old layout
- **Saved reference result:** Current chosen 160x160 / 8-player layout is P0 `(142,106)`, P1 `(106,142)`, P2 `(54,142)`, P3 `(18,106)`, P4 `(18,54)`, P5 `(54,18)`, P6 `(106,18)`, P7 `(142,54)`.
- **Open questions:** Should next tuning focus on additional board sizes / aspect ratios, alternate player counts, or empirical simulation validation beyond geometric scoring?
- **Next steps:** Resume work in `/home/jakejgordon/Fungus-Toast` and continue from the starting-position/balance-testing thread.

### 2026-03-21 (later)
- **Focus:** Clean 6-player fairness validation and simulation-tooling improvements.
- **Changed:** Added simulation flags to disable nutrient patches and mycovariants. Replaced temporary candidate-override source edits with reusable starting-position override parameters so candidate layouts can be tested via CLI instead of editing `StartingSporeUtility`.
- **Learned:** Candidate 6 looked promising in short samples but failed to hold up in a clean 100-game run with nutrients and mycovariants disabled.
- **Evidence:** Clean 100-game result for candidate 6 on `160x160` with same AI in all slots and fixed positions was `17,12,19,16,18,18` (range `7`).
- **Open questions:** Which of the saved 6-player candidates remains strongest under the clean no-nutrients/no-mycovariants setup?
- **Next steps:** Commit the new plumbing, then run a clean staged bakeoff across the top saved 6-player candidates using `--starting-positions`.

## Session Checkpoint Template

### YYYY-MM-DD
- **Focus:**
- **Changed:**
- **Learned:**
- **Open questions:**
- **Next steps:**
.
- **Learned:** Of the tested clean 100-game validations, candidate 6 performed best; candidate 15 and 18 both regressed badly despite looking promising in short screens.
- **Evidence:** Candidate 6 clean 100-game result on `160x160` with same AI in all slots and fixed positions was `17,12,19,16,18,18` (range `7`). Candidate 15 was `22,12,19,13,20,14` (range `10`). Candidate 18 was `21,16,23,11,16,13` (range `12`).
- **Open questions:** 5-player validation has not been documented yet.
- **Next steps:** Start clean 5-player validation, record the current auto-selected 5-player baseline, and compare candidates if needed.

### 2026-03-21 (5-player decision)
- **Focus:** Finish 5-player starting-position selection.
- **Changed:** Accepted candidate 11 as the selected 5-player layout and added it to the precomputed fast-path.
- **Learned:** The previous 5-player auto-selected layout was too biased. A tighter 5-player ring performed substantially better under clean validation.
- **Evidence:** Previous auto-selected layout produced `22,15,27,14,22` (range `13`). Selected candidate 11 produced `22,18,22,16,22` (range `6`) on `160x160` with same AI in all slots, fixed positions, nutrients off, and mycovariants off.
- **Selected layout:** P0 `(114,104)`, P1 `(67,120)`, P2 `(38,80)`, P3 `(67,40)`, P4 `(114,56)`.
- **Open questions:** Remaining player counts below 5 have not been validated in this pass.
- **Next steps:** Commit/push the selected 5-player layout and continue only if additional player-count tuning is desired.

### 2026-03-27 (campaign modernization pass)
- **Focus:** Promote curated modern molds into the actual campaign roster and retune Campaign5-10 using the safe-proxy harness.
- **Changed:** Added `CMP_*` campaign-safe aliases to `AIRoster`, tagged them with campaign metadata, and rewired Campaign5-10 board presets to use the curated roster instead of legacy placeholder `AI4/AI5` or overly spiky elite inserts.
- **Learned:** The real blocker was roster mismatch, not just preset composition: the balance harness only validated the `Campaign` set, so medium modern molds had to be promoted there first. Hard molds introduced before Campaign10 were consistently too sharp for the safe baseline in 20-game screens.
- **Evidence:** See `/home/jakejgordon/Fungus-Toast/docs/campaign-modernization-2026-03-27.md` for commands, lineups, and per-level proxy results. Final proxy results were Campaign5 `25% (5/20)`, Campaign6 `25% (5/20)`, Campaign7 `5% (1/20)`, Campaign8 `10% (2/20)`, Campaign9 `5% (1/20)`, Campaign10 `10% (2/20)`.
- **Open questions:** Campaign7 and Campaign9 still look a little harsher than the surrounding curve; decide whether to soften them further or accept them as current checkpoints pending broader campaign pass data.
- **Next steps:** If continuing this thread, rerun Campaign7-10 at a larger sample size (50-100 games) after deciding whether to swap one medium mold in 7/9 for a weaker training/easy curve shaper.

### 2026-03-27 (late-campaign continuation / blocker)
- **Focus:** Continue safe-proxy campaign tuning upward into Campaign11-Campaign14.
- **Changed:** Screened the currently authored late levels plus two softened curated candidate sets for Campaign12-14. Did not rewrite late board presets because the harness evidence did not justify picking a new lineup yet.
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

## Session Checkpoint Template

### YYYY-MM-DD
- **Focus:**
- **Changed:**
- **Learned:**
- **Open questions:**
- **Next steps:**
