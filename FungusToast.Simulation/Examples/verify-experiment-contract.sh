#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"

stamp="$(date -u +%Y%m%dT%H%M%S)_$$"
experiment_id="contract_${stamp}"
log_root="/tmp/fungus-toast-contract-${stamp}"
project="FungusToast.Simulation/FungusToast.Simulation.csproj"

dotnet build FungusToast.sln --no-restore >"${log_root}-build.log"

common=(
  --experiment-id "$experiment_id"
  --purpose "P2.6 replay and selective resume contract"
  --games 1
  --players 2
  --strategy-set Testing
  --seed 24680
  --fixed-slots
  --no-keyboard
  --parquet
  --no-nutrient-patches
  --no-mycovariants
)

dotnet run --project "$project" --no-build -- \
  "${common[@]}" --board-sizes 20x20,22x20 \
  >"${log_root}-initial.log"

dotnet run --project "$project" --no-build -- \
  "${common[@]}" --board-sizes 20x20,22x20,24x20 --resume \
  >"${log_root}-resume.log"

skip_count="$(grep -c 'Skipping completed matching condition' "${log_root}-resume.log")"
run_count="$(grep -c '^Game 1/1' "${log_root}-resume.log")"
if [[ "$skip_count" -ne 2 || "$run_count" -ne 1 ]]; then
  echo "Selective resume failed: expected 2 skips and 1 new run; got ${skip_count} skips and ${run_count} runs." >&2
  exit 1
fi

source_artifact="FungusToast.Simulation/bin/Debug/net8.0/SimulationParquet/${experiment_id}__p2_w20_h20_sTesting"
replay_id="${experiment_id}_replay"
dotnet run --project "$project" --no-build -- \
  --replay-manifest "${source_artifact}/resolved-manifest.json" \
  --replay-experiment-id "$replay_id" \
  >"${log_root}-replay.log"

grep -q 'Replay outcome verified:' "${log_root}-replay.log"
(
  cd "$source_artifact"
  sha256sum -c resolved-manifest.sha256
)

echo "Experiment contract verified."
echo "Source experiment: ${experiment_id}"
echo "Replay experiment: ${replay_id}"
echo "Logs: ${log_root}-*.log"
