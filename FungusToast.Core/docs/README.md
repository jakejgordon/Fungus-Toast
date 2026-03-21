# Fungus Toast Documentation Map

This file is the complete documentation index for the repository.

Use `.github/copilot-instructions.md` as the top-level router.
Use this file for the full hierarchy, secondary references, and discoverability for docs that are not linked directly from the root router.

## 1. Core Entry Docs

These are the main task-entry documents and are also referenced from `.github/copilot-instructions.md`.

- `BUILD_INSTRUCTIONS.md` — canonical CLI build commands and platform notes
- `SIMULATION_HELPER.md` — simulation workflows, reproducibility, fairness testing, and output conventions
- `AI_STRATEGY_AUTHORING.md` — AI roster organization, metadata, and strategy authoring patterns
- `NEW_MUTATION_HELPER.md` — mutation authoring workflow
- `MYCOVARIANT_HELPER.md` — entry point for all Mycovariant work
- `ADAPTATION_HELPER.md` — entry point for all Adaptation work
- `CAMPAIGN_HELPER.md` — campaign systems and progression context
- `DOMINANCE_DIAGNOSIS_WORKFLOW.md` — controlled analysis workflow for explaining dominance
- `GAME_BALANCE_CONSTANTS.md` — canonical gameplay balance levers and tuning guidance
- `UI_ARCHITECTURE_HELPER.md` — Unity UI architecture, service extraction, tooltips, and pooling
- `UI_STYLE_GUIDE.md` — UI semantic styling rules
- `DESIGN_PRINCIPLES.md` — architecture and terminology context

## 2. Authoring / Technical Subdocs

These are intentionally second-hop documents: they are discovered from entry docs rather than from the root router.

### Adaptations
- `ADAPTATION_TECHNICAL_FLOW.md` — runtime and persistence wiring details

### Mycovariants
- `MYCOVARIANT_AUTHORING_STYLE.md` — concise mechanics/copy style rules
- `MYCOVARIANT_TECHNICAL_FLOW.md` — end-to-end technical implementation flow
- `MYCOVARIANT_PR_CHECKLIST.md` — pre-review completion checklist

### Shared naming
- `MUTATION_MYCOVARIANT_ADAPTATION_NAMING.md` — naming constraints shared across content systems

## 3. Specialized / Secondary References

These are useful for narrower tasks and should remain discoverable here even when not listed in the root router.

- `ANIMATION_HELPER.md` — gameplay animation trigger/timing guidance
- `PLAYER_ACTIVITY_LOG_HELPER.md` — player activity log semantics and aggregation
- `FUTURE_IMPROVEMENTS.md` — backlog / longer-horizon ideas
- `UI_POLISH_TASKLIST.md` — UI polish tracker

## 4. Tracking / Operational Docs

These are not canonical design docs, but they are useful for active work.

- `../../docs/WORKLOG.md` — current thread, handoff, progress, and next steps

## 5. Documentation Hierarchy Rules

- Every important doc should be discoverable from somewhere in the hierarchy.
- The root router (`.github/copilot-instructions.md`) should list only first-hop entry docs.
- Docs not listed at the root must be linked from a more general doc such as this one or from their parent helper.
- Before adding a new doc, decide whether an existing doc should be expanded instead.
