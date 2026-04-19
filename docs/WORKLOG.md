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
- **Current focus:** campaign-layer moldiness UX polish and remaining edge cases. The main moldiness reward flow, campaign summary surfaces, and reward-card presentation are now in place, and the remaining work is around pending-state clarity, save/resume verification, and further reward-panel polish.
- **How to update this section:** whenever we pivot, replace this with the current active thread in one or two lines

## Current Plan

1. Finish campaign pending-state clarity, especially around resume/new-campaign surfaces when moldiness reward resolution is waiting.
2. Verify save/resume behavior across pending moldiness rewards, pending adaptation drafts, and pending defeat carryover selection.
3. Continue polishing the moldiness reward panel, especially selection clarity, reward-type readability, and compact context messaging.
4. Smoke-test the combined campaign progression flow, then tune reward pacing, unlock pacing, UI clarity, and the initial moldiness reward catalog.
5. Expand reward content and categories once the adaptation-first loop and permanent-upgrade presentation feel solid.

## Pending Tasks

1. Verify campaign save/resume behavior when moldiness progress, moldiness draft state, adaptation draft state, or defeat carryover selection is pending.
2. Smoke-test a full campaign flow covering: no moldiness event, threshold crossed, moldiness draft resolution, normal adaptation reward, defeat carryover selection, chained reward states, and resume after each pending state.
3. Add or polish campaign pending-state hints where needed, especially if unresolved moldiness rewards should be surfaced more clearly on resume/new-campaign screens.
4. Further polish the moldiness reward panel UX, including selection highlighting, card readability, and concise context summary at the top of the reward screen.
5. Tune moldiness reward pacing, threshold pacing, and moldiness draft pool composition after the end-to-end flow is playable.
6. Decide whether some moldiness rewards should apply immediately to the current run, future runs only, or both.
7. Expand permanent campaign upgrade presentation beyond the current text summary if needed, for example with richer icon display on campaign screens.
8. Explore longer-term expansion to additional unlock categories such as mycovariants and mutation-related rewards once the adaptation-first loop is solid.

## Current Handoff

- `WORKLOG.md` is intentionally present-tense only. Historical task logs and stale tuning notes have been removed.
- Moldiness was chosen as the campaign meta-currency name.
- Moldiness progression foundation is committed in `90b81d3` and the first unlock-plumbing prototype is committed in `32b2b44`.
- Current moldiness prototype in repo:
  - level clears award persistent moldiness using rewards `1,1,2,2,3,3,4,4,5,5,6,6,7,7,8`
  - threshold tiers are `6,9,12,15,18,21,24,27,30,34`, then continue with `+4` growth beyond the table
  - overflow carries over and multiple unlock thresholds can trigger from one award
  - moldiness rewards now block the normal adaptation draft until resolved instead of auto-applying silently
  - defeat carryover selection now blocks defeat reset when the player has carryover capacity
  - pending-threshold reward generation now correctly considers newly triggered unlock levels, so level-appropriate rewards appear immediately on threshold reach
- Completed moldiness UI/presentation work now in repo:
  - moldiness reward cards render visibly and can be selected on the endgame panel (`fcad81d`)
  - campaign menu moldiness summary card exists as the primary persistent home for moldiness state outside gameplay (`d71f2c3`)
  - end-of-run moldiness summaries exist on campaign end panels (`619305e`)
  - reward generation bug for pending thresholds is fixed (`cc56710`)
  - moldiness reward cards now support icon/category presentation, and permanent campaign upgrades have distinct tags and campaign-menu visibility (`2dbb202`)
  - follow-up remote changes after that expanded moldiness-related campaign/start-screen UI further; inspect current files before assuming older layout details
- Recovered design intent from the earlier moldiness transcript:
  - moldiness should be a single meta-progression currency first, with more milestone-based systems added later if needed
  - reward gain should come from campaign progress, especially cleared campaign levels, and scale with progression depth
  - threshold crossings should surface multiple unlock choices, not a single forced result
  - current preference is that moldiness unlock resolution happens before the normal post-victory adaptation choice
  - some unlocks may eventually affect the current run immediately rather than only future runs
- Design correction from Jake:
  - ordinary existing adaptations should not be treated as automatic permanent moldiness unlocks
  - moldiness should instead drive a separate moldiness draft that can unlock explicit locked content and repeatable meta rewards
  - `MoldinessUnlockLevel` controls what can appear in the moldiness draft, not what automatically appears in the normal adaptation draft
  - the system should be extensible enough to support Adaptations first, then later Mycovariants, Mutations, or other reward types
  - moldiness progression should not be shown during live gameplay; it should live on campaign-layer surfaces instead
- MVP content direction:
  - start with a hybrid model
  - moldiness draft offers should include both locked-content unlock rewards and repeatable universal meta rewards
  - first repeatable universal reward should permanently increase failed-run adaptation carryover capacity by +1 per draft
  - first locked-content rewards include at least `Spore Salvo`, `Hyphal Bridge`, `Vesicle Burst`, and `Hyphal Priming` as level-1 adaptation unlock rewards
- Longer-term unlock categories discussed in the recovered transcript and follow-up clarification:
  - additional draftable adaptations
  - additional draftable mycovariants
  - mutation-related unlocks
  - failed-run carryover adaptation systems
- UI direction discussed and currently adopted:
  - moldiness should feel organic, atmospheric, scientific, fungal, and slightly quirky
  - a toast-corruption / colonized-tiles mini-toast is the primary moldiness visualization
  - exact numbers support the visualization rather than replace it
  - the visualization belongs in campaign menus, win/loss result panels, and moldiness reward context, not in the live match HUD
  - permanent campaign upgrades should keep the same overall card footprint as other moldiness rewards while using distinct iconography, accent styling, and category labels rather than different geometry
- Immediate next implementation target: verify pending-state behavior end to end, then continue pending-state clarity and reward-panel polish before broader content expansion. Latest polish fixes addressed moldiness-threshold reward-card layout, defeat carryover selection clarity/selection gating, defeat-reset carryover restoration, and campaign-menu moldiness summary loading/threshold-driven toast visualization.
