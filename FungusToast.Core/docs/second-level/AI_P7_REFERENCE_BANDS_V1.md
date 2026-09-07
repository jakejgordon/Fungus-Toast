# AI P7 Reference Bands V1

**Status:** frozen measured evidence. This file records what was measured, not what
should be done about it; tuning decisions belong in their own slices with their own
evidence.

**Matrix:** `p7-solo-panel-v1` (`FungusToast.Simulation/Examples/calibration-matrix.v1.example.json`)
**Classifier:** `fungus-toast.ai-bands.v1`
**Panel:** the 19 `Proven` strategies solo play draws from
**Campaign:** 1,600 games across 7 conditions, 0 failures, 5,781 seconds
**Per-strategy evidence:** 145-195 games each, pooled across the 5 calibration contexts

Thresholds were frozen before the campaign finished, so they could not be drawn
around the results. Bands are distances from parity, placed on the interval bound
that argues against the band. Holdout contexts are measured and reported but never
pooled into the overall band.

## Known limitation

Lineups are drawn randomly per game, which buys unbiased opposition at the cost of
uneven exposure: observed counts ranged 18-39 per strategy per context against an
expected 31.6. Six strategy/context cells landed under the classifier's 25-game
floor and are reported as `TooFewGames` rather than placed. Overall bands are
unaffected. Raising `minimumGamesPerStrategy` above the floor, or adding repeats,
would buffer that variance.

- Classifier: `fungus-toast.ai-bands.v1`
- Panel: Proven (19 strategies)
- Bands are distances from parity: Elite above `1.25`, Hard above `1.05`, Easy below `0.95`, placed on the interval bound that argues against the band.
- Thresholds were frozen before the matrix finished running, so they could not be drawn around the results.

## Overall

| Strategy | Band | Pooled share | Games | Evidence | Contextual |
|---|---|---:|---:|---|---|
| TST_CampaignMirror_AI13_AnabolicFirst | Elite | 1.915 | 186 | Sufficient | — |
| TST_BalancedControl_AnabolicFirst | Elite | 1.866 | 182 | Sufficient | — |
| Anabolic>Grow>CatabR>PutreRegen | Elite | 1.542 | 189 | Sufficient | crowded.large.square.rectangle.generated.180x180, smalltable.small.tall.rectangle.generated.60x100 |
| Grow>Kill>Reclaim(Econ/Reclaim) | Hard | 1.373 | 181 | Sufficient | smalltable.large.square.rectangle.generated.180x180 |
| Grow>Kill>Reclaim(Econ) | Hard | 1.372 | 188 | Sufficient | crowded.large.square.rectangle.generated.180x180, smalltable.medium.square.rectangle.generated.120x120, duel.medium.wide.rectangle.generated.140x100 |
| TST_BalancedControl_MaxEconomy | Hard | 1.347 | 177 | Sufficient | duel.small.square.rectangle.generated.80x80, smalltable.large.square.rectangle.generated.180x180, smalltable.small.tall.rectangle.generated.60x100 |
| TST_CampaignMirror_AI13_BalancedControl_MaxEconomy | Hard | 1.271 | 180 | Sufficient | duel.small.square.rectangle.generated.80x80, duel.medium.wide.rectangle.generated.140x100, smalltable.small.tall.rectangle.generated.60x100 |
| Grow>Defend>Kill | Normal | 1.088 | 173 | Sufficient | smalltable.small.tall.rectangle.generated.60x100 |
| Filament Regrowth | Normal | 1.058 | 187 | Sufficient | — |
| Mutate>Grow>Kill(Max Econ) | Normal | 0.975 | 180 | Sufficient | crowded.large.square.rectangle.generated.180x180 |
| Creeping>Necrosporulation | Normal | 0.965 | 190 | Sufficient | — |
| Power Mutations Max Econ | Normal | 0.943 | 145 | Sufficient | crowded.large.square.rectangle.generated.180x180 |
| TST_AnabolicCreepingNecroRegressionCascade | Easy | 0.705 | 166 | Sufficient | duel.medium.square.rectangle.generated.120x120, duel.small.square.rectangle.generated.80x80, duel.medium.wide.rectangle.generated.140x100 |
| TST_CampaignPlayer_SafeBaseline | Easy | 0.620 | 173 | Sufficient | duel.medium.square.rectangle.generated.120x120, duel.small.square.rectangle.generated.80x80, duel.medium.wide.rectangle.generated.140x100 |
| Grow>Mutate>Kill(Max Econ) | Easy | 0.492 | 173 | Sufficient | — |
| TST_CreepingNecroRegressionCascade | Easy | 0.490 | 183 | Sufficient | — |
| TST_AnabolicBeaconNecroRegressionCascade | Easy | 0.366 | 195 | Sufficient | — |
| Growth/Resilience | Easy | 0.333 | 185 | Sufficient | — |
| Best_MaxEcon_Surge10_HyphalSurge | Easy | 0.153 | 167 | Sufficient | — |

## Authored label vs measurement

- **TST_CampaignMirror_AI13_AnabolicFirst** is authored `Strong`, but measures **Elite** at 1.915.
- **TST_BalancedControl_AnabolicFirst** is authored `Strong`, but measures **Elite** at 1.866.
- **Filament Regrowth** is authored `Strong`, but measures **Normal** at 1.058.
- **Power Mutations Max Econ** is authored `Spike`, but measures **Normal** at 0.943.
- **TST_CreepingNecroRegressionCascade** is authored `Strong` and used as a **Boss**, but measures **Easy** at 0.490.
- **Best_MaxEcon_Surge10_HyphalSurge** is authored `Spike`, but measures **Easy** at 0.153.

## By context

Holdout contexts are measured and shown but never pooled into the overall band, since they exist to check it.

| Strategy | Context | Band | Share | 95% CI | Games |
|---|---|---|---:|---|---:|
| TST_CampaignMirror_AI13_AnabolicFirst | crowded.large.square.rectangle.generated.180x180 | Elite | 2.325 | 2.071–2.580 | 32 |
| TST_CampaignMirror_AI13_AnabolicFirst | duel.medium.square.rectangle.generated.120x120 | Elite | 1.580 | 1.454–1.705 | 29 |
| TST_CampaignMirror_AI13_AnabolicFirst | duel.small.square.rectangle.generated.80x80 | Elite | 1.516 | 1.419–1.613 | 32 |
| TST_CampaignMirror_AI13_AnabolicFirst | smalltable.large.square.rectangle.generated.180x180 | Elite | 2.140 | 1.938–2.342 | 45 |
| TST_CampaignMirror_AI13_AnabolicFirst | smalltable.medium.square.rectangle.generated.120x120 | Elite | 1.897 | 1.731–2.063 | 48 |
| TST_CampaignMirror_AI13_AnabolicFirst | duel.medium.wide.rectangle.generated.140x100 *(holdout)* | Elite | 1.577 | 1.425–1.730 | 25 |
| TST_CampaignMirror_AI13_AnabolicFirst | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Elite | 1.531 | 1.437–1.625 | 44 |
| TST_BalancedControl_AnabolicFirst | crowded.large.square.rectangle.generated.180x180 | Elite | 2.273 | 1.984–2.563 | 33 |
| TST_BalancedControl_AnabolicFirst | duel.medium.square.rectangle.generated.120x120 | Elite | 1.588 | 1.487–1.689 | 43 |
| TST_BalancedControl_AnabolicFirst | duel.small.square.rectangle.generated.80x80 | Elite | 1.431 | 1.332–1.530 | 26 |
| TST_BalancedControl_AnabolicFirst | smalltable.large.square.rectangle.generated.180x180 | Elite | 2.095 | 1.820–2.371 | 39 |
| TST_BalancedControl_AnabolicFirst | smalltable.medium.square.rectangle.generated.120x120 | Elite | 1.886 | 1.704–2.069 | 41 |
| TST_BalancedControl_AnabolicFirst | duel.medium.wide.rectangle.generated.140x100 *(holdout)* | Elite | 1.532 | 1.412–1.651 | 35 |
| TST_BalancedControl_AnabolicFirst | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Elite | 1.617 | 1.487–1.747 | 45 |
| Anabolic>Grow>CatabR>PutreRegen | crowded.large.square.rectangle.generated.180x180 | Hard | 1.482 | 1.238–1.726 | 37 |
| Anabolic>Grow>CatabR>PutreRegen | duel.medium.square.rectangle.generated.120x120 | Elite | 1.432 | 1.279–1.585 | 37 |
| Anabolic>Grow>CatabR>PutreRegen | duel.small.square.rectangle.generated.80x80 | Elite | 1.480 | 1.338–1.622 | 32 |
| Anabolic>Grow>CatabR>PutreRegen | smalltable.large.square.rectangle.generated.180x180 | Elite | 1.683 | 1.430–1.936 | 42 |
| Anabolic>Grow>CatabR>PutreRegen | smalltable.medium.square.rectangle.generated.120x120 | Elite | 1.601 | 1.396–1.805 | 41 |
| Anabolic>Grow>CatabR>PutreRegen | duel.medium.wide.rectangle.generated.140x100 *(holdout)* | Elite | 1.488 | 1.327–1.649 | 40 |
| Anabolic>Grow>CatabR>PutreRegen | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Hard | 1.292 | 1.169–1.415 | 46 |
| Grow>Kill>Reclaim(Econ/Reclaim) | crowded.large.square.rectangle.generated.180x180 | Hard | 1.364 | 1.183–1.545 | 31 |
| Grow>Kill>Reclaim(Econ/Reclaim) | duel.medium.square.rectangle.generated.120x120 | Hard | 1.225 | 1.077–1.373 | 31 |
| Grow>Kill>Reclaim(Econ/Reclaim) | duel.small.square.rectangle.generated.80x80 | Hard | 1.325 | 1.199–1.452 | 30 |
| Grow>Kill>Reclaim(Econ/Reclaim) | smalltable.large.square.rectangle.generated.180x180 | Elite | 1.637 | 1.438–1.837 | 48 |
| Grow>Kill>Reclaim(Econ/Reclaim) | smalltable.medium.square.rectangle.generated.120x120 | Hard | 1.215 | 1.079–1.351 | 41 |
| Grow>Kill>Reclaim(Econ/Reclaim) | duel.medium.wide.rectangle.generated.140x100 *(holdout)* | Hard | 1.261 | 1.107–1.415 | 41 |
| Grow>Kill>Reclaim(Econ/Reclaim) | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Hard | 1.274 | 1.175–1.373 | 42 |
| Grow>Kill>Reclaim(Econ) | crowded.large.square.rectangle.generated.180x180 | Elite | 1.519 | 1.336–1.702 | 30 |
| Grow>Kill>Reclaim(Econ) | duel.medium.square.rectangle.generated.120x120 | Hard | 1.324 | 1.190–1.458 | 42 |
| Grow>Kill>Reclaim(Econ) | duel.small.square.rectangle.generated.80x80 | Hard | 1.307 | 1.183–1.432 | 33 |
| Grow>Kill>Reclaim(Econ) | smalltable.large.square.rectangle.generated.180x180 | Hard | 1.315 | 1.125–1.506 | 38 |
| Grow>Kill>Reclaim(Econ) | smalltable.medium.square.rectangle.generated.120x120 | Elite | 1.412 | 1.255–1.569 | 45 |
| Grow>Kill>Reclaim(Econ) | duel.medium.wide.rectangle.generated.140x100 *(holdout)* | Elite | 1.468 | 1.297–1.640 | 30 |
| Grow>Kill>Reclaim(Econ) | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Hard | 1.198 | 1.127–1.270 | 46 |
| TST_BalancedControl_MaxEconomy | crowded.large.square.rectangle.generated.180x180 | Hard | 1.380 | 1.160–1.599 | 33 |
| TST_BalancedControl_MaxEconomy | duel.medium.square.rectangle.generated.120x120 | Hard | 1.235 | 1.055–1.416 | 27 |
| TST_BalancedControl_MaxEconomy | duel.small.square.rectangle.generated.80x80 | Normal | 1.087 | 0.926–1.247 | 28 |
| TST_BalancedControl_MaxEconomy | smalltable.large.square.rectangle.generated.180x180 | Elite | 1.599 | 1.342–1.856 | 41 |
| TST_BalancedControl_MaxEconomy | smalltable.medium.square.rectangle.generated.120x120 | Hard | 1.324 | 1.169–1.479 | 48 |
| TST_BalancedControl_MaxEconomy | duel.medium.wide.rectangle.generated.140x100 *(holdout)* | Hard | 1.282 | 1.103–1.462 | 35 |
| TST_BalancedControl_MaxEconomy | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Normal | 0.991 | 0.905–1.077 | 49 |
| TST_CampaignMirror_AI13_BalancedControl_MaxEconomy | crowded.large.square.rectangle.generated.180x180 | Hard | 1.283 | 1.078–1.487 | 31 |
| TST_CampaignMirror_AI13_BalancedControl_MaxEconomy | duel.medium.square.rectangle.generated.120x120 | Hard | 1.216 | 1.071–1.361 | 38 |
| TST_CampaignMirror_AI13_BalancedControl_MaxEconomy | duel.small.square.rectangle.generated.80x80 | Normal | 1.101 | 0.955–1.247 | 31 |
| TST_CampaignMirror_AI13_BalancedControl_MaxEconomy | smalltable.large.square.rectangle.generated.180x180 | Hard | 1.454 | 1.233–1.675 | 41 |
| TST_CampaignMirror_AI13_BalancedControl_MaxEconomy | smalltable.medium.square.rectangle.generated.120x120 | Hard | 1.256 | 1.104–1.407 | 39 |
| TST_CampaignMirror_AI13_BalancedControl_MaxEconomy | duel.medium.wide.rectangle.generated.140x100 *(holdout)* | Normal | 1.200 | 1.019–1.381 | 34 |
| TST_CampaignMirror_AI13_BalancedControl_MaxEconomy | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Normal | 0.997 | 0.900–1.094 | 25 |
| Grow>Defend>Kill | crowded.large.square.rectangle.generated.180x180 | Normal | 0.944 | 0.843–1.046 | 33 |
| Grow>Defend>Kill | duel.medium.square.rectangle.generated.120x120 | Normal | 1.116 | 0.898–1.335 | 25 |
| Grow>Defend>Kill | duel.small.square.rectangle.generated.80x80 | Normal | 1.121 | 0.970–1.272 | 26 |
| Grow>Defend>Kill | smalltable.large.square.rectangle.generated.180x180 | Normal | 1.115 | 0.941–1.288 | 41 |
| Grow>Defend>Kill | smalltable.medium.square.rectangle.generated.120x120 | Normal | 1.133 | 1.005–1.261 | 48 |
| Grow>Defend>Kill | duel.medium.wide.rectangle.generated.140x100 *(holdout)* | Normal | 1.026 | 0.838–1.214 | 32 |
| Grow>Defend>Kill | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Hard | 1.207 | 1.116–1.298 | 42 |
| Filament Regrowth | crowded.large.square.rectangle.generated.180x180 | Normal | 0.904 | 0.774–1.033 | 38 |
| Filament Regrowth | duel.medium.square.rectangle.generated.120x120 | Normal | 0.962 | 0.787–1.136 | 34 |
| Filament Regrowth | duel.small.square.rectangle.generated.80x80 | Normal | 1.155 | 0.990–1.320 | 34 |
| Filament Regrowth | smalltable.large.square.rectangle.generated.180x180 | Normal | 1.241 | 1.033–1.449 | 37 |
| Filament Regrowth | smalltable.medium.square.rectangle.generated.120x120 | Normal | 1.038 | 0.913–1.162 | 44 |
| Filament Regrowth | duel.medium.wide.rectangle.generated.140x100 *(holdout)* | — | 1.084 | 0.860–1.308 | 23 |
| Filament Regrowth | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Normal | 0.968 | 0.905–1.030 | 36 |
| Mutate>Grow>Kill(Max Econ) | crowded.large.square.rectangle.generated.180x180 | Easy | 0.708 | 0.581–0.835 | 33 |
| Mutate>Grow>Kill(Max Econ) | duel.medium.square.rectangle.generated.120x120 | Normal | 1.200 | 0.954–1.447 | 38 |
| Mutate>Grow>Kill(Max Econ) | duel.small.square.rectangle.generated.80x80 | Normal | 1.181 | 0.998–1.363 | 39 |
| Mutate>Grow>Kill(Max Econ) | smalltable.large.square.rectangle.generated.180x180 | Normal | 0.887 | 0.614–1.160 | 38 |
| Mutate>Grow>Kill(Max Econ) | smalltable.medium.square.rectangle.generated.120x120 | Normal | 0.837 | 0.725–0.950 | 32 |
| Mutate>Grow>Kill(Max Econ) | duel.medium.wide.rectangle.generated.140x100 *(holdout)* | Normal | 1.095 | 0.813–1.378 | 28 |
| Mutate>Grow>Kill(Max Econ) | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Normal | 1.033 | 0.957–1.109 | 52 |
| Creeping>Necrosporulation | crowded.large.square.rectangle.generated.180x180 | Normal | 0.901 | 0.770–1.033 | 31 |
| Creeping>Necrosporulation | duel.medium.square.rectangle.generated.120x120 | Normal | 1.108 | 0.917–1.300 | 26 |
| Creeping>Necrosporulation | duel.small.square.rectangle.generated.80x80 | Normal | 0.995 | 0.844–1.146 | 31 |
| Creeping>Necrosporulation | smalltable.large.square.rectangle.generated.180x180 | Normal | 0.944 | 0.812–1.075 | 48 |
| Creeping>Necrosporulation | smalltable.medium.square.rectangle.generated.120x120 | Normal | 0.936 | 0.826–1.046 | 54 |
| Creeping>Necrosporulation | duel.medium.wide.rectangle.generated.140x100 *(holdout)* | Normal | 1.039 | 0.844–1.235 | 28 |
| Creeping>Necrosporulation | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Normal | 0.970 | 0.890–1.051 | 46 |
| Power Mutations Max Econ | crowded.large.square.rectangle.generated.180x180 | Easy | 0.785 | 0.650–0.919 | 25 |
| Power Mutations Max Econ | duel.medium.square.rectangle.generated.120x120 | — | 1.145 | 0.949–1.341 | 22 |
| Power Mutations Max Econ | duel.small.square.rectangle.generated.80x80 | Normal | 1.063 | 0.932–1.194 | 30 |
| Power Mutations Max Econ | smalltable.large.square.rectangle.generated.180x180 | Normal | 0.889 | 0.714–1.064 | 38 |
| Power Mutations Max Econ | smalltable.medium.square.rectangle.generated.120x120 | Normal | 0.874 | 0.741–1.007 | 30 |
| Power Mutations Max Econ | duel.medium.wide.rectangle.generated.140x100 *(holdout)* | — | 1.116 | 0.842–1.390 | 19 |
| Power Mutations Max Econ | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Normal | 1.005 | 0.948–1.061 | 43 |
| TST_AnabolicCreepingNecroRegressionCascade | crowded.large.square.rectangle.generated.180x180 | Easy | 0.638 | 0.582–0.693 | 33 |
| TST_AnabolicCreepingNecroRegressionCascade | duel.medium.square.rectangle.generated.120x120 | Normal | 0.868 | 0.601–1.136 | 25 |
| TST_AnabolicCreepingNecroRegressionCascade | duel.small.square.rectangle.generated.80x80 | Normal | 0.922 | 0.791–1.052 | 28 |
| TST_AnabolicCreepingNecroRegressionCascade | smalltable.large.square.rectangle.generated.180x180 | Easy | 0.516 | 0.465–0.567 | 38 |
| TST_AnabolicCreepingNecroRegressionCascade | smalltable.medium.square.rectangle.generated.120x120 | Easy | 0.689 | 0.630–0.748 | 42 |
| TST_AnabolicCreepingNecroRegressionCascade | duel.medium.wide.rectangle.generated.140x100 *(holdout)* | Normal | 0.798 | 0.642–0.954 | 30 |
| TST_AnabolicCreepingNecroRegressionCascade | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Easy | 0.814 | 0.741–0.887 | 42 |
| TST_CampaignPlayer_SafeBaseline | crowded.large.square.rectangle.generated.180x180 | Easy | 0.575 | 0.501–0.649 | 28 |
| TST_CampaignPlayer_SafeBaseline | duel.medium.square.rectangle.generated.120x120 | Normal | 0.758 | 0.550–0.966 | 38 |
| TST_CampaignPlayer_SafeBaseline | duel.small.square.rectangle.generated.80x80 | Normal | 0.811 | 0.668–0.953 | 33 |
| TST_CampaignPlayer_SafeBaseline | smalltable.large.square.rectangle.generated.180x180 | Easy | 0.382 | 0.293–0.470 | 41 |
| TST_CampaignPlayer_SafeBaseline | smalltable.medium.square.rectangle.generated.120x120 | Easy | 0.604 | 0.544–0.664 | 33 |
| TST_CampaignPlayer_SafeBaseline | duel.medium.wide.rectangle.generated.140x100 *(holdout)* | Normal | 0.796 | 0.576–1.016 | 39 |
| TST_CampaignPlayer_SafeBaseline | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Easy | 0.882 | 0.837–0.928 | 44 |
| Grow>Mutate>Kill(Max Econ) | crowded.large.square.rectangle.generated.180x180 | Easy | 0.406 | 0.380–0.433 | 31 |
| Grow>Mutate>Kill(Max Econ) | duel.medium.square.rectangle.generated.120x120 | Easy | 0.452 | 0.283–0.621 | 31 |
| Grow>Mutate>Kill(Max Econ) | duel.small.square.rectangle.generated.80x80 | Easy | 0.727 | 0.594–0.860 | 34 |
| Grow>Mutate>Kill(Max Econ) | smalltable.large.square.rectangle.generated.180x180 | Easy | 0.325 | 0.280–0.370 | 38 |
| Grow>Mutate>Kill(Max Econ) | smalltable.medium.square.rectangle.generated.120x120 | Easy | 0.549 | 0.491–0.607 | 39 |
| Grow>Mutate>Kill(Max Econ) | duel.medium.wide.rectangle.generated.140x100 *(holdout)* | Easy | 0.690 | 0.487–0.894 | 41 |
| Grow>Mutate>Kill(Max Econ) | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Easy | 0.772 | 0.727–0.818 | 39 |
| TST_CreepingNecroRegressionCascade | crowded.large.square.rectangle.generated.180x180 | Easy | 0.428 | 0.381–0.475 | 33 |
| TST_CreepingNecroRegressionCascade | duel.medium.square.rectangle.generated.120x120 | Easy | 0.528 | 0.301–0.756 | 28 |
| TST_CreepingNecroRegressionCascade | duel.small.square.rectangle.generated.80x80 | Easy | 0.717 | 0.589–0.846 | 31 |
| TST_CreepingNecroRegressionCascade | smalltable.large.square.rectangle.generated.180x180 | Easy | 0.364 | 0.317–0.410 | 46 |
| TST_CreepingNecroRegressionCascade | smalltable.medium.square.rectangle.generated.120x120 | Easy | 0.485 | 0.438–0.531 | 45 |
| TST_CreepingNecroRegressionCascade | duel.medium.wide.rectangle.generated.140x100 *(holdout)* | Easy | 0.552 | 0.407–0.698 | 36 |
| TST_CreepingNecroRegressionCascade | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Easy | 0.553 | 0.509–0.597 | 32 |
| TST_AnabolicBeaconNecroRegressionCascade | crowded.large.square.rectangle.generated.180x180 | Easy | 0.288 | 0.259–0.318 | 33 |
| TST_AnabolicBeaconNecroRegressionCascade | duel.medium.square.rectangle.generated.120x120 | Easy | 0.388 | 0.216–0.561 | 35 |
| TST_AnabolicBeaconNecroRegressionCascade | duel.small.square.rectangle.generated.80x80 | Easy | 0.498 | 0.367–0.630 | 38 |
| TST_AnabolicBeaconNecroRegressionCascade | smalltable.large.square.rectangle.generated.180x180 | Easy | 0.281 | 0.160–0.401 | 48 |
| TST_AnabolicBeaconNecroRegressionCascade | smalltable.medium.square.rectangle.generated.120x120 | Easy | 0.387 | 0.336–0.437 | 41 |
| TST_AnabolicBeaconNecroRegressionCascade | duel.medium.wide.rectangle.generated.140x100 *(holdout)* | Easy | 0.509 | 0.272–0.746 | 25 |
| TST_AnabolicBeaconNecroRegressionCascade | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Easy | 0.568 | 0.505–0.632 | 36 |
| Growth/Resilience | crowded.large.square.rectangle.generated.180x180 | — | 0.338 | 0.298–0.379 | 22 |
| Growth/Resilience | duel.medium.square.rectangle.generated.120x120 | Easy | 0.154 | 0.098–0.210 | 31 |
| Growth/Resilience | duel.small.square.rectangle.generated.80x80 | Easy | 0.496 | 0.350–0.641 | 32 |
| Growth/Resilience | smalltable.large.square.rectangle.generated.180x180 | Easy | 0.230 | 0.193–0.268 | 47 |
| Growth/Resilience | smalltable.medium.square.rectangle.generated.120x120 | Easy | 0.429 | 0.382–0.476 | 53 |
| Growth/Resilience | duel.medium.wide.rectangle.generated.140x100 *(holdout)* | — | 0.217 | 0.132–0.302 | 18 |
| Growth/Resilience | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Easy | 0.744 | 0.690–0.797 | 49 |
| Best_MaxEcon_Surge10_HyphalSurge | crowded.large.square.rectangle.generated.180x180 | Easy | 0.162 | 0.129–0.195 | 33 |
| Best_MaxEcon_Surge10_HyphalSurge | duel.medium.square.rectangle.generated.120x120 | — | 0.094 | 0.034–0.155 | 20 |
| Best_MaxEcon_Surge10_HyphalSurge | duel.small.square.rectangle.generated.80x80 | Easy | 0.249 | 0.174–0.324 | 32 |
| Best_MaxEcon_Surge10_HyphalSurge | smalltable.large.square.rectangle.generated.180x180 | Easy | 0.098 | 0.080–0.116 | 46 |
| Best_MaxEcon_Surge10_HyphalSurge | smalltable.medium.square.rectangle.generated.120x120 | Easy | 0.160 | 0.137–0.184 | 36 |
| Best_MaxEcon_Surge10_HyphalSurge | duel.medium.wide.rectangle.generated.140x100 *(holdout)* | Easy | 0.182 | 0.114–0.250 | 41 |
| Best_MaxEcon_Surge10_HyphalSurge | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Easy | 0.321 | 0.281–0.361 | 42 |

## Evidence gaps

- Filament Regrowth in duel.medium.wide.rectangle.generated.140x100: TooFewGames on 23 games.
- Power Mutations Max Econ in duel.medium.square.rectangle.generated.120x120: TooFewGames on 22 games.
- Power Mutations Max Econ in duel.medium.wide.rectangle.generated.140x100: TooFewGames on 19 games.
- Growth/Resilience in crowded.large.square.rectangle.generated.180x180: TooFewGames on 22 games.
- Growth/Resilience in duel.medium.wide.rectangle.generated.140x100: TooFewGames on 18 games.
- Best_MaxEcon_Surge10_HyphalSurge in duel.medium.square.rectangle.generated.120x120: TooFewGames on 20 games.

## Addendum 2026-09-07: why the weakest strategies underperform

### What the panel's builds look like

Observed under the shared characterization script, levels bought per category:

| Share | Strategy | Cap | Growth | Fungicide | Economy | Substrate |
|---:|---|---|---:|---:|---:|---:|
| 1.915 | TST_CampaignMirror_AI13_AnabolicFirst | Tier10 | 19 | 7 | 18 | 0 |
| 1.866 | TST_BalancedControl_AnabolicFirst | Tier10 | 19 | 7 | 18 | 0 |
| 0.705 | TST_AnabolicCreepingNecroRegressionCascade | Tier10 | 20 | **0** | 16 | 12 |
| 0.490 | TST_CreepingNecroRegressionCascade | Tier10 | 21 | **0** | 15 | 14 |
| 0.366 | TST_AnabolicBeaconNecroRegressionCascade | Tier10 | 20 | **0** | 14 | 16 |
| 0.333 | Growth/Resilience | Tier3 | 18 | **0** | 10 | 0 |
| 0.153 | Best_MaxEcon_Surge10_HyphalSurge | Tier4 | **5** | **0** | 21 | 0 |

### Correlations with measured strength

- **Fungicide levels: r = 0.792.** The 14 strategies that buy any average `1.202`; the 5 that
  buy none average `0.409`. Those five are the bottom of the panel.
- Growth levels r = 0.288, economy r = 0.133, resilience r = -0.148, total levels r = 0.173.
- The three `RegressionCascade` strategies each sink 12-16 levels into Substrate Ecology and
  buy no Fungicide at all; they occupy three of the bottom five places.
- Tier-capped strategies average `0.243` against `1.082` for uncapped.

These are correlations over a scripted build, not causes. Two were tested directly.

### Causal tests, both negative

Paired 50-game comparisons against a near-parity opponent, each changing exactly one gene on
`Best_MaxEcon_Surge10_HyphalSurge`:

- **Lifting the Tier4 cap to Tier10:** estimate `-0.0004`, 95% CI `[-0.011, +0.010]`,
  `not_supported`. The cap is not what breaks it. It is not even binding - the strategy already
  buys a Tier5 mutation through its goal path.
- **Changing MaxEconomy to Neutral:** estimate exactly `0.000`, and the two arms produced
  bit-identical games. The gene is inert for this strategy, because its economy spending comes
  from its goal path rather than from the fallback the bias governs.

The tier-cap arms did differ from each other, so the harness detects real differences; these
nulls are findings, not failures to measure.

### What actually explains it

`Best_MaxEcon_Surge10_HyphalSurge` has only two goals, `HyphalSurge` and `HyperadaptiveDrift`,
which pull it into Mycelial Surges and Genetic Drift. It ends up with **5 Growth levels against
16-21 for every other strategy in the panel**, and no Fungicide. Its behaviour is dominated by
that goal list; the structural genes around it barely matter, which is exactly what the two null
results show. Fixing it means changing what it is told to build, not which tier it may reach or
how it values economy.

The `RegressionCascade` family is a different shape of the same problem: adequate Growth, but
Substrate Ecology investment where the strong strategies buy Fungicide.

### Addendum: the Fungicide correlation is confounded, not causal

Tested at mutation level on 2026-09-07, and the result corrects the reading above.

**All seven Fungicide levels in every top strategy are one Tier-1 mutation, `Mycotoxin Tracer`
(id 2), and none of them declares it as a goal** - they all reach it through the fallback path.
So the category-level correlation was really a single-mutation correlation.

Ablating it is **not possible**, and the reason is the finding: `Mycotoxin Tracer` is a
prerequisite of `Necrosporulation`, which the strongest strategies target as a goal, and
`Catabolic Rebirth` depends on `Necrosporulation` in turn. The generator emits the ablation, marks
it unachievable, and the characterization gate rejects it as `ViolatedExclusions` - the strategy
buys the mutation anyway to reach what depends on it.

**So Fungicide investment is largely an artifact of the prerequisite tree, not an independent
choice.** Strategies that "buy Fungicide" are mostly strategies that target the
Necrosporulation / Catabolic Rebirth line; the Fungicide levels come along for free. The three
`RegressionCascade` strategies target `NecrophyticBloom` instead - a different mutation despite the
similar name - and so are never forced to buy `Mycotoxin Tracer` at all.

The honest conclusion is that `r = 0.792` measures *"targets the Necrosporulation line"*, and no
strategy in this panel buys `Mycotoxin Tracer` without needing it, so its own contribution cannot
be isolated by ablation within this panel. If the mutation's individual value matters, it needs a
purpose-built strategy pair that differs only in that mutation and shares no dependent goals.

### Tooling defect found while testing this

`PickBestTendrilMutation` substitutes the best-direction tendril for whichever tendril candidate
was offered, and did not re-apply the exclusion list - so an excluded tendril could be reacquired
through the substitution. That is the same bypass free upgrades had. No registered strategy
excludes a tendril, so no existing result changes, but it would have silently broken any ablation
of a tendril and any controlled treatment that removed one. Fixed with a regression test.

### Addendum 2026-09-07: the process failure this exposed, and the guard added

The Fungicide correction above is right, but it was reached the expensive way. The confound was
knowable from the prerequisite graph alone, before the correlation was computed, let alone before
an ablation was designed around it. `RequiresTransitively` had been in the codebase the whole time;
it was simply run *after* the hypothesis was formed instead of before.

Worse, this is not a one-off. Every root mutation and every low-tier gateway will produce the same
spurious signal, and any category dominated by one - Fungicide here is entirely `Mycotoxin Tracer` -
will inherit it. Left alone, the pattern recurs on every diagnostic pass.

`StructuralConfoundScreen` closes it. It partitions each mutation in an observed build into `Goal`
(chosen), `RequiredByGoal` (forced by the prerequisite closure of something the strategy targets),
or `Free` (bought discretionarily through the fallback path with nothing forcing it), then screens
a whole panel: a mutation whose owners were all forced into it is `StructurallyEntailed`, a mixed
one is `PartiallyEntailed`, and only `FreeVariation` may be reported as a lead.
`ExplainWhyNotEvidence` returns the sentence to write instead of the correlation.

Run against this panel it flags `Mycotoxin Tracer` immediately, which is what a regression test now
asserts. The deeper rule - recorded in the plan's *Structural confounding* section - is to correlate
over genes rather than builds, because goals and biases are what an author actually chose and
purchases are merely what the tree then forced.

### What the screen says about this panel

Run over the nineteen Proven strategies, the result is much larger than the Fungicide case that
prompted it: **17 of the 24 mutations the panel buys are not usable as purchase-level evidence.**

| Mutation | Category | Owners | Chose it | Forced | Verdict |
|---|---|---:|---:|---:|---|
| Mycelial Bloom | Growth | 19 | 2 | 17 | PartiallyEntailed |
| Homeostatic Harmony | CellularResilience | 19 | 1 | 18 | PartiallyEntailed |
| Mutator Phenotype | GeneticDrift | 19 | 4 | 15 | PartiallyEntailed |
| Tendril (each of four) | Growth | 17 | 2 | 15 | PartiallyEntailed |
| Mycotropic Induction | Growth | 16 | 3 | 13 | PartiallyEntailed |
| Mycotoxin Tracer | Fungicide | 14 | 1 | 13 | PartiallyEntailed |
| Adaptive Expression | GeneticDrift | 14 | 2 | 12 | PartiallyEntailed |
| Creeping Mold | Growth | 13 | 13 | 0 | **FreeVariation** |
| Chronoresilient Cytoplasm | CellularResilience | 10 | 2 | 8 | PartiallyEntailed |
| Anabolic Inversion | GeneticDrift | 10 | 7 | 3 | PartiallyEntailed |
| Aerated Frontier | SubstrateEcology | 8 | 6 | 2 | PartiallyEntailed |
| Chitin Fortification | MycelialSurges | 4 | 0 | 4 | StructurallyEntailed |
| Regenerative Hyphae | CellularResilience | 3 | 3 | 0 | **FreeVariation** |
| Mycotoxin Catabolism | GeneticDrift | 3 | 0 | 3 | StructurallyEntailed |
| Crustward Tropism | SubstrateEcology | 3 | 3 | 0 | **FreeVariation** |
| Detrital Enzymes | SubstrateEcology | 3 | 0 | 3 | StructurallyEntailed |

Three readings matter.

First, the mutations owned by *everyone* are the most confounded, not the least. `Mycelial Bloom`,
`Homeostatic Harmony`, and `Mutator Phenotype` appear in all nineteen builds and were forced in
15–18 of them. Universality is a symptom of being a gateway, not of being good, and a correlation
over any of them would have been the Fungicide error again.

Second, `Mycotoxin Tracer` is `PartiallyEntailed` rather than fully so: one panel member does
declare it as a goal, thirteen were dragged through it. That single free owner is the entire
independent evidence available for it in this panel, which is why ablation was the right instinct
and why it had nowhere to run.

Third, only four mutations vary freely across three or more owners, and `Creeping Mold` is the only
one with real sample. **That is the honest size of the purchase-level evidence base for this panel** —
and it is a reason to correlate over genes instead, not a reason to measure the confounded
variables harder.
