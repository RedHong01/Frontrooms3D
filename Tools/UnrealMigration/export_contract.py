#!/usr/bin/env python3
"""Export the Unity FrontRooms data contract for the Unreal migration.

This exporter intentionally reads the serialized Unity assets and source
contracts without importing Unity.  The output is a reviewable, deterministic
JSON snapshot that becomes the first input to the Unreal DataAsset importer.
It does not attempt to generate gameplay geometry; that remains an Unreal
runtime concern and is verified against this contract.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import subprocess
from collections import Counter
from pathlib import Path
from typing import Any, Iterable


EDGE_NAMES = {0: "Wall", 1: "Open", 2: "Arch"}
HEIGHT_NAMES = {0: "Low", 1: "Standard", 2: "Tall"}
THEME_NAMES = {0: "Level0", 1: "Office"}
COLUMN_NAMES = {0: "Auto", 1: "None", 2: "Custom"}
FILL_NAMES = {0: "None", 1: "Auto", 2: "Office", 3: "Pile"}
MARKER_NAMES = {0: "KeySpot", 1: "RelayEntry"}


def scalar(value: str) -> Any:
    """Parse the small scalar subset emitted by Unity YAML assets."""

    value = value.strip()
    if value == "[]":
        return []
    if value in ("", "null", "Null", "NULL"):
        return None
    if value.startswith("'") and value.endswith("'"):
        return value[1:-1].replace("''", "'")
    if value.startswith('"') and value.endswith('"'):
        return value[1:-1].replace('\\"', '"')
    if value in ("true", "True"):
        return True
    if value in ("false", "False"):
        return False
    if re.fullmatch(r"[-+]?\d+", value):
        return int(value)
    if re.fullmatch(r"[-+]?(?:\d+\.\d*|\.\d+|\d+)(?:[eE][-+]?\d+)?[fF]?", value):
        return float(value.rstrip("fF"))
    return value


def named_scalars(
    lines: list[str],
    heading: str,
    indent: int,
    preserve_digit_strings: set[str] | None = None,
) -> dict[str, Any]:
    """Read scalar fields under an indented YAML heading."""

    heading_re = re.compile(rf"^{' ' * indent}{re.escape(heading)}:\s*$")
    field_re = re.compile(rf"^{' ' * (indent + 2)}([A-Za-z_]\w*):\s*(.*)$")
    start = next((i for i, line in enumerate(lines) if heading_re.match(line)), None)
    if start is None:
        return {}
    result: dict[str, Any] = {}
    for line in lines[start + 1 :]:
        if line.strip() and len(line) - len(line.lstrip(" ")) <= indent:
            break
        match = field_re.match(line)
        if match:
            key, raw = match.group(1), match.group(2)
            result[key] = raw.strip() if preserve_digit_strings and key in preserve_digit_strings else scalar(raw)
    return result


def top_level_scalars(lines: list[str]) -> dict[str, Any]:
    result: dict[str, Any] = {}
    field_re = re.compile(r"^  ([A-Za-z_]\w*):\s*(.*)$")
    for line in lines:
        match = field_re.match(line)
        if match and match.group(1) not in {"generation", "modules"}:
            result[match.group(1)] = scalar(match.group(2))
    return result


def folded_note(lines: list[str]) -> str | None:
    for index, line in enumerate(lines):
        if not line.startswith("  notes:"):
            continue
        value = line.split(":", 1)[1].strip()
        pieces = [value[1:] if value.startswith("'") else value]
        for continuation in lines[index + 1 :]:
            if continuation.startswith("    "):
                pieces.append(continuation.strip())
            else:
                break
        joined = " ".join(pieces)
        if joined.endswith("'"):
            joined = joined[:-1]
        return joined.replace("''", "'")
    return None


def struct_list(lines: list[str], heading: str) -> list[dict[str, Any]]:
    """Read Unity's list-of-inline-records form, e.g. props and markers."""

    heading_re = re.compile(rf"^    {re.escape(heading)}:\s*$")
    field_re = re.compile(r"^      ([A-Za-z_]\w*):\s*(.*)$")
    start = next((i for i, line in enumerate(lines) if heading_re.match(line)), None)
    if start is None:
        return []
    result: list[dict[str, Any]] = []
    current: dict[str, Any] | None = None
    for line in lines[start + 1 :]:
        if line.strip() and len(line) - len(line.lstrip(" ")) < 4:
            break
        if line.startswith("    - "):
            if current is not None:
                result.append(current)
            current = {}
            inline = line[6:]
            match = re.match(r"([A-Za-z_]\w*):\s*(.*)$", inline)
            if match:
                current[match.group(1)] = scalar(match.group(2))
            continue
        match = field_re.match(line)
        if match and current is not None:
            current[match.group(1)] = scalar(match.group(2))
    if current is not None:
        result.append(current)
    return result


def compact_digits(value: Any) -> Any:
    if isinstance(value, str) and re.fullmatch(r"\d+", value):
        return [int(char) for char in value]
    return value


def repo_relative_asset(path: Path) -> str:
    """Return a stable repository-relative path for an Assets file."""

    try:
        index = path.parts.index("Assets")
    except ValueError:
        return path.name
    return Path(*path.parts[index:]).as_posix()


def parse_module(path: Path) -> dict[str, Any]:
    lines = path.read_text(encoding="utf-8").splitlines()
    name = next((m.group(1) for line in lines if (m := re.match(r"^  m_Name:\s*(.*)$", line))), path.stem)
    data = named_scalars(
        lines,
        "data",
        2,
        preserve_digit_strings={"south", "north", "west", "east", "innerEast", "innerNorth", "lamps"},
    )
    for key in ("south", "north", "west", "east", "innerEast", "innerNorth", "lamps"):
        if key in data:
            data[key + "Values"] = compact_digits(data[key])
    for key, table in (
        ("height", HEIGHT_NAMES),
        ("theme", THEME_NAMES),
        ("columns", COLUMN_NAMES),
        ("fill", FILL_NAMES),
    ):
        if isinstance(data.get(key), int):
            data[key + "Name"] = table.get(data[key], f"Unknown({data[key]})")
    props = struct_list(lines, "props")
    markers = struct_list(lines, "markers")
    for marker in markers:
        if isinstance(marker.get("kind"), int):
            marker["kindName"] = MARKER_NAMES.get(marker["kind"], f"Unknown({marker['kind']})")
    return {
        "name": name,
        "source": repo_relative_asset(path),
        "notes": folded_note(lines),
        "data": data,
        "props": props,
        "markers": markers,
        "customColumns": struct_list(lines, "customColumns"),
    }


def parse_profile(path: Path) -> dict[str, Any]:
    lines = path.read_text(encoding="utf-8").splitlines()
    generation = named_scalars(lines, "generation", 2)
    top = top_level_scalars(lines)
    module_guids = [m.group(1) for line in lines if (m := re.search(r"guid: ([0-9a-f]{32})", line))]
    return {
        "name": next((m.group(1) for line in lines if (m := re.match(r"^  m_Name:\s*(.*)$", line))), path.stem),
        "source": repo_relative_asset(path),
        "generation": generation,
        "run": {key: top[key] for key in ("runSeed", "buildRadius", "chunksPerFrame", "shiftAfterSeconds", "doorsNeedKeys") if key in top},
        "lighting": {key: top[key] for key in ("lightRadius", "shadowRadius") if key in top},
        "modules": module_guids,
        "dressing": {key: top[key] for key in ("dressOffices", "pileChance") if key in top},
    }


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for block in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def git(repo: Path, *args: str) -> str | None:
    try:
        return subprocess.check_output(["git", "-C", str(repo), *args], text=True).strip()
    except (OSError, subprocess.CalledProcessError):
        return None


def inventory(repo: Path) -> dict[str, int]:
    counts: Counter[str] = Counter()
    for root in (repo / "Assets", repo / "FMOD", repo / "AudioSource", repo / "Tools"):
        if not root.exists():
            continue
        for path in root.rglob("*"):
            if "__pycache__" in path.parts:
                continue
            if path.is_file() and path.suffix.lower() != ".meta":
                counts[path.suffix.lower() or "<no-extension>"] += 1
    return dict(sorted(counts.items()))


def sound_contract(repo: Path) -> dict[str, Any]:
    path = repo / "Assets/Scripts/Audio/FrontRoomsSoundIds.cs"
    text = path.read_text(encoding="utf-8") if path.exists() else ""
    event_paths = re.findall(r'public const string \w+ = "(event:/[^"]+)";', text)
    bus_paths = re.findall(r'public const string \w+ = "(bus:/[^"]*)";', text)
    params = re.findall(r'public const string (\w+) = "([^"]+)";', text[text.find("public static class Param") :])
    return {
        "source": path.relative_to(repo).as_posix(),
        "eventCount": len(event_paths),
        "eventPaths": event_paths,
        "busCount": len(bus_paths),
        "busPaths": bus_paths,
        "parameterCount": len(params),
        "parameters": [{"identifier": name, "value": value} for name, value in params],
    }


def build_contract(repo: Path) -> dict[str, Any]:
    profile_path = repo / "Assets/Levels/FrontRoomsLevel0.asset"
    module_paths = sorted((repo / "Assets/Levels/Modules").glob("*.asset"))
    scene_settings = repo / "ProjectSettings/EditorBuildSettings.asset"
    enabled_scenes = []
    if scene_settings.exists():
        scene_lines = scene_settings.read_text(encoding="utf-8").splitlines()
        for index, line in enumerate(scene_lines[:-1]):
            enabled = re.match(r"\s*- enabled:\s*(\d+)", line)
            path = re.match(r"\s*path:\s*(Assets/Scenes/[^\r\n]+\.unity)\s*$", scene_lines[index + 1])
            if enabled and path and int(enabled.group(1)) == 1:
                enabled_scenes.append(path.group(1))
    source_files = [
        "Assets/Scripts/FrontRoomsMap/FrontRoomsMap.cs",
        "Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs",
        "Assets/Scripts/FrontRoomsMap/FrontRoomsMapHunter.cs",
        "Assets/Scripts/FrontRoomsMap/FrontRoomsLevelProfile.cs",
        "Assets/Scripts/FrontRoomsMap/FrontRoomsRoomModuleData.cs",
        "Assets/Scripts/Audio/FrontRoomsSoundIds.cs",
    ]
    source_hashes = {
        path: sha256(repo / path) for path in source_files if (repo / path).exists()
    }
    manifest = repo / "Packages/manifest.json"
    unity_version_file = repo / "ProjectSettings/ProjectVersion.txt"
    unity_version_text = unity_version_file.read_text(encoding="utf-8") if unity_version_file.exists() else ""
    unity_version = re.search(r"^m_EditorVersion:\s*(\S+)", unity_version_text, re.MULTILINE)
    unity_revision = re.search(r"^m_EditorVersionWithRevision:\s*(.*)$", unity_version_text, re.MULTILINE)
    return {
        "schema": "frontrooms.unreal.migration.contract",
        "schemaVersion": 1,
        "source": {
            "gitHead": git(repo, "rev-parse", "HEAD"),
            "gitBranch": git(repo, "branch", "--show-current"),
            "unityVersion": unity_version.group(1) if unity_version else None,
            "unityVersionWithRevision": unity_revision.group(1).strip() if unity_revision else None,
            "unityManifestSha256": sha256(manifest) if manifest.exists() else None,
            "sourceHashes": source_hashes,
        },
        "constants": {
            "cellSizeMeters": 3.0,
            "chunkCells": 8,
            "chunkSizeMeters": 24.0,
            "worldRebaseThresholdMeters": 192.0,
            "ceilingMeters": {"Low": 2.4, "Standard": 2.9, "Tall": 5.4},
        },
        "validationSeeds": [2554, 20388, 20261001],
        "buildScenes": enabled_scenes,
        "levelProfile": parse_profile(profile_path) if profile_path.exists() else None,
        "modules": [parse_module(path) for path in module_paths],
        "assetInventory": inventory(repo),
        "audio": sound_contract(repo),
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--output", type=Path, default=None)
    args = parser.parse_args()
    repo = args.root.resolve()
    output = (args.output or repo / "Migration/exports/frontrooms_contract.json").resolve()
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(build_contract(repo), indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(f"wrote {output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
