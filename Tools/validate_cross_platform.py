#!/usr/bin/env python3
"""Validate repository invariants that differ between macOS and Windows.

The check intentionally uses only Git and the Python standard library, so it
can run before Unity is installed and on every supported desktop OS.  It
checks tracked paths, text encoding/line endings, JSON/Python syntax, and
case-sensitive path references.  Use ``--fix-line-endings`` only in a clean
worktree when normalising an older checkout.
"""

from __future__ import annotations

import argparse
import ast
import json
import re
import subprocess
import sys
import unicodedata
from pathlib import Path


TEXT_SUFFIXES = {
    ".asmdef", ".asmref", ".asset", ".anim", ".c", ".cginc", ".compute",
    ".cpp", ".cs", ".csv", ".h", ".hlsl", ".inputactions", ".json",
    ".js", ".m", ".mat", ".md", ".meta", ".mm", ".playable", ".prefab",
    ".py", ".shader", ".sh", ".swift", ".ts", ".tsv", ".txt", ".unity",
    ".uss", ".uxml", ".xml", ".yml", ".yaml",
}
TEXT_NAMES = {".editorconfig", ".gitattributes", ".gitignore"}
REFERENCE_ROOTS = (
    "Assets", "Packages", "ProjectSettings", "Migration", "Tools",
    "Documentation", "Docs", "NativePlugin", "AudioSource", "Verification",
)
WINDOWS_RESERVED = {"CON", "PRN", "AUX", "NUL"}
WINDOWS_RESERVED.update(f"COM{i}" for i in range(1, 10))
WINDOWS_RESERVED.update(f"LPT{i}" for i in range(1, 10))
REFERENCE_RE = re.compile(
    r"(?P<quote>[\"'])(?P<path>(?:"
    + "|".join(REFERENCE_ROOTS)
    + r")(?:/[A-Za-z0-9_ .()+@\-]+)+)(?P=quote)"
)
GUID_RE = re.compile(r"^guid:\s*([0-9a-fA-F]{32})\s*$", re.MULTILINE)


def git(root: Path, *args: str) -> bytes:
    result = subprocess.run(
        ["git", "-C", str(root), *args],
        check=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    return result.stdout


def tracked_paths(root: Path) -> list[str]:
    return [p.decode("utf-8") for p in git(root, "ls-files", "-z").split(b"\0") if p]


def norm_path(value: str) -> str:
    return unicodedata.normalize("NFC", value.replace("\\", "/")).casefold()


def is_text_path(path: str) -> bool:
    p = Path(path)
    return p.name in TEXT_NAMES or p.suffix.casefold() in TEXT_SUFFIXES


def read_text_bytes(root: Path, rel: str) -> tuple[bytes | None, str | None]:
    data = (root / rel).read_bytes()
    if b"\0" in data:
        return None, None
    try:
        return data, data.decode("utf-8")
    except UnicodeDecodeError as exc:
        return data, f"invalid UTF-8 at byte {exc.start}"


def check_paths(paths: list[str]) -> list[str]:
    issues: list[str] = []
    folded: dict[str, list[str]] = {}
    for rel in paths:
        folded.setdefault(norm_path(rel), []).append(rel)
        for component in rel.split("/"):
            base = component.rstrip(" .").split(".", 1)[0].upper()
            if any(ord(ch) < 32 for ch in component):
                issues.append(f"control character in path: {rel}")
            if any(ch in '<>:"\\|?*' for ch in component):
                issues.append(f"Windows-illegal character in path: {rel}")
            if component.endswith((" ", ".")):
                issues.append(f"Windows-illegal trailing space/dot in path: {rel}")
            if base in WINDOWS_RESERVED:
                issues.append(f"Windows-reserved filename in path: {rel}")
        if len(rel) > 240:
            issues.append(f"path is over 240 characters: {rel}")
    for values in folded.values():
        if len(values) > 1:
            issues.append("case/Unicode collision: " + " | ".join(values))
    return issues


def check_text(root: Path, paths: list[str], fix_line_endings: bool) -> list[str]:
    issues: list[str] = []
    for rel in paths:
        if not is_text_path(rel):
            continue
        full = root / rel
        try:
            data, decoded_or_error = read_text_bytes(root, rel)
        except OSError as exc:
            issues.append(f"cannot read {rel}: {exc}")
            continue
        if data is None:
            continue
        if decoded_or_error is not None and decoded_or_error.startswith("invalid UTF-8"):
            issues.append(f"{rel}: {decoded_or_error}")
            continue
        if b"\r\n" in data or b"\r" in data.replace(b"\r\n", b""):
            if fix_line_endings:
                full.write_bytes(data.replace(b"\r\n", b"\n").replace(b"\r", b"\n"))
            else:
                issues.append(f"{rel}: CRLF/CR line endings; run with --fix-line-endings")
    return issues


def check_json(root: Path, paths: list[str]) -> list[str]:
    issues: list[str] = []
    for rel in paths:
        if Path(rel).suffix.casefold() not in {".json", ".asmdef", ".inputactions"}:
            continue
        try:
            json.loads((root / rel).read_text(encoding="utf-8"))
        except (OSError, UnicodeDecodeError, json.JSONDecodeError) as exc:
            issues.append(f"{rel}: invalid JSON ({exc})")
    return issues


def check_python(root: Path, paths: list[str]) -> list[str]:
    issues: list[str] = []
    for rel in paths:
        if Path(rel).suffix.casefold() != ".py":
            continue
        try:
            ast.parse((root / rel).read_text(encoding="utf-8"), filename=rel)
        except (OSError, UnicodeDecodeError, SyntaxError) as exc:
            issues.append(f"{rel}: Python syntax error ({exc})")
    return issues


def check_references(root: Path, paths: list[str]) -> list[str]:
    folded = {norm_path(path): path for path in paths}
    issues: list[str] = []
    for rel in paths:
        if not is_text_path(rel):
            continue
        data, decoded_or_error = read_text_bytes(root, rel)
        if data is None or decoded_or_error is None or decoded_or_error.startswith("invalid UTF-8"):
            continue
        for match in REFERENCE_RE.finditer(decoded_or_error):
            reference = match.group("path").rstrip(".,;:!?)]}")
            actual = folded.get(norm_path(reference))
            if actual is not None and actual != reference:
                issues.append(f"{rel}: path case mismatch '{reference}' (tracked as '{actual}')")
    return issues


def check_unity_guids(root: Path, paths: list[str]) -> list[str]:
    """Catch duplicate Unity asset GUIDs before they become cross-machine drift."""
    owners: dict[str, str] = {}
    issues: list[str] = []
    for rel in paths:
        if not rel.startswith("Assets/") or not rel.endswith(".meta"):
            continue
        try:
            text = (root / rel).read_text(encoding="utf-8")
        except (OSError, UnicodeDecodeError):
            continue
        match = GUID_RE.search(text)
        if not match:
            continue
        guid = match.group(1).lower()
        previous = owners.get(guid)
        if previous is not None and previous != rel:
            issues.append(f"duplicate Unity GUID {guid}: {previous} | {rel}")
        else:
            owners[guid] = rel
    return issues


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=None, help="repository root (default: discover from this file)")
    parser.add_argument("--fix-line-endings", action="store_true", help="rewrite tracked text files to UTF-8 LF")
    args = parser.parse_args()
    root = (args.root or Path(__file__).resolve().parents[1]).resolve()
    try:
        paths = tracked_paths(root)
    except (OSError, subprocess.CalledProcessError) as exc:
        print(f"cross-platform check cannot inspect Git repository: {exc}", file=sys.stderr)
        return 2

    issues = []
    issues.extend(check_paths(paths))
    issues.extend(check_text(root, paths, args.fix_line_endings))
    issues.extend(check_json(root, paths))
    issues.extend(check_python(root, paths))
    issues.extend(check_references(root, paths))
    issues.extend(check_unity_guids(root, paths))

    if issues:
        print("cross-platform check failed:")
        for issue in issues:
            print(f"- {issue}")
        return 1
    print(f"cross-platform check passed ({len(paths)} tracked paths)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
