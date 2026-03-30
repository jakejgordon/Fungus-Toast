# Fungus Toast Sound Guide

> Purpose: define where sound belongs in Fungus Toast, where assets and sound code should live, and the default implementation plan for the first gameplay sound cues.
> Audience: Unity developers and GitHub Copilot.
> Scope (v1): short-form gameplay SFX and phase-transition cues. Music, ambience, and voice are out of scope for this document unless they affect storage or service architecture.

## 1. Ownership and Layer Boundary

- Keep sound triggering in `FungusToast.Unity`.
- Do not place audio behavior in `FungusToast.Core`; Core should remain deterministic and presentation-free.
- Use Core events and Unity phase runners as trigger sources, but play clips only from Unity-side controllers, presenters, or services.

Practical rule:
- If a sound exists to confirm a UI action, phase banner, draft prompt, or board animation timing, it belongs in Unity.
- If a mechanic needs a new trigger surface, prefer exposing or reusing a Core event and consuming it from Unity rather than moving gameplay resolution into Unity.

## 2. Recommended Asset Storage

Use the following audio asset structure in this repository:

- `FungusToast.Unity/Assets/Audio/SFX/UI` for button-like or UI-timed confirmation cues, including the current mutation/phase start pass.
- `FungusToast.Unity/Assets/Audio/SFX/Phases` for future phase-specific libraries if the audio set grows beyond the current UI-timed cues.
- `FungusToast.Unity/Assets/Audio/SFX/Board` for board-state or world-space feedback.
- `FungusToast.Unity/Assets/Audio/Music` for menu or match music.
- `FungusToast.Unity/Assets/Audio/Mixers` for Unity mixer assets and audio routing configuration.

Treat this folder layout as the default convention for new audio content. Do not place gameplay SFX directly into unrelated asset folders.

Recommended naming:

- `sfx_ui_mutation_upgrade_success_01.wav`
- `sfx_ui_mutation_store_points_01.wav`
- `sfx_phase_mutation_start_01.wav`
- `sfx_phase_growth_start_01.wav`
- `sfx_phase_decay_start_01.wav`
- `sfx_phase_draft_start_01.wav`
- `sfx_phase_growth_cycle_tick_01.wav`

Keep variations grouped by suffix (`_01`, `_02`, `_03`) rather than inventing new names for the same cue family.

### Batch Converting iPhone `.m4a` Files to `.wav`

When recording rough sound ideas on iPhone, batch-convert the `.m4a` files into mono `44.1 kHz` `16-bit` `.wav` files before importing them into Unity.

Recommended workflow:

1. Open PowerShell in the folder containing the `.m4a` files.
2. Create a subfolder named `Fungus Toast WAVs` if it does not already exist.
3. Run the following command:

```powershell
Get-ChildItem -Filter *.m4a | ForEach-Object {
  ffmpeg -i $_.FullName -map 0:a:0 -vn -ac 1 -ar 44100 -sample_fmt s16 -map_metadata -1 "Fungus Toast WAVs\$($_.BaseName).wav"
}
```

What this does:

- converts each `.m4a` file in the current folder
- keeps audio only
- outputs mono audio
- resamples to `44100 Hz`
- writes `16-bit` PCM `.wav` files
- strips metadata
- saves the converted files into the `Fungus Toast WAVs` subfolder using the same base filename

Use the converted `.wav` files as the editing/import source for Fungus Toast rather than importing the original phone recordings directly.

## 3. Recommended Code Location

If a reusable sound layer is added, follow the existing Unity service pattern from `UI_ARCHITECTURE_HELPER.md`.

Recommended Unity-side locations:

- `FungusToast.Unity/Assets/Scripts/Unity/Services/SoundEffectService.cs` for one-shot SFX playback and cue routing.
- `FungusToast.Unity/Assets/Scripts/Unity/Audio/` for sound-specific data classes if the system grows beyond one service.
- A serialized catalog asset or serialized clip references owned by the sound service; avoid `Resources` lookups.

Recommended responsibility split:

- `SoundEffectService` owns clip references, cooldowns, optional random variation, and mixer routing.
- UI/controller classes decide when a gameplay or UI moment happened.
- `GameManager` bootstraps the service and passes narrow dependencies instead of becoming the audio manager itself.

## 4. Sound Design Guidelines

### Core principles

- Prioritize clarity over spectacle. Sounds should confirm state changes, not dominate the match.
- Keep repeated cues short and low-fatigue, especially for growth cycles.
- Use distinct timbral families so the player can infer meaning:
  - mutation upgrade: positive, fungal-tech, lightly rewarding
  - bank/store: soft confirm, reserved, non-triumphant
  - phase starts: broader stingers with more identity
  - growth cycle starts: tiny pulse or tick, not a second banner
- Avoid large low-frequency hits for routine actions; phase starts happen every round.
- Prefer dry or lightly textured sounds over long reverb tails so the board remains readable.

### Loudness and overlap

- Default gameplay SFX should sit below music and below major endgame moments.
- Do not stack multiple equally prominent sounds on the same frame when a banner, toast, and board animation all begin together.
- If two cues collide, prefer the more important cue and suppress the other one.

### Duration targets

- Tiny repeated feedback: `0.08s` to `0.25s`
- Standard UI confirmation: `0.15s` to `0.45s`
- Phase-start stinger: `0.50s` to `1.20s`
- Draft-start stinger: `0.80s` to `1.50s`

Anything longer than `1.5s` should be treated as a special case, not a default SFX.

## 5. Current Implementation Snapshot

These cues are currently wired in-game:

- `sfx_ui_mutation_upgrade_success_01.wav`
- `sfx_ui_mutation_store_points_01.wav`
- `sfx_phase_mutation_start_01.wav`
- `sfx_phase_growth_start_01.wav`
- `sfx_phase_decay_start_01.wav`
- `sfx_phase_growth_cycle_tick_01.wav`

These are all Unity-side one-shot SFX and they currently respect the shared SFX enable/volume settings exposed in the main menu and in-game pause menu.

## 6. Initial Sound Plan

The first sound pass should cover the following cues.

| Cue | Unity trigger point | Recommended moment | Recommended duration | Notes |
| --- | --- | --- | --- | --- |
| Mutation upgrade success | `UI_MutationManager.TryUpgradeMutation(...)` | Play immediately after a successful non-targeted upgrade | `0.25s` to `0.60s` | Implemented. Positive confirmation. Use a separate success hook in targeted-surge flows only after the placement fully succeeds. |
| Targeted upgrade success | `UI_MutationManager.ResolveChemotacticBeaconUpgrade(...)` | Play only after `TryActivateReservedTargetedSurge(...)` returns success | `0.25s` to `0.60s` | Implemented through the targeted success path. Do not play when the player reserves cost or enters tile-selection mode. |
| Store mutation points | `UI_MutationManager.OnStoreMutationPointsClicked()` | Play after `WantsToBankPointsThisTurn = true` and before ending the mutation turn | `0.18s` to `0.40s` | Implemented. This should read as a deliberate hold/save action, not a reward fanfare. |
| Mutation phase start | `GameManager.StartNextRound()` | Play alongside the mutation banner when `PhaseBanner.Show("Mutation Phase Begins!", 2f)` fires | `0.60s` to `1.10s` | Implemented. Skip if a higher-priority onboarding banner replaces the normal mutation banner. |
| Growth phase start | `GameManager.StartGrowthPhase()` or `GrowthPhaseRunner.StartGrowthPhase()` | Prefer the same moment the growth banner is shown | `0.60s` to `1.10s` | Implemented from `GameManager.StartGrowthPhase()`. Use one growth-phase cue, not duplicate cues in both methods. |
| Decay phase start | `GameManager.StartDecayPhase()` | Play alongside `PhaseBanner.Show("Decay Phase Begins!", 2f)` | `0.55s` to `1.00s` | Implemented. Slightly darker tone than growth. Keep it short; decay is frequent. |
| Drafting phase start | `GameManager.StartMycovariantDraftPhase(...)` | Play with the draft-phase banner after the controller is initialized | `0.80s` to `1.40s` | Planned. This cue can be a bit more ceremonial than standard phases because it is less frequent and more strategically important. |
| Growth cycle start | `GrowthPhaseRunner.RunNextCycle(...)` | Play immediately after `phaseCycle++` and before `ExecuteSingleCycle(...)` | `0.08s` to `0.22s` | Implemented. Must be subtle. This cue repeats several times per round and should not compete with colony growth animations. |

## 7. Trigger Placement Notes

### 7.1 Mutation upgrade

- Use the UI layer for the initial hook because the user action and confirmation both originate there.
- Successful upgrade sounds should not play from failed attempts.
- If future AI-visible upgrade sounds are desired, gate them carefully so AI auto-spending does not create noisy burst sequences.

### 7.2 Banking mutation points

- Treat banking as a choice confirmation, not a spend event.
- The cue should happen before the panel closes so the player receives immediate confirmation.

### 7.3 Phase starts

- The safest first implementation is to pair phase-start SFX with `UI_PhaseBanner.Show(...)` call sites rather than trying to infer phase changes from low-level Core events.
- This keeps the audio aligned with the visible banner timing.
- Drafting uses a different controller flow, so trigger from the draft-phase start path rather than trying to generalize phase sounds too early.

### 7.4 Growth cycle starts

- Growth cycles are the highest repetition cue in this first pass.
- Keep the clip very short and consider a per-cue cooldown if cycle timing is ever shortened.
- If the phase-start growth stinger and cycle-1 tick feel redundant in playtests, suppress the first cycle tick and only play cycles `2+`.

## 8. Implementation Recommendation

Start with a thin one-shot SFX service rather than a full audio framework.

Recommended first pass:

1. Add a `SoundEffectService` under Unity services.
2. Give it serialized `AudioClip` references for the initial cue set.
3. Expose small explicit methods such as `PlayMutationUpgradeSuccess()`, `PlayMutationBanked()`, `PlayPhaseStart(PhaseSoundCue cue)`, and `PlayGrowthCycleStart(int cycle)`. 
4. Bootstrap it from `GameManager` and call it only from Unity-side presentation hooks.
5. Add a simple suppression rule so higher-priority phase cues can mute lower-priority overlapping cues on the same frame.

Avoid starting with:

- a large event bus just for audio
- string-based clip lookup
- `Resources.Load(...)`
- world-space positional audio for these UI-phase cues

## 9. Review Checklist For New Sounds

- Is the trigger in Unity rather than Core?
- Is the cue short enough for its frequency?
- Does the cue overlap a banner, toast, or board animation in an unpleasant way?
- Is the cue family stored under the correct `Assets/Audio` subfolder?
- Is the clip name specific and versioned consistently?
- If this is a repeated cue, has fatigue been considered?

## 10. Open Questions For Future Passes

- Should growth cycle `1` use the same tick as later cycles, or should cycle `1` stay silent to let the phase-start stinger breathe?
- Should AI-only mutation upgrades ever produce audible feedback, or should upgrade sounds remain human-action focused?
- Do campaign-only events need a separate cue family, or should they reuse the draft/phase palette first?