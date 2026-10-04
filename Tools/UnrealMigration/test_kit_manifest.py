import json
import sys
import unittest
from pathlib import Path

THIS_DIR = Path(__file__).resolve().parent
if str(THIS_DIR) not in sys.path:
    sys.path.insert(0, str(THIS_DIR))

from export_kit_manifest import build_manifest  # noqa: E402


REPO = Path(__file__).resolve().parents[2]


class KitManifestTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.manifest = build_manifest(REPO)

    def test_all_sidecars_have_a_mesh(self):
        self.assertEqual(self.manifest["count"], 113)
        self.assertEqual(self.manifest["missingFbx"], [])

    def test_sidecar_contract_preserves_collision_and_placement_data(self):
        copier = next(model for model in self.manifest["models"] if model["name"] == "Kit_Copier")
        self.assertEqual(copier["triangles"], 2204)
        self.assertEqual(len(copier["colliders"]), 2)
        self.assertEqual(len(copier["anchors"]), 2)
        self.assertEqual(copier["mesh"], "Assets/Resources/Props/Models/Kit_Copier.fbx")

    def test_totals_are_explicit(self):
        totals = self.manifest["totals"]
        self.assertEqual(totals["triangles"], 225673)
        self.assertEqual(totals["trianglesLod1"], 55208)
        self.assertEqual(totals["anchors"], 490)
        self.assertEqual(totals["supports"], 43)
        self.assertEqual(totals["colliders"], 55)
        self.assertEqual(totals["piles"], 45)

    def test_json_is_portable(self):
        self.assertNotIn(str(REPO), json.dumps(self.manifest))


if __name__ == "__main__":
    unittest.main()
