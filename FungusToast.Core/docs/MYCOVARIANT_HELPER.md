# Mycovariant Helper Index

Use this page as the entry point for all Mycovariant work.

## What Mycovariants Are

Mycovariants are drafted abilities that either:
- apply a **one-time active effect** (usually during draft), or
- grant a **passive effect that can trigger repeatedly for the rest of the game**.

## Primary Docs

- **Naming rules and candidate-name workflow:** [MUTATION_MYCOVARIANT_ADAPTATION_NAMING.md](MUTATION_MYCOVARIANT_ADAPTATION_NAMING.md)
- **Authoring standards (copy/UX text):** [MYCOVARIANT_AUTHORING_STYLE.md](MYCOVARIANT_AUTHORING_STYLE.md)
- **Technical implementation flow:** [MYCOVARIANT_TECHNICAL_FLOW.md](MYCOVARIANT_TECHNICAL_FLOW.md)
- **Review checklist for PRs:** [MYCOVARIANT_PR_CHECKLIST.md](MYCOVARIANT_PR_CHECKLIST.md)

## Suggested Agent Workflow

1. Read `MUTATION_MYCOVARIANT_ADAPTATION_NAMING.md` before naming a new Mycovariant.
2. Present **5 candidate names** that all satisfy the naming rules, each with a brief biological explanation and gameplay implication.
3. Read `MYCOVARIANT_AUTHORING_STYLE.md` before editing descriptions/flavor text.
4. Read `MYCOVARIANT_TECHNICAL_FLOW.md` before adding or changing behavior.
5. Generate a unique icon for the Mycovariant so draft cards, tooltips, and any sidebar/profile surfaces do not fall back to generic art. The first pass can be provisional and replaced later, but every new Mycovariant should ship with distinct iconography.
6. Implement changes in category factories and processors.
7. Proactively list the proposed test cases for the new or changed Mycovariant, including happy path behavior, edge cases, timing/cadence checks, interaction coverage, and likely regressions.
8. Validate with Core + Simulation builds.
9. Complete `MYCOVARIANT_PR_CHECKLIST.md` before requesting review.

## Common Tasks

### Add a new Mycovariant
1. Add or confirm the ID in `FungusToast.Core/Mycovariants/MycovariantIds.cs`.
2. Name it using `MUTATION_MYCOVARIANT_ADAPTATION_NAMING.md`, starting with 5 candidate names before finalizing.
3. Write concise description and optional flavor text using `MYCOVARIANT_AUTHORING_STYLE.md`.
4. Generate a unique icon keyed off the Mycovariant's `IconId`. It can be temporary and replaced later, but it should be distinct from every other Mycovariant.
5. Define the Mycovariant in the correct category factory and wire any gameplay behavior through the appropriate processors, observers, and Unity draft hooks.

### Add Mycovariant UI presence
1. Reuse the existing Unity tooltip system with `ITooltipContentProvider` and `TooltipTrigger`.
2. Reuse a centralized art lookup path for icons rather than binding sprites ad hoc.
3. Ensure the centralized art repository has a unique generated icon for the Mycovariant even if it is only a first-pass placeholder.
4. Keep draft, tooltip, and any persistent UI presentation consistent.

### Validate Mycovariant behavior
1. Build `FungusToast.Core/FungusToast.Core.csproj`.
2. Build `FungusToast.Simulation/FungusToast.Simulation.csproj` when the change affects shared core behavior.
3. Run a smoke simulation and inspect output for expected behavior when gameplay changed.
4. Verify Unity draft behavior when the Mycovariant needs interactive input, custom visuals, or new icon wiring.

## Notes

- Keep Mycovariant logic deterministic and Unity-free in Core.
- Treat the Mycovariant definition plus its `IconId` as the source of truth for card metadata.
- Reuse Adaptation guidance for icon distinctness and centralized art lookup patterns where helpful, but do not assume the same runtime flow.
