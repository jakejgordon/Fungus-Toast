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
