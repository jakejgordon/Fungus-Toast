# Save Compatibility

This document is the canonical reference for save/resume compatibility, breaking-change risk, and migration expectations across Fungus Toast persistence systems.

Use this doc whenever a change touches:
- serialized save models
- runtime snapshot models
- persisted IDs or enums
- resume / restore flow
- campaign progression ordering or authored preset identity

See also:
- `CAMPAIGN_HELPER.md` for campaign systems and progression context
- `ADAPTATION_HELPER.md` for adaptation authoring
- `MYCOVARIANT_HELPER.md` for mycovariant authoring
- `ARCHITECTURE_OVERVIEW.md` for layer ownership and runtime patterns

## Why this exists

Fungus Toast currently persists save data through Unity JSON serialization and restores mid-level state from runtime snapshots.
That makes many additive changes safe, but it also means some refactors, ID changes, enum changes, or restore-semantics changes can break existing saves or silently alter resumed runs.

This doc exists to make those risks visible before changes ship.

## Current persistence surfaces

### Campaign meta save
- File: `Application.persistentDataPath/campaign_save.json` in editor/debug development runs
- File: `Application.persistentDataPath/production/campaign_save.json` in non-debug production builds
- Existing production installs migrate the legacy root save into the production path on first launch after this storage split ships.
- Save model: `FungusToast.Unity/Assets/Scripts/Unity/Campaign/CampaignState.cs`
- Save/load service: `FungusToast.Unity/Assets/Scripts/Unity/Campaign/CampaignSaveService.cs`

This stores campaign run state such as:
- current level index
- selected adaptations
- pending reward / defeat-carryover state
- resolved AI lineup for the active level
- moldiness progression
- current in-level checkpoint and RNG state

### Mid-level gameplay checkpoint
- Stored inside `CampaignState` as `inLevelRuntimeSnapshot` and `inLevelRandomState`
- Runtime snapshot model: `FungusToast.Core/Persistence/RoundStartRuntimeSnapshot.cs`
- Export/restore logic: `FungusToast.Core/Persistence/RoundStartRuntimeSnapshotFactory.cs`

This stores round-start gameplay state such as:
- board dimensions and round counters
- players, mutations, mycovariants, adaptations, active surges
- cells, nutrient patches, chemobeacons
- pending hypervariation draft state
- mycovariant pool state
- RNG state for deterministic resume

### Non-campaign hotseat save
- File: `Application.persistentDataPath/solo_save.json` in editor/debug development runs
- File: `Application.persistentDataPath/production/solo_save.json` in non-debug production builds
- Existing production installs migrate the legacy root save into the production path on first launch after this storage split ships.
- Save model/service: `FungusToast.Unity/Assets/Scripts/Unity/Campaign/CampaignSaveService.cs` (`SoloGameSaveState`, `SoloGameSaveService`)

This uses the same runtime snapshot compatibility rules as campaign mid-level resume.

## Compatibility tiers

### Usually safe
These are often safe if defaults are valid and resume code tolerates missing data:
- adding a new serialized field with a safe default value
- adding new optional content that old saves do not reference
- adding new normalization/sanitization for older save shapes
- adding alias handling for old IDs or strategy names

Even these changes should still be checked against the PR checklist below.

### Soft-breaking
These changes may not crash or fail to load, but they can change the resumed run in player-visible ways:
- removing or renaming adaptation IDs referenced by an active run
- removing or renaming AI strategy names used by a saved level
- removing or renaming board preset IDs
- changing reward catalogs or unlock IDs without migration/alias handling
- changing restore heuristics so the resumed board behaves differently
- changing RNG restore behavior so resumed turns diverge from the pre-update run

### Hard-breaking
These changes can corrupt restore behavior, invalidate saves outright, or make resume semantics untrustworthy:
- renaming, removing, or changing the type/meaning of serialized fields without compatibility handling
- changing persisted enum numeric meanings/order
- changing the interpretation of runtime snapshot fields in incompatible ways
- reusing old IDs for different gameplay concepts
- restructuring campaign progression so saved `levelIndex` values no longer mean the same thing

## Breaking-change risk checklist by change type

### 1) Serialized field changes
High risk:
- renaming serialized fields
- removing serialized fields that resume logic still expects
- changing field types
- changing a field from "state" to "derived value" without migration logic

Relevant examples:
- `CampaignState`
- `CampaignVictorySnapshot`
- `RoundStartRuntimeSnapshot`
- nested runtime snapshot classes
- `RandomStateSnapshot`
- `SoloGameSaveState`

Guidance:
- prefer additive fields over renames/removals
- if a rename is necessary, keep compatibility handling or perform explicit migration
- if a field becomes obsolete, leave a compatibility path long enough to bridge old saves

### 2) Persisted ID changes
Very high risk when IDs are stored in save data.

Examples of persisted identifiers:
- adaptation IDs
- moldiness unlock/reward IDs
- board preset IDs
- AI strategy names
- mutation IDs
- mycovariant IDs

Risks:
- old saves may load but silently skip missing content
- resumed runs may use fallback behavior instead of the original authored state
- pending reward/selection state may be cleared or regenerated

Guidance:
- prefer stable IDs for all persisted content
- do not repurpose an old ID for a different concept
- when renaming is unavoidable, add aliases or explicit migration
- audit any change to IDs referenced by authored campaign assets and save data together

### 3) Enum changes
High risk whenever enum values are serialized into save data.

Examples include runtime snapshot enums for:
- player type / AI type
- fungal cell type
- cause of death
- growth source
- nutrient patch source/type/reward type
- mycovariant effect types

Guidance:
- do not reorder persisted enums casually
- do not change numeric meanings of existing enum entries without migration
- adding new enum members is safer than reassigning old ones, but still requires restore-path review

### 4) Campaign progression and authored content changes
Risk depends on whether a saved run references the authored data directly.

Examples:
- reordering levels in `CampaignProgression`
- changing what a saved `levelIndex` should point to
- renaming `BoardPreset.presetId`
- changing fixed lineup or AI pool expectations for a saved level

Guidance:
- treat `levelIndex` and `boardPresetId` as persisted compatibility surfaces
- if a level must be re-authored incompatibly, decide explicitly whether to migrate or invalidate affected saves
- avoid silent meaning changes for in-progress campaign levels when a stable alias or transitional preset can preserve continuity

### 5) Runtime restore semantics
This is the biggest risk area for mid-level resume.

Examples:
- changing how players restore controlled tiles, starting tiles, mutations, or surges
- changing how cell ownership or resistance state is inferred/restored
- changing how pending hypervariation state is restored
- changing mycovariant pool restore semantics
- changing RNG capture/restore behavior

Guidance:
- any restore-path change should be treated as a save-compatibility review trigger
- if exact compatibility cannot be preserved, prefer deliberate checkpoint invalidation over silently restoring into misleading state

## Current known compatibility patterns already in use

These existing patterns are good precedents:
- null/default normalization for newer `CampaignState` fields during resume
- moldiness progression normalization for older save shapes
- adaptation draft sanitization when persisted draft choices are no longer valid
- AI strategy alias normalization for legacy campaign names
- fallback handling for older mycovariant drafted-round data

When adding new persisted data, prefer extending these patterns instead of inventing ad hoc fixes.

## Migration and mitigation options

Choose the lightest option that preserves trust in resumed runs.

### Option A: additive compatibility handling
Use when the old save is still semantically valid.

Examples:
- add a field and default it during load/normalize
- accept a legacy field name/value
- map an old ID to a new canonical ID

### Option B: aliasing / canonicalization
Use when names change but the concept is still the same.

Examples:
- legacy AI strategy name aliases
- reward ID aliases
- preset ID aliases
- adaptation ID aliases

### Option C: targeted migration
Use when the save shape must change but old saves can still be translated safely.

Examples:
- convert old field semantics into new fields during load
- remap old progression data to new authored content
- translate old snapshot values into new runtime state expectations

If migrations become common, add explicit save versioning rather than growing unstructured one-off normalization.

### Option D: deliberate invalidation
Use when compatibility cannot be preserved honestly.

Examples:
- invalidate only the in-level checkpoint and restart the level fresh
- force a new campaign start after a major progression rewrite
- clear only the affected pending selection state if the alternative would misrepresent player progress

Prefer narrow invalidation over deleting unrelated progress.

## Release / PR review checklist

Before merging a change, ask:

1. Does this change touch any serialized save model or runtime snapshot model?
2. Does it rename, remove, reorder, or repurpose any persisted field, enum, or ID?
3. Does it change what a saved `levelIndex`, `boardPresetId`, adaptation ID, strategy name, mutation ID, or mycovariant ID means?
4. Does it change restore behavior for players, cells, mutations, mycovariants, surges, nutrient patches, or RNG?
5. If an old save resumes after this change, will it:
   - restore exactly,
   - restore with a safe migration,
   - restore with a deliberate narrowed invalidation,
   - or break / silently diverge?
6. If compatibility is intentionally broken, is that documented in release notes or developer notes, and is the invalidation behavior explicit?

If any answer suggests risk, do not assume the change is safe just because the game still loads.

## Recommended validation when persistence-sensitive code changes

Pick the smallest meaningful check that matches the change:
- load an older campaign meta save
- resume an in-progress campaign level from checkpoint
- load an older hotseat save if shared runtime snapshot logic changed
- verify pending reward / carryover / moldiness resume flows
- verify AI lineup continuity on pooled campaign levels
- verify old IDs still resolve or migrate as intended

For major persistence changes, keep a small set of representative saves/checkpoints for manual compatibility smoke tests.

## Documentation rule

When a change introduces a new compatibility hazard or a new migration pattern:
- update this document
- link any deeper design/migration detail from the most relevant helper doc
- keep `CAMPAIGN_HELPER.md` and other feature docs focused on their systems, with this file as the canonical compatibility reference
