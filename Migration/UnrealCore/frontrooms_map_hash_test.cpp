#include "FrontRoomsMapHash.hpp"
#include "FrontRoomsMapData.hpp"

#include <cmath>
#include <cstdint>
#include <iostream>

using frontrooms::migration::MapHash;

namespace {

int failures = 0;

void expect(bool condition, const char* label) {
    if (!condition) {
        std::cerr << "FAIL: " << label << '\n';
        ++failures;
    }
}

void expect_u32(std::uint32_t actual, std::uint32_t expected, const char* label) {
    expect(actual == expected, label);
}

}  // namespace

int main() {
    using frontrooms::migration::EdgeKind;
    using frontrooms::migration::LocalIndex;
    using frontrooms::migration::MapConstants;
    using frontrooms::migration::ZoneHeight;
    expect(MapConstants::ChunkCells == 8, "chunk cell count");
    expect(MapConstants::CellsPerChunk == 64, "chunk cell total");
    expect(MapConstants::CeilingMeters(ZoneHeight::Low) == 2.4f, "low ceiling");
    expect(MapConstants::CeilingMeters(ZoneHeight::Standard) == 2.9f, "standard ceiling");
    expect(MapConstants::CeilingMeters(ZoneHeight::Tall) == 5.4f, "tall ceiling");
    expect(LocalIndex(7, 7) == 63, "local cell indexing");
    expect(static_cast<std::uint8_t>(EdgeKind::Window) == 4, "edge enum contract");

    expect_u32(MapHash::Mix(0x00000000u), 0x00000000u, "Mix(0)");
    expect_u32(MapHash::Mix(0x00000001u), 0x688990c0u, "Mix(1)");
    expect_u32(MapHash::Mix(0x12345678u), 0xf5e71c96u, "Mix(0x12345678)");
    expect_u32(MapHash::Mix(0xffffffffu), 0x6768824au, "Mix(UINT32_MAX)");
    expect_u32(MapHash::Mix(0x80000000u), 0xcc4b4124u, "Mix(high bit)");

    expect_u32(MapHash::Hash(2554, 0, 0, MapHash::SiteX, 0), 0xcd2545a4u,
               "Hash(seed 2554)");
    expect_u32(MapHash::Hash(20261001, -3, 7, MapHash::EdgeEast, 0), 0x529c7d25u,
               "Hash(negative coordinate)");
    expect_u32(MapHash::Hash(-1, INT32_MIN, INT32_MAX, MapHash::ModuleSpot, 3), 0x9f18a844u,
               "Hash(signed limits)");
    expect_u32(MapHash::Hash(20388, 16, 10, MapHash::Rooms, 2), 0x4c534946u,
               "Hash(revision)");

    expect(MapHash::Unit(0u) == 0.0f, "Unit(0)");
    expect(MapHash::Unit(0xffu) == 0.0f, "Unit(low byte discarded)");
    expect(MapHash::Unit(0xffffffffu) < 1.0f, "Unit(max) below one");

    std::uint32_t state = 1u;
    const std::uint32_t expected[] = {270369u, 67634689u, 2647435461u, 307599695u, 2398689233u};
    for (std::uint32_t value : expected) {
        expect_u32(MapHash::Next(state), value, "Next(xorshift sequence)");
    }

    if (failures != 0) {
        std::cerr << failures << " assertion(s) failed\n";
        return 1;
    }
    std::cout << "FrontRoomsMapHash: all checks passed\n";
    return 0;
}
