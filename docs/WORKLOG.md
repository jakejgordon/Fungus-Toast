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
- **Current focus:** campaign moldiness meta-progression. The current backend prototype exists, and the design direction is now a hybrid model: moldiness drafts unlock future adaptation-draft eligibility for explicit locked content and repeatable meta rewards.
- **How to update this section:** whenever we pivot, replace this with the current active thread in one or two lines

## Current Plan

1. Finalize the hybrid moldiness model: a moldiness reward catalog for threshold drafts plus native locked-content metadata on actual game content.
2. Add the first repeatable universal moldiness reward, which permanently increases failed-run adaptation carryover capacity.
3. Create at least three new locked level-1 adaptations whose unlock rewards can appear in the moldiness draft once moldiness level 1 is reached.
4. Replace the temporary auto-apply behavior with a real post-victory moldiness draft UI that presents multiple options before the normal adaptation reward flow.
5. Build and smoke-test the combined campaign progression flow, then tune reward pacing, unlock pacing, and the initial moldiness reward catalog.

## Pending Tasks

1. Refactor the current moldiness reward definition model into a true reward-card system with support for repeatable universal rewards and locked-content unlock rewards.
2. Add `failedRunAdaptationCarryoverCount` or equivalent persistent state and wire the first repeatable universal reward to increase it by +1 per draft.
3. Add native locked metadata to `AdaptationDefinition`, including `IsLocked` and required moldiness level fields.
4. Create at least three new level-1 locked adaptations and corresponding moldiness reward entries that permanently unlock them for future normal adaptation drafts.
5. Ensure moldiness draft eligibility is based on moldiness level, while normal adaptation draft eligibility depends on whether a locked adaptation has actually been unlocked.
6. Replace the temporary auto-apply moldiness behavior with a real player-facing moldiness draft step that offers multiple choices.
7. Ensure moldiness draft resolution happens before the normal adaptation reward selection, unless a better chained-flow design emerges.
8. Decide whether some moldiness rewards should apply immediately to the current run, future runs only, or both.
9. Add player-facing UI copy for moldiness progress, threshold crossings, unlock level, locked-content rewards, and carryover rewards.
10. Explore visual direction for moldiness presentation, with current inspiration being toast corruption / corruption cells and an organic, atmospheric, scientific, fungal feel.
11. Verify campaign save/resume behavior when moldiness progress or moldiness draft state is pending.
12. Smoke-test a full campaign victory flow covering: no moldiness event, unlock threshold crossed, moldiness draft resolution, normal adaptation reward, and chained reward states.
13. Tune moldiness reward pacing, threshold pacing, and moldiness draft pool composition after the end-to-end flow is playable.

## Current Handoff

- `WORKLOG.md` is intentionally present-tense only. Historical task logs and stale tuning notes have been removed.
- Moldiness was chosen as the campaign meta-currency name.
- Moldiness progression foundation is committed in `90b81d3` and the first unlock-plumbing prototype is committed in `32b2b44`.
- Current moldiness prototype in repo:
  - level clears award persistent moldiness using rewards `1,1,2,2,3,3,4,4,5,5,6,6,7,7,8`
  - threshold tiers are `6,9,12,15,18,21,24,27,30,34`, then continue with `+4` growth beyond the table
  - overflow carries over and multiple unlock thresholds can trigger from one award
  - a temporary unlock/offer plumbing path exists in campaign state/controller code
  - the endgame service currently contains a temporary shortcut that auto-applies a pending moldiness unlock before the normal adaptation draft
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
- MVP content direction:
  - start with a hybrid model
  - moldiness draft offers should include both locked-content unlock rewards and repeatable universal meta rewards
  - first repeatable universal reward should permanently increase failed-run adaptation carryover capacity by +1 per draft
  - first locked-content rewards should unlock at least three new level-1 adaptations for future normal adaptation drafts
- Longer-term unlock categories discussed in the recovered transcript and follow-up clarification:
  - additional draftable adaptations
  - additional draftable mycovariants
  - mutation-related unlocks
  - failed-run carryover adaptation systems
- UI direction discussed in the recovered transcript:
  - moldiness should feel organic, atmospheric, scientific, fungal, and slightly quirky
  - a toast-corruption board / corruption-cell visual metaphor was discussed as a promising presentation direction
- Immediate next implementation target: update the prototype to a hybrid moldiness reward-card model, document it in `docs/MOLDINESS_HELPER.md`, then build the real moldiness draft UI flow on top of that model.
