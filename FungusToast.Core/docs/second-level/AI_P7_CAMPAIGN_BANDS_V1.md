# AI P7 Campaign Bands V1

Frozen measurement of all 53 Campaign strategies, run 2026-09-07. Six contexts, 1,800 games, no
failed or skipped conditions. Systems are off, matching `p7-solo-panel-v1`, so a band reflects
strategy strength rather than draft or nutrient luck and the two panels are comparable. Matrix:
`FungusToast.Simulation/Examples/calibration-matrix.campaign.v1.example.json`.

The headline is in the *Campaign difficulty vs measured strength* section below, not in the band
table: the authored difficulty ladder is close to uncorrelated with measured strength, and `Elite`
is the fourth-strongest tier of five.


- Classifier: `fungus-toast.ai-bands.v1`
- Panel: Campaign (53 strategies)
- Bands are distances from parity: Elite above `1.25`, Hard above `1.05`, Easy below `0.95`, placed on the interval bound that argues against the band.
- Thresholds were frozen before the matrix finished running, so they could not be drawn around the results.

## Overall

| Strategy | Band | Pooled share | Games | Evidence | Contextual |
|---|---|---:|---:|---|---|
| CMP_Bloom_ToxinborneBallistospore_Hard | — | 2.244 | 135 | IntervalTooWide | smalltable.small.square.rectangle.generated.80x80 |
| AI13 | Elite | 2.076 | 131 | Sufficient | — |
| CMP_Bloom_ToxinborneJetting_Medium | — | 2.062 | 122 | IntervalTooWide | swarm.medium.wide.rectangle.generated.140x100 |
| AI12 | Elite | 1.959 | 126 | Sufficient | — |
| CMP_Bloom_AnabolicRegression_Medium | Elite | 1.934 | 149 | Sufficient | — |
| CMP_Control_AnabolicFirst_Hard | Elite | 1.727 | 137 | Sufficient | smalltable.small.tall.rectangle.generated.60x100 |
| CMP_Economy_Economancer_Elite | Elite | 1.708 | 126 | Sufficient | smalltable.small.tall.rectangle.generated.60x100 |
| CMP_Control_AnabolicRebirth_Medium | Elite | 1.619 | 122 | Sufficient | crowded.small.square.rectangle.generated.80x80 |
| CMP_Control_RebirthFurnace_Medium | Elite | 1.613 | 120 | Sufficient | — |
| CMP_Economy_KillReclaim_Medium | Elite | 1.610 | 128 | Sufficient | smalltable.small.square.rectangle.generated.80x80, smalltable.small.tall.rectangle.generated.60x100 |
| TST_Campaign7_KillReclaim_Offset2 | Elite | 1.588 | 115 | Sufficient | — |
| CMP_Growth_PutridTendrils_Medium | Elite | 1.571 | 145 | Sufficient | smalltable.small.tall.rectangle.generated.60x100 |
| TST_Campaign7_KillReclaim_Offset3 | Hard | 1.501 | 113 | Sufficient | crowded.medium.square.rectangle.generated.100x100, smalltable.small.square.rectangle.generated.80x80, smalltable.small.tall.rectangle.generated.60x100, swarm.medium.wide.rectangle.generated.140x100 |
| TST_Campaign7_KillReclaim_Offset1 | Elite | 1.485 | 130 | Sufficient | smalltable.small.square.rectangle.generated.80x80 |
| CMP_Economy_HoardsporeRegent_Elite | Hard | 1.366 | 114 | Sufficient | smalltable.small.square.rectangle.generated.80x80, smalltable.small.tall.rectangle.generated.60x100 |
| TST_Campaign7_KillReclaim_Offset8 | Hard | 1.317 | 128 | Sufficient | crowded.small.square.rectangle.generated.80x80, swarm.medium.square.rectangle.generated.120x120 |
| CMP_Bloom_BeaconRegression_Medium | Hard | 1.295 | 134 | Sufficient | crowded.medium.square.rectangle.generated.100x100 |
| CMP_Defense_IronShell_Elite | Hard | 1.284 | 123 | Sufficient | smalltable.small.square.rectangle.generated.80x80, smalltable.small.tall.rectangle.generated.60x100, swarm.medium.wide.rectangle.generated.140x100 |
| CMP_Bloom_CreepingNecro_Medium | Normal | 1.212 | 145 | Sufficient | crowded.small.square.rectangle.generated.80x80 |
| AI4 | Normal | 1.197 | 151 | Sufficient | crowded.small.square.rectangle.generated.80x80, swarm.medium.square.rectangle.generated.120x120 |
| AI5 | Normal | 1.194 | 112 | Sufficient | crowded.small.square.rectangle.generated.80x80, swarm.medium.wide.rectangle.generated.140x100 |
| CMP_Economy_LateSpike_Hard | Normal | 1.173 | 132 | Sufficient | swarm.medium.wide.rectangle.generated.140x100 |
| CMP_Bloom_CreepingRegression_Elite | Normal | 1.112 | 129 | Sufficient | — |
| CMP_Economy_TempoReclaim_Medium | Normal | 1.111 | 134 | Sufficient | — |
| CMP_Reclaim_Scavenger_Easy | Normal | 1.092 | 127 | Sufficient | smalltable.small.tall.rectangle.generated.60x100 |
| CMP_Growth_Pressure_Medium | Normal | 0.934 | 106 | Sufficient | crowded.medium.square.rectangle.generated.100x100, swarm.medium.square.rectangle.generated.120x120, smalltable.small.tall.rectangle.generated.60x100 |
| TST_CampaignPlayer_SafeBaseline | Normal | 0.902 | 137 | Sufficient | crowded.medium.square.rectangle.generated.100x100, swarm.medium.square.rectangle.generated.120x120, swarm.medium.wide.rectangle.generated.140x100 |
| CMP_Bloom_Thanatophyte_Elite | Normal | 0.867 | 126 | Sufficient | crowded.medium.square.rectangle.generated.100x100, swarm.medium.square.rectangle.generated.120x120, swarm.medium.wide.rectangle.generated.140x100 |
| CMP_Bloom_NecrotoxinGauntlet_Elite | Easy | 0.831 | 140 | Sufficient | crowded.small.square.rectangle.generated.80x80, smalltable.small.square.rectangle.generated.80x80, smalltable.small.tall.rectangle.generated.60x100 |
| CMP_Growth_WildfireBloom_Medium | Easy | 0.809 | 135 | Sufficient | crowded.small.square.rectangle.generated.80x80, smalltable.small.square.rectangle.generated.80x80 |
| CMP_Bloom_FortifyMimic_Medium | Easy | 0.756 | 131 | Sufficient | crowded.small.square.rectangle.generated.80x80 |
| CMP_TierCap_GrowthResilience_Easy | Easy | 0.725 | 115 | Sufficient | smalltable.small.square.rectangle.generated.80x80 |
| AI6 | Easy | 0.720 | 139 | Sufficient | — |
| CMP_Defense_ReclaimShell_Easy | Easy | 0.704 | 132 | Sufficient | — |
| TST_AI10_CreepingRegression | Easy | 0.648 | 115 | Sufficient | — |
| CMP_Surge_Pulsar_Easy | Easy | 0.644 | 149 | Sufficient | — |
| TST_AI10_BeaconRegression | Easy | 0.524 | 111 | Sufficient | — |
| CMP_AnabolicBeaconRhizolith_Elite | Easy | 0.498 | 130 | Sufficient | — |
| CMP_Mobility_Overextender_Training | Easy | 0.490 | 133 | Sufficient | — |
| CMP_Defense_ResilientShell_Easy | Easy | 0.455 | 129 | Sufficient | — |
| CMP_Mobility_Overextender_Training_Offset1 | Easy | 0.447 | 126 | Sufficient | — |
| CMP_Mobility_Overextender_Training_Offset2 | Easy | 0.446 | 119 | Sufficient | — |
| CMP_Mobility_Overextender_Training_Offset3 | Easy | 0.415 | 132 | Sufficient | — |
| TST_Training_ResilientMycelium_Offset3 | Easy | 0.415 | 123 | Sufficient | — |
| TST_Training_ResilientMycelium_Offset1 | Easy | 0.401 | 128 | Sufficient | — |
| TST_Training_ResilientMycelium | Easy | 0.370 | 126 | Sufficient | — |
| CMP_Surge_BeaconTempo_Medium | Easy | 0.361 | 118 | Sufficient | — |
| CMP_Surge_BeaconSprinter_Medium | Easy | 0.358 | 138 | Sufficient | — |
| CMP_Surge_GrowthTempo_Medium | Easy | 0.306 | 135 | Sufficient | — |
| CMP_Reclaim_InfiltrationSurge_Easy | Easy | 0.271 | 134 | Sufficient | — |
| CMP_Attrition_ToxicTurtle_Training | Easy | 0.152 | 105 | Sufficient | — |
| CMP_Attrition_ToxicTurtle_Training_Offset1 | Easy | 0.141 | 130 | Sufficient | — |
| CMP_Attrition_ToxicTurtle_Training_Offset2 | Easy | 0.140 | 130 | Sufficient | — |

## Authored label vs measurement

- **AI13** is authored `Strong` and used as a **Boss**, but measures **Elite** at 2.076.
- **AI12** is authored `Weak`, but measures **Elite** at 1.959.
- **CMP_Control_AnabolicFirst_Hard** is authored `Strong`, but measures **Elite** at 1.727.
- **CMP_Economy_Economancer_Elite** is authored `Strong` and used as a **Boss**, but measures **Elite** at 1.708.
- **CMP_Bloom_CreepingRegression_Elite** is authored `Strong` and used as a **Boss**, but measures **Normal** at 1.112.
- **CMP_Reclaim_Scavenger_Easy** is authored `Weak`, but measures **Normal** at 1.092.
- **CMP_Bloom_Thanatophyte_Elite** is authored `Strong` and used as a **Boss**, but measures **Normal** at 0.867.
- **CMP_Bloom_NecrotoxinGauntlet_Elite** is authored `Strong` and used as a **Boss**, but measures **Easy** at 0.831.
- **TST_AI10_CreepingRegression** is authored `Strong` and used as a **Boss**, but measures **Easy** at 0.648.
- **TST_AI10_BeaconRegression** is authored `Strong` and used as a **Boss**, but measures **Easy** at 0.524.
- **CMP_AnabolicBeaconRhizolith_Elite** is authored `Strong` and used as a **Boss**, but measures **Easy** at 0.498.

## By context

Holdout contexts are measured and shown but never pooled into the overall band, since they exist to check it.

| Strategy | Context | Band | Share | 95% CI | Games |
|---|---|---|---:|---|---:|
| CMP_Bloom_ToxinborneBallistospore_Hard | crowded.medium.square.rectangle.generated.100x100 | — | 2.332 | 1.712–2.953 | 39 |
| CMP_Bloom_ToxinborneBallistospore_Hard | crowded.small.square.rectangle.generated.80x80 | — | 1.937 | 1.280–2.593 | 29 |
| CMP_Bloom_ToxinborneBallistospore_Hard | smalltable.small.square.rectangle.generated.80x80 | Elite | 3.082 | 2.787–3.378 | 33 |
| CMP_Bloom_ToxinborneBallistospore_Hard | swarm.medium.square.rectangle.generated.120x120 | — | 1.592 | 1.060–2.125 | 34 |
| CMP_Bloom_ToxinborneBallistospore_Hard | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | — | 2.709 | 2.296–3.123 | 40 |
| CMP_Bloom_ToxinborneBallistospore_Hard | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | — | 0.935 | 0.589–1.281 | 34 |
| AI13 | crowded.medium.square.rectangle.generated.100x100 | Elite | 2.351 | 2.102–2.601 | 38 |
| AI13 | crowded.small.square.rectangle.generated.80x80 | Elite | 1.813 | 1.593–2.033 | 32 |
| AI13 | smalltable.small.square.rectangle.generated.80x80 | — | 1.532 | 1.272–1.792 | 22 |
| AI13 | swarm.medium.square.rectangle.generated.120x120 | Elite | 2.329 | 2.087–2.570 | 39 |
| AI13 | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Elite | 1.787 | 1.519–2.055 | 30 |
| AI13 | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | — | 2.478 | 2.157–2.799 | 34 |
| CMP_Bloom_ToxinborneJetting_Medium | crowded.medium.square.rectangle.generated.100x100 | — | 2.216 | 1.623–2.808 | 36 |
| CMP_Bloom_ToxinborneJetting_Medium | crowded.small.square.rectangle.generated.80x80 | — | 1.881 | 1.316–2.445 | 29 |
| CMP_Bloom_ToxinborneJetting_Medium | smalltable.small.square.rectangle.generated.80x80 | — | 3.019 | 2.635–3.404 | 24 |
| CMP_Bloom_ToxinborneJetting_Medium | swarm.medium.square.rectangle.generated.120x120 | — | 1.358 | 0.871–1.845 | 33 |
| CMP_Bloom_ToxinborneJetting_Medium | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | — | 2.969 | 2.589–3.350 | 40 |
| CMP_Bloom_ToxinborneJetting_Medium | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Normal | 0.982 | 0.713–1.252 | 34 |
| AI12 | crowded.medium.square.rectangle.generated.100x100 | Elite | 1.904 | 1.674–2.134 | 40 |
| AI12 | crowded.small.square.rectangle.generated.80x80 | Elite | 1.815 | 1.634–1.996 | 41 |
| AI12 | smalltable.small.square.rectangle.generated.80x80 | — | 1.907 | 1.654–2.160 | 24 |
| AI12 | swarm.medium.square.rectangle.generated.120x120 | — | 2.403 | 2.094–2.713 | 21 |
| AI12 | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | — | 1.786 | 1.460–2.112 | 26 |
| AI12 | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | — | 2.582 | 2.238–2.926 | 31 |
| CMP_Bloom_AnabolicRegression_Medium | crowded.medium.square.rectangle.generated.100x100 | Elite | 2.044 | 1.786–2.301 | 36 |
| CMP_Bloom_AnabolicRegression_Medium | crowded.small.square.rectangle.generated.80x80 | Elite | 1.757 | 1.596–1.919 | 46 |
| CMP_Bloom_AnabolicRegression_Medium | smalltable.small.square.rectangle.generated.80x80 | Elite | 1.876 | 1.651–2.102 | 35 |
| CMP_Bloom_AnabolicRegression_Medium | swarm.medium.square.rectangle.generated.120x120 | Elite | 2.126 | 1.835–2.416 | 32 |
| CMP_Bloom_AnabolicRegression_Medium | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Elite | 1.610 | 1.386–1.834 | 33 |
| CMP_Bloom_AnabolicRegression_Medium | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | — | 2.418 | 2.002–2.833 | 25 |
| CMP_Control_AnabolicFirst_Hard | crowded.medium.square.rectangle.generated.100x100 | Elite | 1.744 | 1.510–1.978 | 38 |
| CMP_Control_AnabolicFirst_Hard | crowded.small.square.rectangle.generated.80x80 | Elite | 1.565 | 1.388–1.741 | 39 |
| CMP_Control_AnabolicFirst_Hard | smalltable.small.square.rectangle.generated.80x80 | Elite | 1.581 | 1.317–1.845 | 33 |
| CMP_Control_AnabolicFirst_Hard | swarm.medium.square.rectangle.generated.120x120 | — | 2.117 | 1.781–2.453 | 27 |
| CMP_Control_AnabolicFirst_Hard | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Hard | 1.457 | 1.172–1.741 | 25 |
| CMP_Control_AnabolicFirst_Hard | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | — | 2.092 | 1.706–2.477 | 25 |
| CMP_Economy_Economancer_Elite | crowded.medium.square.rectangle.generated.100x100 | Elite | 1.679 | 1.431–1.927 | 31 |
| CMP_Economy_Economancer_Elite | crowded.small.square.rectangle.generated.80x80 | Elite | 1.732 | 1.554–1.910 | 44 |
| CMP_Economy_Economancer_Elite | smalltable.small.square.rectangle.generated.80x80 | — | 1.647 | 1.354–1.940 | 23 |
| CMP_Economy_Economancer_Elite | swarm.medium.square.rectangle.generated.120x120 | Elite | 1.755 | 1.498–2.012 | 28 |
| CMP_Economy_Economancer_Elite | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Hard | 1.336 | 1.106–1.566 | 33 |
| CMP_Economy_Economancer_Elite | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Elite | 1.568 | 1.392–1.745 | 27 |
| CMP_Control_AnabolicRebirth_Medium | crowded.medium.square.rectangle.generated.100x100 | — | 1.609 | 1.263–1.954 | 30 |
| CMP_Control_AnabolicRebirth_Medium | crowded.small.square.rectangle.generated.80x80 | Hard | 1.353 | 1.161–1.546 | 25 |
| CMP_Control_AnabolicRebirth_Medium | smalltable.small.square.rectangle.generated.80x80 | Elite | 1.613 | 1.374–1.853 | 36 |
| CMP_Control_AnabolicRebirth_Medium | swarm.medium.square.rectangle.generated.120x120 | Elite | 1.849 | 1.583–2.115 | 31 |
| CMP_Control_AnabolicRebirth_Medium | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Elite | 1.661 | 1.400–1.922 | 29 |
| CMP_Control_AnabolicRebirth_Medium | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | — | 2.119 | 1.703–2.534 | 31 |
| CMP_Control_RebirthFurnace_Medium | crowded.medium.square.rectangle.generated.100x100 | Elite | 1.630 | 1.341–1.918 | 38 |
| CMP_Control_RebirthFurnace_Medium | crowded.small.square.rectangle.generated.80x80 | Elite | 1.438 | 1.290–1.586 | 28 |
| CMP_Control_RebirthFurnace_Medium | smalltable.small.square.rectangle.generated.80x80 | Elite | 1.718 | 1.465–1.970 | 35 |
| CMP_Control_RebirthFurnace_Medium | swarm.medium.square.rectangle.generated.120x120 | — | 1.642 | 1.378–1.905 | 19 |
| CMP_Control_RebirthFurnace_Medium | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Elite | 1.572 | 1.331–1.812 | 27 |
| CMP_Control_RebirthFurnace_Medium | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | — | 1.691 | 1.363–2.019 | 24 |
| CMP_Economy_KillReclaim_Medium | crowded.medium.square.rectangle.generated.100x100 | Elite | 1.584 | 1.395–1.773 | 39 |
| CMP_Economy_KillReclaim_Medium | crowded.small.square.rectangle.generated.80x80 | Elite | 1.593 | 1.393–1.792 | 33 |
| CMP_Economy_KillReclaim_Medium | smalltable.small.square.rectangle.generated.80x80 | Hard | 1.443 | 1.203–1.684 | 26 |
| CMP_Economy_KillReclaim_Medium | swarm.medium.square.rectangle.generated.120x120 | Elite | 1.808 | 1.517–2.099 | 30 |
| CMP_Economy_KillReclaim_Medium | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Hard | 1.442 | 1.152–1.732 | 29 |
| CMP_Economy_KillReclaim_Medium | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Elite | 1.853 | 1.619–2.088 | 41 |
| TST_Campaign7_KillReclaim_Offset2 | crowded.medium.square.rectangle.generated.100x100 | Elite | 1.720 | 1.518–1.922 | 28 |
| TST_Campaign7_KillReclaim_Offset2 | crowded.small.square.rectangle.generated.80x80 | Elite | 1.480 | 1.297–1.663 | 33 |
| TST_Campaign7_KillReclaim_Offset2 | smalltable.small.square.rectangle.generated.80x80 | Elite | 1.570 | 1.279–1.862 | 26 |
| TST_Campaign7_KillReclaim_Offset2 | swarm.medium.square.rectangle.generated.120x120 | — | 1.598 | 1.295–1.901 | 28 |
| TST_Campaign7_KillReclaim_Offset2 | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | — | 1.569 | 1.261–1.877 | 28 |
| TST_Campaign7_KillReclaim_Offset2 | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Elite | 1.826 | 1.570–2.081 | 26 |
| CMP_Growth_PutridTendrils_Medium | crowded.medium.square.rectangle.generated.100x100 | Elite | 1.546 | 1.299–1.792 | 40 |
| CMP_Growth_PutridTendrils_Medium | crowded.small.square.rectangle.generated.80x80 | Elite | 1.419 | 1.256–1.583 | 38 |
| CMP_Growth_PutridTendrils_Medium | smalltable.small.square.rectangle.generated.80x80 | Elite | 1.531 | 1.319–1.743 | 35 |
| CMP_Growth_PutridTendrils_Medium | swarm.medium.square.rectangle.generated.120x120 | Elite | 1.825 | 1.535–2.114 | 32 |
| CMP_Growth_PutridTendrils_Medium | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Hard | 1.369 | 1.150–1.588 | 43 |
| CMP_Growth_PutridTendrils_Medium | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Elite | 1.652 | 1.474–1.830 | 31 |
| TST_Campaign7_KillReclaim_Offset3 | crowded.medium.square.rectangle.generated.100x100 | Elite | 1.540 | 1.304–1.776 | 28 |
| TST_Campaign7_KillReclaim_Offset3 | crowded.small.square.rectangle.generated.80x80 | — | 1.376 | 1.211–1.542 | 20 |
| TST_Campaign7_KillReclaim_Offset3 | smalltable.small.square.rectangle.generated.80x80 | Normal | 1.301 | 1.032–1.570 | 33 |
| TST_Campaign7_KillReclaim_Offset3 | swarm.medium.square.rectangle.generated.120x120 | — | 1.750 | 1.436–2.063 | 32 |
| TST_Campaign7_KillReclaim_Offset3 | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Elite | 1.530 | 1.318–1.742 | 25 |
| TST_Campaign7_KillReclaim_Offset3 | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Elite | 1.623 | 1.388–1.858 | 38 |
| TST_Campaign7_KillReclaim_Offset1 | crowded.medium.square.rectangle.generated.100x100 | — | 1.634 | 1.314–1.955 | 38 |
| TST_Campaign7_KillReclaim_Offset1 | crowded.small.square.rectangle.generated.80x80 | Elite | 1.387 | 1.252–1.522 | 38 |
| TST_Campaign7_KillReclaim_Offset1 | smalltable.small.square.rectangle.generated.80x80 | Hard | 1.382 | 1.131–1.633 | 26 |
| TST_Campaign7_KillReclaim_Offset1 | swarm.medium.square.rectangle.generated.120x120 | Elite | 1.510 | 1.327–1.693 | 28 |
| TST_Campaign7_KillReclaim_Offset1 | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Elite | 1.539 | 1.316–1.762 | 32 |
| TST_Campaign7_KillReclaim_Offset1 | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Elite | 1.880 | 1.588–2.172 | 30 |
| CMP_Economy_HoardsporeRegent_Elite | crowded.medium.square.rectangle.generated.100x100 | — | 1.391 | 1.165–1.617 | 24 |
| CMP_Economy_HoardsporeRegent_Elite | crowded.small.square.rectangle.generated.80x80 | Hard | 1.375 | 1.212–1.538 | 42 |
| CMP_Economy_HoardsporeRegent_Elite | smalltable.small.square.rectangle.generated.80x80 | Normal | 1.241 | 0.943–1.539 | 27 |
| CMP_Economy_HoardsporeRegent_Elite | swarm.medium.square.rectangle.generated.120x120 | — | 1.480 | 1.242–1.719 | 21 |
| CMP_Economy_HoardsporeRegent_Elite | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Normal | 1.220 | 0.956–1.484 | 33 |
| CMP_Economy_HoardsporeRegent_Elite | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | — | 1.691 | 1.371–2.012 | 21 |
| TST_Campaign7_KillReclaim_Offset8 | crowded.medium.square.rectangle.generated.100x100 | — | 1.422 | 1.122–1.723 | 31 |
| TST_Campaign7_KillReclaim_Offset8 | crowded.small.square.rectangle.generated.80x80 | Normal | 1.181 | 1.032–1.330 | 45 |
| TST_Campaign7_KillReclaim_Offset8 | smalltable.small.square.rectangle.generated.80x80 | — | 1.187 | 0.835–1.538 | 23 |
| TST_Campaign7_KillReclaim_Offset8 | swarm.medium.square.rectangle.generated.120x120 | Elite | 1.517 | 1.254–1.781 | 29 |
| TST_Campaign7_KillReclaim_Offset8 | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Hard | 1.308 | 1.082–1.535 | 25 |
| TST_Campaign7_KillReclaim_Offset8 | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Hard | 1.374 | 1.174–1.574 | 30 |
| CMP_Bloom_BeaconRegression_Medium | crowded.medium.square.rectangle.generated.100x100 | Normal | 1.113 | 0.913–1.314 | 39 |
| CMP_Bloom_BeaconRegression_Medium | crowded.small.square.rectangle.generated.80x80 | Hard | 1.292 | 1.109–1.474 | 30 |
| CMP_Bloom_BeaconRegression_Medium | smalltable.small.square.rectangle.generated.80x80 | Hard | 1.373 | 1.122–1.624 | 32 |
| CMP_Bloom_BeaconRegression_Medium | swarm.medium.square.rectangle.generated.120x120 | Hard | 1.439 | 1.197–1.681 | 33 |
| CMP_Bloom_BeaconRegression_Medium | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Hard | 1.490 | 1.199–1.781 | 29 |
| CMP_Bloom_BeaconRegression_Medium | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Hard | 1.301 | 1.124–1.479 | 30 |
| CMP_Defense_IronShell_Elite | crowded.medium.square.rectangle.generated.100x100 | — | 1.317 | 1.006–1.627 | 27 |
| CMP_Defense_IronShell_Elite | crowded.small.square.rectangle.generated.80x80 | — | 1.331 | 1.197–1.466 | 24 |
| CMP_Defense_IronShell_Elite | smalltable.small.square.rectangle.generated.80x80 | Normal | 1.169 | 0.982–1.356 | 36 |
| CMP_Defense_IronShell_Elite | swarm.medium.square.rectangle.generated.120x120 | Hard | 1.344 | 1.112–1.576 | 36 |
| CMP_Defense_IronShell_Elite | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Normal | 0.964 | 0.694–1.234 | 27 |
| CMP_Defense_IronShell_Elite | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Normal | 1.112 | 1.008–1.215 | 32 |
| CMP_Bloom_CreepingNecro_Medium | crowded.medium.square.rectangle.generated.100x100 | Normal | 1.228 | 0.962–1.494 | 38 |
| CMP_Bloom_CreepingNecro_Medium | crowded.small.square.rectangle.generated.80x80 | Hard | 1.303 | 1.112–1.493 | 37 |
| CMP_Bloom_CreepingNecro_Medium | smalltable.small.square.rectangle.generated.80x80 | Normal | 1.142 | 0.894–1.389 | 34 |
| CMP_Bloom_CreepingNecro_Medium | swarm.medium.square.rectangle.generated.120x120 | Normal | 1.168 | 1.020–1.315 | 36 |
| CMP_Bloom_CreepingNecro_Medium | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | — | 1.297 | 1.069–1.524 | 23 |
| CMP_Bloom_CreepingNecro_Medium | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Normal | 1.199 | 1.022–1.375 | 39 |
| AI4 | crowded.medium.square.rectangle.generated.100x100 | Normal | 1.104 | 0.931–1.276 | 43 |
| AI4 | crowded.small.square.rectangle.generated.80x80 | Hard | 1.341 | 1.137–1.546 | 36 |
| AI4 | smalltable.small.square.rectangle.generated.80x80 | Normal | 1.161 | 0.994–1.329 | 34 |
| AI4 | swarm.medium.square.rectangle.generated.120x120 | Hard | 1.198 | 1.065–1.332 | 38 |
| AI4 | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Normal | 1.128 | 0.951–1.305 | 36 |
| AI4 | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | — | 1.471 | 1.159–1.783 | 35 |
| AI5 | crowded.medium.square.rectangle.generated.100x100 | Normal | 1.247 | 1.034–1.460 | 28 |
| AI5 | crowded.small.square.rectangle.generated.80x80 | Hard | 1.259 | 1.136–1.382 | 35 |
| AI5 | smalltable.small.square.rectangle.generated.80x80 | Normal | 1.128 | 0.918–1.339 | 26 |
| AI5 | swarm.medium.square.rectangle.generated.120x120 | — | 1.104 | 0.943–1.264 | 23 |
| AI5 | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | — | 1.284 | 1.115–1.454 | 17 |
| AI5 | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Hard | 1.365 | 1.171–1.559 | 34 |
| CMP_Economy_LateSpike_Hard | crowded.medium.square.rectangle.generated.100x100 | Normal | 1.283 | 1.036–1.530 | 31 |
| CMP_Economy_LateSpike_Hard | crowded.small.square.rectangle.generated.80x80 | Normal | 1.149 | 1.044–1.255 | 38 |
| CMP_Economy_LateSpike_Hard | smalltable.small.square.rectangle.generated.80x80 | Normal | 1.018 | 0.866–1.170 | 38 |
| CMP_Economy_LateSpike_Hard | swarm.medium.square.rectangle.generated.120x120 | — | 1.309 | 1.008–1.610 | 25 |
| CMP_Economy_LateSpike_Hard | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | — | 1.238 | 0.885–1.591 | 27 |
| CMP_Economy_LateSpike_Hard | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Hard | 1.280 | 1.078–1.482 | 27 |
| CMP_Bloom_CreepingRegression_Elite | crowded.medium.square.rectangle.generated.100x100 | Normal | 1.142 | 0.953–1.331 | 40 |
| CMP_Bloom_CreepingRegression_Elite | crowded.small.square.rectangle.generated.80x80 | Normal | 1.109 | 0.834–1.384 | 31 |
| CMP_Bloom_CreepingRegression_Elite | smalltable.small.square.rectangle.generated.80x80 | — | 1.109 | 0.750–1.469 | 22 |
| CMP_Bloom_CreepingRegression_Elite | swarm.medium.square.rectangle.generated.120x120 | Normal | 1.083 | 0.871–1.295 | 36 |
| CMP_Bloom_CreepingRegression_Elite | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | — | 1.188 | 0.923–1.453 | 22 |
| CMP_Bloom_CreepingRegression_Elite | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | — | 1.249 | 0.937–1.560 | 23 |
| CMP_Economy_TempoReclaim_Medium | crowded.medium.square.rectangle.generated.100x100 | Normal | 1.072 | 0.913–1.231 | 37 |
| CMP_Economy_TempoReclaim_Medium | crowded.small.square.rectangle.generated.80x80 | Normal | 1.030 | 0.905–1.156 | 33 |
| CMP_Economy_TempoReclaim_Medium | smalltable.small.square.rectangle.generated.80x80 | Normal | 1.221 | 0.951–1.491 | 39 |
| CMP_Economy_TempoReclaim_Medium | swarm.medium.square.rectangle.generated.120x120 | Normal | 1.101 | 0.865–1.336 | 25 |
| CMP_Economy_TempoReclaim_Medium | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Normal | 1.156 | 0.882–1.430 | 28 |
| CMP_Economy_TempoReclaim_Medium | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | — | 1.123 | 0.790–1.455 | 23 |
| CMP_Reclaim_Scavenger_Easy | crowded.medium.square.rectangle.generated.100x100 | Normal | 1.041 | 0.805–1.277 | 36 |
| CMP_Reclaim_Scavenger_Easy | crowded.small.square.rectangle.generated.80x80 | Normal | 1.042 | 0.849–1.234 | 35 |
| CMP_Reclaim_Scavenger_Easy | smalltable.small.square.rectangle.generated.80x80 | — | 1.094 | 0.878–1.309 | 23 |
| CMP_Reclaim_Scavenger_Easy | swarm.medium.square.rectangle.generated.120x120 | Normal | 1.200 | 0.975–1.425 | 33 |
| CMP_Reclaim_Scavenger_Easy | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Hard | 1.306 | 1.067–1.546 | 36 |
| CMP_Reclaim_Scavenger_Easy | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Normal | 0.947 | 0.842–1.052 | 29 |
| CMP_Growth_Pressure_Medium | crowded.medium.square.rectangle.generated.100x100 | Easy | 0.830 | 0.718–0.943 | 26 |
| CMP_Growth_Pressure_Medium | crowded.small.square.rectangle.generated.80x80 | Normal | 1.009 | 0.873–1.144 | 27 |
| CMP_Growth_Pressure_Medium | smalltable.small.square.rectangle.generated.80x80 | Normal | 1.120 | 0.919–1.321 | 26 |
| CMP_Growth_Pressure_Medium | swarm.medium.square.rectangle.generated.120x120 | Easy | 0.780 | 0.698–0.862 | 27 |
| CMP_Growth_Pressure_Medium | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Easy | 0.830 | 0.716–0.944 | 32 |
| CMP_Growth_Pressure_Medium | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | — | 0.783 | 0.703–0.863 | 24 |
| TST_CampaignPlayer_SafeBaseline | crowded.medium.square.rectangle.generated.100x100 | Easy | 0.801 | 0.728–0.874 | 42 |
| TST_CampaignPlayer_SafeBaseline | crowded.small.square.rectangle.generated.80x80 | Normal | 1.092 | 0.981–1.204 | 36 |
| TST_CampaignPlayer_SafeBaseline | smalltable.small.square.rectangle.generated.80x80 | Normal | 0.932 | 0.855–1.009 | 31 |
| TST_CampaignPlayer_SafeBaseline | swarm.medium.square.rectangle.generated.120x120 | Easy | 0.774 | 0.683–0.865 | 28 |
| TST_CampaignPlayer_SafeBaseline | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | — | 1.020 | 0.763–1.277 | 19 |
| TST_CampaignPlayer_SafeBaseline | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Easy | 0.759 | 0.679–0.839 | 31 |
| CMP_Bloom_Thanatophyte_Elite | crowded.medium.square.rectangle.generated.100x100 | Easy | 0.722 | 0.614–0.830 | 26 |
| CMP_Bloom_Thanatophyte_Elite | crowded.small.square.rectangle.generated.80x80 | Normal | 1.008 | 0.945–1.071 | 39 |
| CMP_Bloom_Thanatophyte_Elite | smalltable.small.square.rectangle.generated.80x80 | Normal | 0.898 | 0.692–1.105 | 35 |
| CMP_Bloom_Thanatophyte_Elite | swarm.medium.square.rectangle.generated.120x120 | Easy | 0.756 | 0.692–0.819 | 26 |
| CMP_Bloom_Thanatophyte_Elite | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Normal | 0.848 | 0.743–0.952 | 30 |
| CMP_Bloom_Thanatophyte_Elite | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Easy | 0.826 | 0.769–0.883 | 37 |
| CMP_Bloom_NecrotoxinGauntlet_Elite | crowded.medium.square.rectangle.generated.100x100 | Easy | 0.674 | 0.579–0.768 | 40 |
| CMP_Bloom_NecrotoxinGauntlet_Elite | crowded.small.square.rectangle.generated.80x80 | Normal | 0.860 | 0.753–0.968 | 40 |
| CMP_Bloom_NecrotoxinGauntlet_Elite | smalltable.small.square.rectangle.generated.80x80 | Normal | 0.971 | 0.822–1.119 | 37 |
| CMP_Bloom_NecrotoxinGauntlet_Elite | swarm.medium.square.rectangle.generated.120x120 | — | 0.830 | 0.752–0.907 | 23 |
| CMP_Bloom_NecrotoxinGauntlet_Elite | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Normal | 1.101 | 0.838–1.364 | 33 |
| CMP_Bloom_NecrotoxinGauntlet_Elite | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Easy | 0.791 | 0.704–0.878 | 25 |
| CMP_Growth_WildfireBloom_Medium | crowded.medium.square.rectangle.generated.100x100 | Easy | 0.727 | 0.668–0.786 | 35 |
| CMP_Growth_WildfireBloom_Medium | crowded.small.square.rectangle.generated.80x80 | Normal | 0.950 | 0.884–1.017 | 45 |
| CMP_Growth_WildfireBloom_Medium | smalltable.small.square.rectangle.generated.80x80 | Normal | 0.795 | 0.634–0.955 | 26 |
| CMP_Growth_WildfireBloom_Medium | swarm.medium.square.rectangle.generated.120x120 | Easy | 0.701 | 0.625–0.777 | 29 |
| CMP_Growth_WildfireBloom_Medium | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Easy | 0.765 | 0.658–0.872 | 36 |
| CMP_Growth_WildfireBloom_Medium | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | — | 0.686 | 0.641–0.731 | 23 |
| CMP_Bloom_FortifyMimic_Medium | crowded.medium.square.rectangle.generated.100x100 | Easy | 0.695 | 0.631–0.759 | 29 |
| CMP_Bloom_FortifyMimic_Medium | crowded.small.square.rectangle.generated.80x80 | Normal | 0.893 | 0.817–0.968 | 33 |
| CMP_Bloom_FortifyMimic_Medium | smalltable.small.square.rectangle.generated.80x80 | Easy | 0.544 | 0.464–0.623 | 28 |
| CMP_Bloom_FortifyMimic_Medium | swarm.medium.square.rectangle.generated.120x120 | Easy | 0.834 | 0.775–0.892 | 41 |
| CMP_Bloom_FortifyMimic_Medium | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | — | 0.622 | 0.510–0.734 | 23 |
| CMP_Bloom_FortifyMimic_Medium | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Easy | 0.765 | 0.706–0.824 | 35 |
| CMP_TierCap_GrowthResilience_Easy | crowded.medium.square.rectangle.generated.100x100 | Easy | 0.598 | 0.491–0.705 | 30 |
| CMP_TierCap_GrowthResilience_Easy | crowded.small.square.rectangle.generated.80x80 | Easy | 0.817 | 0.690–0.944 | 28 |
| CMP_TierCap_GrowthResilience_Easy | smalltable.small.square.rectangle.generated.80x80 | Normal | 0.853 | 0.738–0.968 | 28 |
| CMP_TierCap_GrowthResilience_Easy | swarm.medium.square.rectangle.generated.120x120 | Easy | 0.644 | 0.494–0.793 | 29 |
| CMP_TierCap_GrowthResilience_Easy | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Easy | 0.721 | 0.594–0.849 | 47 |
| CMP_TierCap_GrowthResilience_Easy | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Easy | 0.587 | 0.495–0.679 | 25 |
| AI6 | crowded.medium.square.rectangle.generated.100x100 | Easy | 0.654 | 0.574–0.733 | 34 |
| AI6 | crowded.small.square.rectangle.generated.80x80 | Easy | 0.787 | 0.691–0.882 | 37 |
| AI6 | smalltable.small.square.rectangle.generated.80x80 | Easy | 0.829 | 0.717–0.942 | 30 |
| AI6 | swarm.medium.square.rectangle.generated.120x120 | Easy | 0.628 | 0.550–0.707 | 38 |
| AI6 | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Easy | 0.775 | 0.651–0.898 | 31 |
| AI6 | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Easy | 0.676 | 0.615–0.737 | 34 |
| CMP_Defense_ReclaimShell_Easy | crowded.medium.square.rectangle.generated.100x100 | Easy | 0.647 | 0.586–0.708 | 35 |
| CMP_Defense_ReclaimShell_Easy | crowded.small.square.rectangle.generated.80x80 | Easy | 0.771 | 0.707–0.836 | 40 |
| CMP_Defense_ReclaimShell_Easy | smalltable.small.square.rectangle.generated.80x80 | Easy | 0.721 | 0.614–0.828 | 28 |
| CMP_Defense_ReclaimShell_Easy | swarm.medium.square.rectangle.generated.120x120 | Easy | 0.663 | 0.597–0.730 | 29 |
| CMP_Defense_ReclaimShell_Easy | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Easy | 0.644 | 0.566–0.722 | 34 |
| CMP_Defense_ReclaimShell_Easy | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Easy | 0.670 | 0.629–0.711 | 25 |
| TST_AI10_CreepingRegression | crowded.medium.square.rectangle.generated.100x100 | Easy | 0.620 | 0.524–0.717 | 34 |
| TST_AI10_CreepingRegression | crowded.small.square.rectangle.generated.80x80 | Easy | 0.622 | 0.529–0.715 | 26 |
| TST_AI10_CreepingRegression | smalltable.small.square.rectangle.generated.80x80 | Easy | 0.677 | 0.591–0.763 | 33 |
| TST_AI10_CreepingRegression | swarm.medium.square.rectangle.generated.120x120 | — | 0.677 | 0.541–0.814 | 22 |
| TST_AI10_CreepingRegression | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Easy | 0.548 | 0.425–0.672 | 30 |
| TST_AI10_CreepingRegression | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Easy | 0.553 | 0.479–0.626 | 26 |
| CMP_Surge_Pulsar_Easy | crowded.medium.square.rectangle.generated.100x100 | Easy | 0.592 | 0.534–0.649 | 35 |
| CMP_Surge_Pulsar_Easy | crowded.small.square.rectangle.generated.80x80 | Easy | 0.743 | 0.695–0.792 | 48 |
| CMP_Surge_Pulsar_Easy | smalltable.small.square.rectangle.generated.80x80 | Easy | 0.655 | 0.579–0.730 | 33 |
| CMP_Surge_Pulsar_Easy | swarm.medium.square.rectangle.generated.120x120 | Easy | 0.543 | 0.500–0.587 | 33 |
| CMP_Surge_Pulsar_Easy | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Easy | 0.590 | 0.477–0.703 | 25 |
| CMP_Surge_Pulsar_Easy | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Easy | 0.537 | 0.480–0.594 | 28 |
| TST_AI10_BeaconRegression | crowded.medium.square.rectangle.generated.100x100 | Easy | 0.476 | 0.411–0.540 | 35 |
| TST_AI10_BeaconRegression | crowded.small.square.rectangle.generated.80x80 | Easy | 0.556 | 0.469–0.644 | 26 |
| TST_AI10_BeaconRegression | smalltable.small.square.rectangle.generated.80x80 | — | 0.584 | 0.460–0.709 | 24 |
| TST_AI10_BeaconRegression | swarm.medium.square.rectangle.generated.120x120 | Easy | 0.499 | 0.370–0.628 | 26 |
| TST_AI10_BeaconRegression | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Easy | 0.558 | 0.447–0.669 | 33 |
| TST_AI10_BeaconRegression | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Easy | 0.495 | 0.434–0.557 | 35 |
| CMP_AnabolicBeaconRhizolith_Elite | crowded.medium.square.rectangle.generated.100x100 | Easy | 0.445 | 0.363–0.527 | 28 |
| CMP_AnabolicBeaconRhizolith_Elite | crowded.small.square.rectangle.generated.80x80 | Easy | 0.514 | 0.435–0.593 | 33 |
| CMP_AnabolicBeaconRhizolith_Elite | smalltable.small.square.rectangle.generated.80x80 | Easy | 0.592 | 0.476–0.708 | 35 |
| CMP_AnabolicBeaconRhizolith_Elite | swarm.medium.square.rectangle.generated.120x120 | Easy | 0.431 | 0.368–0.493 | 34 |
| CMP_AnabolicBeaconRhizolith_Elite | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Easy | 0.638 | 0.451–0.826 | 25 |
| CMP_AnabolicBeaconRhizolith_Elite | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Easy | 0.452 | 0.402–0.501 | 35 |
| CMP_Mobility_Overextender_Training | crowded.medium.square.rectangle.generated.100x100 | Easy | 0.431 | 0.375–0.488 | 35 |
| CMP_Mobility_Overextender_Training | crowded.small.square.rectangle.generated.80x80 | Easy | 0.574 | 0.502–0.646 | 34 |
| CMP_Mobility_Overextender_Training | smalltable.small.square.rectangle.generated.80x80 | Easy | 0.521 | 0.421–0.622 | 26 |
| CMP_Mobility_Overextender_Training | swarm.medium.square.rectangle.generated.120x120 | Easy | 0.446 | 0.403–0.489 | 38 |
| CMP_Mobility_Overextender_Training | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Easy | 0.428 | 0.338–0.518 | 25 |
| CMP_Mobility_Overextender_Training | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | — | 0.341 | 0.289–0.392 | 23 |
| CMP_Defense_ResilientShell_Easy | crowded.medium.square.rectangle.generated.100x100 | Easy | 0.436 | 0.370–0.502 | 41 |
| CMP_Defense_ResilientShell_Easy | crowded.small.square.rectangle.generated.80x80 | Easy | 0.451 | 0.390–0.512 | 33 |
| CMP_Defense_ResilientShell_Easy | smalltable.small.square.rectangle.generated.80x80 | Easy | 0.451 | 0.341–0.561 | 32 |
| CMP_Defense_ResilientShell_Easy | swarm.medium.square.rectangle.generated.120x120 | — | 0.500 | 0.408–0.592 | 23 |
| CMP_Defense_ResilientShell_Easy | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Easy | 0.422 | 0.340–0.505 | 30 |
| CMP_Defense_ResilientShell_Easy | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Easy | 0.421 | 0.364–0.479 | 28 |
| CMP_Mobility_Overextender_Training_Offset1 | crowded.medium.square.rectangle.generated.100x100 | Easy | 0.430 | 0.387–0.473 | 33 |
| CMP_Mobility_Overextender_Training_Offset1 | crowded.small.square.rectangle.generated.80x80 | Easy | 0.465 | 0.391–0.539 | 29 |
| CMP_Mobility_Overextender_Training_Offset1 | smalltable.small.square.rectangle.generated.80x80 | Easy | 0.470 | 0.403–0.537 | 34 |
| CMP_Mobility_Overextender_Training_Offset1 | swarm.medium.square.rectangle.generated.120x120 | Easy | 0.422 | 0.370–0.474 | 30 |
| CMP_Mobility_Overextender_Training_Offset1 | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | — | 0.424 | 0.359–0.488 | 22 |
| CMP_Mobility_Overextender_Training_Offset1 | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Easy | 0.398 | 0.354–0.443 | 33 |
| CMP_Mobility_Overextender_Training_Offset2 | crowded.medium.square.rectangle.generated.100x100 | Easy | 0.419 | 0.373–0.464 | 44 |
| CMP_Mobility_Overextender_Training_Offset2 | crowded.small.square.rectangle.generated.80x80 | — | 0.499 | 0.436–0.561 | 21 |
| CMP_Mobility_Overextender_Training_Offset2 | smalltable.small.square.rectangle.generated.80x80 | — | 0.473 | 0.388–0.558 | 24 |
| CMP_Mobility_Overextender_Training_Offset2 | swarm.medium.square.rectangle.generated.120x120 | Easy | 0.426 | 0.381–0.470 | 30 |
| CMP_Mobility_Overextender_Training_Offset2 | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Easy | 0.392 | 0.328–0.456 | 29 |
| CMP_Mobility_Overextender_Training_Offset2 | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Easy | 0.385 | 0.340–0.430 | 31 |
| CMP_Mobility_Overextender_Training_Offset3 | crowded.medium.square.rectangle.generated.100x100 | Easy | 0.411 | 0.359–0.463 | 38 |
| CMP_Mobility_Overextender_Training_Offset3 | crowded.small.square.rectangle.generated.80x80 | Easy | 0.554 | 0.476–0.631 | 32 |
| CMP_Mobility_Overextender_Training_Offset3 | smalltable.small.square.rectangle.generated.80x80 | Easy | 0.331 | 0.278–0.384 | 32 |
| CMP_Mobility_Overextender_Training_Offset3 | swarm.medium.square.rectangle.generated.120x120 | Easy | 0.360 | 0.305–0.415 | 30 |
| CMP_Mobility_Overextender_Training_Offset3 | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Easy | 0.405 | 0.342–0.467 | 30 |
| CMP_Mobility_Overextender_Training_Offset3 | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Easy | 0.402 | 0.358–0.447 | 25 |
| TST_Training_ResilientMycelium_Offset3 | crowded.medium.square.rectangle.generated.100x100 | Easy | 0.319 | 0.269–0.369 | 33 |
| TST_Training_ResilientMycelium_Offset3 | crowded.small.square.rectangle.generated.80x80 | Easy | 0.405 | 0.357–0.453 | 31 |
| TST_Training_ResilientMycelium_Offset3 | smalltable.small.square.rectangle.generated.80x80 | Easy | 0.506 | 0.408–0.605 | 27 |
| TST_Training_ResilientMycelium_Offset3 | swarm.medium.square.rectangle.generated.120x120 | Easy | 0.445 | 0.377–0.514 | 32 |
| TST_Training_ResilientMycelium_Offset3 | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Easy | 0.339 | 0.283–0.396 | 36 |
| TST_Training_ResilientMycelium_Offset3 | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Easy | 0.443 | 0.374–0.511 | 30 |
| TST_Training_ResilientMycelium_Offset1 | crowded.medium.square.rectangle.generated.100x100 | Easy | 0.394 | 0.330–0.458 | 25 |
| TST_Training_ResilientMycelium_Offset1 | crowded.small.square.rectangle.generated.80x80 | Easy | 0.468 | 0.411–0.525 | 35 |
| TST_Training_ResilientMycelium_Offset1 | smalltable.small.square.rectangle.generated.80x80 | Easy | 0.258 | 0.209–0.307 | 32 |
| TST_Training_ResilientMycelium_Offset1 | swarm.medium.square.rectangle.generated.120x120 | Easy | 0.466 | 0.402–0.530 | 36 |
| TST_Training_ResilientMycelium_Offset1 | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Easy | 0.273 | 0.225–0.321 | 36 |
| TST_Training_ResilientMycelium_Offset1 | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Easy | 0.422 | 0.369–0.475 | 42 |
| TST_Training_ResilientMycelium | crowded.medium.square.rectangle.generated.100x100 | Easy | 0.345 | 0.277–0.413 | 34 |
| TST_Training_ResilientMycelium | crowded.small.square.rectangle.generated.80x80 | Easy | 0.377 | 0.321–0.432 | 31 |
| TST_Training_ResilientMycelium | smalltable.small.square.rectangle.generated.80x80 | Easy | 0.377 | 0.288–0.467 | 27 |
| TST_Training_ResilientMycelium | swarm.medium.square.rectangle.generated.120x120 | Easy | 0.384 | 0.313–0.454 | 34 |
| TST_Training_ResilientMycelium | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | — | 0.306 | 0.236–0.376 | 21 |
| TST_Training_ResilientMycelium | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Easy | 0.437 | 0.373–0.501 | 34 |
| CMP_Surge_BeaconTempo_Medium | crowded.medium.square.rectangle.generated.100x100 | — | 0.270 | 0.214–0.326 | 22 |
| CMP_Surge_BeaconTempo_Medium | crowded.small.square.rectangle.generated.80x80 | Easy | 0.409 | 0.335–0.484 | 33 |
| CMP_Surge_BeaconTempo_Medium | smalltable.small.square.rectangle.generated.80x80 | Easy | 0.443 | 0.362–0.525 | 30 |
| CMP_Surge_BeaconTempo_Medium | swarm.medium.square.rectangle.generated.120x120 | Easy | 0.298 | 0.247–0.350 | 33 |
| CMP_Surge_BeaconTempo_Medium | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Easy | 0.344 | 0.270–0.417 | 31 |
| CMP_Surge_BeaconTempo_Medium | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Easy | 0.276 | 0.224–0.328 | 30 |
| CMP_Surge_BeaconSprinter_Medium | crowded.medium.square.rectangle.generated.100x100 | Easy | 0.248 | 0.190–0.307 | 30 |
| CMP_Surge_BeaconSprinter_Medium | crowded.small.square.rectangle.generated.80x80 | Easy | 0.379 | 0.320–0.437 | 38 |
| CMP_Surge_BeaconSprinter_Medium | smalltable.small.square.rectangle.generated.80x80 | Easy | 0.433 | 0.311–0.554 | 35 |
| CMP_Surge_BeaconSprinter_Medium | swarm.medium.square.rectangle.generated.120x120 | Easy | 0.356 | 0.276–0.437 | 35 |
| CMP_Surge_BeaconSprinter_Medium | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Easy | 0.384 | 0.267–0.501 | 29 |
| CMP_Surge_BeaconSprinter_Medium | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Easy | 0.322 | 0.270–0.373 | 34 |
| CMP_Surge_GrowthTempo_Medium | crowded.medium.square.rectangle.generated.100x100 | Easy | 0.259 | 0.217–0.300 | 34 |
| CMP_Surge_GrowthTempo_Medium | crowded.small.square.rectangle.generated.80x80 | Easy | 0.350 | 0.301–0.399 | 37 |
| CMP_Surge_GrowthTempo_Medium | smalltable.small.square.rectangle.generated.80x80 | Easy | 0.339 | 0.283–0.396 | 37 |
| CMP_Surge_GrowthTempo_Medium | swarm.medium.square.rectangle.generated.120x120 | Easy | 0.260 | 0.187–0.334 | 27 |
| CMP_Surge_GrowthTempo_Medium | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Easy | 0.277 | 0.228–0.326 | 39 |
| CMP_Surge_GrowthTempo_Medium | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Easy | 0.254 | 0.200–0.307 | 38 |
| CMP_Reclaim_InfiltrationSurge_Easy | crowded.medium.square.rectangle.generated.100x100 | Easy | 0.185 | 0.146–0.224 | 37 |
| CMP_Reclaim_InfiltrationSurge_Easy | crowded.small.square.rectangle.generated.80x80 | Easy | 0.339 | 0.264–0.413 | 37 |
| CMP_Reclaim_InfiltrationSurge_Easy | smalltable.small.square.rectangle.generated.80x80 | Easy | 0.304 | 0.196–0.412 | 28 |
| CMP_Reclaim_InfiltrationSurge_Easy | swarm.medium.square.rectangle.generated.120x120 | Easy | 0.262 | 0.205–0.320 | 32 |
| CMP_Reclaim_InfiltrationSurge_Easy | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Easy | 0.480 | 0.278–0.682 | 31 |
| CMP_Reclaim_InfiltrationSurge_Easy | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Easy | 0.255 | 0.211–0.299 | 29 |
| CMP_Attrition_ToxicTurtle_Training | crowded.medium.square.rectangle.generated.100x100 | Easy | 0.150 | 0.098–0.203 | 26 |
| CMP_Attrition_ToxicTurtle_Training | crowded.small.square.rectangle.generated.80x80 | — | 0.089 | 0.060–0.119 | 22 |
| CMP_Attrition_ToxicTurtle_Training | smalltable.small.square.rectangle.generated.80x80 | Easy | 0.190 | 0.111–0.269 | 27 |
| CMP_Attrition_ToxicTurtle_Training | swarm.medium.square.rectangle.generated.120x120 | Easy | 0.166 | 0.098–0.233 | 30 |
| CMP_Attrition_ToxicTurtle_Training | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Easy | 0.167 | 0.111–0.223 | 40 |
| CMP_Attrition_ToxicTurtle_Training | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Easy | 0.139 | 0.079–0.200 | 26 |
| CMP_Attrition_ToxicTurtle_Training_Offset1 | crowded.medium.square.rectangle.generated.100x100 | Easy | 0.161 | 0.112–0.209 | 27 |
| CMP_Attrition_ToxicTurtle_Training_Offset1 | crowded.small.square.rectangle.generated.80x80 | Easy | 0.128 | 0.090–0.167 | 36 |
| CMP_Attrition_ToxicTurtle_Training_Offset1 | smalltable.small.square.rectangle.generated.80x80 | Easy | 0.155 | 0.108–0.201 | 38 |
| CMP_Attrition_ToxicTurtle_Training_Offset1 | swarm.medium.square.rectangle.generated.120x120 | Easy | 0.119 | 0.070–0.168 | 29 |
| CMP_Attrition_ToxicTurtle_Training_Offset1 | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Easy | 0.177 | 0.107–0.247 | 31 |
| CMP_Attrition_ToxicTurtle_Training_Offset1 | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Easy | 0.181 | 0.124–0.238 | 33 |
| CMP_Attrition_ToxicTurtle_Training_Offset2 | crowded.medium.square.rectangle.generated.100x100 | Easy | 0.148 | 0.102–0.195 | 39 |
| CMP_Attrition_ToxicTurtle_Training_Offset2 | crowded.small.square.rectangle.generated.80x80 | Easy | 0.099 | 0.061–0.137 | 32 |
| CMP_Attrition_ToxicTurtle_Training_Offset2 | smalltable.small.square.rectangle.generated.80x80 | Easy | 0.119 | 0.075–0.164 | 32 |
| CMP_Attrition_ToxicTurtle_Training_Offset2 | swarm.medium.square.rectangle.generated.120x120 | Easy | 0.202 | 0.131–0.272 | 27 |
| CMP_Attrition_ToxicTurtle_Training_Offset2 | smalltable.small.tall.rectangle.generated.60x100 *(holdout)* | Easy | 0.131 | 0.076–0.186 | 29 |
| CMP_Attrition_ToxicTurtle_Training_Offset2 | swarm.medium.wide.rectangle.generated.140x100 *(holdout)* | Easy | 0.192 | 0.119–0.264 | 27 |

## Evidence gaps

- CMP_Bloom_ToxinborneBallistospore_Hard: IntervalTooWide on 135 games overall.
- CMP_Bloom_ToxinborneJetting_Medium: IntervalTooWide on 122 games overall.
- CMP_Bloom_ToxinborneBallistospore_Hard in crowded.medium.square.rectangle.generated.100x100: IntervalTooWide on 39 games.
- CMP_Bloom_ToxinborneBallistospore_Hard in crowded.small.square.rectangle.generated.80x80: IntervalTooWide on 29 games.
- CMP_Bloom_ToxinborneBallistospore_Hard in swarm.medium.square.rectangle.generated.120x120: IntervalTooWide on 34 games.
- CMP_Bloom_ToxinborneBallistospore_Hard in smalltable.small.tall.rectangle.generated.60x100: IntervalTooWide on 40 games.
- CMP_Bloom_ToxinborneBallistospore_Hard in swarm.medium.wide.rectangle.generated.140x100: IntervalTooWide on 34 games.
- AI13 in smalltable.small.square.rectangle.generated.80x80: TooFewGames on 22 games.
- AI13 in swarm.medium.wide.rectangle.generated.140x100: IntervalTooWide on 34 games.
- CMP_Bloom_ToxinborneJetting_Medium in crowded.medium.square.rectangle.generated.100x100: IntervalTooWide on 36 games.
- CMP_Bloom_ToxinborneJetting_Medium in crowded.small.square.rectangle.generated.80x80: IntervalTooWide on 29 games.
- CMP_Bloom_ToxinborneJetting_Medium in smalltable.small.square.rectangle.generated.80x80: TooFewGames on 24 games.
- CMP_Bloom_ToxinborneJetting_Medium in swarm.medium.square.rectangle.generated.120x120: IntervalTooWide on 33 games.
- CMP_Bloom_ToxinborneJetting_Medium in smalltable.small.tall.rectangle.generated.60x100: IntervalTooWide on 40 games.
- AI12 in smalltable.small.square.rectangle.generated.80x80: TooFewGames on 24 games.
- AI12 in swarm.medium.square.rectangle.generated.120x120: TooFewGames on 21 games.
- AI12 in smalltable.small.tall.rectangle.generated.60x100: IntervalTooWide on 26 games.
- AI12 in swarm.medium.wide.rectangle.generated.140x100: IntervalTooWide on 31 games.
- CMP_Bloom_AnabolicRegression_Medium in swarm.medium.wide.rectangle.generated.140x100: IntervalTooWide on 25 games.
- CMP_Control_AnabolicFirst_Hard in swarm.medium.square.rectangle.generated.120x120: IntervalTooWide on 27 games.
- CMP_Control_AnabolicFirst_Hard in swarm.medium.wide.rectangle.generated.140x100: IntervalTooWide on 25 games.
- CMP_Economy_Economancer_Elite in smalltable.small.square.rectangle.generated.80x80: TooFewGames on 23 games.
- CMP_Control_AnabolicRebirth_Medium in crowded.medium.square.rectangle.generated.100x100: IntervalTooWide on 30 games.
- CMP_Control_AnabolicRebirth_Medium in swarm.medium.wide.rectangle.generated.140x100: IntervalTooWide on 31 games.
- CMP_Control_RebirthFurnace_Medium in swarm.medium.square.rectangle.generated.120x120: TooFewGames on 19 games.
- CMP_Control_RebirthFurnace_Medium in swarm.medium.wide.rectangle.generated.140x100: TooFewGames on 24 games.
- TST_Campaign7_KillReclaim_Offset2 in swarm.medium.square.rectangle.generated.120x120: IntervalTooWide on 28 games.
- TST_Campaign7_KillReclaim_Offset2 in smalltable.small.tall.rectangle.generated.60x100: IntervalTooWide on 28 games.
- TST_Campaign7_KillReclaim_Offset3 in crowded.small.square.rectangle.generated.80x80: TooFewGames on 20 games.
- TST_Campaign7_KillReclaim_Offset3 in swarm.medium.square.rectangle.generated.120x120: IntervalTooWide on 32 games.
- TST_Campaign7_KillReclaim_Offset1 in crowded.medium.square.rectangle.generated.100x100: IntervalTooWide on 38 games.
- CMP_Economy_HoardsporeRegent_Elite in crowded.medium.square.rectangle.generated.100x100: TooFewGames on 24 games.
- CMP_Economy_HoardsporeRegent_Elite in swarm.medium.square.rectangle.generated.120x120: TooFewGames on 21 games.
- CMP_Economy_HoardsporeRegent_Elite in swarm.medium.wide.rectangle.generated.140x100: TooFewGames on 21 games.
- TST_Campaign7_KillReclaim_Offset8 in crowded.medium.square.rectangle.generated.100x100: IntervalTooWide on 31 games.
- TST_Campaign7_KillReclaim_Offset8 in smalltable.small.square.rectangle.generated.80x80: TooFewGames on 23 games.
- CMP_Defense_IronShell_Elite in crowded.medium.square.rectangle.generated.100x100: IntervalTooWide on 27 games.
- CMP_Defense_IronShell_Elite in crowded.small.square.rectangle.generated.80x80: TooFewGames on 24 games.
- CMP_Bloom_CreepingNecro_Medium in smalltable.small.tall.rectangle.generated.60x100: TooFewGames on 23 games.
- AI4 in swarm.medium.wide.rectangle.generated.140x100: IntervalTooWide on 35 games.
- AI5 in swarm.medium.square.rectangle.generated.120x120: TooFewGames on 23 games.
- AI5 in smalltable.small.tall.rectangle.generated.60x100: TooFewGames on 17 games.
- CMP_Economy_LateSpike_Hard in swarm.medium.square.rectangle.generated.120x120: IntervalTooWide on 25 games.
- CMP_Economy_LateSpike_Hard in smalltable.small.tall.rectangle.generated.60x100: IntervalTooWide on 27 games.
- CMP_Bloom_CreepingRegression_Elite in smalltable.small.square.rectangle.generated.80x80: TooFewGames on 22 games.
- CMP_Bloom_CreepingRegression_Elite in smalltable.small.tall.rectangle.generated.60x100: TooFewGames on 22 games.
- CMP_Bloom_CreepingRegression_Elite in swarm.medium.wide.rectangle.generated.140x100: TooFewGames on 23 games.
- CMP_Economy_TempoReclaim_Medium in swarm.medium.wide.rectangle.generated.140x100: TooFewGames on 23 games.
- CMP_Reclaim_Scavenger_Easy in smalltable.small.square.rectangle.generated.80x80: TooFewGames on 23 games.
- CMP_Growth_Pressure_Medium in swarm.medium.wide.rectangle.generated.140x100: TooFewGames on 24 games.
- TST_CampaignPlayer_SafeBaseline in smalltable.small.tall.rectangle.generated.60x100: TooFewGames on 19 games.
- CMP_Bloom_NecrotoxinGauntlet_Elite in swarm.medium.square.rectangle.generated.120x120: TooFewGames on 23 games.
- CMP_Growth_WildfireBloom_Medium in swarm.medium.wide.rectangle.generated.140x100: TooFewGames on 23 games.
- CMP_Bloom_FortifyMimic_Medium in smalltable.small.tall.rectangle.generated.60x100: TooFewGames on 23 games.
- TST_AI10_CreepingRegression in swarm.medium.square.rectangle.generated.120x120: TooFewGames on 22 games.
- TST_AI10_BeaconRegression in smalltable.small.square.rectangle.generated.80x80: TooFewGames on 24 games.
- CMP_Mobility_Overextender_Training in swarm.medium.wide.rectangle.generated.140x100: TooFewGames on 23 games.
- CMP_Defense_ResilientShell_Easy in swarm.medium.square.rectangle.generated.120x120: TooFewGames on 23 games.
- CMP_Mobility_Overextender_Training_Offset1 in smalltable.small.tall.rectangle.generated.60x100: TooFewGames on 22 games.
- CMP_Mobility_Overextender_Training_Offset2 in crowded.small.square.rectangle.generated.80x80: TooFewGames on 21 games.
- CMP_Mobility_Overextender_Training_Offset2 in smalltable.small.square.rectangle.generated.80x80: TooFewGames on 24 games.
- TST_Training_ResilientMycelium in smalltable.small.tall.rectangle.generated.60x100: TooFewGames on 21 games.
- CMP_Surge_BeaconTempo_Medium in crowded.medium.square.rectangle.generated.100x100: TooFewGames on 22 games.
- CMP_Attrition_ToxicTurtle_Training in crowded.small.square.rectangle.generated.80x80: TooFewGames on 22 games.

## Campaign difficulty vs measured strength

This is what the matrix was run to answer, and the answer is that the ladder does not hold.

| Authored tier | Strategies | Median share | Range | Spread |
|---|---:|---:|---|---:|
| Training | 3 | 0.415 | 0.370 – 0.720 | 1.9× |
| Easy | 11 | 0.455 | 0.140 – 1.959 | 14.0× |
| Medium | 25 | 1.194 | 0.141 – 2.062 | 14.7× |
| Hard | 4 | 1.902 | 1.173 – 2.244 | 1.9× |
| **Elite** | 10 | **0.990** | 0.498 – 1.708 | 3.4× |

Three things are wrong, in descending order of how much a player would notice.

**The ladder is non-monotonic at the top.** Ordered by measured median the tiers run
Training `0.415` < Easy `0.455` < Elite `0.990` < Medium `1.194` < Hard `1.902`. `Elite` is the
fourth-strongest tier of five and its median sits *below parity*, meaning the median Elite
encounter loses ground to an average opponent. A player who beats Hard and is then promoted to
Elite gets an easier fight, which is the one thing a difficulty ladder must not do.

**Six of the ten Elite bosses are at or below parity, four of them Easy.**

| Boss | Share | Band |
|---|---:|---|
| CMP_Bloom_CreepingRegression_Elite | 1.112 | Normal |
| CMP_Bloom_Thanatophyte_Elite | 0.867 | Normal |
| CMP_Bloom_NecrotoxinGauntlet_Elite | 0.831 | Easy |
| TST_AI10_CreepingRegression | 0.648 | Easy |
| TST_AI10_BeaconRegression | 0.524 | Easy |
| CMP_AnabolicBeaconRhizolith_Elite | 0.498 | Easy |

`CMP_AnabolicBeaconRhizolith_Elite` is the clearest failure: authored `Strong`, slotted `Elite`,
used as a **Boss**, and measuring `0.498` — it holds half the board share of an average opponent.
That is a broken promise to the player rather than a tuning rounding error.

**The middle tiers are not tiers at all.** `Easy` and `Medium` each span roughly 14×, and they
overlap almost completely: `AI12` is slotted `Easy` and measures `1.959`, second strongest in the
whole panel, while `CMP_Attrition_ToxicTurtle_Training_Offset1` is slotted `Medium` and measures
`0.141`. Twenty-five of the 53 strategies sit in `Medium`, so for nearly half the roster the label
carries almost no information about what the player will face.

By contrast `Training` and `Hard` are internally tight (1.9× each) and correctly ordered relative
to each other. The ladder's ends work; its middle and its top do not.

### Two strategies could not be placed

`CMP_Bloom_ToxinborneBallistospore_Hard` (2.244) and `CMP_Bloom_ToxinborneJetting_Medium` (2.062)
both come back `IntervalTooWide` in every context despite adequate game counts. High pooled means
with intervals that never tighten is the signature of a boom-or-bust strategy, not a reliably
strong one, and they should not be slotted on the pooled mean alone. Measuring their variance
directly is the natural follow-up.

## Proposed reslotting — not applied

The evidence supports a reslotting, but how many encounters belong in each tier and how the
campaign paces them is a design decision, so nothing here has been changed. Applying it is a
metadata edit plus a Unity validation pass.

The defensible mapping is to slot on measured band: `Elite` for pooled share above `1.25`, `Hard`
above `1.05`, `Medium` for the parity band, `Easy` below `0.95`, and `Training` for the bottom.
On this panel that moves the six under-strength bosses down out of `Elite`, promotes
`CMP_Bloom_AnabolicRegression_Medium` (1.934), `CMP_Control_AnabolicRebirth_Medium` (1.619) and
`CMP_Control_RebirthFurnace_Medium` (1.613) into the top tier, and moves `AI12` (1.959) out of
`Easy` entirely.

Two caveats worth settling first. `Boss` is a role, not a difficulty, so a boss that measures Easy
may be a design intent about *shape* rather than an error — but six of ten is too many for that to
be the explanation. And reslotting on pooled strength alone discards the contextual specialists the
report flags; a strategy that is Elite at four players and Easy at eight is badly served by any
single label.
