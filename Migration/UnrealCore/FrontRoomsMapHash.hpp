#pragma once

// Engine-independent first slice of the FrontRooms deterministic contract.
// Keep this header free of Unreal types so it can be tested before the UE
// editor/toolchain is installed, then wrap it in UE data types later.

#include <cstdint>

namespace frontrooms::migration {

using uint32 = std::uint32_t;

struct MapHash final {
    static constexpr int SiteX = 11;
    static constexpr int SiteZ = 13;
    static constexpr int Height = 17;
    static constexpr int EdgeEast = 23;
    static constexpr int EdgeNorth = 29;
    static constexpr int GateEast = 31;
    static constexpr int GateNorth = 37;
    static constexpr int Tree = 41;
    static constexpr int Pillar = 43;
    static constexpr int ZoneTint = 47;
    static constexpr int Rooms = 53;
    static constexpr int Theme = 59;
    static constexpr int Columns = 61;
    static constexpr int Modules = 67;
    static constexpr int ModulePick = 71;
    static constexpr int ModuleSpot = 73;

    static constexpr uint32 Mix(uint32 h) noexcept {
        h ^= h >> 16;
        h *= UINT32_C(0x7feb352d);
        h ^= h >> 15;
        h *= UINT32_C(0x846ca68b);
        h ^= h >> 16;
        return h;
    }

    static constexpr uint32 Hash(std::int32_t seed, std::int32_t a,
                                 std::int32_t b, std::int32_t salt,
                                 std::int32_t revision = 0) noexcept {
        uint32 h = Mix(static_cast<uint32>(seed) ^ UINT32_C(0x9e3779b9));
        h = Mix(h ^ static_cast<uint32>(a) * UINT32_C(0x85ebca6b));
        h = Mix(h ^ static_cast<uint32>(b) * UINT32_C(0xc2b2ae35));
        h = Mix(h ^ static_cast<uint32>(salt) * UINT32_C(0x27d4eb2f));
        h = Mix(h ^ static_cast<uint32>(revision) * UINT32_C(0x165667b1));
        return h;
    }

    static constexpr float Unit(uint32 h) noexcept {
        return static_cast<float>(h >> 8) * (1.0f / 16777216.0f);
    }

    static constexpr uint32 Next(uint32& state) noexcept {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return state;
    }
};

}  // namespace frontrooms::migration
