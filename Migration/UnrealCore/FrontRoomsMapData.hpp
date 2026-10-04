#pragma once

#include <array>
#include <cstdint>

namespace frontrooms::migration {

struct GridCoord final {
    std::int32_t x = 0;
    std::int32_t y = 0;

    constexpr bool operator==(const GridCoord& other) const noexcept {
        return x == other.x && y == other.y;
    }
};

struct CellRect final {
    std::int32_t x = 0;
    std::int32_t y = 0;
    std::int32_t w = 0;
    std::int32_t h = 0;
};

enum class EdgeKind : std::uint8_t {
    Open = 0,
    Arch = 1,
    Wall = 2,
    Door = 3,
    Window = 4,
};

enum class ZoneHeight : std::uint8_t {
    Low = 0,
    Standard = 1,
    Tall = 2,
};

enum class ZoneTheme : std::uint8_t {
    Level0 = 0,
    Office = 1,
};

struct MapConstants final {
    static constexpr std::int32_t ChunkCells = 8;
    static constexpr std::int32_t CellsPerChunk = ChunkCells * ChunkCells;
    static constexpr float CellSizeMeters = 3.0f;
    static constexpr float ChunkSizeMeters = 24.0f;
    static constexpr float RebaseThresholdMeters = 192.0f;

    static constexpr float CeilingMeters(ZoneHeight height) noexcept {
        switch (height) {
        case ZoneHeight::Low: return 2.4f;
        case ZoneHeight::Tall: return 5.4f;
        default: return 2.9f;
        }
    }
};

struct ZoneInfo final {
    GridCoord id;
    ZoneHeight height = ZoneHeight::Standard;
    ZoneTheme theme = ZoneTheme::Level0;
    float siteX = 0.0f;
    float siteZ = 0.0f;
};

// This is the data-only shape exported by the Unity golden-chunk exporter.
// Unreal's USTRUCT wrapper may add TObjectPtr/DataAsset references, but must
// preserve these arrays and their local indexing exactly.
struct ChunkSnapshot final {
    GridCoord coord;
    std::int32_t revision = 0;
    std::int32_t tier = 1;
    ZoneInfo ownZone;
    std::array<GridCoord, MapConstants::CellsPerChunk> zone{};
    std::array<ZoneHeight, MapConstants::CellsPerChunk> height{};
    std::array<EdgeKind, MapConstants::CellsPerChunk> east{};
    std::array<EdgeKind, MapConstants::CellsPerChunk> north{};
    std::array<EdgeKind, MapConstants::ChunkCells> west{};
    std::array<EdgeKind, MapConstants::ChunkCells> south{};
    std::array<bool, (MapConstants::ChunkCells + 1) * (MapConstants::ChunkCells + 1)> pillar{};
    std::array<std::uint8_t, (MapConstants::ChunkCells + 1) * (MapConstants::ChunkCells + 1)> pillarStyle{};
    bool hasKey = false;
    GridCoord keyCell;
    GridCoord keySiteCell;
    bool keySpot = false;
    float keyX = 0.0f;
    float keyZ = 0.0f;
    float keyY = 0.0f;
    float keyYaw = 0.0f;
};

constexpr std::int32_t LocalIndex(std::int32_t i, std::int32_t j) noexcept {
    return i + j * MapConstants::ChunkCells;
}

}  // namespace frontrooms::migration
