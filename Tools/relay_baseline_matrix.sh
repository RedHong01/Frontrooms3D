#!/bin/zsh
# Serial Step 0 baseline matrix from RELAY_PURSUIT_REDESIGN.md §13/§14.
# Default: 3 seeds × 9 current bot modes × T1/T5 = 54 cases.
# Override SEEDS, TIERS, or MODES with space-separated values for a smaller smoke run.

set -u

ROOT="${0:A:h:h}"
RUN="$ROOT/Tools/relay_baseline_run.sh"
SEEDS=(${=SEEDS:-2554 20388 7})
TIERS=(${=TIERS:-1 5})
MODES=(${=MODES:-quiet noisy evader03 evader06 staller shiftholder doorspammer edgerunner closedzone})

TOTAL=$(( ${#SEEDS} * ${#TIERS} * ${#MODES} ))
PASS=0
FAIL=0
INDEX=0
for seed in $SEEDS; do
  for tier in $TIERS; do
    for mode in $MODES; do
      INDEX=$((INDEX + 1))
      echo "[$INDEX/$TOTAL] seed=$seed mode=$mode tier=$tier"
      if [[ "$mode" == staller ]]; then
        "$RUN" "$seed" "$mode" "$tier" 5
      else
        "$RUN" "$seed" "$mode" "$tier" 4
      fi
      if [[ $? -eq 0 ]]; then
        PASS=$((PASS + 1))
      else
        FAIL=$((FAIL + 1))
      fi
    done
  done
done

echo "Step 0 matrix: $PASS PASS / $FAIL FAIL / $TOTAL total"
[[ $FAIL -eq 0 ]]
