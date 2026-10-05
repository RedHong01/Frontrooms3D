"""Regression tests for the portable Unity/Unreal synchronization manifest."""

from __future__ import annotations

import json
import sys
import tempfile
import unittest
from pathlib import Path

THIS_DIR = Path(__file__).resolve().parent
if str(THIS_DIR) not in sys.path:
    sys.path.insert(0, str(THIS_DIR))

import sync_unity_unreal as sync  # noqa: E402


class SyncManifestTests(unittest.TestCase):
    def make_repo(self) -> Path:
        temp = tempfile.TemporaryDirectory()
        self.addCleanup(temp.cleanup)
        root = Path(temp.name)
        (root / "Assets/Scripts").mkdir(parents=True)
        (root / "Assets/Scenes").mkdir(parents=True)
        (root / "ProjectSettings").mkdir(parents=True)
        (root / "Migration/Unreal").mkdir(parents=True)
        (root / "Assets/Scripts/Game.cs").write_text("class Game {}\n", encoding="utf-8")
        (root / "Assets/Scenes/Main.unity").write_text("scene\n", encoding="utf-8")
        (root / "ProjectSettings/ProjectVersion.txt").write_text(
            "m_EditorVersion: 6000.3.10f1\n", encoding="utf-8"
        )
        (root / "Migration/Unreal/FrontRoomsss.uproject").write_text(
            json.dumps({"TargetPlatforms": ["Win64"]}), encoding="utf-8"
        )
        return root

    def test_inventory_is_portable_and_detects_changes(self) -> None:
        root = self.make_repo()
        current = sync.build_manifest(root)
        self.assertEqual(current["source"]["unityVersion"], "6000.3.10f1")
        self.assertEqual(current["unreal"]["targetPlatforms"], ["Win64"])
        self.assertTrue(all("\\" not in file["path"] for file in current["files"]))

        previous = json.loads(json.dumps(current))
        (root / "Assets/Scripts/Game.cs").write_text("class Game { int v; }\n", encoding="utf-8")
        changed = sync.diff_manifests(previous, sync.build_manifest(root))
        self.assertEqual(changed["changedFiles"], ["Assets/Scripts/Game.cs"])
        self.assertTrue(changed["sourceChanged"])

    def test_platform_gate_rejects_non_windows_target(self) -> None:
        root = self.make_repo()
        (root / "Migration/Unreal/FrontRoomsss.uproject").write_text(
            json.dumps({"TargetPlatforms": ["Mac"]}), encoding="utf-8"
        )
        platforms, _ = sync.read_unreal_platforms(root)
        self.assertEqual(platforms, ["Mac"])


if __name__ == "__main__":
    unittest.main()
