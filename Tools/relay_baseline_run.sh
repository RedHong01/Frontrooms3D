#!/usr/bin/env bash
# Run one Relay Pursuit v2 Step 0 baseline case.
# Usage: relay_baseline_run.sh <seed> <mode> <tier> [minutes]
# The game stays on the legacy hunter; this only drives the editor-only bot.

set -u

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd -- "$SCRIPT_DIR/.." && pwd)"
UNITY="${UNITY:-${UNITY_PATH:-}}"
if [[ -z "$UNITY" ]]; then
  case "${OSTYPE:-}" in
    darwin*) UNITY="/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity" ;;
    msys*|cygwin*|win32*)
      echo "Set UNITY_PATH (or UNITY) to the Unity 6000.3.10f1 executable on Windows." >&2
      exit 2
      ;;
    *) UNITY="Unity" ;;
  esac
fi
SEED="${1:-}"
MODE="${2:-}"
TIER="${3:-}"
MINUTES="${4:-}"

case "$MODE" in
  quiet|noisy|evader03|evader06|staller|shiftholder|doorspammer|edgerunner|closedzone) ;;
  *) echo "usage: $0 <seed> <mode> <tier> [minutes]" >&2; exit 2 ;;
esac

if [[ ! -d "$ROOT/Assets" ]]; then
  echo "Unity project or editor not found: $ROOT / $UNITY" >&2
  exit 2
fi
if [[ "$UNITY" == */* && ! -x "$UNITY" ]] || [[ "$UNITY" != */* ]] && ! command -v "$UNITY" >/dev/null 2>&1; then
  echo "Unity editor not found: $UNITY" >&2
  exit 2
fi

# A baseline must own the project editor because it enters Play Mode.
if command -v pgrep >/dev/null 2>&1 && pgrep -f "[Uu]nity.*-[pP]roject[Pp]ath[ =]$ROOT([ /]|$)" >/dev/null 2>&1; then
  echo "busy: a Unity editor already has $ROOT open" >&2
  exit 2
fi

OUT="$ROOT/Verification/relay-baseline"
LOGS="$OUT/logs"
mkdir -p "$LOGS"
NAME="$SEED-$MODE-T$TIER"
JSON="$OUT/$NAME.json"
LOG="$LOGS/$NAME${MINUTES:+-${MINUTES}min}.log"

if [[ -n "$MINUTES" ]]; then
  "$UNITY" -batchmode -projectPath "$ROOT" -executeMethod FrontRoomsMainScenePlaytest.RunBatch \
    -autopilotSeed "$SEED" -autopilotBot "$MODE" -autopilotTier "$TIER" -autopilotMinutes "$MINUTES" -logFile "$LOG"
else
  "$UNITY" -batchmode -projectPath "$ROOT" -executeMethod FrontRoomsMainScenePlaytest.RunBatch \
    -autopilotSeed "$SEED" -autopilotBot "$MODE" -autopilotTier "$TIER" -logFile "$LOG"
fi
RC=$?

if [[ ! -f "$JSON" ]]; then
  echo "$NAME rc=$RC · no JSON written; see $LOG" >&2
  exit 1
fi

VERDICT=$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["verdict"])' "$JSON" 2>/dev/null || echo "unreadable report")
echo "$NAME rc=$RC · $VERDICT"
[[ "$VERDICT" == PASS* && $RC -eq 0 ]]
