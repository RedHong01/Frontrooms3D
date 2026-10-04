import json
import sys
import unittest
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
THIS_DIR = Path(__file__).resolve().parent
if str(THIS_DIR) not in sys.path:
    sys.path.insert(0, str(THIS_DIR))

from export_contract import build_contract  # noqa: E402


class ExportContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.contract = build_contract(REPO)

    def test_contract_has_expected_unity_baseline(self):
        self.assertEqual(self.contract["schemaVersion"], 1)
        self.assertEqual(self.contract["source"]["unityVersion"], "6000.3.10f1")
        self.assertEqual(self.contract["source"]["unityVersionWithRevision"], "6000.3.10f1 (e35f0c77bd8e)")
        self.assertEqual(self.contract["validationSeeds"], [2554, 20388, 20261001])
        self.assertEqual(self.contract["buildScenes"], ["Assets/Scenes/FrontRooms3D.unity"])

    def test_profile_and_module_data_are_exported(self):
        profile = self.contract["levelProfile"]
        self.assertEqual(profile["generation"]["seed"], 20261001)
        self.assertEqual(profile["run"]["buildRadius"], 2)
        self.assertEqual(len(self.contract["modules"]), 4)
        storage = next(m for m in self.contract["modules"] if m["name"] == "Low_Storage_2x3")
        self.assertEqual(storage["data"]["southValues"], [0, 2, 0, 0])
        self.assertEqual(storage["props"][0]["kit"], "Kit_Bookcase")
        self.assertEqual(storage["markers"][0]["kindName"], "KeySpot")

    def test_audio_contract_and_asset_counts(self):
        audio = self.contract["audio"]
        self.assertEqual(audio["eventCount"], 31)
        self.assertEqual(audio["busCount"], 5)
        self.assertEqual(audio["parameterCount"], 19)
        self.assertEqual(self.contract["assetInventory"][".fbx"], 123)
        self.assertEqual(self.contract["assetInventory"][".mat"], 91)

    def test_contract_is_portable_json(self):
        encoded = json.dumps(self.contract)
        self.assertNotIn(str(REPO), encoded)
        self.assertIn("Assets/Scripts/Audio/FrontRoomsSoundIds.cs", encoded)


if __name__ == "__main__":
    unittest.main()
