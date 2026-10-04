"""Regression tests for :mod:`map_hash_reference`.

Run directly with ``python -m unittest Tools/UnrealMigration/test_map_hash_reference.py``
or with pytest if the project adds it later.
"""

from __future__ import annotations

import pathlib
import sys
import unittest


# Support both ``python Tools/.../test_map_hash_reference.py`` and discovery
# from the repository root without requiring a package marker in Tools/.
_THIS_DIR = pathlib.Path(__file__).resolve().parent
if str(_THIS_DIR) not in sys.path:
    sys.path.insert(0, str(_THIS_DIR))

from map_hash_reference import MapHash, U32_MASK, u32  # noqa: E402


class MapHashReferenceTests(unittest.TestCase):
    def test_constants_match_unity_source(self) -> None:
        self.assertEqual(
            {
                "SiteX": 11,
                "SiteZ": 13,
                "Height": 17,
                "EdgeEast": 23,
                "EdgeNorth": 29,
                "GateEast": 31,
                "GateNorth": 37,
                "Tree": 41,
                "Pillar": 43,
                "ZoneTint": 47,
                "Rooms": 53,
                "Theme": 59,
                "Columns": 61,
                "Modules": 67,
                "ModulePick": 71,
                "ModuleSpot": 73,
            },
            {name: getattr(MapHash, name) for name in (
                "SiteX", "SiteZ", "Height", "EdgeEast", "EdgeNorth", "GateEast",
                "GateNorth", "Tree", "Pillar", "ZoneTint", "Rooms", "Theme",
                "Columns", "Modules", "ModulePick", "ModuleSpot",
            )},
        )

    def test_mix_golden_vectors(self) -> None:
        # Includes high-bit inputs to catch accidental signed shifts and
        # omitted overflow masks.
        vectors = {
            0x00000000: 0x00000000,
            0x00000001: 0x688990C0,
            0x12345678: 0xF5E71C96,
            0xFFFFFFFF: 0x6768824A,
            0x80000000: 0xCC4B4124,
        }
        for value, expected in vectors.items():
            with self.subTest(value=hex(value)):
                self.assertEqual(MapHash.mix(value), expected)

    def test_hash_golden_vectors(self) -> None:
        # Filled with values produced independently from the C# source; these
        # lock signed casts, salt ordering, revision, and uint32 overflow.
        vectors = [
            (2554, 0, 0, MapHash.SiteX, 0, 0xCD2545A4),
            (20261001, -3, 7, MapHash.EdgeEast, 0, 0x529C7D25),
            (-1, -2147483648, 2147483647, MapHash.ModuleSpot, 3, 0x9F18A844),
            (20388, 16, 10, MapHash.Rooms, 2, 0x4C534946),
        ]
        for seed, a, b, salt, revision, expected in vectors:
            with self.subTest(seed=seed, a=a, b=b, salt=salt, revision=revision):
                self.assertEqual(MapHash.hash(seed, a, b, salt, revision), expected)

    def test_unit_bounds_and_bit_selection(self) -> None:
        self.assertEqual(MapHash.unit(0), 0.0)
        self.assertEqual(MapHash.unit(0x000000FF), 0.0)
        self.assertEqual(MapHash.unit(0x00000100), 1.0 / 16_777_216.0)
        self.assertEqual(MapHash.unit(0xFFFFFFFF), (0x00FF_FFFF) / 16_777_216.0)
        self.assertGreaterEqual(MapHash.unit(0xFFFFFFFF), 0.0)
        self.assertLess(MapHash.unit(0xFFFFFFFF), 1.0)

    def test_next_xorshift_golden_sequence(self) -> None:
        state = 1
        expected = [
            0x00042021,
            0x04080601,
            0x9DCCA8C5,
            0x1255994F,
            0x8EF917D1,
        ]
        actual = []
        for _ in expected:
            state = MapHash.next(state)
            actual.append(state)
        self.assertEqual(actual, expected)

        # Explicitly verify high-bit wrap and that zero remains zero.
        self.assertEqual(MapHash.next(0), 0)
        self.assertEqual(MapHash.next(0xFFFFFFFF), 0x0003E01F)

    def test_unsigned_wrap_helper(self) -> None:
        self.assertEqual(u32(-1), U32_MASK)
        self.assertEqual(u32(0x1_0000_0001), 1)
        self.assertEqual(MapHash.hash(-1, -1, -1, -1), MapHash.hash(U32_MASK, U32_MASK, U32_MASK, U32_MASK))


if __name__ == "__main__":
    unittest.main()
