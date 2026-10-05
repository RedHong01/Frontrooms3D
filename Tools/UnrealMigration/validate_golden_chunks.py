#!/usr/bin/env python3
"""Validate Unity's exported golden chunks before Unreal consumes them.

The Unity Editor exporter is the authority for chunk data.  This checker is
engine-independent so it can run on Windows after a Mac Unity export (and in
CI without opening Unreal).  It validates the export shape against the
portable contract and rechecks the MapHash vectors shared by Unity and UE.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import sys
from pathlib import Path
from typing import Any


SEEDS = (2554, 20388, 20261001)
MAP_HASH_VECTORS = (
    (2554, 0, 0, 11, 0, 0xCD2545A4),
    (20261001, -3, 7, 23, 0, 0x529C7D25),
    (-1, -(2**31), 2**31 - 1, 73, 3, 0x9F18A844),
    (20388, 16, 10, 53, 2, 0x4C534946),
)


def map_hash(seed: int, a: int, b: int, salt: int, revision: int = 0) -> int:
    """Byte-for-byte equivalent of FrontRooms.MapHash in Unity and UE."""

    def u32(value: int) -> int:
        return value & 0xFFFFFFFF

    def mix(value: int) -> int:
        value = u32(value ^ (value >> 16))
        value = u32(value * 0x7FEB352D)
        value = u32(value ^ (value >> 15))
        value = u32(value * 0x846CA68B)
        return u32(value ^ (value >> 16))

    value = mix(u32(seed) ^ 0x9E3779B9)
    value = mix(value ^ u32(u32(a) * 0x85EBCA6B))
    value = mix(value ^ u32(u32(b) * 0xC2B2AE35))
    value = mix(value ^ u32(u32(salt) * 0x27D4EB2F))
    return mix(value ^ u32(u32(revision) * 0x165667B1))


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def number(value: Any, label: str) -> float:
    require(isinstance(value, (int, float)) and not isinstance(value, bool), f"{label}: expected number")
    return float(value)


def validate_chunk(chunk: dict[str, Any], seed: int, radius: int) -> dict[str, Any]:
    require(isinstance(chunk, dict), f"seed {seed}: chunk must be an object")
    x, y = chunk.get("x"), chunk.get("y")
    require(isinstance(x, int) and isinstance(y, int), f"seed {seed}: chunk coordinate")
    require(-radius <= x <= radius and -radius <= y <= radius, f"seed {seed}: chunk ({x},{y}) outside radius")
    require(chunk.get("revision") == 0 and chunk.get("tier") == 1, f"seed {seed} chunk ({x},{y}): revision/tier")

    own_zone = chunk.get("ownZone")
    require(isinstance(own_zone, dict), f"seed {seed} chunk ({x},{y}): ownZone")
    require(own_zone.get("x") == x and own_zone.get("y") == y, f"seed {seed} chunk ({x},{y}): ownZone id")
    require(own_zone.get("height") in (0, 1, 2), f"seed {seed} chunk ({x},{y}): ownZone height")
    require(own_zone.get("theme") in (0, 1), f"seed {seed} chunk ({x},{y}): ownZone theme")
    site_x, site_z = number(own_zone.get("siteX"), "siteX"), number(own_zone.get("siteZ"), "siteZ")
    require(x * 24.0 + 3.6 <= site_x <= x * 24.0 + 20.4, f"seed {seed} chunk ({x},{y}): siteX range")
    require(y * 24.0 + 3.6 <= site_z <= y * 24.0 + 20.4, f"seed {seed} chunk ({x},{y}): siteZ range")

    lengths = {
        "zone": 64,
        "height": 64,
        "east": 64,
        "north": 64,
        "west": 8,
        "south": 8,
        "pillar": 81,
        "pillarStyle": 81,
    }
    for field, expected in lengths.items():
        value = chunk.get(field)
        require(isinstance(value, list) and len(value) == expected, f"seed {seed} chunk ({x},{y}): {field} length")
    require(all(isinstance(value, int) and 0 <= value <= 4 for value in chunk["east"] + chunk["north"] + chunk["west"] + chunk["south"]), f"seed {seed} chunk ({x},{y}): edge enum")
    require(all(isinstance(value, int) and 0 <= value <= 2 for value in chunk["height"]), f"seed {seed} chunk ({x},{y}): height enum")
    require(all(isinstance(value, bool) for value in chunk["pillar"]), f"seed {seed} chunk ({x},{y}): pillar values")
    require(all(isinstance(value, int) and 0 <= value <= 7 for value in chunk["pillarStyle"]), f"seed {seed} chunk ({x},{y}): pillar style")

    rooms = chunk.get("rooms")
    module_flags = chunk.get("roomModulePresent")
    require(isinstance(rooms, list) and isinstance(module_flags, list) and len(rooms) == len(module_flags), f"seed {seed} chunk ({x},{y}): rooms")
    for room in rooms:
        require(isinstance(room, dict), f"seed {seed} chunk ({x},{y}): room object")
        require(all(isinstance(room.get(field), int) for field in ("x", "y", "w", "h")), f"seed {seed} chunk ({x},{y}): room fields")
        require(0 <= room["x"] < 8 and 0 <= room["y"] < 8 and 1 <= room["w"] <= 8 and 1 <= room["h"] <= 8, f"seed {seed} chunk ({x},{y}): room bounds")
        require(room["x"] + room["w"] <= 8 and room["y"] + room["h"] <= 8, f"seed {seed} chunk ({x},{y}): room extent")
    require(all(isinstance(value, bool) for value in module_flags), f"seed {seed} chunk ({x},{y}): module flags")
    return {"x": x, "y": y, "rooms": len(rooms), "hasKey": bool(chunk.get("hasKey"))}


def validate(root: Path, report_path: Path) -> dict[str, Any]:
    contract_path = root / "Migration/exports/frontrooms_contract.json"
    contract = json.loads(contract_path.read_text(encoding="utf-8"))
    require(contract.get("schema") == "frontrooms.unreal.migration.contract", "contract schema")
    require(contract.get("schemaVersion") == 1, "contract schemaVersion")
    require(tuple(contract.get("validationSeeds", ())) == SEEDS, "contract validationSeeds")
    constants = contract.get("constants", {})
    require(constants.get("cellSizeMeters") == 3 and constants.get("chunkCells") == 8 and constants.get("chunkSizeMeters") == 24, "contract map constants")

    chunks_root = root / "Migration/exports/unity_chunks"
    files: list[dict[str, Any]] = []
    for seed in SEEDS:
        path = chunks_root / f"seed-{seed}.json"
        require(path.is_file(), f"missing golden export: {path}")
        data = json.loads(path.read_text(encoding="utf-8"))
        require(data.get("seed") == seed and data.get("radius") == 1, f"seed {seed}: header")
        chunks = data.get("chunks")
        require(isinstance(chunks, list) and len(chunks) == 9, f"seed {seed}: expected 9 chunks")
        seen = set()
        chunk_reports = []
        for chunk in chunks:
            item = validate_chunk(chunk, seed, 1)
            coordinate = (item["x"], item["y"])
            require(coordinate not in seen, f"seed {seed}: duplicate chunk {coordinate}")
            seen.add(coordinate)
            chunk_reports.append(item)
        require(seen == {(x, y) for y in range(-1, 2) for x in range(-1, 2)}, f"seed {seed}: incomplete radius square")
        files.append({"path": path.relative_to(root).as_posix(), "bytes": path.stat().st_size, "sha256": sha256(path), "chunkCount": len(chunks), "chunks": chunk_reports})

    vectors = []
    for seed, a, b, salt, revision, expected in MAP_HASH_VECTORS:
        actual = map_hash(seed, a, b, salt, revision)
        require(actual == expected, f"MapHash vector mismatch: seed {seed}, salt {salt}")
        vectors.append({"seed": seed, "a": a, "b": b, "salt": salt, "revision": revision, "expected": f"0x{expected:08X}", "actual": f"0x{actual:08X}"})

    report = {
        "schema": "frontrooms.unity.golden-chunk-validation",
        "schemaVersion": 1,
        "status": "passed",
        "unityVersion": "6000.3.10f1",
        "contract": {"schema": contract["schema"], "schemaVersion": contract["schemaVersion"], "validationSeeds": list(SEEDS), "cellSizeMeters": constants["cellSizeMeters"], "chunkCells": constants["chunkCells"], "chunkSizeMeters": constants["chunkSizeMeters"]},
        "export": {"radius": 1, "seedFiles": files},
        "mapHashVectors": vectors,
        "checks": ["per-seed JSON parsed", "contract seeds/constants match", "9 chunks per seed cover [-1,1]²", "chunk arrays/enums/room bounds valid", "Unity/Unreal MapHash vectors match"],
    }
    report_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    return report


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--report", type=Path, default=None)
    args = parser.parse_args()
    report_path = args.report or args.root / "Migration/exports/unity_golden_chunk_validation.json"
    try:
        report = validate(args.root.resolve(), report_path.resolve())
    except (OSError, ValueError, json.JSONDecodeError) as exc:
        print(f"FrontRooms golden chunk validation FAILED: {exc}", file=sys.stderr)
        return 1
    print(f"FrontRooms golden chunk validation passed: {len(report['export']['seedFiles'])} seeds, 9 chunks/seed, {len(report['mapHashVectors'])} MapHash vectors")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
