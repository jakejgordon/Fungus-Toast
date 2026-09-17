# AI P8 Bloom-20 Review Dossier V1

**Status:** promotion approved 2026-09-16. The reviewed candidate is promoted
to the Proven solo roster as **Verdant Reclaimer** under durable strategy ID
`ai.growth.verdant-reclaimer.v1`. Campaign placement remains deferred.

## Candidate and player-facing hypothesis

- Candidate: `CAND_p8-bloom20-contextual-v1_GoalInsertion_9c5157e4`
  (`candidate.anabolic-grow-catabr-putreregen.9c5157e4e166`)
- Parent: `legacy.proven.anabolic-grow-catabr-putreregen.v1`
  (`Anabolic>Grow>CatabR>PutreRegen`)
- Definition change: insert a Mycelial Bloom level-20 goal immediately after
  the Anabolic opener.
- Readable identity: an Elite large-board scaler that invests early in scalable
  growth to reclaim territory after late-board exchanges. This is distinct from
  the parent's regeneration-first recovery framing.

The candidate's observed deterministic build reaches Bloom 21 and Growth 26;
the parent reaches Bloom 10 and Growth 16. It is not an exact observed-build
match for either retained Elite reference.

## Evidence ladder

All parent/candidate stages used rotating slots, no nutrient patches,
Mycovariants, or starting Adaptations, and checksum-valid resolved manifests.
The target metric was paired normalized board share, candidate minus parent,
with a preregistered Increase margin of `+0.05`.

| Stage | Context | Result |
|---|---|---|
| Smoke | 180x140, seed `2026091301`, 5 games/arm | Both arms completed with no parity mismatch. |
| Calibration | 180x140, seed `2026091301`, 20 pairs | `+0.3771` (95% CI `+0.0979..+0.6564`); advanced. |
| Comparison | same screening context, 50 pairs | `supported`: `+0.3513` (95% CI `+0.1634..+0.5391`); 647.956 s within the 900 s budget. |
| Holdout | 200x160, seed `2026091501`, 100 pairs | `supported`: `+0.5494` (95% CI `+0.4484..+0.6503`); 1757.365 s within the preregistered 2100 s budget. |

The originally declared holdout (`2026091401`) is excluded: its control arm
stopped at 97/100 games due to the 900-second cap. It contributed no treatment
result or outcome analysis. The replacement seed and budget were preregistered
from that execution pace before its arms ran.

## Contextual band evidence

Candidate-only calibration against the frozen P7 Proven panel used seven
checksum-valid, per-game-lineup contexts. The five calibration contexts all
conservatively clear the frozen Elite threshold (`1.25`); the pooled lower
bound therefore classifies the candidate **Elite** under
`fungus-toast.ai-bands.v1`. Both untouched classification holdouts also clear
Elite:

| Holdout context | 95% normalized-share interval | Games |
|---|---:|---:|
| Duel-wide | `1.573..1.790` | 26 |
| Small-table tall | `1.574..1.813` | 44 |

One fixed-lineup manual duel-small artifact is retained as an integrity record
but excluded from classification because it omitted `--per-game-lineups`.

## Review conclusion and promotion decision

No mechanical evidence gate remained: the candidate cleared staged performance,
distinct-seed/geometry holdout, identity, and contextual Elite classification.
Jake approved the following product decisions on 2026-09-16:

1. Promote the candidate into the intended solo roster slot E1.
2. Use player-facing name **Verdant Reclaimer**, durable strategy ID
   `ai.growth.verdant-reclaimer.v1`, and the fantasy: "Builds a deep growth
   engine, then reclaims territory after the board breaks open."
3. Enter the Proven solo pool only. Campaign placement remains a separate,
   deferred pacing evaluation.

## Provenance

- Search protocol and preregistration:
  `AI_P8_CANDIDATE_SEARCH_PROTOCOL_V1.md`.
- Contextual band evidence: the Bloom-20 classification section of that
  protocol and the ignored local artifacts under
  `SimulationParquet/p8_e1_bloom20/`.
- Candidate comparison artifacts:
  `calibration2-analysis/paired_comparison.csv`,
  `comparison-analysis/paired_comparison.csv`, and
  `holdout2-analysis/paired_comparison.csv` in that artifact root.
