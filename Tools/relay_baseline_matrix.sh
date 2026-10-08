#!/bin/zsh
# Serial Step 0 baseline matrix (RELAY_PURSUIT_REDESIGN.md §13/§14; fixes in Verification/relay-baseline/summary.md §13).
# Default: 6 seeds × 10 bot modes × T1/T5 = 120 cases. Run it in a private clone, never on the project Red has open.
#   SMOKE=1        the fidelity smoke run: shiftholder, noisy, closedzone, doorspammer on one seed at T1 and T5 (8 cases).
#   SEEDS, TIERS, MODES   space-separated overrides.
#   REPEAT_CHECK=1 runs the first case twice first and stops unless both reports have the same behaviourHash.
#   LABEL          what the build is, written into MATRIX_RESULT.txt (default "v1 + path hearing (Step 1)": the
#                  reference v2 is graded against; the Oct 4 set in Verification/relay-baseline is pure v1).
#   TAG            the folder the reports go to, Verification/relay-baseline/<TAG> (default step1-<date>), so an
#                  earlier set is never overwritten.
# Every case's log stays in Verification/relay-baseline/logs.

set -u

ROOT="${0:A:h:h}"
RUN="$ROOT/Tools/relay_baseline_run.sh"
OUT="$ROOT/Verification/relay-baseline"
LABEL="${LABEL:-v1 + path hearing (Step 1)}"
TAG="${TAG:-step1-$(date +%Y%m%d-%H%M)${SMOKE:+-smoke}}"
DEST="$OUT/$TAG"
mkdir -p "$DEST"
RESULT="$DEST/MATRIX_RESULT.txt"
{
  echo "label: $LABEL"
  echo "git: $(git -C "$ROOT" rev-parse HEAD 2>/dev/null || echo unknown) dirty files: $(git -C "$ROOT" status --porcelain --untracked-files=no -- Assets Packages ProjectSettings 2>/dev/null | grep -v -E '^ D .* [0-9]+(\.[^/]*)?"?$' | wc -l | tr -d ' ')"
  echo "started: $(date '+%F %T')"
} > "$RESULT"
if [[ -n "${SMOKE:-}" ]]; then
  SEEDS=(${=SEEDS:-2554})
  MODES=(${=MODES:-shiftholder noisy closedzone doorspammer})
else
  SEEDS=(${=SEEDS:-2554 20388 7 101 4242 9001})
  MODES=(${=MODES:-quiet noisy evader03 evader06 evaderv2 staller shiftholder doorspammer edgerunner closedzone})
fi
TIERS=(${=TIERS:-1 5})

minutes_for() { [[ "$1" == staller ]] && echo 5 || echo 4; }

if [[ -n "${REPEAT_CHECK:-}" ]]; then
  s=${SEEDS[1]} m=${MODES[1]} t=${TIERS[1]}
  echo "[repeat check] seed=$s mode=$m tier=$t, twice"
  "$RUN" "$s" "$m" "$t" "$(minutes_for $m)"
  h1=$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["behaviourHash"])' "$OUT/$s-$m-T$t.json" 2>/dev/null)
  cp "$OUT/$s-$m-T$t.json" "$OUT/$s-$m-T$t.repeat1.json" 2>/dev/null
  "$RUN" "$s" "$m" "$t" "$(minutes_for $m)"
  h2=$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["behaviourHash"])' "$OUT/$s-$m-T$t.json" 2>/dev/null)
  if [[ -z "$h1" || "$h1" != "$h2" ]]; then
    echo "repeat check FAILED: behaviourHash $h1 vs $h2"
    exit 1
  fi
  echo "repeat check OK: behaviourHash $h1"
fi

TOTAL=$(( ${#SEEDS} * ${#TIERS} * ${#MODES} ))
PASS=0
FAIL=0
INDEX=0
for seed in $SEEDS; do
  for tier in $TIERS; do
    for mode in $MODES; do
      INDEX=$((INDEX + 1))
      echo "[$INDEX/$TOTAL] seed=$seed mode=$mode tier=$tier"
      if line=$("$RUN" "$seed" "$mode" "$tier" "$(minutes_for $mode)" 2>&1); then
        PASS=$((PASS + 1))
      else
        FAIL=$((FAIL + 1))
      fi
      echo "$line"
      echo "$line" >> "$RESULT"
      [[ -f "$OUT/$seed-$mode-T$tier.json" ]] && mv "$OUT/$seed-$mode-T$tier.json" "$DEST/"
    done
  done
done

echo "Step 0 matrix: $PASS PASS / $FAIL FAIL / $TOTAL total" | tee -a "$RESULT"
echo "finished: $(date '+%F %T') · reports in $DEST" | tee -a "$RESULT"
[[ $FAIL -eq 0 ]]
