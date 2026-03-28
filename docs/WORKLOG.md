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
- Added three early-campaign training molds to the roster and first five campaign presets: `TST_Training_ResilientMycelium`, `TST_Training_Overextender`, and `TST_Training_ToxicTurtle`. Current early campaign pass uses them heavily in Campaign0-4 to improve readability and onboarding.
- Early/medium curated pool placement is still being filled in around that training shell. Latest evidence: `CMP_Reclaim_Scavenger_Easy`, `CMP_Surge_Pulsar_Easy`, `CMP_Defense_ResilientShell_Easy`, and `CMP_Defense_ReclaimShell_Easy` are safe to keep in the `Campaign1` duel pool, while `CMP_Growth_Pressure_Medium` is too sharp for `Campaign5` and `CMP_Bloom_FortifyMimic_Medium` overshot `Campaign8` badly enough that `Campaign8` was restored to `CMP_Bloom_BeaconRegression_Medium`. `CMP_Attrition_ToxicTurtle_Easy` failed screening and should stay out of campaign pools for now.
- Converted Campaign0 and Campaign1 presets to use the new AI-pool path for Unity validation. Both still spawn 1 AI, but now draw deterministically from a small curated pool instead of a fixed lineup.
- Reclassified `CMP_Surge_Pulsar` upward to Medium. Also reclassified the Bloom/fortify/mimic mold upward as `CMP_Bloom_FortifyMimic_Medium` and introduced it into the later-medium campaign band (Campaign8+), instead of trying to force it into the easy bucket.
- TODO: Investigate Mimetic Resilience presentation bug in Unity. Current core tests suggest the 20% eligibility gate is functioning, so focus next on the visual pipeline: `GrowthSource.MimeticResilience` is currently routed through the generic post-growth resistance pulse path instead of a reclaim-specific deferred-overlay sequence. Desired order: old cell fades out, new cell fades in, field pulse plays, then shield appears on the new resistant cell at the end.

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
- **Next steps:** If continuing the same thread, rerun `Campaign4` at a larger sample and then decide whether `Campaign3` also wants one more easing pass or is acceptable as the first real nutrient-era pressure step.

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

### 2026-03-27 (Campaign1 pool integrity pass)
- **Focus:** Audit the full `Campaign1` pooled duel roster instead of trusting a single resolved seed.
- **Changed:** Fixed a parser bug in `scripts/run_campaign_balance.py` where pooled board-preset parsing could accidentally treat the Unity YAML header (`--- !u!114 ...`) as a fake AI pool entry. Also replaced stale `Growth/Resilience` in `FungusToast.Unity/Assets/Configs/Board Presets/15x15 1 AI.asset` with the actual campaign-safe alias `CMP_TierCap_GrowthResilience_Easy`.
- **Learned:** `Campaign1` had two quiet integrity problems: one in the harness and one in the asset. The old pool entry `Growth/Resilience` is not present in the `Campaign` strategy set at all, so the authored duel pool was carrying a dead ID; the intended curated alias is softer and valid.
- **Evidence:** Direct 50-game no-nutrient duel screens with safe proxy `TST_CampaignPlayer_SafeBaseline`: `AI12` -> `68.0% (34/50), avg living 90.2, avg dead 28.0`; `CMP_TierCap_GrowthResilience_Easy` -> `74.0% (37/50), avg living 100.0, avg dead 30.8`.
- **Open questions:** The rest of the `Campaign1` pool now looks sane on paper, but a true multi-seed per-member authored-level sweep would still be the cleanest way to measure how often each pool member appears and whether any one candidate produces outlier variance.
- **Next steps:** If continuing this lane, run a scripted multi-seed `Campaign1` harness sweep now that pooled parsing is fixed, then decide whether `AI12` should stay in the duel pool or give way to another softer curated easy mold.

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

## Session Checkpoint Template

### YYYY-MM-DD
- **Focus:**
- **Changed:**
- **Learned:**
- **Open questions:**
- **Next steps:**
