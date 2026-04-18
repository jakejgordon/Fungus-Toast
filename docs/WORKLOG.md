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
- **Current focus:** campaign moldiness meta-progression and player-facing UI. Core flow now exists for moldiness rewards, defeat carryover selection, and locked adaptation unlocks, but the moldiness UI still needs visibility/polish and the new moldiness reward panel currently has a rendering/interaction issue under investigation.
- **How to update this section:** whenever we pivot, replace this with the current active thread in one or two lines

## Current Plan

1. Fix the current moldiness reward panel rendering/interaction issue so reward offers are visible and selectable during the post-victory moldiness draft.
2. Add persistent moldiness UI to the campaign HUD, likely in the right sidebar, showing current moldiness level, progress toward the next threshold, and unlock proximity.
3. Add end-of-level moldiness summary UI on the endgame panel, including gained moldiness, updated progress, threshold crossings, and why a moldiness draft triggered.
4. Reserve and prototype a strong location for the longer-term moldiness toast/corruption visualization on the endgame panel and/or campaign HUD.
5. Smoke-test the combined campaign progression flow, then tune reward pacing, unlock pacing, UI clarity, and the initial moldiness reward catalog.

## Pending Tasks

1. Fix the current post-victory moldiness reward panel so reward cards actually render and can be selected reliably.
2. Add persistent moldiness HUD UI showing at minimum: moldiness unlock level, current progress, next threshold, and pending draft state when applicable.
3. Add end-of-level moldiness summaries for both wins and losses, including `+X Moldiness`, updated progress, and threshold/draft messaging.
4. Decide and implement the first good placement for moldiness toast visualization so the corruption metaphor has a visible home in the campaign UI.
5. Polish the moldiness reward panel UX, including clearer selection highlighting and reward-type presentation.
6. Verify campaign save/resume behavior when moldiness progress, moldiness draft state, adaptation draft state, or defeat carryover selection is pending.
7. Smoke-test a full campaign flow covering: no moldiness event, threshold crossed, moldiness draft resolution, normal adaptation reward, defeat carryover selection, and chained reward states.
8. Tune moldiness reward pacing, threshold pacing, and moldiness draft pool composition after the end-to-end flow is playable.
9. Decide whether some moldiness rewards should apply immediately to the current run, future runs only, or both.
10. Explore longer-term expansion to additional unlock categories such as mycovariants and mutation-related rewards once the adaptation-first loop is solid.

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
  - the current moldiness reward panel is functional in structure, but is presently showing a rendering/interaction issue where reward cards may fail to appear, leaving the button inert
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
  - first locked-content rewards now include at least three level-1 locked adaptations for future normal adaptation drafts, and Jake has also added a new adaptation from another PC that should be incorporated into the unlock pool via sync/pull first
- Longer-term unlock categories discussed in the recovered transcript and follow-up clarification:
  - additional draftable adaptations
  - additional draftable mycovariants
  - mutation-related unlocks
  - failed-run carryover adaptation systems
- UI direction discussed in the recovered transcript:
  - moldiness should feel organic, atmospheric, scientific, fungal, and slightly quirky
  - a toast-corruption board / corruption-cell visual metaphor was discussed as a promising presentation direction
- Immediate next implementation target: fix the current moldiness reward panel rendering/selection issue, then add persistent moldiness progress UI to the campaign HUD and end-of-level summary surfaces so players can see level, progress, gains, and why moldiness drafts trigger.
