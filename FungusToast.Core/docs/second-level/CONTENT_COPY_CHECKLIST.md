# Content Copy Checklist

One checklist for every player-facing `Name`, `Description`, and `FlavorText` across Mutations, Mycovariants, and Adaptations. Run it before any PR that adds or changes ability copy. The system-specific docs ([NEW_MUTATION_HELPER.md](../NEW_MUTATION_HELPER.md), [MYCOVARIANT_AUTHORING_STYLE.md](MYCOVARIANT_AUTHORING_STYLE.md), [ADAPTATION_HELPER.md](../ADAPTATION_HELPER.md)) add their own template rules on top; this file is the shared floor.

Terminology rules referenced below live in [GAMEPLAY_TERMINOLOGY.md](../GAMEPLAY_TERMINOLOGY.md). Voice rules live in [UI_STYLE_GUIDE.md](../UI_STYLE_GUIDE.md) section 1.

## Name

- [ ] Passes the shared naming rules in [MUTATION_MYCOVARIANT_ADAPTATION_NAMING.md](MUTATION_MYCOVARIANT_ADAPTATION_NAMING.md) (word count, word length, one advanced term, pronounceable, fungal, mechanic-linked).
- [ ] Unique across all Mutations, Mycovariants, Adaptations, unlocks, and reward labels (repo search done).
- [ ] Mycovariant ranks use Roman numerals; nothing else does.

## Description: vocabulary

- [ ] Every board-state change uses its canonical verb: Colonize, Reclaim, Infest, Overgrow, Replace, Toxify, Poison, Kill/Die, Clear, Move, Expire. No *invade*, *occupy*, *convert*, *take over*, *remove*, *consume*, *neutralize*, *relocate*, *redeploy*.
- [ ] `Resistant` / `non-Resistant` / `Resistance` capitalized; "becomes Resistant" / "loses Resistance"; no *killable*. If immunity is explained, it is the exact sentence "Resistant cells cannot be killed, infested, or poisoned."
- [ ] `orthogonal` / `diagonal`, never *cardinal*; the "(up / down / left / right)" hint appears once, on first use.
- [ ] `Growth Phase`, `Decay Phase`, `Mutation Phase`, `Growth Cycle` in Title Case; no *during decay*, *at growth start*, bare *cycles*.
- [ ] `mutation points` lowercase.
- [ ] `starting spore`, never *starting cell* / *starting tile*.
- [ ] `dead cell`, never *dead tile* or *corpse* in the technical block.
- [ ] `enemy` for opponents; *rival* only in flavor.
- [ ] `Mycelial Surge(s)` for the category; no *Surge Mutations*.
- [ ] Named abilities in full Title Case; no *chemobeacon* or other internal shorthand.
- [ ] Nutrient patch labels match the in-game names (`Adaptogen Patch`, `Sporemeal Patch`, `Hypervariation Patch`).
- [ ] `crust` is glossed as "the board edge (the crust)" on first use.

## Description: structure

- [ ] Opens with the right scope or cadence phrase for its system: Mutation summary sentence; Mycovariant `One-time on draft:` / `For the rest of the game, ...`; draftable Adaptation `For the rest of the campaign, ...` / `At the start of round N, ...`.
- [ ] Stands alone: no "same as X", no "standard targeting rules", no references to another card for the core rule.
- [ ] Every number comes from a named balance constant, formatted through the shared helper. No hard-coded percentages.
- [ ] Pluralization is handled for any constant that could be 1 (`point` / `points`, `tile` / `tiles`); no "(s)".
- [ ] No formula-in-prose ("X% chance (X = ...)"). Say what the chance is in words.
- [ ] Contains no flavor prose. Adaptations have no `FlavorText` field, so their descriptions must be pure mechanics.
- [ ] `<b>` only wraps `Technical:` and `Max Level Bonus:`. No bold numbers.
- [ ] ASCII only: no em dashes, no multiplication sign, no smart quotes.
- [ ] Redundancy trimmed: "up to N" already means "fewer if not enough exist".

## FlavorText

- [ ] One sentence (two at most), lab-notebook register per the voice guide.
- [ ] Does not restate or contradict the mechanics.
- [ ] No banned superlatives (*ultimate*, *impossible*, *unstoppable*, *unassailable*, *lethal precision*), no martial setting words (*battlefield*, *terrain*, *war*).
- [ ] Reinforces the toast/mold motif where it can (loaf, crust, pores, substrate, spores, hyphae).

## Verification

- [ ] The technical copy matches the processor: trigger timing, target rules, scaling, caps, and Resistance interaction were checked against code, not memory.
- [ ] Icon reviewed on the `tools/icon-preview` sheet at draft-card size.
