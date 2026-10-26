#!/usr/bin/env bash
set -euo pipefail

# Build an iOS/TestFlight export from a clean Unity cache. The checked-in
# project lives in iCloud Drive and may contain conflict copies such as
# Library/Bee/PlayerScriptAssemblies 2; never let those generated files enter
# an App Store build.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
UNITY_BIN="${UNITY_BIN:-/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity}"
if [[ -n "${FRONTROOMS_BUILD_WORKDIR:-}" ]]; then
  WORK_ROOT="$FRONTROOMS_BUILD_WORKDIR"
else
  WORK_ROOT="$(mktemp -d "${TMPDIR:-/tmp}/frontrooms-ios-build.XXXXXX")"
fi
BUILD_ROOT="$WORK_ROOT/Builds/Mobile"
LOG_ROOT="$WORK_ROOT/Logs"
RESERIALIZE_LOG="$LOG_ROOT/reserialize.log"
BUILD_LOG="$LOG_ROOT/export-ios.log"
OUTPUT="$BUILD_ROOT/iOS/FRONTROOMSSS"

mkdir -p "$LOG_ROOT"
if [[ "$WORK_ROOT" != "$PROJECT_ROOT" ]]; then
  mkdir -p "$WORK_ROOT"
  rsync -a --delete \
    --exclude '.git/' \
    --exclude 'Library/' \
    --exclude 'Builds/' \
    --exclude 'Logs/' \
    --exclude 'UserSettings/' \
    --exclude 'Temp/' \
    --exclude 'Obj/' \
    --exclude 'MemoryCaptures/' \
    --exclude '.vs/' \
    "$PROJECT_ROOT/" "$WORK_ROOT/"
fi

if [[ ! -x "$UNITY_BIN" ]]; then
  echo "Unity executable not found: $UNITY_BIN" >&2
  exit 2
fi

run_unity() {
  local log="$1"
  shift
  "$UNITY_BIN" -batchmode -quit -projectPath "$WORK_ROOT" "$@" -logFile "$log"
}

echo "[FrontRoomsBuild] clean worktree: $WORK_ROOT"
echo "[FrontRoomsBuild] pass 1: force-reserialize scene/profile assets"
run_unity "$RESERIALIZE_LOG" \
  -buildTarget iOS \
  -executeMethod FrontRoomsMobileBuild.ReserializeMobileLevelAssets
if rg -q 'TypeDB: Class .* already registered|Error building Player|Exiting batchmode with failure' "$RESERIALIZE_LOG"; then
  echo "[FrontRoomsBuild] FAIL: reserialize pass reported duplicate TypeDB/failure diagnostics" >&2
  exit 3
fi

echo "[FrontRoomsBuild] pass 2: export iOS Xcode project"
FRONTROOMS_MOBILE_OUTPUT="$BUILD_ROOT" \
  run_unity "$BUILD_LOG" \
    -buildTarget iOS \
    -executeMethod FrontRoomsMobileBuild.ExportIOS

if rg -q 'TypeDB: Class .* already registered|Duplicate Unity Library copies detected|errors=[1-9][0-9]*' "$BUILD_LOG"; then
  echo "[FrontRoomsBuild] FAIL: duplicate TypeDB/error diagnostics found in $BUILD_LOG" >&2
  exit 9
fi
rg -q '\[FrontRoomsMobileBuild\] iOS Succeeded errors=0 ' "$BUILD_LOG"

PBX="$OUTPUT/Unity-iPhone.xcodeproj/project.pbxproj"
if [[ ! -f "$PBX" ]]; then
  echo "[FrontRoomsBuild] FAIL: missing exported Xcode project: $PBX" >&2
  exit 10
fi

EXPECTED_ID="com.redwang.frontroomsss"
EXPECTED_BUILD="19"
rg -q "PRODUCT_BUNDLE_IDENTIFIER = $EXPECTED_ID;" "$PBX" || { echo "[FrontRoomsBuild] FAIL: bundle id missing from Xcode project" >&2; exit 11; }
PLIST="$OUTPUT/Info.plist"
[[ -f "$PLIST" ]] || { echo "[FrontRoomsBuild] FAIL: missing exported Info.plist" >&2; exit 12; }
ACTUAL_BUILD="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleVersion' "$PLIST")"
[[ "$ACTUAL_BUILD" == "$EXPECTED_BUILD" ]] || { echo "[FrontRoomsBuild] FAIL: build number $ACTUAL_BUILD" >&2; exit 13; }
[[ -s "$OUTPUT/Data/level0" ]] || { echo "[FrontRoomsBuild] FAIL: missing Data/level0" >&2; exit 8; }
[[ -s "$OUTPUT/Data/sharedassets0.assets" ]] || { echo "[FrontRoomsBuild] FAIL: missing Data/sharedassets0.assets" >&2; exit 14; }

echo "[FrontRoomsBuild] PASS: id=$EXPECTED_ID build=$EXPECTED_BUILD"
echo "[FrontRoomsBuild] Xcode export: $OUTPUT"
echo "[FrontRoomsBuild] logs: $RESERIALIZE_LOG $BUILD_LOG"
