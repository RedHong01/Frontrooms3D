#!/usr/bin/env bash
set -euo pipefail

# Export the current Unity source as a macOS Xcode project, add the App Store
# sandbox capability required by Mac App Store/TestFlight, then archive and
# upload it with Xcode's automatic/cloud-managed signing.
#
# This script deliberately stops before any upload if signing or provisioning
# is unavailable. It never uses the old unsigned Builds/Mac/*.app artifacts.
#
# Before export it launches the signed archive once (smoke test) and requires
# the game's READY log line plus the five FMOD banks. Build 20 skipped this and
# was rejected by App Review (2.1(a), 2026-10-22): it hung in mono_arch_init at
# launch because the Hardened Runtime blocked Mono's JIT.
#   FRONTROOMS_MAC_SKIP_UPLOAD=1  stop after the archive and smoke test (no export, no upload)
#   FRONTROOMS_MAC_SMOKE=0        skip the smoke launch (not recommended)

project_dir="${PROJECT_DIR:-$(cd "$(dirname "$0")/.." && pwd)}"
unity_bin="${UNITY_BIN:-/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity}"
output_root="${FRONTROOMS_CLOUD_OUTPUT:-$(mktemp -d /tmp/frontrooms-mac-testflight.XXXXXX)}"
build_number="${FRONTROOMS_MAC_BUILD_NUMBER:-21}"
team_id="${DEVELOPMENT_TEAM:-T9WR6QR3UY}"
skip_upload="${FRONTROOMS_MAC_SKIP_UPLOAD:-0}"
smoke="${FRONTROOMS_MAC_SMOKE:-1}"

if [[ ! -x "$unity_bin" ]]; then
  echo "Unity executable not found: $unity_bin" >&2
  exit 2
fi

export FRONTROOMS_CLOUD_OUTPUT="$output_root"
export FRONTROOMS_MAC_BUILD_NUMBER="$build_number"

"$unity_bin" \
  -batchmode -quit \
  -projectPath "$project_dir" \
  -executeMethod FrontRoomsCloudBuild.BuildMacTestFlight \
  -logFile "$output_root/unity-mac-testflight.log"

xcode_dir="$output_root/MacTestFlight/FRONTROOMSSS"
project_file="$xcode_dir/FRONTROOMSSS.xcodeproj/project.pbxproj"
entitlements="$xcode_dir/FRONTROOMSSS/FRONTROOMSSS.entitlements"
archive_path="$output_root/Archives/FRONTROOMSSS-macOS-${build_number}.xcarchive"
export_path="$output_root/Exports/FRONTROOMSSS-macOS-${build_number}"
export_options="$project_dir/Builds/Mac/ExportOptions-macOS-TestFlight.plist"

if [[ ! -f "$project_file" ]]; then
  echo "Unity did not export the Xcode project: $xcode_dir" >&2
  exit 3
fi

cat > "$entitlements" <<'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
	<key>com.apple.security.app-sandbox</key>
	<true/>
	<key>com.apple.security.network.client</key>
	<true/>
	<!-- The player uses Mono, which JIT-compiles managed code. Under the
	     Hardened Runtime that needs allow-jit, or the app hangs at launch. -->
	<key>com.apple.security.cs.allow-jit</key>
	<true/>
</dict>
</plist>
PLIST

# Unity's bundled FMOD haptics plug-in ships with an underscore in its
# CFBundleIdentifier. App Store Connect rejects that identifier even though
# the plug-in file name must remain fmod_haptics.bundle for runtime loading.
fmod_info="$xcode_dir/FRONTROOMSSS/PlugIns/fmod_haptics.bundle/Contents/Info.plist"
if [[ -f "$fmod_info" ]]; then
  /usr/bin/plutil -replace CFBundleIdentifier -string com.fmod.fmodhaptics "$fmod_info"
  # Unity exports this plug-in with a stale signature whose identifier still
  # contains the rejected underscore. Remove it so Xcode CodeSignOnCopy signs
  # the bundle again using the patched Bundle Identifier.
  fmod_binary="$xcode_dir/FRONTROOMSSS/PlugIns/fmod_haptics.bundle/Contents/MacOS/fmod_haptics"
  /usr/bin/codesign --remove-signature "$fmod_binary" 2>/dev/null || true
  /usr/bin/find "$xcode_dir/FRONTROOMSSS/PlugIns/fmod_haptics.bundle" -type d -name _CodeSignature -prune -exec /bin/rm -rf {} +
fi

# Unity can emit an empty placeholder icon when no macOS icon is configured.
# Build a valid ICNS from the maintained 1024px source artwork so the Mac
# App Store package contains the required 512pt @2x representation.
icon_source="$project_dir/Assets/Resources/Brand/FrontRoomsAppIcon1024.png"
iconset_dir="$output_root/FRONTROOMSSS.iconset"
if [[ -f "$icon_source" ]] && command -v iconutil >/dev/null 2>&1; then
  rm -rf "$iconset_dir"
  mkdir -p "$iconset_dir"
  for size in 16 32 128 256 512; do
    /usr/bin/sips -z "$size" "$size" "$icon_source" --out "$iconset_dir/icon_${size}x${size}.png" >/dev/null
    double=$((size * 2))
    /usr/bin/sips -z "$double" "$double" "$icon_source" --out "$iconset_dir/icon_${size}x${size}@2x.png" >/dev/null
  done
  /usr/bin/iconutil -c icns "$iconset_dir" -o "$xcode_dir/FRONTROOMSSS/Resources/PlayerIcon.icns"
  rm -rf "$iconset_dir"
fi

# Xcode can consume an entitlements file through the build setting without a
# PBX file reference. Keep the edit deterministic and scoped to the generated
# FRONTROOMSSS target's Debug/Release configurations.
python3 - "$project_file" "$team_id" <<'PY'
import pathlib, re, sys

path = pathlib.Path(sys.argv[1])
team = sys.argv[2]
text = path.read_text()

text = text.replace(
    'CODE_SIGN_STYLE = Automatic;\n\t\t\t\tCOMBINE_HIDPI_IMAGES',
    f'CODE_SIGN_STYLE = Automatic;\n\t\t\t\tCODE_SIGN_INJECT_BASE_ENTITLEMENTS = YES;\n\t\t\t\tCODE_SIGN_ENTITLEMENTS = FRONTROOMSSS/FRONTROOMSSS.entitlements;\n\t\t\t\tDEVELOPMENT_TEAM = {team};\n\t\t\t\tENABLE_HARDENED_RUNTIME = YES;\n\t\t\t\tCOMBINE_HIDPI_IMAGES',
)
if 'CODE_SIGN_ENTITLEMENTS = FRONTROOMSSS/FRONTROOMSSS.entitlements;' not in text:
    raise SystemExit('Could not patch the generated FRONTROOMSSS signing settings')
path.write_text(text)
PY

mkdir -p "$(dirname "$archive_path")" "$export_path"

xcodebuild \
  -project "$xcode_dir/FRONTROOMSSS.xcodeproj" \
  -scheme FRONTROOMSSS \
  -configuration Release \
  -destination 'generic/platform=macOS' \
  -archivePath "$archive_path" \
  -allowProvisioningUpdates \
  DEVELOPMENT_TEAM="$team_id" \
  CODE_SIGN_STYLE=Automatic \
  archive

archived_app="$archive_path/Products/Applications/FRONTROOMSSS.app"
for bank in Master Master.strings Ambience Music SFX; do
  if [[ ! -f "$archived_app/Contents/Resources/Data/StreamingAssets/FMOD/$bank.bank" ]]; then
    echo "The archive has no FMOD bank $bank.bank (the game would be silent)" >&2
    exit 4
  fi
done

if [[ "$smoke" == 1 ]]; then
  # Launch the signed, sandboxed archive and wait for the game's READY line.
  smoke_log="$output_root/smoke-launch.log"
  "$archived_app/Contents/MacOS/FRONTROOMSSS" -logFile - > "$smoke_log" 2>&1 &
  smoke_pid=$!
  ready=0
  for _ in $(seq 1 90); do
    sleep 1
    if grep -q '\[FrontRooms3D\] READY' "$smoke_log"; then ready=1; break; fi
    kill -0 "$smoke_pid" 2>/dev/null || break
  done
  kill "$smoke_pid" 2>/dev/null || true
  sleep 3
  kill -9 "$smoke_pid" 2>/dev/null || true
  if [[ "$ready" != 1 ]]; then
    echo "Smoke launch failed: no READY within 90 s, nothing exported (log: $smoke_log)" >&2
    exit 5
  fi
  echo "Smoke launch OK: $(grep -m1 '\[FrontRooms3D\] READY' "$smoke_log")"
fi

if [[ "$skip_upload" == 1 ]]; then
  echo "Stopped before export and upload (FRONTROOMS_MAC_SKIP_UPLOAD=1): $archive_path"
  exit 0
fi

xcodebuild \
  -exportArchive \
  -archivePath "$archive_path" \
  -exportPath "$export_path" \
  -exportOptionsPlist "$export_options" \
  -allowProvisioningUpdates

echo "Mac TestFlight export completed: $export_path"
