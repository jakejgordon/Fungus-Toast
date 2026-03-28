# Campaign modernization pass — 2026-03-27

## Goal

Promote the intended modern medium molds into the actual `Campaign` roster so `scripts/run_campaign_balance.py` can validate the same strategies that Unity campaign presets reference, then retune Campaign5-10 using the safe player proxy (`TST_CampaignPlayer_SafeBaseline`).

## Roster changes

Added curated campaign-safe aliases to `FungusToast.Core/AI/AIRoster.cs` and tagged them with campaign metadata:

- `CMP_TierCap_GrowthResilience_Easy`
- `CMP_Economy_KillReclaim_Medium`
- `CMP_Bloom_CreepingNecro_Medium`
- `CMP_Bloom_BeaconRegression_Medium`
- `CMP_Bloom_AnabolicRegression_Medium`
- `CMP_Control_AnabolicFirst_Hard`
- `CMP_Economy_LateSpike_Hard`
- `CMP_Bloom_CreepingRegression_Elite`

This avoids the previous mismatch where campaign presets wanted modern molds but the campaign harness could only validate legacy `AI*` entries.

## Validation workflow

Build smoke check:

```bash
dotnet build FungusToast.Core/FungusToast.Core.csproj
dotnet build FungusToast.Simulation/FungusToast.Simulation.csproj
```

Balance command pattern:

```bash
python3 scripts/run_campaign_balance.py --games 20 --seed 20260327 --level <N>
```

Common settings across the runs below:

- strategy set: `Campaign`
- slot policy: fixed
- nutrients: on
- mycovariants: on
- player proxy: `TST_CampaignPlayer_SafeBaseline`

## Final tuned presets and proxy results

### Campaign5

Lineup:
- `AI7`
- `AI8`
- `AI9`
- `CMP_Economy_KillReclaim_Medium`
- `CMP_TierCap_GrowthResilience_Easy`

Result:
- proxy win rate: **25.0%**
- wins/played: **5/20**
- avg living cells: **258.4**
- avg dead cells: **195.5**

### Campaign6

Lineup:
- `AI7`
- `AI8`
- `AI9`
- `CMP_Bloom_CreepingNecro_Medium`

Result:
- proxy win rate: **25.0%**
- wins/played: **5/20**
- avg living cells: **665.7**
- avg dead cells: **447.9**

### Campaign7

Initial attempt with `CMP_Bloom_BeaconRegression_Medium` was too sharp for this slot. Final lineup softens the tail by reintroducing the easy tier-cap mold.

Final lineup:
- `AI7`
- `AI8`
- `AI9`
- `CMP_Economy_KillReclaim_Medium`
- `CMP_Bloom_CreepingNecro_Medium`
- `CMP_TierCap_GrowthResilience_Easy`

Result:
- proxy win rate: **5.0%**
- wins/played: **1/20**
- avg living cells: **659.7**
- avg dead cells: **409.4**

### Campaign8

Initial attempt with `CMP_Control_AnabolicFirst_Hard` collapsed the proxy to 0/20. Final lineup keeps the modern roster but stays medium-only.

Final lineup:
- `AI8`
- `AI9`
- `AI11`
- `CMP_Economy_KillReclaim_Medium`
- `CMP_Bloom_CreepingNecro_Medium`
- `CMP_Bloom_BeaconRegression_Medium`

Result:
- proxy win rate: **10.0%**
- wins/played: **2/20**
- avg living cells: **696.0**
- avg dead cells: **585.9**

### Campaign9

Initial attempt with two hard molds was too punishing. Final lineup uses four modern medium molds and no hard entries.

Final lineup:
- `AI9`
- `AI11`
- `CMP_Economy_KillReclaim_Medium`
- `CMP_Bloom_CreepingNecro_Medium`
- `CMP_Bloom_BeaconRegression_Medium`
- `CMP_Bloom_AnabolicRegression_Medium`

Result:
- proxy win rate: **5.0%**
- wins/played: **1/20**
- avg living cells: **755.1**
- avg dead cells: **617.5**

### Campaign10

Initial attempt including `AI1` was too spiky. Final lineup removes the elite legacy boss while still introducing two hard curated molds.

Final lineup:
- `AI11`
- `CMP_Economy_KillReclaim_Medium`
- `CMP_Bloom_CreepingNecro_Medium`
- `CMP_Bloom_BeaconRegression_Medium`
- `CMP_Control_AnabolicFirst_Hard`
- `CMP_Economy_LateSpike_Hard`

Result:
- proxy win rate: **10.0%**
- wins/played: **2/20**
- avg living cells: **984.2**
- avg dead cells: **819.0**

## Takeaways

- The important structural fix was promoting the modern curated molds into the real `Campaign` roster; without that, the harness could not validate the intended campaign identities.
- Hard molds arrive too early if introduced before Campaign10 in this pass; even one hard mold at Campaign8 drove the proxy to `0/20`.
- The medium roster is now actually wired into campaign presets, but Campaign7 and Campaign9 still look slightly overtuned for the safe baseline and are good candidates for another pass if we want a smoother progression.

## Early-campaign continuation — reclaim/surge easy mold

Follow-up goal: add a new easy campaign-safe mold around Jake's suggested plan (`Necrohyphal Infiltration` max first, then `Hyphal Surge`), validate it on the same safe-proxy harness, and keep pushing the early Campaign0-4 curve toward a less erratic ramp.

### Implementation changes

- Added `CMP_Reclaim_InfiltrationSurge_Easy` to `FungusToast.Core/AI/AIRoster.cs`.
- Strategy shape:
  - `prioritizeHighTier: true`
  - `economyBias: MinorEconomy`
  - target goals: `Necrohyphal Infiltration` to `GameBalance.NecrohyphalInfiltrationMaxLevel`, then `Hyphal Surge` to `GameBalance.HyphalSurgeMaxLevel`
  - preferred mycovariant categories: `Reclamation`, then `Growth`
- Marked it as campaign `Easy`, `Training`, `Weak`, `Active`.
- Extended `scripts/run_campaign_balance.py` so pooled presets resolve a deterministic lineup from `aiStrategyPool` using the same stable-hash / seed flow as `CampaignController`.
- Added the new mold to the `Campaign1` (`15x15 1 AI`) authored pool.
- Replaced `AI12` with `CMP_Reclaim_InfiltrationSurge_Easy` in `Campaign4` (`40x40 4 AI`) after a direct swap screen.

### Exact authored Campaign0-4 screen

Command pattern:

```bash
python3 scripts/run_campaign_balance.py --games 20 --seed 20260327 --level <0-4>
```

Results (`TST_CampaignPlayer_SafeBaseline` proxy):

| Level | Authored lineup resolved by harness | Proxy result |
|---|---|---|
| Campaign0 | `TST_Training_ResilientMycelium` | `90.0%` (`18/20`), avg living `50.8`, avg dead `14.9` |
| Campaign1 | `TST_Training_Overextender` (pooled level resolved to this pick for seed `20260327`) | `60.0%` (`12/20`), avg living `69.4`, avg dead `20.9` |
| Campaign2 | `TST_Training_ResilientMycelium`, `TST_Training_Overextender` | `65.0%` (`13/20`), avg living `105.9`, avg dead `32.5` |
| Campaign3 | `TST_Training_ResilientMycelium`, `TST_Training_Overextender`, `TST_Training_ToxicTurtle` | `55.0%` (`11/20`), avg living `166.9`, avg dead `113.9` |
| Campaign4 (before swap) | `TST_Training_ResilientMycelium`, `TST_Training_Overextender`, `TST_Training_ToxicTurtle`, `AI12` | `5.0%` (`1/20`), avg living `172.2`, avg dead `133.8` |

Immediate read: the first four levels are noisy but climb reasonably; the real outlier was Campaign4, where `AI12` turned the nutrient-introduction board into a brick wall.

### Direct screens for the new mold

Commands:

```bash
dotnet run --project FungusToast.Simulation/FungusToast.Simulation.csproj -- --games 20 --width 15 --height 15 --strategy-set Campaign --strategy-names TST_CampaignPlayer_SafeBaseline,CMP_Reclaim_InfiltrationSurge_Easy --seed 20260421 --experiment-id cmp_reclaim_infiltrationsurge_w15_g20_seed20260421 --no-keyboard --players 2 --no-nutrient-patches

dotnet run --project FungusToast.Simulation/FungusToast.Simulation.csproj -- --games 20 --width 20 --height 20 --strategy-set Campaign --strategy-names TST_CampaignPlayer_SafeBaseline,TST_Training_ResilientMycelium,CMP_Reclaim_InfiltrationSurge_Easy --seed 20260422 --experiment-id cmp_reclaim_infiltrationsurge_w20_g20_seed20260422 --no-keyboard --players 3 --no-nutrient-patches

dotnet run --project FungusToast.Simulation/FungusToast.Simulation.csproj -- --games 20 --width 40 --height 40 --strategy-set Campaign --strategy-names TST_CampaignPlayer_SafeBaseline,TST_Training_ResilientMycelium,TST_Training_Overextender,TST_Training_ToxicTurtle,CMP_Reclaim_InfiltrationSurge_Easy --seed 20260423 --experiment-id cmp_reclaim_infiltrationsurge_campaign4swap_w40_g20_seed20260423 --no-keyboard --players 5
```

Results:

| Scenario | Lineup | Proxy result |
|---|---|---|
| 15x15 duel | `CMP_Reclaim_InfiltrationSurge_Easy` | `85.0%` (`17/20`), avg living `103.2`, avg dead `29.2` |
| 20x20 early skirmish | `TST_Training_ResilientMycelium`, `CMP_Reclaim_InfiltrationSurge_Easy` | `95.0%` (`19/20`), avg living `146.2`, avg dead `39.5` |
| 40x40 Campaign4 swap | `TST_Training_ResilientMycelium`, `TST_Training_Overextender`, `TST_Training_ToxicTurtle`, `CMP_Reclaim_InfiltrationSurge_Easy` | `35.0%` (`7/20`), avg living `201.7`, avg dead `150.6` |

Takeaways:

- `CMP_Reclaim_InfiltrationSurge_Easy` is clearly an *easy* mold, not a Campaign2 pressure piece; it is too soft for the 20x20 two-AI board.
- That softness is useful on Campaign1 pool duty and as a direct replacement for `AI12` on Campaign4, where the old lineup was much harsher than the surrounding curve.
- Best current placement from this pass: `Campaign1` pool member and `Campaign4` fixed easy anchor. Do **not** promote it upward into Campaign2/3 fixed lineups without a different surrounding mix.

## Roster expansion continuation — weak/easy variety + conservative medium aliases

Follow-up goal: keep expanding the curated campaign roster with 1-2 more truly weak/easy molds for the early duel pool plus named medium aliases that can start replacing raw legacy `AI7` / `AI8` / `AI11` IDs in the first-medium authored levels.

### Added molds

New curated entries added to `FungusToast.Core/AI/AIRoster.cs` in this pass:

- `CMP_Defense_ResilientShell_Easy`
- `CMP_Defense_ReclaimShell_Easy`
- `CMP_Surge_BeaconTempo_Medium`
- `CMP_Control_AnabolicRebirth_Medium`
- `CMP_Surge_GrowthTempo_Medium`

A stronger easy draft (`CMP_Attrition_ToxicTurtle_Easy`) was screened and rejected rather than kept in campaign pools.

### Authored placement changes

- `Campaign1` (`15x15 1 AI`) pool now includes `CMP_Defense_ResilientShell_Easy` and `CMP_Defense_ReclaimShell_Easy` for extra weak-only variety.
- Campaign5/6/7 authored boards now use `CMP_Surge_BeaconTempo_Medium` and `CMP_Control_AnabolicRebirth_Medium` in the slots previously occupied by `AI7` and `AI8`.
- Campaign8 now uses `CMP_Control_AnabolicRebirth_Medium` and `CMP_Surge_GrowthTempo_Medium` in the corresponding `AI8` / `AI11` slots.

### Validation commands

```bash
dotnet run --project FungusToast.Simulation/FungusToast.Simulation.csproj -- --games 20 --width 15 --height 15 --strategy-set Campaign --strategy-names TST_CampaignPlayer_SafeBaseline,CMP_Defense_ResilientShell_Easy --seed 20260327 --experiment-id cmp_defense_resilientshell_w15_g20_seed20260327 --no-keyboard --players 2 --no-nutrient-patches

dotnet run --project FungusToast.Simulation/FungusToast.Simulation.csproj -- --games 20 --width 15 --height 15 --strategy-set Campaign --strategy-names TST_CampaignPlayer_SafeBaseline,CMP_Defense_ReclaimShell_Easy --seed 20260327 --experiment-id cmp_defense_reclaimshell_w15_g20_seed20260327 --no-keyboard --players 2 --no-nutrient-patches

python3 scripts/run_campaign_balance.py --games 20 --seed 20260327 --level 5
python3 scripts/run_campaign_balance.py --games 20 --seed 20260327 --level 6
python3 scripts/run_campaign_balance.py --games 20 --seed 20260327 --level 8
```

### Results

#### Direct weak/easy screens

| Candidate | Proxy result |
|---|---|
| `CMP_Defense_ResilientShell_Easy` | `0.0%` (`0/20`), avg living `65.7`, avg dead `5.1` |
| `CMP_Defense_ReclaimShell_Easy` | `35.0%` (`7/20`), avg living `78.1`, avg dead `12.2` |
| `CMP_Attrition_ToxicTurtle_Easy` *(rejected)* | `65.0%` (`13/20`), avg living `64.7`, avg dead `16.4` |

#### Authored campaign screens after medium alias placement

| Level | Authored lineup resolved by harness | Proxy result |
|---|---|---|
| Campaign5 | `CMP_Surge_BeaconTempo_Medium`, `CMP_Control_AnabolicRebirth_Medium`, `AI9`, `CMP_Economy_KillReclaim_Medium`, `CMP_TierCap_GrowthResilience_Easy` | `30.0%` (`6/20`), avg living `272.9`, avg dead `181.4` |
| Campaign6 | `CMP_Surge_BeaconTempo_Medium`, `CMP_Control_AnabolicRebirth_Medium`, `AI9`, `CMP_Bloom_CreepingNecro_Medium` | `25.0%` (`5/20`), avg living `665.6`, avg dead `447.9` |
| Campaign8 | `CMP_Control_AnabolicRebirth_Medium`, `AI9`, `CMP_Surge_GrowthTempo_Medium`, `CMP_Economy_KillReclaim_Medium`, `CMP_Bloom_CreepingNecro_Medium`, `CMP_Bloom_BeaconRegression_Medium` | `10.0%` (`2/20`), avg living `694.8`, avg dead `584.5` |

Takeaways:

- `Campaign1` can safely gain more variety without sneaking in another pressure spike; the two defense molds stay in the weak/easy lane and the rejected toxin turtle does not.
- The first-medium band now has stable named `CMP_*` molds in real authored boards instead of raw `AI7` / `AI8` / `AI11` IDs, and the proxy results stayed in-family with prior conservative medium screens.
- `CMP_Surge_GrowthTempo_Medium` is clearly not an early-medium bully on Campaign8; it remains a safe conservative medium alias for the later part of the first-medium band.
- Guardrail preserved: `CMP_Bloom_FortifyMimic_Medium` still does not appear before Campaign9.

## Late-campaign continuation screen — Campaign11-14

Follow-up goal: continue the same safe-proxy tuning upward into the higher authored campaign levels without introducing molds earlier than the guardrails in `docs/CAMPAIGN_AI_CURATION.md`.

### Baseline screen of currently authored late levels

Command pattern:

```bash
python3 scripts/run_campaign_balance.py --games 10 --seed 20260327 --level <11-14>
```

Results:

| Level | Authored lineup at time of screen | Proxy result |
|---|---|---|
| Campaign11 | `AI1, AI2, AI3, AI7, AI8, AI9, AI11` | `20.0%` (`2/10`), avg living `1101.4`, avg dead `1039.1` |
| Campaign12 | `AI1, AI2, AI3, AI10, AI7, AI8, AI9` | `0.0%` (`0/10`), avg living `1196.8`, avg dead `982.2` |
| Campaign13 | `AI1, AI2, AI3, AI10, AI1, AI7, AI8` | `0.0%` (`0/10`), avg living `1191.4`, avg dead `1213.4` |
| Campaign14 | `AI1, AI2, AI3, AI10, AI1, AI2, AI7` | `0.0%` (`0/10`), avg living `1368.1`, avg dead `1227.9` |

Immediate read: Campaign11 is noisy but survivable in a short screen; Campaign12-14 are still a brick wall for the current safe proxy.

### Softened curated late-game candidate screens

#### Candidate set A

Commands:

```bash
dotnet run --project FungusToast.Simulation/FungusToast.Simulation.csproj -- --games 10 --width 140 --height 140 --strategy-set Campaign --strategy-names TST_CampaignPlayer_SafeBaseline,AI11,CMP_Economy_KillReclaim_Medium,CMP_Bloom_CreepingNecro_Medium,CMP_Bloom_AnabolicRegression_Medium,AI13,CMP_Control_AnabolicFirst_Hard,CMP_Economy_LateSpike_Hard --seed 20260401 --experiment-id campaign12_screen_a --no-keyboard

dotnet run --project FungusToast.Simulation/FungusToast.Simulation.csproj -- --games 10 --width 150 --height 150 --strategy-set Campaign --strategy-names TST_CampaignPlayer_SafeBaseline,CMP_Bloom_CreepingNecro_Medium,AI13,CMP_Control_AnabolicFirst_Hard,CMP_Economy_LateSpike_Hard,AI3,AI10,CMP_Bloom_CreepingRegression_Elite --seed 20260402 --experiment-id campaign13_screen_a --no-keyboard

dotnet run --project FungusToast.Simulation/FungusToast.Simulation.csproj -- --games 10 --width 160 --height 160 --strategy-set Campaign --strategy-names TST_CampaignPlayer_SafeBaseline,AI13,CMP_Control_AnabolicFirst_Hard,CMP_Economy_LateSpike_Hard,AI3,AI10,AI2,CMP_Bloom_CreepingRegression_Elite --seed 20260403 --experiment-id campaign14_screen_a --no-keyboard
```

Results:

| Level | Candidate lineup | Proxy result |
|---|---|---|
| Campaign12 A | `AI11, CMP_Economy_KillReclaim_Medium, CMP_Bloom_CreepingNecro_Medium, CMP_Bloom_AnabolicRegression_Medium, AI13, CMP_Control_AnabolicFirst_Hard, CMP_Economy_LateSpike_Hard` | `0.0%` (`0/10`), avg living `1115.3`, avg dead `810.8` |
| Campaign13 A | `CMP_Bloom_CreepingNecro_Medium, AI13, CMP_Control_AnabolicFirst_Hard, CMP_Economy_LateSpike_Hard, AI3, AI10, CMP_Bloom_CreepingRegression_Elite` | `0.0%` (`0/10`), avg living `1239.0`, avg dead `983.7` |
| Campaign14 A | `AI13, CMP_Control_AnabolicFirst_Hard, CMP_Economy_LateSpike_Hard, AI3, AI10, AI2, CMP_Bloom_CreepingRegression_Elite` | `0.0%` (`0/10`), avg living `1392.6`, avg dead `1088.6` |

#### Candidate set B

Commands:

```bash
dotnet run --project FungusToast.Simulation/FungusToast.Simulation.csproj -- --games 10 --width 140 --height 140 --strategy-set Campaign --strategy-names TST_CampaignPlayer_SafeBaseline,AI11,CMP_Economy_KillReclaim_Medium,CMP_Bloom_CreepingNecro_Medium,CMP_Bloom_BeaconRegression_Medium,CMP_Bloom_AnabolicRegression_Medium,AI13,CMP_Control_AnabolicFirst_Hard --seed 20260412 --experiment-id campaign12_screen_b --no-keyboard

dotnet run --project FungusToast.Simulation/FungusToast.Simulation.csproj -- --games 10 --width 150 --height 150 --strategy-set Campaign --strategy-names TST_CampaignPlayer_SafeBaseline,AI11,CMP_Economy_KillReclaim_Medium,CMP_Bloom_CreepingNecro_Medium,CMP_Bloom_BeaconRegression_Medium,AI13,CMP_Control_AnabolicFirst_Hard,CMP_Economy_LateSpike_Hard --seed 20260413 --experiment-id campaign13_screen_b --no-keyboard

dotnet run --project FungusToast.Simulation/FungusToast.Simulation.csproj -- --games 10 --width 160 --height 160 --strategy-set Campaign --strategy-names TST_CampaignPlayer_SafeBaseline,CMP_Economy_KillReclaim_Medium,CMP_Bloom_CreepingNecro_Medium,CMP_Bloom_AnabolicRegression_Medium,AI13,CMP_Control_AnabolicFirst_Hard,CMP_Economy_LateSpike_Hard,AI2 --seed 20260414 --experiment-id campaign14_screen_b --no-keyboard
```

Results:

| Level | Candidate lineup | Proxy result |
|---|---|---|
| Campaign12 B | `AI11, CMP_Economy_KillReclaim_Medium, CMP_Bloom_CreepingNecro_Medium, CMP_Bloom_BeaconRegression_Medium, CMP_Bloom_AnabolicRegression_Medium, AI13, CMP_Control_AnabolicFirst_Hard` | `0.0%` (`0/10`), avg living `1001.0`, avg dead `888.4` |
| Campaign13 B | `AI11, CMP_Economy_KillReclaim_Medium, CMP_Bloom_CreepingNecro_Medium, CMP_Bloom_BeaconRegression_Medium, AI13, CMP_Control_AnabolicFirst_Hard, CMP_Economy_LateSpike_Hard` | `0.0%` (`0/10`), avg living `1181.4`, avg dead `1087.5` |
| Campaign14 B | `CMP_Economy_KillReclaim_Medium, CMP_Bloom_CreepingNecro_Medium, CMP_Bloom_AnabolicRegression_Medium, AI13, CMP_Control_AnabolicFirst_Hard, CMP_Economy_LateSpike_Hard, AI2` | `0.0%` (`0/10`), avg living `1424.5`, avg dead `1137.5` |

### Current blocker

Even after removing duplicate elite anchors and replacing several legacy late-game molds with curated medium/hard mixes that respect the documented earliest-introduction guardrails, **every tested Campaign12-14 lineup still left `TST_CampaignPlayer_SafeBaseline` at `0/10`**.

That means the next useful step is probably **not** more blind preset swapping. The stronger hypotheses are:

1. the current safe proxy is simply below the power floor needed for `8-player` late-campaign fields on `140x140+` boards,
2. fixed-slot proxy testing at these sizes is hiding a survivable-but-low win rate that would need a larger sample to reveal, or
3. late-campaign target success bands need to explicitly allow `~0-5%` for the current proxy instead of assuming a nonzero rate is required.

Because of that, I did **not** rewrite Campaign11-14 assets in this continuation pass. The evidence says we need either a proxy-strength review or a design decision on late-campaign target bands before further authored preset churn is likely to pay off.

## Early/medium pool continuation — curated underused molds

Follow-up goal: keep filling the actual early/medium authored campaign pools with the newer `CMP_*` molds that were already in `AIRoster` but not yet well placed in board presets.

### Direct characterization screens

Commands:

```bash
dotnet run --project FungusToast.Simulation/FungusToast.Simulation.csproj -- --games 20 --width 15 --height 15 --strategy-set Campaign --strategy-names TST_CampaignPlayer_SafeBaseline,CMP_Reclaim_Scavenger_Easy --seed 20260430 --experiment-id cmp_reclaim_scavenger_w15_g20_seed20260430 --no-keyboard --players 2 --no-nutrient-patches

dotnet run --project FungusToast.Simulation/FungusToast.Simulation.csproj -- --games 20 --width 15 --height 15 --strategy-set Campaign --strategy-names TST_CampaignPlayer_SafeBaseline,CMP_Surge_Pulsar_Easy --seed 20260431 --experiment-id cmp_surge_pulsar_w15_g20_seed20260431 --no-keyboard --players 2 --no-nutrient-patches

dotnet run --project FungusToast.Simulation/FungusToast.Simulation.csproj -- --games 20 --width 50 --height 50 --strategy-set Campaign --strategy-names TST_CampaignPlayer_SafeBaseline,AI7,AI8,AI9,CMP_Economy_KillReclaim_Medium,CMP_Growth_Pressure_Medium --seed 20260432 --experiment-id cmp_growth_pressure_campaign5swap_w50_g20_seed20260432 --no-keyboard

dotnet run --project FungusToast.Simulation/FungusToast.Simulation.csproj -- --games 20 --width 100 --height 100 --strategy-set Campaign --strategy-names TST_CampaignPlayer_SafeBaseline,AI8,AI9,AI11,CMP_Economy_KillReclaim_Medium,CMP_Bloom_CreepingNecro_Medium,CMP_Bloom_FortifyMimic_Medium --seed 20260433 --experiment-id cmp_bloom_fortifymimic_campaign8_w100_g20_seed20260433 --no-keyboard
```

Results (`TST_CampaignPlayer_SafeBaseline` proxy):

| Scenario | Lineup / candidate | Proxy result |
|---|---|---|
| 15x15 duel | `CMP_Reclaim_Scavenger_Easy` | `55.0%` (`11/20`), avg living `93.3`, avg dead `44.6` |
| 15x15 duel | `CMP_Surge_Pulsar_Easy` | `70.0%` (`14/20`), avg living `91.3`, avg dead `24.8` |
| 50x50 Campaign5 swap | `... , CMP_Growth_Pressure_Medium` | `0.0%` (`0/20`), avg living `225.7`, avg dead `188.9` |
| 100x100 Campaign8 authored variant | `... , CMP_Bloom_FortifyMimic_Medium` | `0.0%` (`0/20`), avg living `727.5`, avg dead `606.1` |

Immediate read:

- `CMP_Reclaim_Scavenger_Easy` is a real usable easy opponent, not just a roster placeholder.
- `CMP_Surge_Pulsar_Easy` is softer than that but still reasonable for an early pooled duel slot.
- `CMP_Growth_Pressure_Medium` is too punishing for first-medium placement at `Campaign5`.
- `CMP_Bloom_FortifyMimic_Medium` overshot the intended `Campaign8` band and should not be the first introduction there.

### Authored preset changes from this pass

- `Campaign1` (`15x15 1 AI`) pool expanded to include:
  - `CMP_Reclaim_Scavenger_Easy`
  - `CMP_Surge_Pulsar_Easy`
- `Campaign8` (`100x100 6 AI`) reverted from `CMP_Bloom_FortifyMimic_Medium` back to `CMP_Bloom_BeaconRegression_Medium`

### Confirmation screens after the asset changes

Commands:

```bash
python3 scripts/run_campaign_balance.py --games 20 --seed 20260327 --level 1
python3 scripts/run_campaign_balance.py --games 20 --seed 20260327 --level 8
python3 scripts/run_campaign_balance.py --games 20 --seed 20260327 --level 9
```

Resolved / authored results:

| Level | Resolved/authored lineup | Proxy result |
|---|---|---|
| Campaign1 | resolved to `TST_Training_Overextender` for seed `20260327` | `60.0%` (`12/20`), avg living `69.4`, avg dead `20.9` |
| Campaign8 | `AI8, AI9, AI11, CMP_Economy_KillReclaim_Medium, CMP_Bloom_CreepingNecro_Medium, CMP_Bloom_BeaconRegression_Medium` | `10.0%` (`2/20`), avg living `696.7`, avg dead `583.2` |
| Campaign9 | `AI9, AI11, CMP_Economy_KillReclaim_Medium, CMP_Bloom_CreepingNecro_Medium, CMP_Bloom_FortifyMimic_Medium, CMP_Bloom_AnabolicRegression_Medium` | `10.0%` (`2/20`), avg living `837.5`, avg dead `762.4` |

### Placement conclusions from this continuation

- **Promote now:** `CMP_Reclaim_Scavenger_Easy`, `CMP_Surge_Pulsar_Easy` as valid `Campaign1` pool members.
- **Hold back:** `CMP_Growth_Pressure_Medium` from `Campaign5`; it looks more like a later medium / hard-preview candidate.
- **Delay first introduction:** `CMP_Bloom_FortifyMimic_Medium` should not be introduced at `Campaign8`; keep it at `Campaign9+` for now if used at all.
- **Keep current softer Campaign8:** `CMP_Bloom_BeaconRegression_Medium` restored the level to the previously acceptable `10%` proxy band.
