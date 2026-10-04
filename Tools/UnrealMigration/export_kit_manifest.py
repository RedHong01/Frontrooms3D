#!/usr/bin/env python3
"""Convert Unity prop JSON sidecars into an Unreal import manifest."""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
from typing import Any


def digest(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def relative(repo: Path, path: Path) -> str:
    return path.relative_to(repo).as_posix()


def build_manifest(repo: Path) -> dict[str, Any]:
    model_root = repo / "Assets/Resources/Props/Models"
    records = []
    totals = {"triangles": 0, "trianglesLod1": 0, "anchors": 0, "supports": 0, "colliders": 0, "piles": 0}
    missing_fbx = []
    for sidecar in sorted(model_root.glob("*.json")):
        data = json.loads(sidecar.read_text(encoding="utf-8"))
        model = model_root / (sidecar.stem + ".fbx")
        record = {
            "name": data.get("name", sidecar.stem),
            "source": relative(repo, sidecar),
            "sourceSha256": digest(sidecar),
            "mesh": relative(repo, model),
            "meshExists": model.exists(),
            "triangles": int(data.get("triangles", 0)),
            "trianglesLod1": int(data.get("trianglesLod1", 0)),
            "lodDistances": data.get("lodDistances", []),
            "lodRatios": data.get("lodRatios", []),
            "lodBudget": data.get("lodBudget", []),
            "boundsMin": data.get("boundsMin", []),
            "boundsMax": data.get("boundsMax", []),
            "footprintCentre": data.get("footprintCentre", []),
            "footprintSize": data.get("footprintSize", []),
            "frontAxis": data.get("frontAxis"),
            "placement": data.get("placement"),
            "service": data.get("service"),
            "minCeiling": data.get("minCeiling"),
            "slots": data.get("slots", []),
            "tags": data.get("tags", []),
            "noCollider": bool(data.get("noCollider", False)),
            "motion": data.get("motion"),
            "openings": data.get("openings", []),
            "anchors": data.get("anchors", []),
            "supports": data.get("supports", []),
            "colliders": data.get("colliders", []),
            "pile": data.get("pile"),
        }
        records.append(record)
        totals["triangles"] += record["triangles"]
        totals["trianglesLod1"] += record["trianglesLod1"]
        totals["anchors"] += len(record["anchors"])
        totals["supports"] += len(record["supports"])
        totals["colliders"] += len(record["colliders"])
        totals["piles"] += int(record["pile"] is not None)
        if not model.exists():
            missing_fbx.append(record["name"])

    return {
        "schema": "frontrooms.unreal.kit-manifest",
        "schemaVersion": 1,
        "sourceRoot": "Assets/Resources/Props/Models",
        "count": len(records),
        "totals": totals,
        "missingFbx": missing_fbx,
        "models": records,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--output", type=Path, default=None)
    args = parser.parse_args()
    repo = args.root.resolve()
    output = (args.output or repo / "Migration/exports/kit_manifest.json").resolve()
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(build_manifest(repo), indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(f"wrote {output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
