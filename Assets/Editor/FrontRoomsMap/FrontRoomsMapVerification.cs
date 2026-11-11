using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FrontRooms.Map;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Data-level checks of the map generator for 100 seeds, using the level
/// profile's generation numbers (FrontRoomsLevelProfiles.Resolve): neighbouring chunks
/// agree on every shared edge, every cell is reachable, doors and windows sit
/// only where the ceiling height changes, every key zone has its key, rebuilds
/// are identical, and a shifted chunk keeps its borders. It does not build
/// geometry or load a scene.
/// It also runs the EXIT planner (C22, FrontRoomsExitPlanner) over 3 floors per seed
/// (FrontRoomsMapValidator.RunExits): floor 1 from the title's start area where the
/// game makes it, floors 2 and 3 from the previous EXIT cell with SeedFor's seed. Every EXIT must also pass
/// C23 step 1's front rules (N Standard, no glass on the EXIT door's sightline); a line reports how often they
/// turned a cell down, whether they made a floor widen its band, and which EXIT sign each floor gets.
/// C23 round 2: a widened band picks among the candidates nearest the band asked; a line lists every widened
/// floor (its X, walk, band, how far outside the band asked, and how many candidates were that near).
/// A26 step 2: the floor-1 start group (the station behind the start door, the EXIT 4 cells north of E1) for the same
/// seeds at all 16 door rows of a world period, with the profile's station module or the stand-in, floors 1-3 each
/// (StartGroup; its own report, Verification/start-group-verification.json).
/// C13 (Relay v2 step V7a), only with the profile's foil on: foiled windows, the one trigger budget, the auto Relay entries
/// and the cover measure over the same seeds (FrontRoomsMapValidator.RunFoil; Verification/foil-verification.json). With foil
/// off nothing of it runs and no file is written. RunFoilBatch runs it with foil forced on, whatever the profile says.
/// C16 (Relay v2 step V7b), only with the profile's alarmDoor on: alarm push-bar doors in the one trigger budget over the same
/// seeds (FrontRoomsMapValidator.RunAlarmDoors; Verification/alarm-door-verification.json). Off, nothing of it runs and no
/// file is written. RunAlarmDoorBatch runs it forced on (foil forced and Run! rooms on too), whatever the profile says.
/// Run! rooms (L7), only with the profile's runRooms on: the structure check per zone at today's plain 9
/// (Verification/run-rooms-verification.json), then the Run! rethink's cards (71 s.9, RunCards): every length and twist per
/// zone with its fit rate (run-lengths-verification.json) and the deck with the calm rule over a run of floors
/// (run-deck-verification.json). RunRunCardsBatch runs them with Run! rooms forced on and the twists' switches on.
/// Run with -executeMethod FrontRoomsMapVerification.RunBatch; failures throw after the JSON report is saved.
/// </summary>
public static class FrontRoomsMapVerification
{
    const int FirstSeed = 20261001;
    const int SeedCount = 100;
    const int RadiusChunks = 4;
    // C22: floors per seed in the EXIT planner check.
    const int ExitFloors = 3;
    // Floor 1's start area as the game makes it when the autopilot presses Space at 1.8 s: the title camera
    // at z 0 with stream room 0 centred on it (FrontRoomsRoomStream.RoomLength 12 m), the terminal door at the
    // end of room 1 (z 18 m) and the oldest room 0 behind it (rear at z -6 m): a strip of 8 rows behind the
    // door, StartAreaHalfCells (2, private in FrontRooms3DGame) cells either side of the door column at
    // TitleCenterX. The door cell is the cell past the door line with the map's root at the origin; RootFor
    // (the game's MapRootFor) then moves the root by whole world periods, as the game does.
    const int TitleHalfCells = 2;
    static GridCoord TitleDoorWorldCell => new GridCoord(
        Mathf.FloorToInt(FrontRooms3DGame.TitleCenterX / MapGrid.CellSize),
        Mathf.FloorToInt((FrontRoomsRoomStream.RoomLength * 1.5f + MapGrid.CellSize * .5f) / MapGrid.CellSize));
    static int TitleRows => Mathf.RoundToInt(FrontRoomsRoomStream.RoomLength * 2f / MapGrid.CellSize);

    [MenuItem("FrontRooms/Map/Verify 100 seeds")]
    public static void Verify() => Run(false);

    public static void RunBatch() => Run(true);

    public static string Run(bool throwOnFailure)
    {
        var profile = FrontRoomsLevelProfiles.Resolve();
        var modules = profile.ModuleData();
        var report = FrontRoomsMapValidator.Run(profile.generation, FirstSeed, SeedCount, RadiusChunks, modules);
        var exitWatch = System.Diagnostics.Stopwatch.StartNew();
        var exits = FrontRoomsMapValidator.RunExits(profile.generation, FirstSeed, SeedCount, ExitFloors, TitleDoorWorldCell, TitleHalfCells, TitleRows, modules);
        var exitSeconds = exitWatch.Elapsed.TotalSeconds;
        report.exits = exits;
        var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "map-verification-latest.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        var placed = report.seeds.Sum(x => x.modulesPlaced);
        // A library the generator never uses over 100 seeds is a broken setup, not a pass.
        var unused = modules.Count > 0 && profile.generation != null && profile.generation.moduleChance > 0f && placed == 0;
        var pass = report.failed == 0 && !unused && exits.failed == 0;
        var summary = "[FrontRoomsMap] " + (pass ? "PASS" : "FAIL") + " · " + report.passed + "/" + report.seedCount
            + " seeds · " + (RadiusChunks * 2) + "×" + (RadiusChunks * 2) + " chunks each · " + modules.Count + " room modules, " + placed + " placed"
            + ", EXIT planner " + exits.passed + "/" + exits.seedCount + " seeds x " + exits.floors + " floors (" + ExitSummary(exits) + ")"
            + " · profile " + (AssetDatabase.GetAssetPath(profile) is string p && p.Length > 0 ? p : "code defaults") + " · " + path;
        if (pass) Debug.Log(summary);
        else
        {
            foreach (var seed in report.seeds)
                if (!seed.passed) { Debug.LogError("[FrontRoomsMap] seed " + seed.seed + ": " + string.Join(" | ", seed.errors)); break; }
            foreach (var seed in exits.seeds)
                if (!seed.passed) { Debug.LogError("[FrontRoomsMap] EXIT planner, seed " + seed.seed + ": " + string.Join(" | ", seed.errors)); break; }
            if (unused) Debug.LogError("[FrontRoomsMap] the profile lists " + modules.Count + " room modules with module chance " + profile.generation.moduleChance + " but no seed placed one: check their height, theme, tier range, weight and size.");
            Debug.LogError(summary);
        }
        Debug.Log("[FrontRoomsMap] EXIT planner per floor (" + exitSeconds.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " s): " + ExitDetail(exits));
        Debug.Log("[FrontRoomsMap] " + FrontRulesSummary(exits));
        Debug.Log("[FrontRoomsMap] " + WidenedFloors(exits));
        Debug.Log("[FrontRoomsMap] " + RootPortCheck(profile, modules));
        var groupPass = StartGroup(profile, modules);
        // Run! rooms (L7): only with the profile's runRooms on; off, nothing here runs and no file is written.
        var runPass = RunRooms(profile, modules);
        // C13 (V7a): only with the profile's foil on; off, nothing here runs and no file is written.
        var foilPass = Foil(profile, modules, profile.FoilRules(), profile.RunRules());
        // C16 (V7b): only with the profile's alarmDoor on; off, nothing here runs and no file is written.
        var alarmPass = AlarmDoors(profile, modules, profile.AlarmDoorRules(), profile.FoilRules(), profile.RunRules());
        if (throwOnFailure && !pass) throw new Exception(summary);
        if (throwOnFailure && !groupPass) throw new Exception("[FrontRoomsMap] start group FAIL");
        if (throwOnFailure && !runPass) throw new Exception("[FrontRoomsMap] Run! rooms FAIL");
        if (throwOnFailure && !foilPass) throw new Exception("[FrontRoomsMap] foil FAIL");
        if (throwOnFailure && !alarmPass) throw new Exception("[FrontRoomsMap] alarm doors FAIL");
        return summary;
    }

    /// <summary>
    /// The walkable stair (MAP_GENERATION.md "Walkable stair") for the combined round, whatever the profile says: the same
    /// seeds and floors as the EXIT planner check, with every EXIT 1 x 2 (ChooseStairExit) and each later floor started in the
    /// previous X1-X2 (SeedForStair), checked by the validator's own loops (CheckStairExit). Writes
    /// Verification/map-verification-walkable-stair.json; the profile is not changed.
    /// -executeMethod FrontRoomsMapVerification.RunWalkableStairBatch; a failure throws after the report is saved.
    /// </summary>
    [MenuItem("FrontRooms/Map/Verify walkable stair EXITs (forced on)")]
    public static void RunWalkableStairBatch()
    {
        var profile = FrontRoomsLevelProfiles.Resolve();
        var modules = profile.ModuleData();
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var exits = FrontRoomsMapValidator.RunStairExits(profile.generation, FirstSeed, SeedCount, ExitFloors, TitleDoorWorldCell, TitleHalfCells, TitleRows, modules);
        var seconds = watch.Elapsed.TotalSeconds.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "map-verification-walkable-stair.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(exits, true));
        var pass = exits.failed == 0;
        var summary = "[FrontRoomsMap] walkable stair " + (pass ? "PASS" : "FAIL") + " - EXIT planner " + exits.passed + "/" + exits.seedCount + " seeds x " + exits.floors
            + " floors (" + ExitSummary(exits) + ") in " + seconds + " s - " + path;
        if (pass) Debug.Log(summary);
        else
        {
            foreach (var seed in exits.seeds)
                if (!seed.passed) { Debug.LogError("[FrontRoomsMap] walkable stair, seed " + seed.seed + ": " + string.Join(" | ", seed.errors)); break; }
            Debug.LogError(summary);
        }
        Debug.Log("[FrontRoomsMap] walkable stair per floor: " + ExitDetail(exits));
        Debug.Log("[FrontRoomsMap] walkable stair " + FrontRulesSummary(exits));
        Debug.Log("[FrontRoomsMap] walkable stair " + WidenedFloors(exits));
        if (!pass) throw new Exception(summary);
    }

    /// <summary>
    /// C16 (V7b) for the combined round, whatever the profile says: alarm doors on with every Low and Standard zone's slot
    /// forced (so every rule meets an alarm door wherever one fits), foil on and forced and Run! rooms on with the profile's
    /// budget (one ledger, and the "unchanged" rows for both), the profile's numbers otherwise.
    /// -executeMethod FrontRoomsMapVerification.RunAlarmDoorBatch; a failure throws after the report is saved.
    /// </summary>
    [MenuItem("FrontRooms/Map/Verify alarm doors (V7b, forced on)")]
    public static void RunAlarmDoorBatch()
    {
        var profile = FrontRoomsLevelProfiles.Resolve();
        var rules = profile.AlarmDoorRules(true);
        rules.force = true;
        var foil = profile.FoilRules(true);
        foil.force = true;
        if (!AlarmDoors(profile, profile.ModuleData(), rules, foil, profile.RunRules(true))) throw new Exception("[FrontRoomsMap] alarm doors FAIL");
    }

    /// <summary>
    /// C16 (V7b): the alarm-door check over the 100 seeds with <paramref name="rules"/> (with foil and Run! rooms when given:
    /// one budget), report in Verification/alarm-door-verification.json. True with no rules (alarmDoor off: nothing runs).
    /// </summary>
    static bool AlarmDoors(FrontRoomsLevelProfile profile, System.Collections.Generic.List<RoomModuleData> modules, FrontRoomsAlarmDoorRules rules, FrontRoomsFoilRules foilRules, FrontRoomsRunRules runRules)
    {
        if (rules == null) return true;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var report = FrontRoomsMapValidator.RunAlarmDoors(profile.generation, FirstSeed, SeedCount, RadiusChunks, modules, rules, foilRules, runRules, FoilTier);
        var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "alarm-door-verification.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var pass = report.failed == 0 && report.seeds.Count == SeedCount;
        var line = "[FrontRoomsMap] alarm doors (V7b) " + (pass ? "PASS" : "FAIL") + " - " + report.passed + "/" + report.seedCount + " seeds, tier " + report.tier + (report.force ? ", slots forced" : "")
            + (report.foil ? ", foil on" : "") + (report.runRooms ? ", Run! rooms on" : "") + " (one budget) - " + report.zones + " Low and Standard zones, " + report.doors + " doors they own ("
            + report.candidates + " chunk-border candidates, " + report.interior + " inside chunks), " + report.plain + " kept plain, " + report.singleBorders + " single-door borders, " + report.alarmed + " alarm doors; "
            + report.slots + " zones whose roll passes; none: " + report.noSlot + " no slot, " + report.noDoor + " no door to alarm, " + report.arrival + " first zone, " + report.taken + " slot taken; "
            + report.excluded + " doors excluded - ledger " + report.ledgerAlarm + " AlarmDoor, " + report.ledgerFoil + " Foil, " + report.ledgerRun + " Run (" + report.runRoomsPlaced + " Run! rooms) - "
            + watch.Elapsed.TotalSeconds.ToString("0.0", inv) + " s - " + path;
        if (pass) Debug.Log(line);
        else
        {
            foreach (var seed in report.seeds)
                if (!seed.passed) { Debug.LogError("[FrontRoomsMap] alarm doors, seed " + seed.seed + ": " + string.Join(" | ", seed.errors)); break; }
            Debug.LogError(line);
        }
        return pass;
    }

    // C13 (V7a): the tier every zone but the first is met at in the foil check (Run! rooms come from tier 2).
    const int FoilTier = 2;

    /// <summary>
    /// C13 (V7a) for the combined round, whatever the profile says: foil on with every Tall zone's slot forced (so every rule
    /// meets a foiled window wherever one fits), Run! rooms on with the profile's budget (one ledger), the profile's numbers
    /// otherwise. -executeMethod FrontRoomsMapVerification.RunFoilBatch; a failure throws after the report is saved.
    /// </summary>
    [MenuItem("FrontRooms/Map/Verify foil (V7a, forced on)")]
    public static void RunFoilBatch()
    {
        var profile = FrontRoomsLevelProfiles.Resolve();
        var rules = profile.FoilRules(true);
        rules.force = true;
        if (!Foil(profile, profile.ModuleData(), rules, profile.RunRules(true))) throw new Exception("[FrontRoomsMap] foil FAIL");
    }

    /// <summary>
    /// C13 (V7a): the foil check over the 100 seeds with <paramref name="rules"/> (and Run! rooms with <paramref name="runRules"/>),
    /// report in Verification/foil-verification.json. True with no rules (foil off: nothing runs).
    /// </summary>
    static bool Foil(FrontRoomsLevelProfile profile, System.Collections.Generic.List<RoomModuleData> modules, FrontRoomsFoilRules rules, FrontRoomsRunRules runRules)
    {
        if (rules == null) return true;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var report = FrontRoomsMapValidator.RunFoil(profile.generation, FirstSeed, SeedCount, RadiusChunks, modules, rules, runRules, FoilTier);
        var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "foil-verification.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var pass = report.failed == 0 && report.seeds.Count == SeedCount;
        var line = "[FrontRoomsMap] foil (V7a) " + (pass ? "PASS" : "FAIL") + " - " + report.passed + "/" + report.seedCount + " seeds, tier " + report.tier + (report.force ? ", slots forced" : "")
            + (report.runRooms ? ", Run! rooms on (one budget)" : "") + " - " + report.tallZones + " Tall zones, " + report.windows + " windows (" + report.candidates + " on chunk borders, " + report.interior
            + " inside chunks), " + report.plain + " plain, " + report.foiled + " foiled; " + report.slots + " zones whose roll passes; no foil: " + report.noSlot + " no slot, " + report.noWindow + " no foilable window, "
            + report.arrival + " first zone, " + report.taken + " slot taken; " + report.excluded + " windows excluded - ledger " + report.ledgerFoil + " Foil, " + report.ledgerRun + " Run (" + report.runRoomsPlaced
            + " Run! rooms) - Relay entries: " + report.entries + " in " + report.entryZones + " zones (" + report.deadEnds + " dead ends, " + report.corridorEnds + " corridor ends, " + report.otherCells + " other cells), "
            + report.smallZones + " zones too small for two 10 cells apart - cover (walk out of a 12 m sight line): Tall worst " + report.coverTallMax.ToString("0", inv) + " m, mean " + report.coverTallMean.ToString("0.0", inv)
            + " m, " + report.coverTallOver + " Tall zones over " + FrontRoomsMapValidator.CoverTargetMetres.ToString("0", inv) + " m; other zones worst " + report.coverOtherMax.ToString("0", inv) + " m, mean "
            + report.coverOtherMean.ToString("0.0", inv) + " m - " + watch.Elapsed.TotalSeconds.ToString("0.0", inv) + " s - " + path;
        if (pass) Debug.Log(line);
        else
        {
            foreach (var seed in report.seeds)
                if (!seed.passed) { Debug.LogError("[FrontRoomsMap] foil, seed " + seed.seed + ": " + string.Join(" | ", seed.errors)); break; }
            Debug.LogError(line);
        }
        return pass;
    }

    /// <summary>
    /// Run! rooms (L7, CR-1 to CR-6), only with the profile's runRooms on: the same 100 seeds with Run! rooms in
    /// (FrontRoomsMapValidator.RunRunRooms: every map rule again, then the Run! rows), report in
    /// Verification/run-rooms-verification.json. True with the switch off (nothing runs).
    /// </summary>
    static bool RunRooms(FrontRoomsLevelProfile profile, System.Collections.Generic.List<RoomModuleData> modules)
    {
        var rules = profile.RunRules();
        if (rules == null) return true;
        return RunRoomsWith(profile, modules, rules);
    }

    static bool RunRoomsWith(FrontRoomsLevelProfile profile, System.Collections.Generic.List<RoomModuleData> modules, FrontRoomsRunRules rules)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var report = FrontRoomsMapValidator.RunRunRooms(profile.generation, FirstSeed, SeedCount, RadiusChunks, modules, rules);
        var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "run-rooms-verification.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        var pass = report.failed == 0;
        var line = "[FrontRoomsMap] Run! rooms " + (pass ? "PASS" : "FAIL") + " - " + report.passed + "/" + report.seedCount + " seeds - " + report.placed + " rooms placed ("
            + report.ownZoneDoors + " goal doors into their own zone, " + report.borderDoors + " on a Standard-Low border, " + report.multiZone + " across zones; call 2 " + (rules.goalDoorForced ? "on" : "off") + ", call 5 " + (rules.office ? "on" : "off")
            + ") - zones without one: " + report.notReady + " met before ready, " + report.ineligible + " not eligible, " + report.noFit + " no straight fits, " + report.suppressed
            + " lost a chunk - " + report.entriesHidden + " module Relay entries within " + rules.minEntryCells + " cells - "
            + watch.Elapsed.TotalSeconds.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " s - " + path;
        if (pass) Debug.Log(line);
        else
        {
            foreach (var seed in report.seeds)
                if (!seed.passed) { Debug.LogError("[FrontRoomsMap] Run! rooms, seed " + seed.seed + ": " + string.Join(" | ", seed.errors)); break; }
            Debug.LogError(line);
        }
        // The Run! rethink (71 s.9): every length and twist, and the deck with the calm rule.
        var cardsPass = RunCards(profile, modules, rules);
        return pass && cardsPass;
    }

    /// <summary>The cards the lengths check builds, per zone (71 s.9 cards 1, 4, 5): plain 7, 9, 12, 14, a Run door 9, receding 9 -> 12 and 9 -> 14.</summary>
    public static readonly RunCard[] LengthCards =
    {
        RunCard.Plain(7), RunCard.Plain(9), RunCard.Plain(12), RunCard.Plain(14), RunCard.Door(9), RunCard.Recede(9, 12), RunCard.Recede(9, 14),
    };

    /// <summary>The lengths check's report: one Run! report per card (Verification/run-lengths-verification.json).</summary>
    [Serializable]
    public sealed class RunLengthsReport
    {
        public string suite = "FrontRooms Run! lengths (71 s.9 card 1: the fit rates on 100 seeds; cards 4, 5: the twists' structure)";
        public string timestampUtc;
        public List<string> fit = new List<string>();
        public List<MapRunReport> cards = new List<MapRunReport>();
    }

    // The deck check's square (chunks each way): 6 x 6 chunks a floor, six floors and their replay per seed.
    const int DeckRadiusChunks = 3;

    /// <summary>
    /// The Run! rethink (71 s.9, cards 1-5) on the 100 seeds: (1) every card of LengthCards per zone, each room checked and
    /// the fit rate counted (how many competing zones hold a straight of that length: 12 and 14 first, card 1), and (2) the
    /// deck with the calm rule over six floors per seed (FrontRoomsMapValidator.RunRunDeck). Reports in Verification/.
    /// </summary>
    static bool RunCards(FrontRoomsLevelProfile profile, List<RoomModuleData> modules, FrontRoomsRunRules rules)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification");
        Directory.CreateDirectory(dir);
        var pass = true;
        var lengths = new RunLengthsReport { timestampUtc = DateTime.UtcNow.ToString("o") };
        foreach (var card in LengthCards)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var r = rules.Clone();
            r.forceCard = card;
            var report = FrontRoomsMapValidator.RunRunRooms(profile.generation, FirstSeed, SeedCount, RadiusChunks, modules, r);
            lengths.cards.Add(report);
            var ok = report.failed == 0;
            pass &= ok;
            var line = "[FrontRoomsMap] Run! " + card + " " + (ok ? "PASS" : "FAIL") + " - " + report.passed + "/" + report.seedCount + " seeds - " + report.placed + " rooms (7: " + report.cells7 + ", 9: "
                + report.cells9 + ", 12: " + report.cells12 + ", 14: " + report.cells14 + "; Run door " + report.doorEntrances + ", receding " + report.receding + ") - fit " + report.fit + " of " + report.competing
                + " competing zones (" + (report.fitRate * 100f).ToString("0.0", inv) + " %) - " + report.noFit + " zones met with no straight, " + report.suppressed + " lost a chunk - "
                + watch.Elapsed.TotalSeconds.ToString("0.0", inv) + " s";
            lengths.fit.Add(card + ": " + report.fit + " / " + report.competing + " = " + (report.fitRate * 100f).ToString("0.0", inv) + " %, " + report.placed + " rooms placed per zone");
            if (ok) Debug.Log(line);
            else
            {
                foreach (var seed in report.seeds)
                    if (!seed.passed) { Debug.LogError("[FrontRoomsMap] Run! " + card + ", seed " + seed.seed + ": " + string.Join(" | ", seed.errors)); break; }
                Debug.LogError(line);
            }
        }
        var lengthsPath = Path.Combine(dir, "run-lengths-verification.json");
        File.WriteAllText(lengthsPath, JsonUtility.ToJson(lengths, true));
        Debug.Log("[FrontRoomsMap] Run! fit rates (card 1): " + string.Join(" | ", lengths.fit.ToArray()) + " - " + lengthsPath);

        var deckWatch = System.Diagnostics.Stopwatch.StartNew();
        var deck = FrontRoomsMapValidator.RunRunDeck(profile.generation, FirstSeed, SeedCount, DeckRadiusChunks, modules, rules);
        var deckPath = Path.Combine(dir, "run-deck-verification.json");
        File.WriteAllText(deckPath, JsonUtility.ToJson(deck, true));
        var deckOk = deck.failed == 0 && deck.seeds.Count == SeedCount;
        pass &= deckOk;
        var deckLine = "[FrontRoomsMap] Run! deck and calm (cards 2, 3) " + (deckOk ? "PASS" : "FAIL") + " - " + deck.passed + "/" + deck.seedCount + " runs of " + deck.floors + " floors (" + deck.tiers + ")"
            + ", Run door " + (deck.doorEntrance ? "on" : "off") + ", receding " + (deck.recede ? "on" : "off") + ", ramp " + (deck.ramps ? "on" : "off") + " - " + deck.rooms + " rooms, " + deck.nothing
            + " floors dealt nothing, " + deck.twists + " twists; " + deck.calmExcluded + " device candidates kept out of calm zones - built: " + string.Join(", ", deck.builtCounts.ToArray())
            + " - devices first (the west half's devices decided before the floor's zones are met): " + deck.firstRooms + " rooms, " + deck.calmRefused + " zones refused (a goal door into a zone with a decided device), "
            + deck.hostDevicesPlain + " devices decided before a room on its host zone's border (built plain)"
            + " - " + deckWatch.Elapsed.TotalSeconds.ToString("0.0", inv) + " s - " + deckPath;
        if (deckOk) Debug.Log(deckLine);
        else
        {
            foreach (var seed in deck.seeds)
                if (!seed.passed) { Debug.LogError("[FrontRoomsMap] Run! deck, seed " + seed.seed + ": " + string.Join(" | ", seed.errors)); break; }
            Debug.LogError(deckLine);
        }
        return pass;
    }

    /// <summary>
    /// The Run! rethink's checks for the combined round, whatever the profile says (71 s.9 cards 1-5): Run! rooms on (the
    /// profile's budget), the Run door entrance and the receding end on so the deck deals them (the ramp stays off: no per-cell
    /// levels), then the structure check, every length and twist with its fit rate, and the deck with the calm rule.
    /// -executeMethod FrontRoomsMapVerification.RunRunCardsBatch; a failure throws after the reports are saved.
    /// </summary>
    [MenuItem("FrontRooms/Map/Verify Run! cards (71 s.9, forced on)")]
    public static void RunRunCardsBatch()
    {
        var profile = FrontRoomsLevelProfiles.Resolve();
        var rules = profile.RunRules(true);
        rules.doorEntrance = true;
        rules.recede = true;
        if (!RunRoomsWith(profile, profile.ModuleData(), rules)) throw new Exception("[FrontRoomsMap] Run! cards FAIL");
    }

    // A26 step 2: the floor-1 start group over every door row of a world period (16 rows, both chunk-local parities).
    const int StartGroupDoorRows = 16;

    /// <summary>
    /// A26 step 2 (notes/floor1_start_group_plan.md 6): the floor-1 start group for the same 100 seeds at all 16 door rows
    /// of a world period, both parities (FrontRoomsMapValidator.RunStartGroups), floors 1-3 each: the plan, its carve
    /// re-derived, its EXIT, a fresh-cache plan and a revisit shift, then floors 2 and 3 from its X. It uses the profile's
    /// station module or, while the profile has none (the landing), the stand-in (StandInStation, in memory). Its report
    /// goes to Verification/start-group-verification.json, so map-verification-latest.json stays as it was with no station.
    /// </summary>
    static bool StartGroup(FrontRoomsLevelProfile profile, System.Collections.Generic.IReadOnlyList<RoomModuleData> modules)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var own = profile.StationData();
        var module = own ?? StandInStation(4);
        var template = StartGroupTemplate.From(module, FrontRoomsMapWorld.KitFootprint, out var why);
        if (template == null)
        {
            Debug.LogError("[FrontRoomsMap] start group FAIL | the " + (own != null ? "profile's station module" : "stand-in station") + " is refused: " + why);
            return false;
        }
        var report = FrontRoomsMapValidator.RunStartGroups(profile.generation, FirstSeed, SeedCount, ExitFloors, TitleDoorWorldCell.x, StartGroupDoorRows, template, modules);
        var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "start-group-verification.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var pass = report.failed == 0 && report.starts == SeedCount * StartGroupDoorRows;
        var floors = new System.Collections.Generic.List<string>();
        foreach (var st in report.stats)
            floors.Add("floor " + st.floor + ": " + st.exits + " EXITs, " + st.minDistance + "-" + st.maxDistance + " walking cells, " + st.widened + " widened, " + st.throughGlass + " through glass"
                + (st.floor > 1 ? ", SeedFor at most " + st.maxSeedTries + " tries, " + st.seedFallbacks + " fallbacks" : ""));
        var line = "[FrontRoomsMap] start group " + (pass ? "PASS" : "FAIL") + " | " + report.passed + "/" + report.starts + " starts (" + report.seedCount + " seeds x " + report.doorRows
            + " door rows: row 2 " + report.evenPassed + ", row 6 " + report.oddPassed + " passed) | " + (own != null ? "profile station module " + AssetDatabase.GetAssetPath(profile.stationModule) : "stand-in station")
            + " " + report.template + " | " + report.retried + " on a later zone root, " + report.warned + " accepted with a warning, ring max " + report.maxRing + ", zone roots max " + report.maxZoneRoots
            + " | plan ms mean/p95/max " + report.planMsMean.ToString("0.00", inv) + "/" + report.planMsP95.ToString("0.00", inv) + "/" + report.planMsMax.ToString("0.00", inv)
            + " | " + string.Join("; ", floors) + " | " + watch.Elapsed.TotalSeconds.ToString("0.0", inv) + " s | " + path;
        if (pass) Debug.Log(line);
        else
        {
            foreach (var row in report.rows)
                if (!row.passed) { Debug.LogError("[FrontRoomsMap] start group, seed " + row.seed + " door row " + row.doorRow + ": " + string.Join(" | ", row.errors)); break; }
            Debug.LogError(line);
        }
        return pass;
    }

    /// <summary>
    /// The stand-in station module (A26 step 2): a 4 x <paramref name="depth"/> room built in memory, never an asset and
    /// never in the profile's pool, to the station contract (plan 4.9) and FD-b (61 4.4): E1 north at column 2 (the
    /// start door's axis), E2 west at row 1, the stand-in lamp table (StartGroupTemplate.StandInLamp), no props, and the
    /// four markers: the key spot at the take point (9.7 m east, 1.6 m south of the north wall, 1.42 m up), the beacon
    /// at the TV (x 9.35 m, 2.3 m up), the placard on the north wall at x 5.5 m (1.56 m up, facing the door) and the
    /// station entrance on the door's axis (x 7.5 m). StartGroupTemplate.From reads it back as the stand-in template.
    /// </summary>
    public static RoomModuleData StandInStation(int depth)
    {
        var stand = StartGroupTemplate.StandIn(depth, RoomModuleData.StationE2Row, StartGroupTemplate.CorridorCells);
        var m = new RoomModuleData
        {
            width = StartGroupTemplate.Width, depth = depth, height = ZoneHeight.Standard, theme = ZoneTheme.Level0,
            columns = ModuleColumns.None, fill = ModuleFill.None, weight = 0f, allowRotate = false,
        };
        m.Normalize();
        m.north[RoomModuleData.StationEntranceColumn] = ModuleEdge.Arch;
        m.west[RoomModuleData.StationE2Row] = ModuleEdge.Arch;
        for (var j = 0; j < depth; j++)
        for (var i = 0; i < StartGroupTemplate.Width; i++)
            m.lamps[i + j * StartGroupTemplate.Width] = stand.LampAt(i, j);
        var north = m.DepthMetres;
        var axis = (RoomModuleData.StationEntranceColumn + .5f) * MapGrid.CellSize;
        m.markers = new[]
        {
            new ModuleMarker { kind = ModuleMarkerKind.KeySpot, x = 9.7f, z = north - 1.6f, y = 1.42f, yaw = 270f, tag = "", host = "" },
            new ModuleMarker { kind = ModuleMarkerKind.StationBeacon, x = 9.35f, z = north - .4f, y = 2.3f, yaw = 180f, tag = "", host = "" },
            new ModuleMarker { kind = ModuleMarkerKind.Placard, x = 5.5f, z = north - ModuleUnits.WallHalf, y = 1.56f, yaw = 180f, tag = "", host = "" },
            new ModuleMarker { kind = ModuleMarkerKind.StationEntrance, x = axis, z = 0f, y = 0f, yaw = 0f, tag = "", host = "" },
        };
        return m;
    }

    /// <summary>EXITs found over all floors and the distance range, for the summary line.</summary>
    static string ExitSummary(MapExitReport exits)
    {
        int found = 0, min = int.MaxValue, max = 0, widened = 0, glass = 0;
        foreach (var st in exits.stats)
        {
            found += st.exits;
            widened += st.widened;
            glass += st.throughGlass;
            if (st.exits == 0) continue;
            min = Math.Min(min, st.minDistance);
            max = Math.Max(max, st.maxDistance);
        }
        return found + " EXITs, " + (found > 0 ? min + "-" + max : "no") + " walking cells, " + widened + " widened, " + glass + " through glass";
    }

    /// <summary>One line per floor: EXITs, distances, widenings, dead ends, candidates, SeedFor tries, what N is.</summary>
    static string ExitDetail(MapExitReport exits)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var parts = new System.Collections.Generic.List<string>();
        foreach (var st in exits.stats)
            parts.Add("floor " + st.floor + ": " + st.exits + " EXITs, distance " + st.minDistance + "-" + st.maxDistance + " (mean " + st.meanDistance.ToString("0.0", inv)
                + "), " + st.widened + " widened, " + st.throughGlass + " through glass, " + st.deadEnds + " dead ends, " + st.meanCandidates.ToString("0.0", inv) + " candidates on average"
                + (st.floor > 1 ? ", SeedFor at most " + st.maxSeedTries + " tries, " + st.seedFallbacks + " fallbacks" : "")
                + ", N low/standard/tall " + st.lowFront + "/" + st.standardFront + "/" + st.tallFront
                + ", N-X generated open/arch/wall/door/window " + string.Join("/", st.generatedEdges)
                + ", front rules: " + st.beforeFrontRules + " cells past every other rule, " + st.rejectedHeight + " turned down for N's height (" + st.exitsWithHeightRejects
                + " EXITs), " + st.rejectedSightline + " for glass on the sightline (" + st.rejectedShiftOnly + " only for a shift; " + st.exitsWithSightlineRejects + " EXITs), "
                + st.widenedByFrontRules + " widened by them, signs end-on/flat " + st.corridorSigns + "/" + st.flatSigns);
        return string.Join("; ", parts);
    }

    /// <summary>
    /// C23 round 2: every widened floor, with the nearest-first pick: seed, floor, X, its walk, the band it was
    /// found in against the band asked, how many walking cells outside the band asked it lies, and how many of the
    /// band's candidates lay that near (dead ends among them).
    /// </summary>
    static string WidenedFloors(MapExitReport exits)
    {
        var parts = new System.Collections.Generic.List<string>();
        foreach (var seed in exits.seeds)
            foreach (var floor in seed.floors)
            {
                var e = floor.exit;
                if (!e.found || !e.widened) continue;
                FrontRoomsExitPlanner.BandFor(floor.floor, out var lo, out var hi);
                parts.Add("seed " + seed.seed + " floor " + floor.floor + ": X " + e.exitCell + ", " + e.distance + " walking cells (band " + e.bandMin + "-" + e.bandMax + ", asked " + lo + "-" + hi
                    + (e.throughGlass ? ", through glass" : "") + "), " + e.nearestGap + " outside the band asked; " + e.nearest + " of " + e.candidates + " candidates that near ("
                    + e.nearestDeadEnds + " dead ends; " + e.lostToFrontRules + " cells of the narrower bands lost to the front rules)");
            }
        return "EXIT widened floors (C23 round 2, nearest the band asked first): " + (parts.Count == 0 ? "none" : parts.Count + ": " + string.Join("; ", parts));
    }

    /// <summary>
    /// C23 step 1's front rules over every floor: how often rule 3 (N Standard height) and rule 4 (no glass on the
    /// EXIT door's sightline, nor an edge a revisit shift could glaze) turned a cell down that every other rule let
    /// through, whether they made any floor widen its band, and which EXIT sign the map mounts (corridor rule).
    /// </summary>
    static string FrontRulesSummary(MapExitReport exits)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        int found = 0, before = 0, height = 0, sight = 0, shiftOnly = 0, withHeight = 0, withSight = 0, widened = 0, byRules = 0, end = 0, flat = 0, standard = 0;
        foreach (var st in exits.stats)
        {
            found += st.exits;
            before += st.beforeFrontRules;
            height += st.rejectedHeight;
            sight += st.rejectedSightline;
            shiftOnly += st.rejectedShiftOnly;
            withHeight += st.exitsWithHeightRejects;
            withSight += st.exitsWithSightlineRejects;
            widened += st.widened;
            byRules += st.widenedByFrontRules;
            end += st.corridorSigns;
            flat += st.flatSigns;
            standard += st.standardFront;
        }
        string Share(int part) => before > 0 ? (100.0 * part / before).ToString("0.0", inv) + " %" : "-";
        return "EXIT front rules (C23 step 1) over " + found + " EXITs: N Standard on " + standard + "; of " + before + " cells that passed every other rule (in the band each EXIT was found in), rule 3 (N not Standard) turned down "
            + height + " (" + Share(height) + ", on " + withHeight + " EXITs), rule 4 (glass on the sightline) " + sight + " (" + Share(sight) + ", on " + withSight + " EXITs; "
            + shiftOnly + " of them only for an edge a revisit shift could glaze); " + widened + " EXITs widened, " + byRules + " of them because of the front rules; signs: "
            + end + " end-on blades (corridor), " + flat + " flat";
    }

    /// <summary>
    /// Informational, never a failure: the planner's RootFor (a port of the game's private MapRootFor) against
    /// the map roots the game logged for the two autopilot seeds (START lines, 2026-10-22, this profile). A
    /// mismatch means the port drifted from the game, or the profile's generation numbers changed since.
    /// </summary>
    static string RootPortCheck(FrontRoomsLevelProfile profile, System.Collections.Generic.IReadOnlyList<RoomModuleData> modules)
    {
        var parts = new System.Collections.Generic.List<string>();
        void Check(int seed, GridCoord rootCells, GridCoord door)
        {
            var plan = FrontRoomsExitPlanner.RootFor(FrontRoomsExitPlanner.CacheFor(profile.generation, modules, seed, 1), TitleDoorWorldCell, TitleHalfCells, TitleRows);
            var same = plan.root == rootCells && plan.doorCell == door;
            parts.Add("seed " + seed + " root " + plan.root + " cells, door " + plan.doorCell + (same ? " = game" : " != game root " + rootCells + ", door " + door));
        }
        // G1 (2026-10-22): the roots moved for the placard's west edge; 2554's is the game's START line, 20388's the port's.
        Check(2554, new GridCoord(-128, -64), new GridCoord(213, 70));
        Check(20388, new GridCoord(128, 0), new GridCoord(-43, 6));
        return "RootFor port vs the game's autopilot START lines: " + string.Join("; ", parts);
    }
}
