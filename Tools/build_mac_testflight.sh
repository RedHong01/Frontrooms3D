#!/usr/bin/env bash
set -euo pipefail

# Export the current Unity source as a macOS Xcode project, add the App Store
# sandbox capability required by Mac App Store/TestFlight, then archive and
# upload it with Xcode's automatic/cloud-managed signing.
#
# This script deliberately stops before any upload if signing or provisioning
# is unavailable. It never uses the old unsigned Builds/Mac/*.app artifacts.

project_dir="${PROJECT_DIR:-$(cd "$(dirname "$0")/.." && pwd)}"
unity_bin="${UNITY_BIN:-/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity}"
output_root="${FRONTROOMS_CLOUD_OUTPUT:-$(mktemp -d /tmp/frontrooms-mac-testflight.XXXXXX)}"
build_number="${FRONTROOMS_MAC_BUILD_NUMBER:-16}"
team_id="${DEVELOPMENT_TEAM:-T9WR6QR3UY}"

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
</dict>
</plist>
PLIST

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
    f'CODE_SIGN_STYLE = Automatic;\n\t\t\t\tCODE_SIGN_INJECT_BASE_ENTITLEMENTS = YES;\n\t\t\t\tCODE_SIGN_ENTITLEMENTS = FRONTROOMSSS/FRONTROOMSSS.entitlements;\n\t\t\t\tDEVELOPMENT_TEAM = {team};\n\t\t\t\tCOMBINE_HIDPI_IMAGES',
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

xcodebuild \
  -exportArchive \
  -archivePath "$archive_path" \
  -exportPath "$export_path" \
  -exportOptionsPlist "$export_options" \
  -allowProvisioningUpdates

echo "Mac TestFlight export completed: $export_path"
