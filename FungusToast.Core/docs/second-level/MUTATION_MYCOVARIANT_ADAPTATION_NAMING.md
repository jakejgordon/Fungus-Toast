# Mutation, Mycovariant, and Adaptation Naming

Use this guide when naming Mutations, Mycovariants, and Adaptations.

## Purpose

Names should feel scientifically grounded, readable in UI, and tightly linked to gameplay behavior.

## Core Naming Rules

### 1) Ground the name in fungal or biological language
- Prefer terms from mycology, spore dispersal, toxicity, growth, decay, resistance, or competition.
- Avoid generic fantasy phrasing that does not teach the player anything about the effect.

### 2) Tie the name to what the effect actually does
- The name should suggest the core mechanic, target, cadence, or biological theme.
- `Mycelial Bloom` fits growth and spread.
- `Perispore Crown` fits a spore-centered radial toxin trap.
- Avoid names that sound fungal but could describe any mechanic.

### 3) Use exactly one or two words
- One word is acceptable when it is clear and readable.
- Two words are preferred when they improve clarity.
- Do not use three-word names for new Mutations, Mycovariants, or Adaptations unless an existing legacy pattern must be preserved.

### 4) Cap each word at 11 characters
- Every individual word must be 11 characters or fewer.
- Check scientific terms carefully before finalizing them.

### 5) Favor clarity over obscurity
- A slightly plainer fungal term is better than a highly obscure term that hides the mechanic.
- If two scientifically grounded names are viable, prefer the one a player can parse faster in a draft card, tooltip, or sidebar.

### 6) Require cross-system uniqueness
- Every proposed Mutation, Mycovariant, and Adaptation name must be unique across all three content systems.
- Before finalizing a name, run a repo search to confirm the exact name is not already used by any Mutation, Mycovariant, Adaptation, unlock, or closely paired player-facing reward label.
- Do not reuse a name that already exists in another content type, even if the mechanics are different.

## Recommended Pattern

- `<Fungal/Biological term> <Mechanic noun>`
- `<Process term> <Outcome term>`

Examples:
- `Conidial Relay`
- `Hyphal Economy`
- `Perispore Crown`
- `Mycelial Bloom`

## Anti-Patterns

- Names with no gameplay signal, such as vague mystical phrases.
- Names longer than two words.
- Any word longer than 11 characters.
- Names that imply the wrong cadence or target.

## New Mycovariant Naming Workflow

When creating a new Mycovariant, generate **5 candidate names** and present them to the user before finalizing. For each candidate:

1. State the name.
2. Give a one-sentence explanation of the biological or fungal concept it draws from.
3. Describe what gameplay mechanic, cadence, or theme it implies to a player reading it in a draft card or tooltip.

This keeps naming collaborative without sacrificing speed, and it gives enough context to judge both clarity and thematic fit. Apply all naming rules above before proposing any candidate — only offer names that pass every checklist item.
Check repo uniqueness before presenting the final shortlist.

## New Adaptation Naming Workflow

When creating a new Adaptation instance, generate **5 candidate names** and present them to the user before finalizing. For each candidate:

1. State the name.
2. Give a one-sentence explanation of the biological or fungal concept it draws from.
3. Describe what gameplay mechanic or theme it implies to a player reading it in a tooltip or draft card.

This gives the user enough context to make an informed choice without having to research the terms independently. Apply all naming rules above before proposing any candidate — only offer names that pass every checklist item.
Check repo uniqueness before presenting the final shortlist.

## Checklist

Before finalizing a name:
- [ ] Is it one or two words only?
- [ ] Is every word 11 characters or fewer?
- [ ] Is it scientifically or biologically grounded?
- [ ] Does it suggest what the effect actually does?
- [ ] Is it readable at a glance in UI?
- [ ] Is it unique across Mutations, Mycovariants, and Adaptations in the repo?