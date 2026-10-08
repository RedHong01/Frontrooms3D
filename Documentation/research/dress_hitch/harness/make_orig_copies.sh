#!/bin/bash
# make_orig_copies.sh <clone>: writes the renamed copies of the clone's CURRENT dressers that the A/B harness compares
# against (FrontRoomsOfficeKitOrig, FrontRoomsFurniturePileOrig, FrontRoomsKitLibraryOrig) into
# <clone>/Assets/Editor/DressMergeTmp/DressPerfOriginal/. Run it on a clone that still holds main's dressers,
# BEFORE copying in the new ones. Clone only: never into Red's project.
set -eu
C=$1; O=$C/Assets/Editor/DressMergeTmp/DressPerfOriginal; mkdir -p $O
for f in FrontRoomsOfficeKit FrontRoomsFurniturePile FrontRoomsKitLibrary; do
  sed -e 's/FrontRoomsOfficeKit/FrontRoomsOfficeKitOrig/g; s/FrontRoomsFurniturePile/FrontRoomsFurniturePileOrig/g; s/FrontRoomsKitLibrary/FrontRoomsKitLibraryOrig/g' \
    "$C/Assets/Scripts/Office/$f.cs" > "$O/${f}Orig.cs"
done
echo "wrote $O"
