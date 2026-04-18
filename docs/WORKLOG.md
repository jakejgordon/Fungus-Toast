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
2. Decide the first gated content set for the MVP, with current bias toward locked adaptations that are stronger, niche, or build-shaping.
3. Replace the temporary auto-apply behavior with a real post-victory moldiness unlock choice UI that presents multiple options.
4. Decide how moldiness unlock flow chains with the normal adaptation reward flow, with current preference that moldiness resolves first.
5. Build and smoke-test the combined campaign progression flow, then tune reward pacing, unlock pacing, and the initial locked-content catalog.

## Pending Tasks

1. Introduce a first-class concept for locked content that can be gated by moldiness progression.
2. Add a `MoldinessUnlockLevel` concept so content can become eligible only after reaching the required moldiness tier.
3. Define whether moldiness progression is lifetime-based, current-balance-based, or both for unlock eligibility and display. Current direction is lifetime-style threshold progression with carryover.
4. Decide which systems should participate in moldiness gating first: Adaptations only, or Adaptations plus Mutations/Mycovariants.
5. Create the first real locked-content catalog instead of reusing ordinary existing adaptations as pseudo-unlocks.
6. Replace the temporary auto-apply moldiness behavior with a real player-facing moldiness unlock choice step that offers multiple choices.
7. Ensure moldiness unlocks resolve before the normal adaptation reward selection, unless a better chained-flow design emerges.
8. Decide whether some moldiness unlock effects should apply immediately to the current run, future runs only, or both.
9. Add player-facing UI copy for moldiness progress, threshold crossings, unlock level, and newly available content.
10. Explore visual direction for moldiness presentation, with current inspiration being toast corruption / corruption cells and an organic, atmospheric, scientific, fungal feel.
11. Verify campaign save/resume behavior when moldiness progress or unlock choice state is pending.
12. Smoke-test a full campaign victory flow covering: no moldiness event, unlock threshold crossed, normal adaptation reward, and chained reward states.
13. Tune moldiness reward pacing and threshold pacing after the end-to-end flow is playable.

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
  - ordinary existing adaptations should not be treated as permanent moldiness unlocks
  - moldiness should instead gate explicit locked content
  - content availability should be controlled by `MoldinessUnlockLevel`
  - the system should be extensible enough to gate Adaptations, and later possibly Mutations, Mycovariants, or other reward types
- MVP content direction:
  - start with locked adaptations as the first gated content type
  - those should be new, stronger, niche, or build-shaping options rather than ordinary baseline adaptations that already exist in the draft pool
- Longer-term unlock categories discussed in the recovered transcript:
  - additional draftable adaptations
  - additional draftable mycovariants
  - mutation-related unlocks
  - possible failed-run carryover adaptation systems
- UI direction discussed in the recovered transcript:
  - moldiness should feel organic, atmospheric, scientific, fungal, and slightly quirky
  - a toast-corruption board / corruption-cell visual metaphor was discussed as a promising presentation direction
- Immediate next implementation target: revise the prototype to match the corrected locked-content design, then build the real moldiness unlock UI flow on top of that model.
