"""Reference implementation of FrontRooms' deterministic ``MapHash``.

The source of truth is ``Assets/Scripts/FrontRoomsMap/FrontRoomsMap.cs``.
This module deliberately keeps the arithmetic explicit so a future Unreal
implementation can compare its ``uint32`` output against the Unity generator.
All integer operations use unchecked 32-bit unsigned arithmetic.  ``unit``
returns the same ``[0, 1)`` fraction as Unity's ``float`` expression because
its denominator is the exact power of two ``2**24``.
"""

from __future__ import annotations

from typing import Final


U32_MASK: Final[int] = 0xFFFF_FFFF
UNIT_DENOMINATOR: Final[float] = 16_777_216.0  # 1f / 16777216f in C#


def u32(value: int) -> int:
    """Return *value* with C# ``uint`` wraparound semantics."""

    return value & U32_MASK


class MapHash:
    """Names and operations copied from the Unity ``MapHash`` class."""

    SiteX: Final[int] = 11
    SiteZ: Final[int] = 13
    Height: Final[int] = 17
    EdgeEast: Final[int] = 23
    EdgeNorth: Final[int] = 29
    GateEast: Final[int] = 31
    GateNorth: Final[int] = 37
    Tree: Final[int] = 41
    Pillar: Final[int] = 43
    ZoneTint: Final[int] = 47
    Rooms: Final[int] = 53
    Theme: Final[int] = 59
    Columns: Final[int] = 61
    Modules: Final[int] = 67
    ModulePick: Final[int] = 71
    ModuleSpot: Final[int] = 73

    @staticmethod
    def mix(value: int) -> int:
        """Port ``Mix(uint h)`` exactly, including every 32-bit wrap."""

        h = u32(value)
        h = u32(h ^ (h >> 16))
        h = u32(h * 0x7FEB_352D)
        h = u32(h ^ (h >> 15))
        h = u32(h * 0x846C_A68B)
        h = u32(h ^ (h >> 16))
        return h

    @staticmethod
    def hash(seed: int, a: int, b: int, salt: int, revision: int = 0) -> int:
        """Port ``Hash(int seed, int a, int b, int salt, int revision = 0)``.

        The casts before each multiply matter for negative coordinates and
        seeds.  The C# method is inside ``unchecked``; ``u32`` makes that
        overflow explicit in Python.
        """

        h = MapHash.mix(u32(seed) ^ 0x9E37_79B9)
        h = MapHash.mix(h ^ u32(u32(a) * 0x85EB_CA6B))
        h = MapHash.mix(h ^ u32(u32(b) * 0xC2B2_AE35))
        h = MapHash.mix(h ^ u32(u32(salt) * 0x27D4_EB2F))
        h = MapHash.mix(h ^ u32(u32(revision) * 0x1656_67B1))
        return h

    @staticmethod
    def unit(value: int) -> float:
        """Port ``Unit(uint h) => (h >> 8) * (1f / 16777216f)``."""

        return (u32(value) >> 8) / UNIT_DENOMINATOR

    @staticmethod
    def next(state: int) -> int:
        """Return the next xorshift state from C# ``Next(ref uint state)``.

        Unity mutates the ``ref`` argument.  Python integers are immutable, so
        callers assign the return value back to their local state.
        """

        state = u32(state)
        state = u32(state ^ (state << 13))
        state = u32(state ^ (state >> 17))
        state = u32(state ^ (state << 5))
        return state


# Short aliases make the reference convenient in commandlets while retaining
# the class-shaped API used by the Unity source.
mix = MapHash.mix
hash_value = MapHash.hash
unit = MapHash.unit
next_state = MapHash.next

__all__ = [
    "MapHash",
    "U32_MASK",
    "u32",
    "mix",
    "hash_value",
    "unit",
    "next_state",
]
