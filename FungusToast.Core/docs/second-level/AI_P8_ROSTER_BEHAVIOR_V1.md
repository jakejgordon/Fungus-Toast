# Roster behavior comparison — Proven

- Schema: `fungus-toast.roster-behavior.v1`
- Strategy set: `Proven` (19 strategies, 171 pairs)
- Commit: `cce2490145b2ca580df51112f3b61889cd413365`
- Simulation assembly SHA-256: `a52482e02d81da56d3e34a291f0cb10dbff0d531e774b059856c9b6a5a86ae6c`
- Core assembly SHA-256: `beb48699c11af157d5a522ccc6e9f09e21d012c13e5a35de2cabca24bf13f04a`
- Characterization: 12 rounds × 8 points, seed 6020301, 12x12 board

This artifact compares deterministic observed mutation spending, not authored theme or difficulty labels. High raw-build similarity identifies a redundancy-review candidate only; it is not evidence to retire either strategy. Counter claims and player-value claims require contextual matchup evidence.

## Exact raw-build matches

These pairs have identical observed mutation builds under this script. Their separate identities may still affect reactive behavior, draft choices, surge timing, or real-game outcomes, so they remain review candidates rather than automatic merge targets.

| First strategy | Second strategy | Category similarity |
|---|---|---:|
| Creeping>Necrosporulation | Filament Regrowth | 1.000 |
| Grow>Kill>Reclaim(Econ) | Grow>Kill>Reclaim(Econ/Reclaim) | 1.000 |
| TST_BalancedControl_AnabolicFirst | TST_CampaignMirror_AI13_AnabolicFirst | 1.000 |

## Observed profiles

| Strategy | Stable ID | Definition fingerprint | Mutation build | Category profile |
|---|---|---|---|---|
| Anabolic>Grow>CatabR>PutreRegen | `legacy.proven.anabolic-grow-catabr-putreregen.v1` | `4685a78de88009bf02df6b560c69029b2c30bcdd9d77766b2e705087b6b4f87b` | Mycelial Bloom 10; Homeostatic Harmony 5; Mycotoxin Tracer 7; Mutator Phenotype 10; Chronoresilient Cytoplasm 5; Tendril Northwest 1; Tendril Northeast 1; Tendril Southeast 1; Tendril Southwest 1; Adaptive Expression 5; Mycotropic Induction 2; Anabolic Inversion 3; Mycotoxin Catabolism 5 | Growth 16; CellularResilience 10; Fungicide 7; GeneticDrift 23 |
| Best_MaxEcon_Surge10_HyphalSurge | `legacy.proven.best-maxecon-surge10-hyphalsurge.v1` | `242b8e06d4ba703be81e4df4fab31ce66137999bb561aa2df574dc31b3f08f32` | Mycelial Bloom 5; Homeostatic Harmony 5; Mutator Phenotype 10; Adaptive Expression 5; Anabolic Inversion 3; Hyperadaptive Drift 3; Autolytic Surge 1; Chitin Fortification 1 | Growth 5; CellularResilience 5; GeneticDrift 21; MycelialSurges 2 |
| Creeping>Necrosporulation | `legacy.proven.creeping-necrosporulation.v1` | `68a881660e51b17cf1ace3aa0f9337bc970df7839c68da53bbe63d99194e294c` | Mycelial Bloom 10; Homeostatic Harmony 5; Mycotoxin Tracer 4; Mutator Phenotype 10; Chronoresilient Cytoplasm 5; Tendril Northwest 1; Tendril Northeast 1; Tendril Southeast 1; Tendril Southwest 1; Mycotropic Induction 3; Regenerative Hyphae 2; Creeping Mold 4 | Growth 21; CellularResilience 12; Fungicide 4; GeneticDrift 10 |
| Filament Regrowth | `legacy.proven.filament-regrowth.v1` | `d6c4e88eae2a339dd1ca5061636d25c59d63fe6a451eb4d565f6a2baf43f8161` | Mycelial Bloom 10; Homeostatic Harmony 5; Mycotoxin Tracer 4; Mutator Phenotype 10; Chronoresilient Cytoplasm 5; Tendril Northwest 1; Tendril Northeast 1; Tendril Southeast 1; Tendril Southwest 1; Mycotropic Induction 3; Regenerative Hyphae 2; Creeping Mold 4 | Growth 21; CellularResilience 12; Fungicide 4; GeneticDrift 10 |
| Grow>Defend>Kill | `legacy.proven.grow-defend-kill.v1` | `1ab73d70e0f141fd9371c677e48865a41713f168aa8e31e49c643ec20bd27b39` | Mycelial Bloom 11; Homeostatic Harmony 5; Mycotoxin Tracer 7; Mutator Phenotype 10; Chronoresilient Cytoplasm 5; Tendril Northwest 2; Tendril Northeast 2; Tendril Southeast 2; Tendril Southwest 2; Adaptive Expression 5; Anabolic Inversion 2; Mycotoxin Catabolism 4 | Growth 19; CellularResilience 10; Fungicide 7; GeneticDrift 21 |
| Grow>Kill>Reclaim(Econ) | `legacy.proven.grow-kill-reclaim-econ.v1` | `6d05c990829b119faf40a7ce598267a70d9e87f1c37eb8e93a9d5ae5097bdcc0` | Mycelial Bloom 10; Homeostatic Harmony 5; Mycotoxin Tracer 8; Mutator Phenotype 10; Chronoresilient Cytoplasm 5; Mycotoxin Potentiation 1; Tendril Northwest 1; Tendril Northeast 1; Tendril Southeast 1; Tendril Southwest 1; Mycotropic Induction 3; Creeping Mold 4; Aerated Frontier 5 | Growth 21; CellularResilience 10; Fungicide 9; GeneticDrift 10; SubstrateEcology 5 |
| Grow>Kill>Reclaim(Econ/Reclaim) | `legacy.proven.grow-kill-reclaim-econ-reclaim.v1` | `f7f29b1cf3d4d8ae359702a221049bd1cfaef872c5e451278e6c10a0fdfeebb3` | Mycelial Bloom 10; Homeostatic Harmony 5; Mycotoxin Tracer 8; Mutator Phenotype 10; Chronoresilient Cytoplasm 5; Mycotoxin Potentiation 1; Tendril Northwest 1; Tendril Northeast 1; Tendril Southeast 1; Tendril Southwest 1; Mycotropic Induction 3; Creeping Mold 4; Aerated Frontier 5 | Growth 21; CellularResilience 10; Fungicide 9; GeneticDrift 10; SubstrateEcology 5 |
| Grow>Mutate>Kill(Max Econ) | `legacy.proven.grow-mutate-kill-max-econ.v1` | `4d93b375cd95e86b5b8218c9a63ea6054b5d8ee25a45ee3053f112638e23012e` | Mycelial Bloom 10; Homeostatic Harmony 5; Mycotoxin Tracer 4; Mutator Phenotype 10; Tendril Northwest 1; Tendril Northeast 1; Tendril Southeast 1; Tendril Southwest 1; Adaptive Expression 5; Mycotropic Induction 3; Anabolic Inversion 2; Creeping Mold 4; Chitin Fortification 1; Aerated Frontier 2 | Growth 21; CellularResilience 5; Fungicide 4; GeneticDrift 17; MycelialSurges 1; SubstrateEcology 2 |
| Growth/Resilience | `legacy.proven.growth-resilience.v1` | `7f21f0cb50e8ea5f65bb0f557e07aa3d15188c3e6c1aff8c7db7b3f31b13a241` | Mycelial Bloom 11; Homeostatic Harmony 13; Mutator Phenotype 10; Chronoresilient Cytoplasm 12; Tendril Northwest 2; Tendril Northeast 1; Tendril Southeast 1; Tendril Southwest 1; Mycotropic Induction 2; Regenerative Hyphae 5 | Growth 18; CellularResilience 30; GeneticDrift 10 |
| Mutate>Grow>Kill(Max Econ) | `legacy.proven.mutate-grow-kill-max-econ.v1` | `af7f9fa83dfc3a4c558f9036d16a14bf878e0b5e89f9cf45bde1ed6123d384b4` | Mycelial Bloom 10; Homeostatic Harmony 5; Mycotoxin Tracer 5; Mutator Phenotype 10; Chronoresilient Cytoplasm 2; Tendril Northwest 1; Tendril Northeast 1; Tendril Southeast 1; Tendril Southwest 1; Adaptive Expression 5; Mycotropic Induction 3; Anabolic Inversion 3; Hyperadaptive Drift 3; Chitin Fortification 1 | Growth 17; CellularResilience 7; Fungicide 5; GeneticDrift 21; MycelialSurges 1 |
| Power Mutations Max Econ | `legacy.proven.power-mutations-max-econ.v1` | `9561fa5d95190ee7fccd24b0c4f90d96ca868e72e4f4a0b6a271962ebe2584ba` | Mycelial Bloom 10; Homeostatic Harmony 5; Mycotoxin Tracer 9; Mutator Phenotype 10; Chronoresilient Cytoplasm 5; Tendril Northwest 1; Tendril Northeast 1; Tendril Southeast 1; Tendril Southwest 1; Adaptive Expression 1; Mycotropic Induction 3; Creeping Mold 4; Mycotoxin Catabolism 1; Aerated Frontier 2 | Growth 21; CellularResilience 10; Fungicide 9; GeneticDrift 12; SubstrateEcology 2 |
| TST_AnabolicBeaconNecroRegressionCascade | `legacy.proven.tst-anabolicbeaconnecroregressioncascade.v1` | `030c7ac4ca864871fd757028e5a23223eac1646d8e0b0b1e1e7a8f3cc75f268c` | Mycelial Bloom 20; Homeostatic Harmony 5; Mutator Phenotype 10; Adaptive Expression 4; Necrophytic Bloom 2; Chitin Fortification 1; Chemotactic Beacon 1; Aerated Frontier 10; Crustward Tropism 1; Detrital Enzymes 3 | Growth 20; CellularResilience 5; GeneticDrift 14; MycelialSurges 2; SubstrateEcology 16 |
| TST_AnabolicCreepingNecroRegressionCascade | `legacy.proven.tst-anaboliccreepingnecroregressioncascade.v1` | `059c3e5b3e793feac0fc6127921047d0f1cce2185ba203531768c3c21b283e94` | Mycelial Bloom 10; Homeostatic Harmony 2; Mutator Phenotype 10; Tendril Northwest 1; Tendril Northeast 1; Tendril Southeast 1; Tendril Southwest 1; Adaptive Expression 5; Mycotropic Induction 3; Anabolic Inversion 1; Creeping Mold 3; Aerated Frontier 10; Crustward Tropism 1; Detrital Enzymes 1 | Growth 20; CellularResilience 2; GeneticDrift 16; SubstrateEcology 12 |
| TST_BalancedControl_AnabolicFirst | `legacy.proven.tst-balancedcontrol-anabolicfirst.v1` | `74cbb8a948e6f7b28af61e9225105a0b83cfb8b2b1f5e3e2c7680badc3f4971e` | Mycelial Bloom 10; Homeostatic Harmony 5; Mycotoxin Tracer 7; Mutator Phenotype 10; Tendril Northwest 1; Tendril Northeast 1; Tendril Southeast 1; Tendril Southwest 1; Adaptive Expression 5; Mycotropic Induction 3; Anabolic Inversion 3; Creeping Mold 2 | Growth 19; CellularResilience 5; Fungicide 7; GeneticDrift 18 |
| TST_BalancedControl_MaxEconomy | `legacy.proven.tst-balancedcontrol-maxeconomy.v1` | `e5f0847b5f196181f1a6533664213994f5ac2af123c94498adc9322a5d7afd48` | Mycelial Bloom 10; Homeostatic Harmony 5; Mycotoxin Tracer 5; Mutator Phenotype 10; Tendril Northwest 1; Tendril Northeast 1; Tendril Southeast 1; Tendril Southwest 1; Adaptive Expression 5; Mycotropic Induction 3; Anabolic Inversion 2; Creeping Mold 4 | Growth 21; CellularResilience 5; Fungicide 5; GeneticDrift 17 |
| TST_CampaignMirror_AI13_AnabolicFirst | `legacy.proven.tst-campaignmirror-ai13-anabolicfirst.v1` | `e9042d174c444b3af56b0979cb0721d54b52583169750951235098658bc7a608` | Mycelial Bloom 10; Homeostatic Harmony 5; Mycotoxin Tracer 7; Mutator Phenotype 10; Tendril Northwest 1; Tendril Northeast 1; Tendril Southeast 1; Tendril Southwest 1; Adaptive Expression 5; Mycotropic Induction 3; Anabolic Inversion 3; Creeping Mold 2 | Growth 19; CellularResilience 5; Fungicide 7; GeneticDrift 18 |
| TST_CampaignMirror_AI13_BalancedControl_MaxEconomy | `legacy.proven.tst-campaignmirror-ai13-balancedcontrol-maxeconomy.v1` | `86d628378de56b8f9a63b2e57fbf494cd9c477d7bb6564553702f67f85e30b2a` | Mycelial Bloom 10; Homeostatic Harmony 5; Mycotoxin Tracer 5; Mutator Phenotype 10; Tendril Northwest 1; Tendril Northeast 1; Tendril Southeast 1; Tendril Southwest 1; Adaptive Expression 5; Mycotropic Induction 3; Anabolic Inversion 2; Creeping Mold 4 | Growth 21; CellularResilience 5; Fungicide 5; GeneticDrift 17 |
| TST_CampaignPlayer_SafeBaseline | `legacy.proven.tst-campaignplayer-safebaseline.v1` | `0b4df83598c36ef155d9c0197c9ae8bbe2e55c76f39c0f297258eddd0ef6cc04` | Mycelial Bloom 11; Homeostatic Harmony 5; Mycotoxin Tracer 1; Mutator Phenotype 10; Chronoresilient Cytoplasm 3; Tendril Northwest 1; Tendril Northeast 1; Tendril Southeast 1; Tendril Southwest 1; Adaptive Expression 5; Mycotropic Induction 3; Creeping Mold 3; Aerated Frontier 2 | Growth 21; CellularResilience 8; Fungicide 1; GeneticDrift 15; SubstrateEcology 2 |
| TST_CreepingNecroRegressionCascade | `legacy.proven.tst-creepingnecroregressioncascade.v1` | `e2ed5ed96c0b0cbc6946e34b400bcc9236b133297fcb54f6fdd4bada8f66a506` | Mycelial Bloom 10; Homeostatic Harmony 2; Mutator Phenotype 10; Tendril Northwest 1; Tendril Northeast 1; Tendril Southeast 1; Tendril Southwest 1; Adaptive Expression 5; Mycotropic Induction 3; Creeping Mold 4; Aerated Frontier 11; Crustward Tropism 1; Detrital Enzymes 2 | Growth 21; CellularResilience 2; GeneticDrift 15; SubstrateEcology 14 |

## Pairwise similarity

Pairs are ordered by raw mutation-build similarity, then strategy name. Category similarity is explanatory only and can be high when strategies buy different mutations in the same categories.

| First strategy | Second strategy | Raw mutation similarity | Category similarity | Raw distance |
|---|---|---:|---:|---:|
| Creeping>Necrosporulation | Filament Regrowth | 1.000 | 1.000 | 0.000 |
| Grow>Kill>Reclaim(Econ) | Grow>Kill>Reclaim(Econ/Reclaim) | 1.000 | 1.000 | 0.000 |
| TST_BalancedControl_AnabolicFirst | TST_CampaignMirror_AI13_AnabolicFirst | 1.000 | 1.000 | 0.000 |
| TST_BalancedControl_MaxEconomy | TST_CampaignMirror_AI13_BalancedControl_MaxEconomy | 1.000 | 1.000 | 0.000 |
| TST_AnabolicCreepingNecroRegressionCascade | TST_CreepingNecroRegressionCascade | 0.995 | 0.997 | 0.005 |
| Grow>Mutate>Kill(Max Econ) | TST_BalancedControl_MaxEconomy | 0.990 | 0.996 | 0.010 |
| Grow>Mutate>Kill(Max Econ) | TST_CampaignMirror_AI13_BalancedControl_MaxEconomy | 0.990 | 0.996 | 0.010 |
| TST_BalancedControl_AnabolicFirst | TST_BalancedControl_MaxEconomy | 0.986 | 0.994 | 0.014 |
| TST_BalancedControl_AnabolicFirst | TST_CampaignMirror_AI13_BalancedControl_MaxEconomy | 0.986 | 0.994 | 0.014 |
| TST_BalancedControl_MaxEconomy | TST_CampaignMirror_AI13_AnabolicFirst | 0.986 | 0.994 | 0.014 |
| TST_CampaignMirror_AI13_AnabolicFirst | TST_CampaignMirror_AI13_BalancedControl_MaxEconomy | 0.986 | 0.994 | 0.014 |
| Anabolic>Grow>CatabR>PutreRegen | Grow>Defend>Kill | 0.985 | 0.993 | 0.015 |
| Grow>Kill>Reclaim(Econ) | Power Mutations Max Econ | 0.982 | 0.992 | 0.018 |
| Grow>Kill>Reclaim(Econ/Reclaim) | Power Mutations Max Econ | 0.982 | 0.992 | 0.018 |
| Grow>Mutate>Kill(Max Econ) | TST_BalancedControl_AnabolicFirst | 0.970 | 0.988 | 0.030 |
| Grow>Mutate>Kill(Max Econ) | TST_CampaignMirror_AI13_AnabolicFirst | 0.970 | 0.988 | 0.030 |
| Mutate>Grow>Kill(Max Econ) | TST_BalancedControl_AnabolicFirst | 0.966 | 0.986 | 0.034 |
| Mutate>Grow>Kill(Max Econ) | TST_CampaignMirror_AI13_AnabolicFirst | 0.966 | 0.986 | 0.034 |
| Grow>Mutate>Kill(Max Econ) | TST_CampaignPlayer_SafeBaseline | 0.959 | 0.985 | 0.041 |
| Creeping>Necrosporulation | Power Mutations Max Econ | 0.952 | 0.976 | 0.048 |
| Filament Regrowth | Power Mutations Max Econ | 0.952 | 0.976 | 0.048 |
| Mutate>Grow>Kill(Max Econ) | TST_BalancedControl_MaxEconomy | 0.950 | 0.977 | 0.050 |
| Mutate>Grow>Kill(Max Econ) | TST_CampaignMirror_AI13_BalancedControl_MaxEconomy | 0.950 | 0.977 | 0.050 |
| Grow>Mutate>Kill(Max Econ) | Mutate>Grow>Kill(Max Econ) | 0.943 | 0.974 | 0.057 |
| TST_BalancedControl_MaxEconomy | TST_CampaignPlayer_SafeBaseline | 0.943 | 0.979 | 0.057 |
| TST_CampaignMirror_AI13_BalancedControl_MaxEconomy | TST_CampaignPlayer_SafeBaseline | 0.943 | 0.979 | 0.057 |
| Creeping>Necrosporulation | Grow>Kill>Reclaim(Econ) | 0.936 | 0.963 | 0.064 |
| Creeping>Necrosporulation | Grow>Kill>Reclaim(Econ/Reclaim) | 0.936 | 0.963 | 0.064 |
| Filament Regrowth | Grow>Kill>Reclaim(Econ) | 0.936 | 0.963 | 0.064 |
| Filament Regrowth | Grow>Kill>Reclaim(Econ/Reclaim) | 0.936 | 0.963 | 0.064 |
| Anabolic>Grow>CatabR>PutreRegen | Mutate>Grow>Kill(Max Econ) | 0.931 | 0.992 | 0.069 |
| Grow>Defend>Kill | Mutate>Grow>Kill(Max Econ) | 0.927 | 0.993 | 0.073 |
| Anabolic>Grow>CatabR>PutreRegen | TST_BalancedControl_AnabolicFirst | 0.922 | 0.970 | 0.078 |
| Anabolic>Grow>CatabR>PutreRegen | TST_CampaignMirror_AI13_AnabolicFirst | 0.922 | 0.970 | 0.078 |
| Creeping>Necrosporulation | TST_CampaignPlayer_SafeBaseline | 0.921 | 0.963 | 0.079 |
| Filament Regrowth | TST_CampaignPlayer_SafeBaseline | 0.921 | 0.963 | 0.079 |
| Mutate>Grow>Kill(Max Econ) | TST_CampaignPlayer_SafeBaseline | 0.919 | 0.953 | 0.081 |
| Grow>Defend>Kill | TST_BalancedControl_AnabolicFirst | 0.918 | 0.986 | 0.082 |
| Grow>Defend>Kill | TST_CampaignMirror_AI13_AnabolicFirst | 0.918 | 0.986 | 0.082 |
| Grow>Defend>Kill | Power Mutations Max Econ | 0.910 | 0.951 | 0.090 |
| Power Mutations Max Econ | TST_BalancedControl_AnabolicFirst | 0.910 | 0.952 | 0.090 |
| Power Mutations Max Econ | TST_CampaignMirror_AI13_AnabolicFirst | 0.910 | 0.952 | 0.090 |
| Anabolic>Grow>CatabR>PutreRegen | Power Mutations Max Econ | 0.910 | 0.914 | 0.090 |
| Power Mutations Max Econ | TST_BalancedControl_MaxEconomy | 0.905 | 0.955 | 0.095 |
| Power Mutations Max Econ | TST_CampaignMirror_AI13_BalancedControl_MaxEconomy | 0.905 | 0.955 | 0.095 |
| TST_BalancedControl_AnabolicFirst | TST_CampaignPlayer_SafeBaseline | 0.905 | 0.959 | 0.095 |
| TST_CampaignMirror_AI13_AnabolicFirst | TST_CampaignPlayer_SafeBaseline | 0.905 | 0.959 | 0.095 |
| Creeping>Necrosporulation | TST_BalancedControl_MaxEconomy | 0.903 | 0.934 | 0.097 |
| Creeping>Necrosporulation | TST_CampaignMirror_AI13_BalancedControl_MaxEconomy | 0.903 | 0.934 | 0.097 |
| Filament Regrowth | TST_BalancedControl_MaxEconomy | 0.903 | 0.934 | 0.097 |
| Filament Regrowth | TST_CampaignMirror_AI13_BalancedControl_MaxEconomy | 0.903 | 0.934 | 0.097 |
| TST_AnabolicBeaconNecroRegressionCascade | TST_AnabolicCreepingNecroRegressionCascade | 0.900 | 0.981 | 0.100 |
| Anabolic>Grow>CatabR>PutreRegen | TST_BalancedControl_MaxEconomy | 0.897 | 0.951 | 0.103 |
| Anabolic>Grow>CatabR>PutreRegen | TST_CampaignMirror_AI13_BalancedControl_MaxEconomy | 0.897 | 0.951 | 0.103 |
| Grow>Mutate>Kill(Max Econ) | Power Mutations Max Econ | 0.896 | 0.951 | 0.104 |
| Grow>Defend>Kill | TST_BalancedControl_MaxEconomy | 0.896 | 0.976 | 0.104 |
| Grow>Defend>Kill | TST_CampaignMirror_AI13_BalancedControl_MaxEconomy | 0.896 | 0.976 | 0.104 |
| Creeping>Necrosporulation | Grow>Mutate>Kill(Max Econ) | 0.896 | 0.931 | 0.104 |
| Filament Regrowth | Grow>Mutate>Kill(Max Econ) | 0.896 | 0.931 | 0.104 |
| TST_AnabolicBeaconNecroRegressionCascade | TST_CreepingNecroRegressionCascade | 0.890 | 0.989 | 0.110 |
| Best_MaxEcon_Surge10_HyphalSurge | Mutate>Grow>Kill(Max Econ) | 0.889 | 0.892 | 0.111 |
| Grow>Kill>Reclaim(Econ) | Grow>Mutate>Kill(Max Econ) | 0.884 | 0.929 | 0.116 |
| Grow>Kill>Reclaim(Econ/Reclaim) | Grow>Mutate>Kill(Max Econ) | 0.884 | 0.929 | 0.116 |
| Mutate>Grow>Kill(Max Econ) | Power Mutations Max Econ | 0.883 | 0.920 | 0.117 |
| Grow>Defend>Kill | Grow>Mutate>Kill(Max Econ) | 0.881 | 0.971 | 0.119 |
| Anabolic>Grow>CatabR>PutreRegen | Grow>Mutate>Kill(Max Econ) | 0.881 | 0.946 | 0.119 |
| Grow>Defend>Kill | TST_CampaignPlayer_SafeBaseline | 0.880 | 0.958 | 0.120 |
| Creeping>Necrosporulation | TST_BalancedControl_AnabolicFirst | 0.879 | 0.914 | 0.121 |
| Creeping>Necrosporulation | TST_CampaignMirror_AI13_AnabolicFirst | 0.879 | 0.914 | 0.121 |
| Filament Regrowth | TST_BalancedControl_AnabolicFirst | 0.879 | 0.914 | 0.121 |
| Filament Regrowth | TST_CampaignMirror_AI13_AnabolicFirst | 0.879 | 0.914 | 0.121 |
| Creeping>Necrosporulation | Mutate>Grow>Kill(Max Econ) | 0.879 | 0.893 | 0.121 |
| Filament Regrowth | Mutate>Grow>Kill(Max Econ) | 0.879 | 0.893 | 0.121 |
| Creeping>Necrosporulation | Grow>Defend>Kill | 0.877 | 0.927 | 0.123 |
| Filament Regrowth | Grow>Defend>Kill | 0.877 | 0.927 | 0.123 |
| Power Mutations Max Econ | TST_CampaignPlayer_SafeBaseline | 0.874 | 0.949 | 0.126 |
| Grow>Kill>Reclaim(Econ) | TST_BalancedControl_MaxEconomy | 0.872 | 0.925 | 0.128 |
| Grow>Kill>Reclaim(Econ) | TST_CampaignMirror_AI13_BalancedControl_MaxEconomy | 0.872 | 0.925 | 0.128 |
| Grow>Kill>Reclaim(Econ/Reclaim) | TST_BalancedControl_MaxEconomy | 0.872 | 0.925 | 0.128 |
| Grow>Kill>Reclaim(Econ/Reclaim) | TST_CampaignMirror_AI13_BalancedControl_MaxEconomy | 0.872 | 0.925 | 0.128 |
| Grow>Kill>Reclaim(Econ) | TST_BalancedControl_AnabolicFirst | 0.872 | 0.919 | 0.128 |
| Grow>Kill>Reclaim(Econ) | TST_CampaignMirror_AI13_AnabolicFirst | 0.872 | 0.919 | 0.128 |
| Grow>Kill>Reclaim(Econ/Reclaim) | TST_BalancedControl_AnabolicFirst | 0.872 | 0.919 | 0.128 |
| Grow>Kill>Reclaim(Econ/Reclaim) | TST_CampaignMirror_AI13_AnabolicFirst | 0.872 | 0.919 | 0.128 |
| Anabolic>Grow>CatabR>PutreRegen | TST_CampaignPlayer_SafeBaseline | 0.871 | 0.927 | 0.129 |
| Anabolic>Grow>CatabR>PutreRegen | Creeping>Necrosporulation | 0.871 | 0.882 | 0.129 |
| Anabolic>Grow>CatabR>PutreRegen | Filament Regrowth | 0.871 | 0.882 | 0.129 |
| TST_AnabolicCreepingNecroRegressionCascade | TST_CampaignPlayer_SafeBaseline | 0.871 | 0.911 | 0.129 |
| Grow>Kill>Reclaim(Econ) | TST_CampaignPlayer_SafeBaseline | 0.871 | 0.931 | 0.129 |
| Grow>Kill>Reclaim(Econ/Reclaim) | TST_CampaignPlayer_SafeBaseline | 0.871 | 0.931 | 0.129 |
| Grow>Defend>Kill | Grow>Kill>Reclaim(Econ) | 0.864 | 0.916 | 0.136 |
| Grow>Defend>Kill | Grow>Kill>Reclaim(Econ/Reclaim) | 0.864 | 0.916 | 0.136 |
| Creeping>Necrosporulation | Growth/Resilience | 0.862 | 0.870 | 0.138 |
| Filament Regrowth | Growth/Resilience | 0.862 | 0.870 | 0.138 |
| Anabolic>Grow>CatabR>PutreRegen | Grow>Kill>Reclaim(Econ) | 0.860 | 0.873 | 0.140 |
| Anabolic>Grow>CatabR>PutreRegen | Grow>Kill>Reclaim(Econ/Reclaim) | 0.860 | 0.873 | 0.140 |
| Grow>Mutate>Kill(Max Econ) | TST_AnabolicCreepingNecroRegressionCascade | 0.860 | 0.919 | 0.140 |
| TST_AnabolicBeaconNecroRegressionCascade | TST_CampaignPlayer_SafeBaseline | 0.857 | 0.872 | 0.143 |
| TST_CampaignPlayer_SafeBaseline | TST_CreepingNecroRegressionCascade | 0.850 | 0.890 | 0.150 |
| Best_MaxEcon_Surge10_HyphalSurge | Grow>Mutate>Kill(Max Econ) | 0.850 | 0.789 | 0.150 |
| Grow>Kill>Reclaim(Econ) | Mutate>Grow>Kill(Max Econ) | 0.850 | 0.879 | 0.150 |
| Grow>Kill>Reclaim(Econ/Reclaim) | Mutate>Grow>Kill(Max Econ) | 0.850 | 0.879 | 0.150 |
| Best_MaxEcon_Surge10_HyphalSurge | TST_BalancedControl_MaxEconomy | 0.841 | 0.784 | 0.159 |
| Best_MaxEcon_Surge10_HyphalSurge | TST_CampaignMirror_AI13_BalancedControl_MaxEconomy | 0.841 | 0.784 | 0.159 |
| Best_MaxEcon_Surge10_HyphalSurge | TST_CampaignPlayer_SafeBaseline | 0.838 | 0.763 | 0.162 |
| Grow>Mutate>Kill(Max Econ) | TST_CreepingNecroRegressionCascade | 0.837 | 0.895 | 0.163 |
| Best_MaxEcon_Surge10_HyphalSurge | TST_BalancedControl_AnabolicFirst | 0.830 | 0.812 | 0.170 |
| Best_MaxEcon_Surge10_HyphalSurge | TST_CampaignMirror_AI13_AnabolicFirst | 0.830 | 0.812 | 0.170 |
| Grow>Mutate>Kill(Max Econ) | TST_AnabolicBeaconNecroRegressionCascade | 0.819 | 0.867 | 0.181 |
| Growth/Resilience | TST_CampaignPlayer_SafeBaseline | 0.796 | 0.779 | 0.204 |
| TST_AnabolicCreepingNecroRegressionCascade | TST_BalancedControl_MaxEconomy | 0.793 | 0.886 | 0.207 |
| TST_AnabolicCreepingNecroRegressionCascade | TST_CampaignMirror_AI13_BalancedControl_MaxEconomy | 0.793 | 0.886 | 0.207 |
| Grow>Kill>Reclaim(Econ) | TST_AnabolicCreepingNecroRegressionCascade | 0.789 | 0.852 | 0.211 |
| Grow>Kill>Reclaim(Econ/Reclaim) | TST_AnabolicCreepingNecroRegressionCascade | 0.789 | 0.852 | 0.211 |
| Anabolic>Grow>CatabR>PutreRegen | Best_MaxEcon_Surge10_HyphalSurge | 0.782 | 0.902 | 0.218 |
| Grow>Kill>Reclaim(Econ) | TST_CreepingNecroRegressionCascade | 0.781 | 0.847 | 0.219 |
| Grow>Kill>Reclaim(Econ/Reclaim) | TST_CreepingNecroRegressionCascade | 0.781 | 0.847 | 0.219 |
| Best_MaxEcon_Surge10_HyphalSurge | Grow>Defend>Kill | 0.774 | 0.854 | 0.226 |
| TST_BalancedControl_MaxEconomy | TST_CreepingNecroRegressionCascade | 0.768 | 0.859 | 0.232 |
| TST_CampaignMirror_AI13_BalancedControl_MaxEconomy | TST_CreepingNecroRegressionCascade | 0.768 | 0.859 | 0.232 |
| TST_AnabolicBeaconNecroRegressionCascade | TST_BalancedControl_MaxEconomy | 0.767 | 0.824 | 0.233 |
| TST_AnabolicBeaconNecroRegressionCascade | TST_CampaignMirror_AI13_BalancedControl_MaxEconomy | 0.767 | 0.824 | 0.233 |
| Mutate>Grow>Kill(Max Econ) | TST_AnabolicBeaconNecroRegressionCascade | 0.765 | 0.797 | 0.235 |
| Grow>Defend>Kill | Growth/Resilience | 0.764 | 0.759 | 0.236 |
| Grow>Kill>Reclaim(Econ) | TST_AnabolicBeaconNecroRegressionCascade | 0.762 | 0.851 | 0.238 |
| Grow>Kill>Reclaim(Econ/Reclaim) | TST_AnabolicBeaconNecroRegressionCascade | 0.762 | 0.851 | 0.238 |
| TST_AnabolicCreepingNecroRegressionCascade | TST_BalancedControl_AnabolicFirst | 0.758 | 0.868 | 0.242 |
| TST_AnabolicCreepingNecroRegressionCascade | TST_CampaignMirror_AI13_AnabolicFirst | 0.758 | 0.868 | 0.242 |
| Growth/Resilience | Power Mutations Max Econ | 0.758 | 0.790 | 0.242 |
| Mutate>Grow>Kill(Max Econ) | TST_AnabolicCreepingNecroRegressionCascade | 0.756 | 0.858 | 0.244 |
| Grow>Kill>Reclaim(Econ) | Growth/Resilience | 0.754 | 0.782 | 0.246 |
| Grow>Kill>Reclaim(Econ/Reclaim) | Growth/Resilience | 0.754 | 0.782 | 0.246 |
| Anabolic>Grow>CatabR>PutreRegen | Growth/Resilience | 0.753 | 0.736 | 0.247 |
| TST_AnabolicBeaconNecroRegressionCascade | TST_BalancedControl_AnabolicFirst | 0.747 | 0.803 | 0.253 |
| TST_AnabolicBeaconNecroRegressionCascade | TST_CampaignMirror_AI13_AnabolicFirst | 0.747 | 0.803 | 0.253 |
| Growth/Resilience | Mutate>Grow>Kill(Max Econ) | 0.736 | 0.703 | 0.264 |
| Creeping>Necrosporulation | TST_AnabolicBeaconNecroRegressionCascade | 0.733 | 0.789 | 0.267 |
| Filament Regrowth | TST_AnabolicBeaconNecroRegressionCascade | 0.733 | 0.789 | 0.267 |
| Grow>Defend>Kill | TST_AnabolicBeaconNecroRegressionCascade | 0.730 | 0.791 | 0.270 |
| Best_MaxEcon_Surge10_HyphalSurge | Creeping>Necrosporulation | 0.725 | 0.637 | 0.275 |
| Best_MaxEcon_Surge10_HyphalSurge | Filament Regrowth | 0.725 | 0.637 | 0.275 |
| TST_BalancedControl_AnabolicFirst | TST_CreepingNecroRegressionCascade | 0.725 | 0.838 | 0.275 |
| TST_CampaignMirror_AI13_AnabolicFirst | TST_CreepingNecroRegressionCascade | 0.725 | 0.838 | 0.275 |
| Power Mutations Max Econ | TST_AnabolicCreepingNecroRegressionCascade | 0.722 | 0.834 | 0.278 |
| Creeping>Necrosporulation | TST_AnabolicCreepingNecroRegressionCascade | 0.722 | 0.805 | 0.278 |
| Filament Regrowth | TST_AnabolicCreepingNecroRegressionCascade | 0.722 | 0.805 | 0.278 |
| Mutate>Grow>Kill(Max Econ) | TST_CreepingNecroRegressionCascade | 0.718 | 0.822 | 0.282 |
| Best_MaxEcon_Surge10_HyphalSurge | TST_AnabolicCreepingNecroRegressionCascade | 0.716 | 0.707 | 0.284 |
| Power Mutations Max Econ | TST_AnabolicBeaconNecroRegressionCascade | 0.712 | 0.813 | 0.288 |
| Power Mutations Max Econ | TST_CreepingNecroRegressionCascade | 0.710 | 0.819 | 0.290 |
| Creeping>Necrosporulation | TST_CreepingNecroRegressionCascade | 0.705 | 0.789 | 0.295 |
| Filament Regrowth | TST_CreepingNecroRegressionCascade | 0.705 | 0.789 | 0.295 |
| Anabolic>Grow>CatabR>PutreRegen | TST_AnabolicBeaconNecroRegressionCascade | 0.704 | 0.763 | 0.296 |
| Grow>Defend>Kill | TST_AnabolicCreepingNecroRegressionCascade | 0.694 | 0.842 | 0.306 |
| Anabolic>Grow>CatabR>PutreRegen | TST_AnabolicCreepingNecroRegressionCascade | 0.689 | 0.817 | 0.311 |
| Best_MaxEcon_Surge10_HyphalSurge | TST_AnabolicBeaconNecroRegressionCascade | 0.687 | 0.641 | 0.313 |
| Grow>Mutate>Kill(Max Econ) | Growth/Resilience | 0.687 | 0.689 | 0.313 |
| Growth/Resilience | TST_BalancedControl_MaxEconomy | 0.683 | 0.687 | 0.317 |
| Growth/Resilience | TST_CampaignMirror_AI13_BalancedControl_MaxEconomy | 0.683 | 0.687 | 0.317 |
| Best_MaxEcon_Surge10_HyphalSurge | TST_CreepingNecroRegressionCascade | 0.676 | 0.657 | 0.324 |
| Best_MaxEcon_Surge10_HyphalSurge | Power Mutations Max Econ | 0.674 | 0.659 | 0.326 |
| Growth/Resilience | TST_BalancedControl_AnabolicFirst | 0.664 | 0.670 | 0.336 |
| Growth/Resilience | TST_CampaignMirror_AI13_AnabolicFirst | 0.664 | 0.670 | 0.336 |
| Grow>Defend>Kill | TST_CreepingNecroRegressionCascade | 0.661 | 0.809 | 0.339 |
| Best_MaxEcon_Surge10_HyphalSurge | Growth/Resilience | 0.660 | 0.556 | 0.340 |
| Anabolic>Grow>CatabR>PutreRegen | TST_CreepingNecroRegressionCascade | 0.654 | 0.779 | 0.346 |
| Best_MaxEcon_Surge10_HyphalSurge | Grow>Kill>Reclaim(Econ) | 0.652 | 0.600 | 0.348 |
| Best_MaxEcon_Surge10_HyphalSurge | Grow>Kill>Reclaim(Econ/Reclaim) | 0.652 | 0.600 | 0.348 |
| Growth/Resilience | TST_AnabolicBeaconNecroRegressionCascade | 0.629 | 0.602 | 0.371 |
| Growth/Resilience | TST_AnabolicCreepingNecroRegressionCascade | 0.550 | 0.562 | 0.450 |
| Growth/Resilience | TST_CreepingNecroRegressionCascade | 0.528 | 0.549 | 0.472 |
