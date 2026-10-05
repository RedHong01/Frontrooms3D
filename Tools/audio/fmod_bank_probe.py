"""Verify and exercise the FrontRooms FMOD banks with the Win64 runtime.

This is intentionally independent of Unity and Unreal.  It loads the same
Windows FMOD Studio DLL and bank payload that the migrated Win64 build stages,
resolves the Unity event/parameter/bus contract, starts a real event instance,
and optionally renders the result through FMOD's WAV writer.  A passing probe
therefore proves more than file existence while keeping the native Unreal
module free of a vendored FMOD SDK header.
"""

from __future__ import annotations

import argparse
import ctypes as C
import hashlib
import json
import os
from pathlib import Path
import re
import sys
import time
import xml.etree.ElementTree as ET


ROOT = Path(__file__).resolve().parents[2]
DEFAULT_DLL = ROOT / "Assets/Plugins/FMOD/platforms/win/lib/x86_64/fmodstudio.dll"
DEFAULT_BANKS = ROOT / "Migration/Unreal/Content/FrontRooms/Audio/FMOD/Banks"
EXPECTED_BANKS = ("Master.bank", "Master.strings.bank", "Ambience.bank", "SFX.bank", "Music.bank")
FMOD_VERSION = 0x00020315
FMOD_OUTPUT_NOSOUND = 2
FMOD_OUTPUT_WAVWRITER_NRT = 5
FMOD_STUDIO_INIT_SYNCHRONOUS_UPDATE = 0x4


def fail(message: str) -> "NoReturn":
    print(f"FMOD bank probe FAILED: {message}", file=sys.stderr)
    raise SystemExit(1)


def checked(result: int, operation: str) -> None:
    if result != 0:
        fail(f"{operation} returned FMOD_RESULT {result}")


def source_contract(root: Path) -> tuple[list[tuple[str, list[str]]], list[str], list[str]]:
    ids = (root / "Assets/Scripts/Audio/FrontRoomsSoundIds.cs").read_text(encoding="utf-8")
    before_params, params_text = ids.split("class Param", 1)
    events = dict(re.findall(r"public const string (\w+) = \"([^\"]+)\"", before_params))
    params = dict(re.findall(r"public const string (\w+) = \"([^\"]+)\"", params_text))
    verify = (root / "Assets/Editor/Audio/FrontRoomsFmodVerify.cs").read_text(encoding="utf-8")
    contract_text = verify.split("static readonly Dictionary", 1)[1].split("};", 1)[0]
    contract: list[tuple[str, list[str]]] = []
    for event_name, names in re.findall(
        r"\{\s*SoundIds\.(\w+)\s*,\s*new\s*(?:\[\]\s*\{([^}]*)\}|string\[0\])\s*\}",
        contract_text,
    ):
        event_path = events.get(event_name)
        if not event_path:
            fail(f"SoundIds.{event_name} is missing")
        contract.append((event_path, [params[n] for n in re.findall(r"SoundIds\.Param\.(\w+)", names)]))

    globals_text = verify.split("static readonly string[] Globals", 1)[1].split(";", 1)[0]
    globals_ = [params[n] for n in re.findall(r"SoundIds\.Param\.(\w+)", globals_text)]
    buses_text = verify.split("static readonly string[] Buses", 1)[1].split(";", 1)[0]
    buses = [events[n] for n in re.findall(r"SoundIds\.(Bus\w+)", buses_text)]
    return contract, globals_, buses


def metadata_event_paths(root: Path) -> list[str]:
    """Read event paths from the checked-in FMOD Studio metadata snapshot."""
    metadata = root / "FMOD/FrontRooms/Metadata"
    folder_names: dict[str, str] = {}
    folder_parents: dict[str, str | None] = {}
    master_ids: set[str] = set()

    def object_root(path: Path):
        try:
            return ET.parse(path).getroot().find("./object")
        except (ET.ParseError, OSError):
            return None

    def property_value(obj, name: str) -> str:
        if obj is None:
            return ""
        for prop in obj.findall("./property"):
            if prop.attrib.get("name") == name:
                value = prop.find("./value")
                return (value.text or "") if value is not None else ""
        return ""

    def relationship_destination(obj, name: str) -> str | None:
        if obj is None:
            return None
        for rel in obj.findall("./relationship"):
            if rel.attrib.get("name") == name:
                destination = rel.find("./destination")
                return destination.text if destination is not None else None
        return None

    folder_dir = metadata / "EventFolder"
    if folder_dir.is_dir():
        for path in folder_dir.glob("*.xml"):
            obj = object_root(path)
            if obj is None:
                continue
            object_id = obj.attrib.get("id")
            if not object_id:
                continue
            folder_names[object_id] = property_value(obj, "name")
            folder_parents[object_id] = relationship_destination(obj, "folder")
            if obj.attrib.get("class") == "MasterEventFolder":
                master_ids.add(object_id)

    def folder_path(folder_id: str | None) -> list[str]:
        segments: list[str] = []
        seen: set[str] = set()
        while folder_id and folder_id not in master_ids and folder_id not in seen:
            seen.add(folder_id)
            name = folder_names.get(folder_id)
            if not name:
                break
            segments.append(name)
            folder_id = folder_parents.get(folder_id)
        return list(reversed(segments))

    events: list[str] = []
    event_dir = metadata / "Event"
    if event_dir.is_dir():
        for path in event_dir.glob("*.xml"):
            obj = object_root(path)
            if obj is None or obj.attrib.get("class") != "Event":
                continue
            name = property_value(obj, "name")
            if not name:
                continue
            segments = folder_path(relationship_destination(obj, "folder"))
            events.append("event:/" + "/".join(segments + [name]))
    return sorted(set(events))


class FmodProbe:
    def __init__(self, dll: Path, banks: Path, wav: Path | None = None):
        self.library = C.CDLL(str(dll))
        self.system = C.c_void_p()
        self.banks: list[C.c_void_p] = []
        checked(self.library.FMOD_Studio_System_Create(C.byref(self.system), FMOD_VERSION), "System_Create")
        core = C.c_void_p()
        checked(self.library.FMOD_Studio_System_GetCoreSystem(self.system, C.byref(core)), "GetCoreSystem")
        checked(
            self.library.FMOD5_System_SetOutput(core, FMOD_OUTPUT_WAVWRITER_NRT if wav else FMOD_OUTPUT_NOSOUND),
            "System_SetOutput",
        )
        userdata = C.c_char_p(str(wav).encode()) if wav else None
        checked(
            self.library.FMOD_Studio_System_Initialize(
                self.system, 256, FMOD_STUDIO_INIT_SYNCHRONOUS_UPDATE, 0, userdata
            ),
            "System_Initialize",
        )
        try:
            for name in EXPECTED_BANKS:
                path = banks / name
                handle = C.c_void_p()
                checked(
                    self.library.FMOD_Studio_System_LoadBankFile(
                        self.system, str(path).encode(), 0, C.byref(handle)
                    ),
                    f"LoadBankFile({name})",
                )
                self.banks.append(handle)
                if name != "Master.strings.bank":
                    checked(self.library.FMOD_Studio_Bank_LoadSampleData(handle), f"LoadSampleData({name})")
            checked(self.library.FMOD_Studio_System_FlushSampleLoading(self.system), "FlushSampleLoading")
        except Exception:
            self.close()
            raise

    def event(self, path: str) -> C.c_void_p | None:
        description = C.c_void_p()
        result = self.library.FMOD_Studio_System_GetEvent(self.system, path.encode(), C.byref(description))
        return description if result == 0 else None

    def has_parameter(self, description: C.c_void_p, name: str) -> bool:
        buffer = C.create_string_buffer(128)
        return self.library.FMOD_Studio_EventDescription_GetParameterDescriptionByName(
            description, name.encode(), buffer
        ) == 0

    def has_global(self, name: str) -> bool:
        buffer = C.create_string_buffer(128)
        return self.library.FMOD_Studio_System_GetParameterDescriptionByName(
            self.system, name.encode(), buffer
        ) == 0

    def has_bus(self, path: str) -> bool:
        bus = C.c_void_p()
        return self.library.FMOD_Studio_System_GetBus(self.system, path.encode(), C.byref(bus)) == 0

    def event_paths(self) -> list[str]:
        """Return every event path present in the loaded Win64 banks.

        FMOD's runtime has no dependency on the Studio editor project.  Reading
        the bank event lists here gives the migration manifest a concrete view
        of what was actually shipped, instead of inferring bank freshness from
        file size or timestamps.
        """
        paths: set[str] = set()
        get_count = self.library.FMOD_Studio_Bank_GetEventCount
        get_list = self.library.FMOD_Studio_Bank_GetEventList
        get_path = self.library.FMOD_Studio_EventDescription_GetPath
        for bank in self.banks:
            count = C.c_int()
            result = get_count(bank, C.byref(count))
            if result != 0 or count.value <= 0:
                continue
            descriptions = (C.c_void_p * count.value)()
            retrieved = C.c_int()
            result = get_list(bank, descriptions, count.value, C.byref(retrieved))
            if result != 0:
                continue
            for description in descriptions[: max(0, retrieved.value)]:
                path = C.create_string_buffer(512)
                length = C.c_int()
                if get_path(description, path, len(path), C.byref(length)) == 0:
                    paths.add(path.value.decode("utf-8", errors="replace"))
        return sorted(paths)

    def play_and_update(self, path: str, seconds: float = 1.5) -> None:
        description = self.event(path)
        if description is None:
            fail(f"playback event not found: {path}")
        instance = C.c_void_p()
        checked(
            self.library.FMOD_Studio_EventDescription_CreateInstance(description, C.byref(instance)),
            f"CreateInstance({path})",
        )
        try:
            checked(self.library.FMOD_Studio_EventInstance_Start(instance), f"EventInstance_Start({path})")
            blocks = max(1, int(seconds * 48000 / 512))
            for _ in range(blocks):
                checked(self.library.FMOD_Studio_System_Update(self.system), "System_Update")
                # WAV writer is non-realtime; a short yield avoids starving the
                # DLL's worker thread without making the probe depend on timing.
                time.sleep(0.0005)
        finally:
            self.library.FMOD_Studio_EventInstance_Release(instance)

    def close(self) -> None:
        if self.system:
            self.library.FMOD_Studio_System_Release(self.system)
            self.system = C.c_void_p()


def write_manifest(
    root: Path,
    dll: Path,
    banks: Path,
    contract: list[tuple[str, list[str]]] | None = None,
    runtime_events: list[str] | None = None,
) -> Path:
    entries = []
    source_bank_entries = []
    for name in EXPECTED_BANKS:
        path = banks / name
        entries.append({"name": name, "path": str(path.relative_to(root)).replace("\\", "/"), "bytes": path.stat().st_size,
                        "sha256": hashlib.sha256(path.read_bytes()).hexdigest()})
        unity_path = root / "Assets/StreamingAssets/FMOD" / name
        if unity_path.is_file():
            source_bank_entries.append({
                "name": name,
                "path": str(unity_path.relative_to(root)).replace("\\", "/"),
                "bytes": unity_path.stat().st_size,
                "sha256": hashlib.sha256(unity_path.read_bytes()).hexdigest(),
            })
    expected_events = sorted(path for path, _ in (contract or []))
    resolved_events = sorted(set(runtime_events or []))
    source_project = root / "FMOD/FrontRooms/FrontRooms.fspro"
    metadata_events = root / "FMOD/FrontRooms/Metadata/Event"
    metadata_event_paths_ = metadata_event_paths(root)
    project_object_count = 0
    if source_project.is_file():
        try:
            project_object_count = len(list(ET.parse(source_project).getroot()))
        except (ET.ParseError, OSError):
            project_object_count = -1
    metadata_event_count = len(list(metadata_events.glob("*.xml"))) if metadata_events.is_dir() else 0
    source_info = {
        "project": str(source_project.relative_to(root)).replace("\\", "/")
        if source_project.exists()
        else "FMOD/FrontRooms/FrontRooms.fspro",
        "projectBytes": source_project.stat().st_size if source_project.is_file() else 0,
        "projectSha256": hashlib.sha256(source_project.read_bytes()).hexdigest()
        if source_project.is_file()
        else None,
        "projectObjectCount": project_object_count,
        "projectHasNoObjects": project_object_count == 0,
        "metadataEventFiles": metadata_event_count,
        "metadataStatus": "metadata-only"
        if project_object_count == 0 and metadata_event_count
        else "project-and-metadata",
        "expectedEvents": expected_events,
        "metadataEvents": metadata_event_paths_,
        "metadataEventPathMismatches": sorted(
            set(expected_events).symmetric_difference(metadata_event_paths_)
        ),
    }
    drift = {
        "expectedEventCount": len(expected_events),
        "resolvedEventCount": len(set(expected_events).intersection(resolved_events)),
        "missingEvents": sorted(set(expected_events).difference(resolved_events)),
        "runtimeEventCount": len(resolved_events),
        "runtimeEvents": resolved_events,
        "status": "matched"
        if expected_events and not set(expected_events).difference(resolved_events)
        else "source-bank-drift",
    }
    staged_hashes = {entry["name"]: entry["sha256"] for entry in entries}
    source_hashes = {entry["name"]: entry["sha256"] for entry in source_bank_entries}
    manifest = {
        "schema": "frontrooms.fmod.win64",
        # Keep the original manifest schema version: the new source/contract
        # fields are additive so existing Windows audit consumers remain valid.
        "schemaVersion": 1,
        "target": "Win64",
        "runtime": {"path": str(dll.relative_to(root)).replace("\\", "/"), "bytes": dll.stat().st_size,
                     "sha256": hashlib.sha256(dll.read_bytes()).hexdigest()},
        "banks": entries,
        "unitySourceBanks": source_bank_entries,
        "unitySourceHashMismatches": sorted(
            name for name in EXPECTED_BANKS
            if name not in source_hashes or source_hashes[name] != staged_hashes.get(name)
        ),
        "source": source_info,
        "contract": drift,
    }
    output = root / "Migration/exports/fmod_bank_manifest.json"
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    return output


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=ROOT)
    parser.add_argument("--dll", type=Path)
    parser.add_argument("--banks", type=Path)
    parser.add_argument("--render", type=Path, help="Render a real FMOD event to this WAV file")
    parser.add_argument("--write-manifest", action="store_true")
    parser.add_argument(
        "--allow-missing-event",
        action="append",
        default=[],
        help="Keep the bank probe green for a known source/bank drift; the gap is printed and remains visible in smoke output.",
    )
    args = parser.parse_args()
    root = args.root.resolve()
    dll = (args.dll or (root / DEFAULT_DLL.relative_to(ROOT))).resolve()
    banks = (args.banks or (root / DEFAULT_BANKS.relative_to(ROOT))).resolve()
    if not dll.is_file():
        fail(f"Win64 FMOD runtime DLL missing: {dll}")
    missing = [name for name in EXPECTED_BANKS if not (banks / name).is_file()]
    if missing:
        fail(f"bank payload missing below {banks}: {', '.join(missing)}")
    contract, globals_, buses = source_contract(root)
    probe = FmodProbe(dll, banks, args.render.resolve() if args.render else None)
    try:
        runtime_events = probe.event_paths()
        if args.write_manifest:
            print(f"FMOD bank manifest: {write_manifest(root, dll, banks, contract, runtime_events)}")
        failures = []
        allowed_gaps = []
        for path, parameters in contract:
            description = probe.event(path)
            if description is None:
                if path in args.allow_missing_event:
                    allowed_gaps.append(path)
                else:
                    failures.append(f"missing event {path}")
                continue
            failures.extend(
                f"missing parameter {parameter} on {path}"
                for parameter in parameters
                if not probe.has_parameter(description, parameter)
            )
        failures.extend(f"missing global parameter {name}" for name in globals_ if not probe.has_global(name))
        failures.extend(f"missing bus {path}" for path in buses if not probe.has_bus(path))
        if failures:
            fail("\n  ".join(failures))
        # This is an actual event instance start/update against the banks. If
        # --render is supplied, the WAV size is checked below as a stronger
        # playback proof; otherwise the output is FMOD's no-sound device.
        probe.play_and_update("event:/Foley/Player/Footstep")
    finally:
        probe.close()

    if args.render:
        if not args.render.is_file() or args.render.stat().st_size < 44 + 512:
            fail(f"FMOD render did not produce a valid WAV: {args.render}")
        print(f"FrontRooms FMOD bank probe passed: {len(EXPECTED_BANKS)} banks loaded, {len(contract) - len(allowed_gaps)} events resolved, rendered playback {args.render.stat().st_size} bytes")
    else:
        print(f"FrontRooms FMOD bank probe passed: {len(EXPECTED_BANKS)} banks loaded, {len(contract) - len(allowed_gaps)} events resolved, event instance started")
    if allowed_gaps:
        print("FrontRooms FMOD bank contract gaps allowed for source/bank drift: " + ", ".join(allowed_gaps))
    return 0


if __name__ == "__main__":
    main()
