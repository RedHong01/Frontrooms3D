#!/usr/bin/env python3
"""Create and compare the Unity -> Unreal migration synchronization manifest.

The Unity checkout is the portable source of truth.  This command is deliberately
stdlib-only so it can run on a Mac or Windows without Unity, Unreal, PowerShell,
or a shell-specific path convention.  It hashes Unity-side source/data/assets,
then records the generated migration contracts and the Unreal Win64 invariant.

Typical usage (from the repository root)::

    python3 Tools/UnrealMigration/sync_unity_unreal.py --update
    python3 Tools/UnrealMigration/sync_unity_unreal.py --check

``--update`` refreshes the committed snapshot and writes a human-readable report.
``--check`` compares against the snapshot and exits non-zero when Unreal work is
required.  A report is still written in check mode, which makes CI diagnostics
useful without mutating the baseline.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Iterable


SCHEMA = "frontrooms.unreal.unity-sync-manifest"
SCHEMA_VERSION = 1
DEFAULT_MANIFEST = Path("Migration/exports/unity_sync_manifest.json")
DEFAULT_REPORT = Path("Migration/exports/unity_sync_report.json")

# These roots are Unity-side inputs.  Unreal's generated Content/Binaries/Saved
# trees are intentionally excluded; they must never become Mac -> Windows sync
# inputs or a source of accidental platform coupling.
SOURCE_ROOTS = (
    "Assets",
    "ProjectSettings",
    "Packages",
    "FMOD",
    "AudioSource",
    "Docs",
    "Documentation",
)
EXCLUDED_DIRS = {
    "Library",
    "Temp",
    "Obj",
    "Build",
    "Builds",
    "Logs",
    "UserSettings",
    "MemoryCaptures",
    "Recordings",
    ".git",
    "__pycache__",
}
EXCLUDED_FILE_NAMES = {"Thumbs.db", ".DS_Store", "desktop.ini"}


def relpath(repo: Path, path: Path) -> str:
    """Return a stable repository-relative POSIX path."""

    return path.relative_to(repo).as_posix()


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def git(repo: Path, *args: str) -> str | None:
    try:
        return subprocess.check_output(
            ["git", "-C", str(repo), *args],
            text=True,
            stderr=subprocess.DEVNULL,
        ).strip()
    except (OSError, subprocess.CalledProcessError):
        return None


def iter_source_files(repo: Path) -> Iterable[Path]:
    """Yield Unity-side files in deterministic order, including untracked edits."""

    paths: set[Path] = set()
    for root_name in SOURCE_ROOTS:
        root = repo / root_name
        if not root.exists():
            continue
        for path in root.rglob("*"):
            if not path.is_file():
                continue
            if path.name in EXCLUDED_FILE_NAMES:
                continue
            if any(part in EXCLUDED_DIRS for part in path.relative_to(repo).parts):
                continue
            # FMOD's analysis cache is machine-generated and is excluded even if
            # a developer has created it outside the normal .gitignore path.
            if "FMOD" in path.relative_to(repo).parts and ".cache" in path.parts:
                continue
            paths.add(path)
    yield from sorted(paths, key=lambda p: relpath(repo, p))


def file_kind(relative: str) -> str:
    path = Path(relative)
    ext = path.suffix.lower()
    first = path.parts[0] if path.parts else ""
    if first == "ProjectSettings":
        return "unity-project-settings"
    if first == "Packages":
        return "unity-package-lock"
    if first == "FMOD":
        return "fmod-source"
    if first in {"Docs", "Documentation"}:
        return "design-contract"
    if ext in {".cs", ".asmdef", ".shader", ".compute", ".hlsl", ".cginc"}:
        return "unity-code"
    if ext in {".unity", ".asset", ".prefab", ".mat", ".controller", ".anim", ".json"}:
        return "unity-data"
    if ext in {".fbx", ".blend", ".obj", ".fobj"}:
        return "mesh"
    if ext in {".png", ".jpg", ".jpeg", ".tga", ".exr", ".hdr", ".svg"}:
        return "texture-or-vector"
    if ext in {".wav", ".ogg", ".mp3", ".flac", ".bank", ".mp4", ".mov"}:
        return "audio-or-video"
    if ext in {".ttf", ".otf"}:
        return "font"
    if ext == ".meta":
        return "unity-guid-sidecar"
    return "unity-source-asset"


def source_inventory(repo: Path) -> list[dict[str, Any]]:
    records: list[dict[str, Any]] = []
    for path in iter_source_files(repo):
        relative = relpath(repo, path)
        records.append(
            {
                "path": relative,
                "kind": file_kind(relative),
                "bytes": path.stat().st_size,
                "sha256": sha256(path),
            }
        )
    return records


def artifact_records(repo: Path) -> list[dict[str, Any]]:
    """Hash the generated inputs consumed by Unreal's contract commandlet."""

    artifacts = (
        "Migration/exports/frontrooms_contract.json",
        "Migration/exports/kit_manifest.json",
        "Migration/exports/asset_bridge.json",
        "Migration/exports/unreal_import_settings.json",
        "Migration/exports/unreal_texture_import_settings.json",
        "Migration/exports/unreal_material_profiles.json",
        "Migration/contract.schema.json",
    )
    result: list[dict[str, Any]] = []
    for relative in artifacts:
        path = repo / relative
        if path.exists():
            result.append(
                {
                    "path": relative,
                    "bytes": path.stat().st_size,
                    "sha256": sha256(path),
                }
            )
        else:
            result.append({"path": relative, "bytes": 0, "sha256": None})
    # The Unity Editor golden-chunk exporter is optional on a workstation but,
    # when present, its per-seed JSON is a first-class Unreal parity input.
    chunk_root = repo / "Migration/exports/unity_chunks"
    if chunk_root.exists():
        for path in sorted(chunk_root.rglob("*"), key=lambda p: relpath(repo, p)):
            if path.is_file():
                relative = relpath(repo, path)
                result.append(
                    {
                        "path": relative,
                        "bytes": path.stat().st_size,
                        "sha256": sha256(path),
                    }
                )
    return result


def exports_fresh(repo: Path) -> dict[str, bool]:
    """Check that generated JSON was produced from the current Unity inputs.

    This prevents a Mac developer from updating the snapshot before running the
    exporters: doing so would hide a Unity change behind an old contract.
    """

    result = {"contract": False, "kitManifest": False, "assetBridge": False, "materialProfiles": False}
    contract_path = repo / "Migration/exports/frontrooms_contract.json"
    try:
        contract = json.loads(contract_path.read_text(encoding="utf-8"))
        source = contract.get("source", {})
        result["contract"] = bool(
            source.get("unityVersion") == unity_version(repo)
            and bool(source.get("sourceHashes"))
            and all(
                isinstance(path, str)
                and (repo / path).exists()
                and sha256(repo / path) == digest
                for path, digest in source.get("sourceHashes", {}).items()
            )
        )
    except (OSError, json.JSONDecodeError, TypeError):
        pass

    kit_path = repo / "Migration/exports/kit_manifest.json"
    try:
        kit = json.loads(kit_path.read_text(encoding="utf-8"))
        models = kit.get("models", [])
        result["kitManifest"] = bool(
            models
            and all(
                isinstance(model.get("source"), str)
                and (repo / model["source"]).exists()
                and sha256(repo / model["source"]) == model.get("sourceSha256")
                for model in models
            )
        )
    except (OSError, json.JSONDecodeError, TypeError):
        pass

    bridge_path = repo / "Migration/exports/asset_bridge.json"
    try:
        bridge = json.loads(bridge_path.read_text(encoding="utf-8"))
        files = bridge.get("files", [])
        result["assetBridge"] = bool(
            files
            and all(
                isinstance(file.get("source"), str)
                and (repo / file["source"]).exists()
                and sha256(repo / file["source"]) == file.get("sha256")
                for file in files
            )
        )
    except (OSError, json.JSONDecodeError, TypeError):
        pass

    material_path = repo / "Migration/exports/unreal_material_profiles.json"
    try:
        profiles = json.loads(material_path.read_text(encoding="utf-8")).get("profiles", [])
        result["materialProfiles"] = bool(
            profiles
            and all(
                isinstance(profile.get("source"), str)
                and isinstance(profile.get("sourceSha256"), str)
                and (repo / profile["source"]).exists()
                and sha256(repo / profile["source"]) == profile["sourceSha256"]
                for profile in profiles
            )
        )
    except (OSError, json.JSONDecodeError, TypeError):
        pass
    return result


def read_unreal_platforms(repo: Path) -> tuple[list[str], str | None]:
    project = repo / "Migration/Unreal/FrontRoomsUE.uproject"
    if not project.exists():
        return [], None
    try:
        document = json.loads(project.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        return [], None
    platforms = document.get("TargetPlatforms", [])
    if not isinstance(platforms, list):
        platforms = []
    return [str(platform) for platform in platforms], sha256(project)


def build_manifest(repo: Path) -> dict[str, Any]:
    platforms, project_hash = read_unreal_platforms(repo)
    return {
        "schema": SCHEMA,
        "schemaVersion": SCHEMA_VERSION,
        "source": {
            "gitHead": git(repo, "rev-parse", "HEAD"),
            "gitBranch": git(repo, "branch", "--show-current"),
            "unityVersion": unity_version(repo),
        },
        "sourceRoots": list(SOURCE_ROOTS),
        "files": source_inventory(repo),
        "generatedArtifacts": artifact_records(repo),
        "exports": exports_fresh(repo),
        "unreal": {
            "project": "Migration/Unreal/FrontRoomsUE.uproject",
            "projectSha256": project_hash,
            "targetPlatforms": platforms,
            "requiredTargetPlatforms": ["Win64"],
        },
    }


def unity_version(repo: Path) -> str | None:
    path = repo / "ProjectSettings/ProjectVersion.txt"
    if not path.exists():
        return None
    match = re.search(r"^m_EditorVersion:\s*(\S+)", path.read_text(encoding="utf-8"), re.MULTILINE)
    return match.group(1) if match else None


def index(records: list[dict[str, Any]]) -> dict[str, dict[str, Any]]:
    return {record["path"]: record for record in records}


def diff_manifests(previous: dict[str, Any] | None, current: dict[str, Any]) -> dict[str, Any]:
    if previous is None:
        return {
            "baseline": "missing",
            "changedFiles": [record["path"] for record in current["files"]],
            "addedFiles": [record["path"] for record in current["files"]],
            "removedFiles": [],
            "changedArtifacts": [record["path"] for record in current["generatedArtifacts"]],
            "sourceChanged": True,
            "artifactsChanged": True,
        }
    old_files = index(previous.get("files", []))
    new_files = index(current["files"])
    added = sorted(set(new_files) - set(old_files))
    removed = sorted(set(old_files) - set(new_files))
    changed = sorted(
        path
        for path in set(old_files) & set(new_files)
        if old_files[path].get("sha256") != new_files[path].get("sha256")
    )
    old_artifacts = index(previous.get("generatedArtifacts", []))
    new_artifacts = index(current["generatedArtifacts"])
    changed_artifacts = sorted(
        path
        for path in set(old_artifacts) | set(new_artifacts)
        if old_artifacts.get(path, {}).get("sha256") != new_artifacts.get(path, {}).get("sha256")
    )
    return {
        "baseline": "present",
        "changedFiles": changed,
        "addedFiles": added,
        "removedFiles": removed,
        "changedArtifacts": changed_artifacts,
        "sourceChanged": bool(changed or added or removed),
        "artifactsChanged": bool(changed_artifacts),
    }


def load_json(path: Path) -> dict[str, Any] | None:
    if not path.exists():
        return None
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        raise SystemExit(f"invalid sync manifest {path}: {error}") from error
    return value if isinstance(value, dict) else None


def write_json(path: Path, value: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument("--report", type=Path, default=DEFAULT_REPORT)
    parser.add_argument("--update", action="store_true", help="write the current manifest as the new baseline")
    parser.add_argument("--check", action="store_true", help="fail when the baseline requires Unreal synchronization")
    args = parser.parse_args()
    if args.update and args.check:
        parser.error("--update and --check are mutually exclusive")
    if not args.update and not args.check:
        args.update = True

    repo = args.root.resolve()
    manifest_path = (repo / args.manifest).resolve() if not args.manifest.is_absolute() else args.manifest.resolve()
    report_path = (repo / args.report).resolve() if not args.report.is_absolute() else args.report.resolve()
    previous = load_json(manifest_path)
    current = build_manifest(repo)
    difference = diff_manifests(previous, current)
    platforms = current["unreal"]["targetPlatforms"]
    win64_only = platforms == ["Win64"]
    exports_ready = all(current["exports"].values())
    if not exports_ready:
        state = "Pending Export"
    elif difference["baseline"] == "missing":
        state = "Contract Exported; Unreal baseline pending"
    elif difference["sourceChanged"] or difference["artifactsChanged"]:
        state = "Pending Unreal Sync"
    else:
        state = "No Unity changes detected"
    report = {
        "schema": "frontrooms.unreal.unity-sync-report",
        "schemaVersion": 1,
        "generatedAtUtc": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
        "state": state,
        "win64Only": win64_only,
        "exportsReady": exports_ready,
        "exports": current["exports"],
        "diff": difference,
        "current": {
            "gitHead": current["source"]["gitHead"],
            "gitBranch": current["source"]["gitBranch"],
            "unityVersion": current["source"]["unityVersion"],
            "fileCount": len(current["files"]),
            "artifactCount": len(current["generatedArtifacts"]),
            "unrealTargetPlatforms": platforms,
        },
    }
    write_json(report_path, report)
    if args.update:
        if not exports_ready:
            print(
                "generated exports are stale or missing; run contract, kit and "
                "asset-bridge exporters before --update",
                file=sys.stderr,
            )
            return 1
        write_json(manifest_path, current)
    elif previous is None:
        print(f"sync baseline missing: {manifest_path}", file=sys.stderr)
        return 2

    print(
        f"FrontRooms Unity/Unreal sync {state}: "
        f"{len(difference['changedFiles'])} changed, "
        f"{len(difference['addedFiles'])} added, "
        f"{len(difference['removedFiles'])} removed, "
        f"{len(difference['changedArtifacts'])} artifacts; "
        f"Win64-only={win64_only}"
    )
    if args.check and (
        difference["sourceChanged"]
        or difference["artifactsChanged"]
        or not win64_only
        or not exports_ready
    ):
        return 1
    if not win64_only or not exports_ready:
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
