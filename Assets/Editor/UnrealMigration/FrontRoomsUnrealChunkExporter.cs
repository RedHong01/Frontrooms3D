using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using FrontRooms.Map;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Exports the Unity generator's authoritative chunk data for the Unreal
/// migration. Geometry is intentionally not exported here: UE will rebuild
/// presentation from the same deterministic data and sidecar assets.
/// </summary>
public static class FrontRoomsUnrealChunkExporter
{
    const string ProfilePath = "Assets/Levels/FrontRoomsLevel0.asset";
    static readonly int[] GoldenSeeds = { 2554, 20388, 20261001 };

    [MenuItem("FrontRooms/Migration/Export Unreal golden chunks")]
    public static void ExportGoldenChunksMenu()
    {
        ExportGoldenChunks(GoldenSeeds, 1);
        Debug.Log("[FrontRoomsMigration] Exported Unreal golden chunks.");
    }

    /// <summary>
    /// Command-line entry point for a later CI job:
    /// -executeMethod FrontRoomsUnrealChunkExporter.ExportGoldenChunksCommandLine
    /// -frontRoomsExportRadius 1
    /// -frontRoomsExportSeeds 2554,20388,20261001
    /// </summary>
    public static void ExportGoldenChunksCommandLine()
    {
        var radius = 1;
        var seeds = GoldenSeeds;
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "-frontRoomsExportRadius" && i + 1 < args.Length)
                int.TryParse(args[++i], NumberStyles.Integer, CultureInfo.InvariantCulture, out radius);
            else if (args[i] == "-frontRoomsExportSeeds" && i + 1 < args.Length)
                seeds = ParseSeeds(args[++i]);
        }

        ExportGoldenChunks(seeds, Mathf.Clamp(radius, 0, 4));
        AssetDatabase.Refresh();
    }

    public static void ExportGoldenChunks(IReadOnlyList<int> seeds, int radius)
    {
        var profile = AssetDatabase.LoadAssetAtPath<FrontRoomsLevelProfile>(ProfilePath);
        if (profile == null) throw new InvalidOperationException("Missing " + ProfilePath);

        var outputRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Migration", "exports", "unity_chunks"));
        Directory.CreateDirectory(outputRoot);
        foreach (var seed in seeds)
        {
            var settings = profile.Generation(seed);
            var generator = new FrontRoomsMapGenerator(settings, profile.ModuleData());
            var export = new SeedExport { seed = seed, radius = radius, chunks = new List<ChunkExport>() };
            for (var y = -radius; y <= radius; y++)
            for (var x = -radius; x <= radius; x++)
                export.chunks.Add(ToExport(generator.Generate(new GridCoord(x, y), 0, 1)));

            var path = Path.Combine(outputRoot, "seed-" + seed.ToString(CultureInfo.InvariantCulture) + ".json");
            File.WriteAllText(path, JsonUtility.ToJson(export, true) + "\n");
        }
    }

    static int[] ParseSeeds(string value)
    {
        var parts = value.Split(',');
        var result = new List<int>();
        foreach (var part in parts)
            if (int.TryParse(part.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var seed)) result.Add(seed);
        return result.Count == 0 ? GoldenSeeds : result.ToArray();
    }

    static ChunkExport ToExport(MapChunk chunk)
    {
        var output = new ChunkExport
        {
            x = chunk.coord.x,
            y = chunk.coord.y,
            revision = chunk.revision,
            tier = chunk.tier,
            ownZone = ToExport(chunk.ownZone),
            zone = new CoordExport[chunk.zone.Length],
            height = new int[chunk.height.Length],
            east = ToInts(chunk.east),
            north = ToInts(chunk.north),
            west = ToInts(chunk.west),
            south = ToInts(chunk.south),
            pillar = chunk.pillar,
            pillarStyle = chunk.pillarStyle,
            hasKey = chunk.hasKey,
            keyCell = ToExport(chunk.keyCell),
            keySiteCell = ToExport(chunk.keySiteCell),
            keySpot = chunk.keySpot,
            keyX = chunk.keyX,
            keyZ = chunk.keyZ,
            keyY = chunk.keyY,
            keyYaw = chunk.keyYaw,
            keyHost = chunk.keyHost,
            rooms = new RectExport[chunk.rooms.Length],
            roomModulePresent = new bool[chunk.roomModules == null ? 0 : chunk.roomModules.Length],
        };

        for (var i = 0; i < chunk.zone.Length; i++)
        {
            output.zone[i] = ToExport(chunk.zone[i]);
            output.height[i] = (int)chunk.height[i];
        }
        for (var i = 0; i < chunk.rooms.Length; i++)
        {
            var room = chunk.rooms[i];
            output.rooms[i] = new RectExport { x = room.x, y = room.y, w = room.w, h = room.h };
            if (i < output.roomModulePresent.Length) output.roomModulePresent[i] = chunk.roomModules[i] != null;
        }
        return output;
    }

    static int[] ToInts(EdgeKind[] values)
    {
        var result = new int[values.Length];
        for (var i = 0; i < values.Length; i++) result[i] = (int)values[i];
        return result;
    }

    static CoordExport ToExport(GridCoord value) => new CoordExport { x = value.x, y = value.y };

    static ZoneExport ToExport(ZoneInfo value) => new ZoneExport
    {
        x = value.id.x,
        y = value.id.y,
        height = (int)value.height,
        theme = (int)value.theme,
        siteX = value.siteX,
        siteZ = value.siteZ,
    };

    [Serializable]
    sealed class SeedExport
    {
        public int seed;
        public int radius;
        public List<ChunkExport> chunks;
    }

    [Serializable]
    sealed class ChunkExport
    {
        public int x, y, revision, tier;
        public ZoneExport ownZone;
        public CoordExport[] zone;
        public int[] height, east, north, west, south;
        public bool[] pillar;
        public byte[] pillarStyle;
        public bool hasKey;
        public CoordExport keyCell, keySiteCell;
        public bool keySpot;
        public float keyX, keyZ, keyY, keyYaw;
        public string keyHost;
        public RectExport[] rooms;
        public bool[] roomModulePresent;
    }

    [Serializable]
    struct CoordExport { public int x, y; }

    [Serializable]
    struct ZoneExport
    {
        public int x, y, height, theme;
        public float siteX, siteZ;
    }

    [Serializable]
    struct RectExport { public int x, y, w, h; }
}
