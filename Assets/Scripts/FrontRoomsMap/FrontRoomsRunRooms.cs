using System;
using System.Collections.Generic;

namespace FrontRooms.Map
{
    /// <summary>A Run! room's alarm (CR-2): armed until the player steps 1.5 m in, tripped until the zone resets (the Relay's WITHDRAW).</summary>
    public enum RunRoomState : byte { Armed, Tripped }

    /// <summary>
    /// A Run! room's one twist (71 s.3.4, s.3.6; Red 11-03 23:18 "全部按推荐", decisions 1, 2, 4, 8c): none (Plain), the 1:8 ramp
    /// (card 6), the Run door entrance (card 5, D1) or the receding end (card 4, the stretch). One twist per room, never two.
    /// Append only: logs and tests read the names.
    /// </summary>
    public enum RunVariant : byte { Plain, Ramp, Door, Recede }

    /// <summary>
    /// One card of the per-floor Run! deck (71 s.3.4, s.9 关卡设计 card 2): nothing, or a variant with its length in whole cells
    /// (the planned length; a receding room's reserved L_max) and, for the receding end, the line its end stands on at
    /// arrival (9 cells, 27 m). Plain data; the default is Nothing.
    /// </summary>
    [Serializable]
    public struct RunCard : IEquatable<RunCard>
    {
        public RunVariant variant;
        /// <summary>The room's cells, 7 .. 14 (a receding room's reserved L_max); 0 = nothing.</summary>
        public byte cells;
        /// <summary>The receding end's first line in cells (9); 0 for every other variant.</summary>
        public byte from;

        public static RunCard Nothing => default;
        public static RunCard Plain(int cells) => new RunCard { variant = RunVariant.Plain, cells = (byte)cells };
        public static RunCard Ramp(int cells) => new RunCard { variant = RunVariant.Ramp, cells = (byte)cells };
        public static RunCard Door(int cells) => new RunCard { variant = RunVariant.Door, cells = (byte)cells };
        public static RunCard Recede(int from, int to) => new RunCard { variant = RunVariant.Recede, cells = (byte)to, from = (byte)from };

        public bool IsNothing => cells == 0;
        /// <summary>A card with a twist (ramp, Run door, receding end).</summary>
        public bool IsTwist => !IsNothing && variant != RunVariant.Plain;

        public bool Equals(RunCard other) => variant == other.variant && cells == other.cells && from == other.from;
        public override bool Equals(object obj) => obj is RunCard other && Equals(other);
        public override int GetHashCode() => (int)variant | cells << 8 | from << 16;
        public static bool operator ==(RunCard a, RunCard b) => a.Equals(b);
        public static bool operator !=(RunCard a, RunCard b) => !a.Equals(b);

        public override string ToString() => IsNothing ? "nothing"
            : variant == RunVariant.Recede ? "recede " + from + "->" + cells
            : (variant == RunVariant.Plain ? "plain " : variant == RunVariant.Ramp ? "ramp " : "door ") + cells;
    }

    /// <summary>One floor's deal from the deck (FrontRoomsRunDeck.History; the planner's Dealt event): what the deck held, what was played, what was built.</summary>
    public sealed class RunDeal
    {
        /// <summary>The floor (1 = LEVEL 0, 2 = B1, 3 = B2 ...) and the tier its host zone was met at.</summary>
        public int Floor, Tier;
        /// <summary>The deck's card; the card played after the rules (plain first, the structure gates, no repeats); the room
        /// built after the fit (Nothing: no Run! on this floor).</summary>
        public RunCard Deck, Played, Built;
        /// <summary>Why Played differs from Deck (null: it does not), and why Built differs from Played (a shorter length that fits).</summary>
        public string Why, Fit;
        /// <summary>The room's id on its floor (0: nothing).</summary>
        public int RoomId;

        public override string ToString() => "floor " + Floor + " T" + Tier + ": " + Built + " (deck " + Deck + (Why != null ? "; " + Why : "") + (Fit != null ? "; " + Fit : "") + ")";
    }

    /// <summary>
    /// The run's Run! deck (71 s.3.4, s.9 关卡设计 card 2; Red 11-03 "全部按推荐", decisions 1, 3, 8b, 8c). Kept by the map world
    /// for the whole run (FrontRoomsMapWorld hands it to every floor's planner through NextFloor, as RunRoomsOpen), so it
    /// holds what one floor's cache cannot: which floor dealt what, and whether a Run! has been met in this run.
    /// - One card per floor, taken at the floor's first zone that would host a Run! (FrontRoomsRunRooms.Meet: eligible, its
    ///   slot roll passed): at most one Run! a floor (F10), and some floors none.
    /// - A deck per tier, shuffled (Left 4 Dead's "Tank, Witch, and Nothing", [LS224]): T2 {9 plain, nothing}; T3 {9 plain,
    ///   9 ramp, 9 Run door, nothing}; T4 {12 plain, 9 ramp, 9 Run door, 9 -> 12 receding, nothing}; T5 {14 plain, 9 ramp, 9 Run
    ///   door, 9 -> 14 receding, nothing}. The floor takes the next card of the tier its host zone was met at; a used-up deck
    ///   is shuffled again, never opening on the card it closed on (no successive repeat). No changed door anywhere (8c).
    /// - The rules on the card (Play): F9 plain first: until a Run! has been met in this run (the player stood in one or at
    ///   one of its openings: MarkMet), every card but nothing is the 7-cell teaching room (plain, arched), so the run's first
    ///   Run! is 7 cells (decision 3); a twist whose structure is not on is the tier's plain room (PlainCellsAt: 9 at T2-T3, 12
    ///   at T4, 14 at T5; the ramp until the map holds per-cell floor levels; the Run door and the receding end until Red has
    ///   walked SP7, rule 5b); the receding end only on B2 or deeper (floor 3+, else the tier's plain room), 9 -> 14 only on
    ///   B3 or deeper (floor 4+, else 9 -> 12); the same twist is never dealt on two floors running (so the receding end never
    ///   is): it is played as the tier's plain room.
    /// - Lengths follow depth, never the player's performance (decision 3: "7 cells for each run's first Run!, then 9, 12 from
    ///   T4, 14 at T5 where it fits"): the cards carry them; FrontRoomsRunRooms.FitOrder shortens only "where it fits" (a plain
    ///   14 to 12, a receding 9 -> 14 to 9 -> 12). No T4-T5 room is a plain 9: a 9-cell room there is a Run door or ramp twist.
    /// Pure in the run's seed and the order the floors deal in (the player's path: the tier a host zone is met at). Plain C#.
    /// </summary>
    public sealed class FrontRoomsRunDeck
    {
        /// <summary>The shuffle's salt ("RUN4").</summary>
        public const int Salt = 0x52554e34;
        /// <summary>The receding end from floor B2 (the game's floor 3), 9 -> 14 from B3 (floor 4): 71 s.3.4, decision 4.</summary>
        public const int RecedeMinFloor = 3, RecedeLongMinFloor = 4;

        static readonly RunCard[][] Decks =
        {
            new[] { RunCard.Plain(9), RunCard.Nothing },
            new[] { RunCard.Plain(9), RunCard.Ramp(9), RunCard.Door(9), RunCard.Nothing },
            new[] { RunCard.Plain(12), RunCard.Ramp(9), RunCard.Door(9), RunCard.Recede(9, 12), RunCard.Nothing },
            new[] { RunCard.Plain(14), RunCard.Ramp(9), RunCard.Door(9), RunCard.Recede(9, 14), RunCard.Nothing },
        };

        readonly int seed;
        // Per tier 2 .. 5 (index tier - 2): the order of its deck, the next position and how many times it was shuffled.
        readonly int[][] order = new int[4][];
        readonly int[] next = new int[4], round = new int[4];
        readonly Dictionary<int, RunDeal> floors = new Dictionary<int, RunDeal>();
        readonly List<RunDeal> history = new List<RunDeal>();

        public FrontRoomsRunDeck(int seed)
        {
            this.seed = seed;
            for (var t = 0; t < order.Length; t++) order[t] = Shuffle(t, 0, RunCard.Nothing, false);
        }

        public int Seed => seed;

        /// <summary>F9's run flag: a Run! has been met in this run (MarkMet). Before it every Run! dealt is the 7-cell teaching room.</summary>
        public bool Met { get; private set; }
        /// <summary>Run! rooms placed this run (every floor).</summary>
        public int Rooms { get; private set; }
        /// <summary>Every floor's deal, in the order they were dealt.</summary>
        public IReadOnlyList<RunDeal> History => history;

        /// <summary>The player met a Run! (stood in it or at one of its openings): from now on the deck's twists may come.</summary>
        public void MarkMet() => Met = true;

        /// <summary>The floor's card has been dealt (a room placed, or nothing): no other Run! on it.</summary>
        public bool Dealt(int floor) => floors.ContainsKey(floor);

        /// <summary>The floor's deal, or null.</summary>
        public RunDeal DealOn(int floor) => floors.TryGetValue(floor, out var d) ? d : null;

        /// <summary>A tier's deck (2 .. 5, clamped), in its written order.</summary>
        public static IReadOnlyList<RunCard> DeckAt(int tier) => Decks[TierIndex(tier)];

        static int TierIndex(int tier) => Math.Max(2, Math.Min(5, tier)) - 2;

        /// <summary>The next card of the tier's deck (no state changes).</summary>
        public RunCard Top(int tier)
        {
            var t = TierIndex(tier);
            return Decks[t][order[t][next[t]]];
        }

        /// <summary>The card the floor takes now at <paramref name="tier"/>: the deck's next card after the rules (Play). Changes nothing.</summary>
        public RunCard Peek(int floor, int tier, FrontRoomsRunRules rules, out RunCard deck, out string why)
        {
            deck = Top(tier);
            return Play(deck, floor, tier, rules, out why);
        }

        /// <summary>
        /// The rules on a card for a floor at a tier (see the class summary). A twist that cannot be dealt here plays as the
        /// tier's plain room (FrontRoomsRunRooms.PlainCellsAt: 9 at T2-T3, 12 at T4, 14 at T5), never a shorter one (decision 3;
        /// 71 s.3.4: a gated receding card is "else 12 plain" at T4). Changes nothing.
        /// </summary>
        public RunCard Play(RunCard card, int floor, int tier, FrontRoomsRunRules rules, out string why)
        {
            why = null;
            if (card.IsNothing) return card;
            if (!Met)
            {
                why = "plain first: the run's first Run! (7 cells, arched) until one is met";
                return RunCard.Plain(FrontRoomsRunRooms.FirstCells);
            }
            var plain = RunCard.Plain(FrontRoomsRunRooms.PlainCellsAt(tier));
            var c = card;
            if (c.variant == RunVariant.Ramp && (rules == null || !rules.ramps))
            {
                why = "the ramp waits for per-cell floor levels (card 6)";
                c = plain;
            }
            else if (c.variant == RunVariant.Door && (rules == null || !rules.doorEntrance))
            {
                why = "the Run door entrance is off until Red has walked SP7 (rule 5b)";
                c = plain;
            }
            else if (c.variant == RunVariant.Recede)
            {
                if (rules == null || !rules.recede)
                {
                    why = "the receding end is off (rule 5b; the recede rig and the map's walk side are not in main)";
                    c = plain;
                }
                else if (floor < RecedeMinFloor)
                {
                    why = "the receding end from B2 only";
                    c = plain;
                }
                else if (c.cells > FrontRoomsRunRooms.LateCells && floor < RecedeLongMinFloor)
                {
                    why = "9 -> 14 from B3 only";
                    c = RunCard.Recede(FrontRoomsRunRooms.RecedeFromCells, FrontRoomsRunRooms.LateCells);
                }
            }
            if (c.IsTwist && floors.TryGetValue(floor - 1, out var above) && !above.Built.IsNothing && above.Built.variant == c.variant)
            {
                why = "the same twist as the floor above (" + above.Built + ")";
                c = plain;
            }
            return c;
        }

        /// <summary>
        /// The floor's card is spent: <paramref name="built"/> is the room placed (or Nothing). The tier's deck moves on; a
        /// used-up deck is shuffled again, never opening on the card it closed on.
        /// </summary>
        public RunDeal Commit(int floor, int tier, RunCard deck, RunCard played, RunCard built, string why, string fit, int roomId)
        {
            var deal = new RunDeal { Floor = floor, Tier = tier, Deck = deck, Played = played, Built = built, Why = why, Fit = fit, RoomId = roomId };
            floors[floor] = deal;
            history.Add(deal);
            if (!built.IsNothing) Rooms++;
            var t = TierIndex(tier);
            var last = Decks[t][order[t][next[t]]];
            if (++next[t] >= order[t].Length)
            {
                round[t]++;
                order[t] = Shuffle(t, round[t], last, true);
                next[t] = 0;
            }
            return deal;
        }

        // Fisher-Yates over the tier's deck, pure in the seed, the tier and the round; a later round never opens on the card
        // the last one closed on (Left 4 Dead: "Successive repeats are not allowed").
        int[] Shuffle(int t, int r, RunCard avoid, bool avoidFirst)
        {
            var n = Decks[t].Length;
            var o = new int[n];
            for (var i = 0; i < n; i++) o[i] = i;
            for (var i = n - 1; i > 0; i--)
            {
                var j = (int)(MapHash.Hash(seed, t + 2, r, Salt, i) % (uint)(i + 1));
                var tmp = o[i];
                o[i] = o[j];
                o[j] = tmp;
            }
            if (avoidFirst && n > 1 && Decks[t][o[0]] == avoid)
            {
                var k = 1 + (int)(MapHash.Hash(seed, t + 2, r, Salt, 97) % (uint)(n - 1));
                var tmp = o[0];
                o[0] = o[k];
                o[k] = tmp;
            }
            return o;
        }
    }

    /// <summary>
    /// The Run! rules for one map, from the level profile (FrontRoomsLevelProfile.RunRules). Plain data. Every switch is
    /// off by default, and with runRooms off no FrontRoomsRunRooms exists at all (today's map, byte for byte).
    /// </summary>
    [Serializable]
    public sealed class FrontRoomsRunRules
    {
        /// <summary>Call 2 (CR-6a): the goal door is a forced Door edge wherever the far cell is (in the room's own zone, or a Low
        /// zone). Off (CR-6b): the corridor must end on a Standard-to-Low border, where the map's rule makes a door anyway.</summary>
        public bool goalDoorForced;
        /// <summary>Call 5: Run! rooms in Office zones too (the same B look). Off: Level 0 zones only.</summary>
        public bool office;
        /// <summary>Tools only (the 100-seed check, -autopilotRunRooms): every zone met once the map is ready is treated as met
        /// at tier minTier or later and after the run's first send, and the budget roll always passes. Placement is per zone (no
        /// deck), every room forceCard's or today's plain 9.</summary>
        public bool force;
        /// <summary>RELAY 6 (系统设计 2026-10-31): Run! rooms are the tier-2 trigger kind.</summary>
        public int minTier = 2;
        /// <summary>RELAY 6: no trigger within 15 cells of the start door (walking cells, bounded below by the Manhattan distance used here).</summary>
        public int minStartCells = 15;
        /// <summary>CR-5: a Relay entry (a module marker) within this many cells of a Run! room is not offered to the Relay.</summary>
        public int minEntryCells = 9;
        /// <summary>The trigger budget, one device per this many zones generated at tier t (index t - 1; FrontRoomsTierV2.zonesPerDevice).</summary>
        public float[] zonesPerDevice = { 3f, 2.5f, 2f, 1.75f, 1.5f };

        // ---------- The Run! rethink (71 s.9, 关卡设计 cards 1-6; Red 11-03 23:18 "全部按推荐") ----------

        /// <summary>Card 2 (decisions 1, 8b): at most one Run! a floor, dealt from the run's deck by tier (FrontRoomsRunDeck).
        /// Off, or with force: every zone whose slot roll passes holds a room, as before the rethink (tools and checks).</summary>
        public bool deck = true;
        /// <summary>Card 3 (decisions 1, 8e): no other trigger kind (foiled window, alarm door) in a Run!'s host zone or in the
        /// zone beyond its goal door, so the player who shuts the door gets the calm the rules promise.</summary>
        public bool calm = true;
        /// <summary>Card 5 (decision 8c, D1): the deck's Run door entrance (RN-F with set A's F5 lite on the entrance edge). Rule
        /// 5b: off until Red has walked SP7 in scene 4 and said so; off, the card is played as the tier's plain room.</summary>
        public bool doorEntrance;
        /// <summary>Card 4 (decision 4): the deck's receding end (9 -> 12, 9 -> 14). Off, the card is played as the tier's plain
        /// room. Tools only for now: the map shuts the hidden length for the Relay's graph alone (HeldShut), and the walk side
        /// (the hidden cells' colliders and walk volume per seat, the blind panel in the L_max frame, the goal door on the
        /// head's DoorMount) waits for 游戏视觉's recede rig in main, so the level profile does not serialize its switch
        /// (FrontRoomsLevelProfile.runRecede); then rule 5b (Red walks SP7 first).</summary>
        public bool recede;
        /// <summary>Card 6 (decision 2): the deck's Run! ramp (1:8, at most 24 m and 3.0 m down). Tools only: the map has no per-cell
        /// floor levels (research/level_space/40 s.11.7) and Apply builds no slope, so the level profile does not serialize its
        /// switch (FrontRoomsLevelProfile.runRamps); off, the card is played as the tier's plain room.</summary>
        public bool ramps;
        /// <summary>Card 5: a Run door's trip depth, past the leaf's swing and a barge's carry (系统设计 card 4: 2.5 m, a starting value).</summary>
        public float doorTripDepth = 2.5f;
        /// <summary>Tools only (the Level Designer, the length checks): every room is this card (Nothing: dealt by the deck, or
        /// today's plain 9 per zone). Per zone it is exact; dealt, FitOrder may still shorten it.</summary>
        public RunCard forceCard;

        /// <summary>The chance a zone met at <paramref name="tier"/> holds a Run! room (1 / zonesPerDevice; 1 when forced).</summary>
        public float ChanceAt(int tier)
        {
            if (force) return 1f;
            if (zonesPerDevice == null || zonesPerDevice.Length == 0) return 0f;
            var z = zonesPerDevice[Math.Max(0, Math.Min(zonesPerDevice.Length - 1, tier - 1))];
            return z > 0f ? Math.Min(1f, 1f / z) : 0f;
        }

        /// <summary>The highest chance at any tier: a zone whose roll is over it never competes for a chunk (FrontRoomsRunRooms.Priority).</summary>
        public float MaxChance
        {
            get
            {
                if (force) return 1f;
                var best = 0f;
                for (var t = 1; zonesPerDevice != null && t <= zonesPerDevice.Length; t++) best = Math.Max(best, ChanceAt(t));
                return best;
            }
        }

        public FrontRoomsRunRules Clone()
        {
            var c = (FrontRoomsRunRules)MemberwiseClone();
            c.zonesPerDevice = (float[])zonesPerDevice?.Clone();
            return c;
        }
    }

    /// <summary>
    /// One Run! room (L7, CR-1): a 1 x L straight of Standard cells of its zone's theme, L 7 .. 14 by depth (card 1; 9 before
    /// the rethink), owned by the zone of the entrance's outside cell <see cref="Outside"/> (the host zone; it may straddle
    /// zones of that height and theme, as a carved room may), ending in the goal door onto <see cref="Beyond"/>. Corridor-local
    /// frame (游戏视觉's FrontRoomsRunRig): z = 0 on the entrance cell line, +z along <see cref="AxisX"/>/<see cref="AxisY"/>
    /// to the goal wall at z = 3 m x L; x = 0 on the axis. Map-space metres (the map root's local space), plain C#.
    /// Its twist (Variant): a plain room is entered by the 1.4 m arch; a Run door room (card 5) by RN-F + F5 on the same edge;
    /// a receding room (card 4) holds its reserved L_max cells, its end standing on SeatCells[SeatIndex] (9 cells on arrival).
    /// </summary>
    public sealed class FrontRoomsRunRoom
    {
        /// <summary>A stable id within the floor (the order rooms were planned in).</summary>
        public int Id;
        /// <summary>The host zone: the entrance's outside cell's (CR-1: "on its own zone's side"); its tier and theme are the room's.</summary>
        public GridCoord Zone;
        /// <summary>Every zone its cells lie in (the host zone first).</summary>
        public GridCoord[] Zones;
        /// <summary>The room's cells, from the entrance (Cells[0]) to the goal (the last): 7 .. 14 (card 1, by depth; a receding
        /// room's reserved L_max). The visual rig reads its length from here (FrontRoomsRunRigBinder.CellsOf: Cells.Length).</summary>
        public GridCoord[] Cells;
        /// <summary>The unit step from the entrance to the goal.</summary>
        public int AxisX, AxisY;
        /// <summary>The entrance's outside cell (Cells[0] - axis) and the goal door's far cell (the last cell + axis).</summary>
        public GridCoord Outside, Beyond;
        /// <summary>The two chunks it lies in (the entrance's, the goal's).</summary>
        public GridCoord ChunkA, ChunkB;
        /// <summary>The zone's tier when it was met (RELAY 12), and the far cell's height (Standard: CR-6a's forced door; Low: a border door).</summary>
        public int Tier;
        public ZoneHeight BeyondHeight;
        public ZoneTheme Theme;

        // ---------- The rethink (71 s.9, cards 1-6) ----------

        /// <summary>The room's twist (one at most).</summary>
        public RunVariant Variant;
        /// <summary>The card it was dealt (before a shorter length was fitted; Variant and Cells say what was built).</summary>
        public RunCard Card;
        /// <summary>The floor it was planned on (1 = LEVEL 0, 2 = B1, ...).</summary>
        public int Floor;
        /// <summary>Card 5: a Run door's trip depth (FrontRoomsRunRules.doorTripDepth), kept with the room.</summary>
        public float DoorTrip = 2.5f;
        /// <summary>Card 4: the receding end's seat lines in whole cells from the entrance, ascending: 9, 11, 12 (9 -> 12) or 9, 11,
        /// 12, 13, 14 (9 -> 14; 71 s.3.6: 27, 33, 36, 39, 42 m). The last is Cells.Length. Null for every other room.</summary>
        public int[] SeatCells;
        /// <summary>Card 4: the seats done (0: the end on its first line). Kept by the record through drops and rebuilds (71 s.4.6:
        /// a rebuilt chunk is rebuilt receded); it never goes back (one recede a visit).</summary>
        public int SeatIndex;
        /// <summary>Card 4: the rig's own record of the recede (游戏视觉's RunRecedeRecord, 8 bytes as the rig packs them), kept here
        /// for it through drops; the map never reads it.</summary>
        public ulong RecedeRecord;
        /// <summary>Card 2's run flag: the player has stood in it or at one of its openings (FrontRoomsRunRooms.MarkMet).</summary>
        public bool Met;

        // ---------- The alarm (CR-2), kept by the room so drops and rebuilds keep it ----------
        public RunRoomState State;
        /// <summary>Re-armed with the player inside (a reset): it trips again only after the player has left it.</summary>
        public bool WaitForExit;
        /// <summary>How many times it tripped this floor, and the map's clock at the last trip (the world's; -1 never).</summary>
        public int Trips;
        public float TrippedAt = -1f;
        /// <summary>The last trip came from the goal end (the room met from its goal door: card 5's "door behind" case).</summary>
        public bool TrippedFromGoal;

        public const int Length = FrontRoomsRunRooms.Length;
        /// <summary>The room's length in cells: its record's cells (Length when it has none). One source for the map and the rig.</summary>
        public int LengthCells => Cells != null && Cells.Length > 0 ? Cells.Length : Length;
        public float LengthMetres => LengthCells * MapGrid.CellSize;
        /// <summary>The goal cell: the last of Cells (the goal door is on its far edge, onto Beyond).</summary>
        public GridCoord GoalCell => Cells[LengthCells - 1];

        /// <summary>Card 5: the entrance is a Run door (RN-F with set A's F5 lite), not the 1.4 m arch.</summary>
        public bool EntranceDoor => Variant == RunVariant.Door;
        /// <summary>Card 4: a receding room (its end moves back once, from SeatCells[0] to the last seat).</summary>
        public bool Receding => Variant == RunVariant.Recede && SeatCells != null && SeatCells.Length > 1;
        /// <summary>Card 4: the end stands on its last seat (always true for a room that does not recede).</summary>
        public bool Seated => !Receding || SeatIndex >= SeatCells.Length - 1;
        /// <summary>The cells the player can walk now: all of them, or a receding room's cells up to its end's line.</summary>
        public int OpenCells => Receding ? SeatCells[Math.Max(0, Math.Min(SeatIndex, SeatCells.Length - 1))] : LengthCells;
        public float OpenMetres => OpenCells * MapGrid.CellSize;
        /// <summary>Where the room arms, in from the entrance: 1.5 m under the arch (CR-2), the Run door's depth, or a receding
        /// room's first compartment frame (FD1, 9 m: Order T, decision 4) until its last seat (then a plain room of its new
        /// length that arms 1.5 m in, 71 s.4.6).</summary>
        public float EntranceTrip => Receding && !Seated ? FrontRoomsRunRooms.RecedeTripCells * MapGrid.CellSize : EntranceDoor ? DoorTrip : FrontRoomsRunRooms.ArmDepth;
        /// <summary>Where the room arms in from the goal door (a room met from its goal end): 1.5 m, or the Run door's depth in a
        /// Run door room (a Run door at both ends: the same race whichever end is met first, 71 s.7.2).</summary>
        public float GoalTrip => EntranceDoor ? DoorTrip : FrontRoomsRunRooms.ArmDepth;

        /// <summary>The corridor's yaw in degrees (0 = +Z, 90 = +X): the rig's local +z in map space.</summary>
        public float Yaw => AxisY == 1 ? 0f : AxisX == 1 ? 90f : AxisY == -1 ? 180f : 270f;

        /// <summary>The entrance edge's centre on the cell line, at floor level (corridor-local origin), map space x and z.</summary>
        public float OriginX => (Cells[0].x + .5f - AxisX * .5f) * MapGrid.CellSize;
        public float OriginZ => (Cells[0].y + .5f - AxisY * .5f) * MapGrid.CellSize;

        /// <summary>Corridor-local (x, z) to map space (x, z).</summary>
        public void ToMap(float localX, float localZ, out float mapX, out float mapZ)
        {
            // Right of the axis (local +x) is the axis turned clockwise: (AxisY, -AxisX).
            mapX = OriginX + AxisX * localZ + AxisY * localX;
            mapZ = OriginZ + AxisY * localZ - AxisX * localX;
        }

        /// <summary>Map space (x, z) to corridor-local z (along the axis from the entrance line) and x.</summary>
        public void ToLocal(float mapX, float mapZ, out float localX, out float localZ)
        {
            var dx = mapX - OriginX;
            var dz = mapZ - OriginZ;
            localZ = dx * AxisX + dz * AxisY;
            localX = dx * AxisY - dz * AxisX;
        }

        /// <summary>The cell's index along the room (0 = the entrance cell), or -1 when it is not one of its cells.</summary>
        public int IndexOf(GridCoord cell)
        {
            if (Cells == null || Cells.Length == 0) return -1;
            var k = (cell.x - Cells[0].x) * AxisX + (cell.y - Cells[0].y) * AxisY;
            return k >= 0 && k < Cells.Length && Cells[k] == cell ? k : -1;
        }

        public bool Contains(GridCoord cell) => IndexOf(cell) >= 0;

        /// <summary>
        /// The trip test (CR-2): on the axis band, EntranceTrip in from the entrance or GoalTrip in from the goal door. A
        /// receding room short of its last seat arms only at FD1 from the entrance side, up to its end's line (its goal end is
        /// the release side, which 71 s.4.6 closes with a blind panel: not built yet, see FrontRoomsRunRooms.HeldShut).
        /// </summary>
        public bool InArmedZone(float mapX, float mapZ)
        {
            ToLocal(mapX, mapZ, out var x, out var z);
            if (Math.Abs(x) >= MapGrid.CellSize * .5f) return false;
            if (Receding && !Seated) return z >= EntranceTrip && z <= OpenMetres;
            return z >= EntranceTrip && z <= LengthMetres - GoalTrip;
        }

        /// <summary>"plain 9", "door 9", "recede 9->14 (end at 9)" ... for logs.</summary>
        public string VariantText => Receding ? "recede " + SeatCells[0] + "->" + LengthCells + " (end at " + OpenCells + ")"
            : (Variant == RunVariant.Plain ? "plain " : Variant == RunVariant.Ramp ? "ramp " : "door ") + LengthCells;

        public override string ToString() => "run " + Id + " zone " + Zone + " " + Cells[0] + "->" + GoalCell + " (" + VariantText + ", " + (EntranceDoor ? "Run door" : "arch") + " from " + Outside + ", goal door to " + Beyond + " " + BeyondHeight + ")";
    }

    /// <summary>
    /// Run! rooms (L7) as map data: the map side of CR-1 (the straight), CR-3 (finish, data), CR-4 (lamps Off), CR-5
    /// (placement) and CR-6 (the goal door's edge), and of the Run! rethink (71 s.9, 关卡设计 cards 1-6; Red 11-03 23:18 "全部按推荐"):
    /// lengths by depth, one Run! a floor from the deck, a calm beyond the door, the receding end's record and seats, the Run
    /// door entrance and the ramp's card. Held by the cache (FrontRoomsMapCache.SetRunRooms) like the A26 carve, only while
    /// the level profile's runRooms is on; with it off nothing here exists and every map is today's.
    ///
    /// How a room comes to be (CR-1, CR-5, cards 1 and 2):
    /// - A zone is decided once, when the cache first meets it (the moment its tier is recorded, RELAY 12): a room only if the
    ///   map is Ready (Begin: the start area, station and EXIT are set), the run's first send has gone out (Open, A40), the
    ///   zone's tier is at least minTier (T2), it is not the arrival zone (P2), it is Standard Level 0 (Office too with call
    ///   5), and its budget roll passes (1 / the tier's zonesPerDevice: the zone's slot). Zones met earlier never get one.
    /// - The deck (rules.deck, the game's way; FrontRoomsRunDeck): the floor's first such zone takes the floor's card at its
    ///   tier. Nothing: no Run! on this floor. A room: the card's length, or where 14 does not fit 12 (FitOrder: a plain 14 as
    ///   12, a receding 9 -> 14 as 9 -> 12; nothing else shortens); when none fits the card waits for the floor's next such
    ///   zone. Once a room is placed (or nothing dealt) no other zone of the floor gets one (F10). Per zone (rules.deck off, or force for tools):
    ///   every such zone gets forceCard's room, else today's plain 9; of two candidates sharing a chunk the lower priority hash
    ///   wins (Priority), whether or not the winner is placed, so no plan depends on the order chunks are generated in.
    /// - The candidate is a pure function of the seed, the zone, the length and the rules (Candidate): a straight of L cells,
    ///   all Standard and of the host zone's theme (it may straddle zones, as a carved room may), entered from a cell of the
    ///   host zone. Both long sides lie inside a chunk (perpendicular chunk-local 1..6) and both ends off a chunk border
    ///   (chunk-local 1..6 along the axis, the far end in the next chunk), so it crosses exactly one chunk border, between two
    ///   of its own cells, and every edge it writes lies in one of its two chunks; both chunks hold a host cell, so neither is
    ///   generated before the host zone is met and decided. That is the cap: L = x + 9 - e with e, x in 1..6 runs 4 to 14
    ///   (71 s.3.2 (a)); 14 has one axial place per chunk pair, 12 three, 9 six, 7 four. The goal's far cell is any Standard or
    ///   Low cell with call 2 (CR-6a), a Low cell without it (CR-6b: a Standard-to-Low border). No cell is reserved (start
    ///   area, X, the station) or next to one, none is any zone's key cell (RELAY 6: the key never only inside a trigger
    ///   room), no long side faces a Tall zone (no glass and no C33 arch is walled), every cell and the entrance's outside cell
    ///   are 15+ cells (Manhattan) from the start door.
    /// - Placed only if every other zone it touches is not the arrival zone and was either not met yet or met at minTier+
    ///   after the first send (ZonesStillOk).
    /// - Card 3 (calm): the host zone and the zone beyond the goal door become calm zones (CalmTouches): the foil and alarm-door
    ///   planners exclude windows and doors touching them, and the world reads one decided before the room but not yet built
    ///   as plain (a Run! planned later wins, as A53 rules for its own cells), so nothing else trips there. A candidate whose
    ///   goal door would open into a zone where a foiled window or an alarm door is already decided is refused
    ///   (BeyondHoldsDevice, CalmRefused): that zone may have been walked first and the device built and armed, and a device
    ///   is never disarmed under its look (RELAY 6: no harmless detector props). The host zone needs no such test: it is met,
    ///   and decided, before any edge touching it is built.
    /// Apply (each Get, after the carve): rooms over the corridor are dropped (columns and key spot again), the corridor's
    /// inside edges Open, its long sides Wall, the entrance Arch (1.4 m, centred: TryArch) or a Run door (card 5), the goal
    /// edge Door; its lamps Off (CR-4); then the rest of the chunk is joined again without the corridor (the corridor is
    /// never the only way round). A receding room is written whole (its reserved L_max cells, its goal door on the L_max
    /// edge); the world's passage reads its cells past the end's line, and its goal door before the last seat, as shut
    /// (HeldShut: the Relay's graph, the autopilot and the path field only; the walk side waits for the recede rig).
    /// </summary>
    public sealed class FrontRoomsRunRooms
    {
        /// <summary>CR-1: 1 x 9 cells, 27 m: today's room, the standard length (card 1).</summary>
        public const int Length = 9;
        /// <summary>Card 1 (decision 3, 71 s.3.2): the run's first Run! (the teaching room), the late length (T4), the deepest (T5,
        /// where it fits), and the bounds: 7 .. 14 cells, cap 14 (42 m: the chunk rule, the 46 m far plane and the Relay's
        /// earliest arrival meet there).</summary>
        public const int FirstCells = 7, LateCells = 12, DeepestCells = 14, MinCells = 7, CapCells = 14;
        /// <summary>Card 4 (decision 4, 71 s.3.6): the receding end stands on 9 cells (27 m) at arrival and the room arms at the end
        /// of cell 3 (FD1, 9 m: "它原本房间最初始长度的三分之一").</summary>
        public const int RecedeFromCells = 9, RecedeTripCells = 3;
        /// <summary>CR-1 (10_run_directions 1.2): the entrance arch, 1.4 m wide, centred.</summary>
        public const float ArchWidth = 1.4f;
        /// <summary>CR-2: the room arms 1.5 m inside.</summary>
        public const float ArmDepth = 1.5f;
        /// <summary>CR-2: the tag RoomTriggered carries.</summary>
        public const string Tag = "run";

        /// <summary>Card 1: the plain length at a tier (9 at T2-T3, 12 at T4, 14 at T5); the run's first Run! is FirstCells.</summary>
        public static int PlainCellsAt(int tier) => tier >= 5 ? DeepestCells : tier >= 4 ? LateCells : Length;

        /// <summary>Card 4: the seat lines of a receding end from <paramref name="from"/> to <paramref name="to"/> cells: a two-cell
        /// flinch, then one cell a step (9 -> 12: 9, 11, 12; 9 -> 14: 9, 11, 12, 13, 14). Null when it does not recede.</summary>
        public static int[] SeatsOf(int from, int to)
        {
            if (from <= 0 || to <= from) return null;
            var list = new List<int> { from };
            var line = Math.Min(to, from + 2);
            list.Add(line);
            while (line < to) list.Add(++line);
            return list.ToArray();
        }

        /// <summary>
        /// Card 1: the cards tried for a played one, in order, until one fits the host zone. Only decision 3's "14 at T5 where it
        /// fits" falls back: a plain 14 as a plain 12, a receding 9 -> 14 as 9 -> 12 (71 s.3.6: "fit permitting, else 12"). The
        /// run's first 7, a 9, a 12, a Run door or ramp 9 and a receding 9 -> 12 never shorten: when none fits, the card waits for
        /// the floor's next zone that may host a Run! (FromDeck), so the teaching room is always 7 and no T4-T5 room is a plain 9.
        /// </summary>
        public static RunCard[] FitOrder(RunCard card)
        {
            if (card.IsNothing) return new RunCard[0];
            if (card.cells > LateCells && card.variant == RunVariant.Plain) return new[] { card, RunCard.Plain(LateCells) };
            if (card.cells > LateCells && card.variant == RunVariant.Recede) return new[] { card, RunCard.Recede(card.from, LateCells) };
            return new[] { card };
        }

        // The budget roll's salt is the shared budget's (FrontRoomsTriggerBudget.RollSalt, the same "RUN1" as before V7a).
        const int BudgetSalt = FrontRoomsTriggerBudget.RollSalt, PickSalt = 0x52554e32, PrioritySalt = 0x52554e33;
        const int N = MapGrid.ChunkCells;

        readonly FrontRoomsRunRules rules;
        FrontRoomsMapCache cache;
        FrontRoomsMapGenerator gen;
        // V7a: the map's one trigger budget (RELAY 6), shared with C13's foiled windows: its roll, its chance and its ledger.
        FrontRoomsTriggerBudget budget;
        FrontRoomsRunDeck deck;

        // Per zone: the decision (null: none) and, per length, the memoised candidate (null: none fits, or the zone does not compete).
        readonly Dictionary<GridCoord, FrontRoomsRunRoom> decided = new Dictionary<GridCoord, FrontRoomsRunRoom>();
        readonly Dictionary<GridCoord, FrontRoomsRunRoom>[] candidates = new Dictionary<GridCoord, FrontRoomsRunRoom>[CapCells + 1];
        readonly Dictionary<GridCoord, FrontRoomsRunRoom> byCell = new Dictionary<GridCoord, FrontRoomsRunRoom>();
        readonly Dictionary<GridCoord, List<FrontRoomsRunRoom>> byChunk = new Dictionary<GridCoord, List<FrontRoomsRunRoom>>();
        readonly Dictionary<long, EdgeKind> borderOverride = new Dictionary<long, EdgeKind>();
        readonly Dictionary<long, FrontRoomsRunRoom> archEdges = new Dictionary<long, FrontRoomsRunRoom>();
        readonly Dictionary<long, FrontRoomsRunRoom> entranceDoors = new Dictionary<long, FrontRoomsRunRoom>();
        readonly Dictionary<long, FrontRoomsRunRoom> goalEdges = new Dictionary<long, FrontRoomsRunRoom>();
        readonly List<FrontRoomsRunRoom> rooms = new List<FrontRoomsRunRoom>();
        // Card 3: every placed room's host zone and the zone beyond its goal door.
        readonly HashSet<GridCoord> calmZones = new HashSet<GridCoord>();
        // Every zone the cache has met: its tier then, and whether the run's first send had gone out (A40).
        readonly Dictionary<GridCoord, int> metTier = new Dictionary<GridCoord, int>();
        readonly HashSet<GridCoord> metOpen = new HashSet<GridCoord>();
        // Each zone's key cell (PlaceKey's search, or the carve's move), memoised.
        readonly Dictionary<GridCoord, GridCoord> keyCells = new Dictionary<GridCoord, GridCoord>();
        // Card 4: how many receding rooms this floor holds (HeldShut and IsHiddenCell answer at once without one).
        int receding;

        public FrontRoomsRunRooms(FrontRoomsRunRules rules) { this.rules = (rules ?? new FrontRoomsRunRules()).Clone(); }

        public FrontRoomsRunRules Rules => rules;

        // ---------- What the world sets (FrontRoomsMapWorld) ----------

        /// <summary>The map is set up (Begin): zones met before it (the EXIT and start-group planning) never get a room.</summary>
        public bool Ready { get; set; }
        /// <summary>The run's first send has gone out (A40). Kept by the world for the run; the game raises it.</summary>
        public bool Open { get; set; }
        /// <summary>The floor's start door cell (RELAY 6's 15 cells) and its zone, the arrival zone (P2).</summary>
        public bool HasStart { get; private set; }
        public GridCoord StartDoor { get; private set; }
        public GridCoord ArrivalZone { get; private set; }
        /// <summary>Cells no corridor, entrance cell or goal cell may use: the start area, X, the station and a ring round each.</summary>
        public Func<GridCoord, bool> Blocked { get; set; }
        /// <summary>Card 2: the floor this planner plans (1 = LEVEL 0, 2 = B1, 3 = B2 ...): the deck's floor gates and its history.</summary>
        public int Floor { get; set; } = 1;
        /// <summary>Card 2: the run's deck (the world's, kept through NextFloor). Set it before the cache attaches the planner; with
        /// none, Attach makes one on the floor's seed (the validator, tools).</summary>
        public FrontRoomsRunDeck Deck
        {
            get => deck;
            set => deck = value;
        }

        /// <summary>Card 2: the deck deals (one Run! a floor); false per zone (rules.deck off, or force).</summary>
        public bool DeckMode => rules.deck && !rules.force;

        public void SetStart(GridCoord doorCell)
        {
            HasStart = true;
            StartDoor = doorCell;
            ArrivalZone = gen != null ? gen.ZoneOf(doorCell).id : doorCell;
        }

        internal void Attach(FrontRoomsMapCache owner)
        {
            cache = owner;
            gen = owner.Generator;
            budget = owner.UseBudget(rules.zonesPerDevice);
            if (deck == null) deck = new FrontRoomsRunDeck(gen.Seed);
            if (HasStart) ArrivalZone = gen.ZoneOf(StartDoor).id;
        }

        /// <summary>The map's one trigger budget (V7a: shared with foiled windows), or null before the planner is attached.</summary>
        public FrontRoomsTriggerBudget Budget => budget;

        // ---------- Counts (the validator and the logs) ----------

        public int ZonesDecided => decided.Count;
        public int NotReady { get; private set; }
        public int Ineligible { get; private set; }
        public int NoFit { get; private set; }
        public int Suppressed { get; private set; }
        /// <summary>Card 2: zones met after the floor's card was dealt (a room placed, or nothing).</summary>
        public int Spent { get; private set; }
        /// <summary>Card 3: zones whose room was refused because its goal door would open into a zone with a decided device (BeyondHoldsDevice).</summary>
        public int CalmRefused { get; private set; }
        public int Placed => rooms.Count;

        /// <summary>Raised when a zone's room is planned (before any of its chunks is generated): the world logs it.</summary>
        public event Action<FrontRoomsRunRoom> Planned;
        /// <summary>Card 2: the floor's card was dealt (a room, or nothing): the world logs it.</summary>
        public event Action<RunDeal> Dealt;

        // ---------- Queries ----------

        public IReadOnlyList<FrontRoomsRunRoom> Rooms => rooms;
        public IReadOnlyDictionary<long, EdgeKind> BorderOverride => borderOverride;
        public bool IsRunCell(GridCoord cell) => byCell.ContainsKey(cell);
        public FrontRoomsRunRoom RoomAt(GridCoord cell) => byCell.TryGetValue(cell, out var r) ? r : null;
        public bool IsGoalEdge(GridCoord a, GridCoord b) => goalEdges.ContainsKey(MapGrid.EdgeId(a, b));
        public FrontRoomsRunRoom GoalRoom(GridCoord a, GridCoord b) => goalEdges.TryGetValue(MapGrid.EdgeId(a, b), out var r) ? r : null;
        public bool IsArchEdge(GridCoord a, GridCoord b) => archEdges.ContainsKey(MapGrid.EdgeId(a, b));
        /// <summary>Card 5: the edge is a Run door room's entrance door (RN-F + F5, never locked).</summary>
        public bool IsEntranceDoorEdge(GridCoord a, GridCoord b) => entranceDoors.ContainsKey(MapGrid.EdgeId(a, b));
        /// <summary>M8 and card 5: the edge is a Run door (a goal door, or a Run door room's entrance).</summary>
        public bool IsRunDoorEdge(GridCoord a, GridCoord b) => IsGoalEdge(a, b) || IsEntranceDoorEdge(a, b);
        /// <summary>The room a Run door edge belongs to, or null.</summary>
        public FrontRoomsRunRoom RunDoorRoom(GridCoord a, GridCoord b)
        {
            var id = MapGrid.EdgeId(a, b);
            return goalEdges.TryGetValue(id, out var r) ? r : entranceDoors.TryGetValue(id, out r) ? r : null;
        }
        public IReadOnlyList<FrontRoomsRunRoom> RoomsIn(GridCoord chunk) => byChunk.TryGetValue(chunk, out var list) ? list : (IReadOnlyList<FrontRoomsRunRoom>)Array.Empty<FrontRoomsRunRoom>();
        public bool Decided(GridCoord zoneId, out FrontRoomsRunRoom room) => decided.TryGetValue(zoneId, out room);

        /// <summary>Card 3: a placed room's host zone or the zone beyond its goal door (no other trigger kind there).</summary>
        public bool IsCalmZone(GridCoord zoneId) => rules.calm && calmZones.Contains(zoneId);

        /// <summary>Card 3: the edge a-b touches a calm zone (either cell's zone): no foiled window and no alarm door on it.</summary>
        public bool CalmTouches(GridCoord a, GridCoord b) => rules.calm && calmZones.Count > 0 && gen != null && (calmZones.Contains(gen.ZoneOf(a).id) || calmZones.Contains(gen.ZoneOf(b).id));

        /// <summary>
        /// Card 4: the edge a-b is shut by a receding room's state: an edge touching one of its cells past the end's line (the
        /// head's line, the hidden length), its goal door's edge before the last seat included. False for every other edge, and
        /// at once with no receding room. The world's passage reads it, so it shuts the Relay's graph, the autopilot and the
        /// path field only: the hidden cells' colliders and walk volume, the blind panel in the L_max frame (71 s.4.6, the
        /// release side) and the goal door on the head's DoorMount are not built yet; they wait for 游戏视觉's recede rig in main,
        /// and until then the level profile cannot switch the receding end on (FrontRoomsLevelProfile.runRecede).
        /// </summary>
        public bool HeldShut(GridCoord a, GridCoord b)
        {
            if (receding == 0) return false;
            var room = RoomAt(a) ?? RoomAt(b);
            if (room == null || !room.Receding || room.Seated) return false;
            var open = room.OpenCells;
            return room.IndexOf(a) >= open || room.IndexOf(b) >= open;
        }

        /// <summary>Card 4: the cell is a receding room's cell past its end's line (hidden: off the Relay's graph until its seat; no walk once the recede rig is in).</summary>
        public bool IsHiddenCell(GridCoord cell)
        {
            if (receding == 0) return false;
            var room = RoomAt(cell);
            return room != null && room.Receding && !room.Seated && room.IndexOf(cell) >= room.OpenCells;
        }

        /// <summary>
        /// Card 4: the receding end seated on <paramref name="lineCells"/> (one of its SeatCells past the current one; the rig's
        /// SeatReached). The cells up to it open; at the last seat the goal door is real. False (nothing changed) for a room
        /// that does not recede, a line that is not a later seat, or a room already seated: a seat never goes back.
        /// </summary>
        public bool Seat(FrontRoomsRunRoom room, int lineCells)
        {
            if (room == null || !room.Receding || room.Seated) return false;
            var index = Array.IndexOf(room.SeatCells, lineCells);
            if (index <= room.SeatIndex) return false;
            room.SeatIndex = index;
            return true;
        }

        /// <summary>Card 2: the player met <paramref name="room"/> (stood in it or at one of its openings): the run flag is raised.</summary>
        public void MarkMet(FrontRoomsRunRoom room)
        {
            if (room == null || room.Met) return;
            room.Met = true;
            deck?.MarkMet();
        }

        /// <summary>The entrance arch's span (CR-1): 1.4 m, centred on its edge (never for a Run door room: its entrance is a door).</summary>
        public bool TryArch(GridCoord a, GridCoord b, out float width, out float center)
        {
            if (archEdges.ContainsKey(MapGrid.EdgeId(a, b))) { width = ArchWidth; center = MapGrid.CellSize * .5f; return true; }
            width = center = 0f;
            return false;
        }

        /// <summary>The zone's budget roll in [0, 1): pure in the seed and the zone (V7a: the shared budget's, the same number).</summary>
        public float Roll(GridCoord zoneId) => budget != null ? budget.Roll(zoneId) : MapHash.Unit(MapHash.Hash(gen.Seed, zoneId.x, zoneId.y, BudgetSalt));

        // V7a: the chance at a tier from the shared budget (the same table and arithmetic as the rules'); forced: 1.
        float ChanceAt(int tier) => rules.force ? 1f : budget != null ? budget.ChanceAt(tier) : rules.ChanceAt(tier);
        float MaxChance => rules.force ? 1f : budget != null ? budget.MaxChance : rules.MaxChance;

        /// <summary>The zone's priority for a shared chunk: the lower wins (ties: the lower zone id).</summary>
        public uint Priority(GridCoord zoneId) => MapHash.Hash(gen.Seed, zoneId.x, zoneId.y, PrioritySalt);

        // ---------- The decision (CR-5, card 2) ----------

        /// <summary>The cache met a zone for the first time, at <paramref name="tier"/>: decide its Run! room once.</summary>
        public void Meet(GridCoord zoneId, int tier)
        {
            if (gen == null || decided.ContainsKey(zoneId)) return;
            metTier[zoneId] = tier;
            if (Ready && (Open || rules.force)) metOpen.Add(zoneId);
            FrontRoomsRunRoom room = null;
            if (!Ready) NotReady++;
            else if (!Eligible(zoneId, tier)) Ineligible++;
            else if (DeckMode) room = FromDeck(zoneId, tier);
            else room = PerZone(zoneId, tier);
            decided[zoneId] = room;
        }

        // Per zone (tools, checks): forceCard's room or today's plain 9, exactly; the chunk rule between candidates (Priority).
        FrontRoomsRunRoom PerZone(GridCoord zoneId, int tier)
        {
            var card = rules.forceCard.IsNothing ? RunCard.Plain(Length) : rules.forceCard;
            var c = Candidate(zoneId, card.cells);
            if (c == null) { NoFit++; return null; }
            if (SuppressedBy(c) != null) { Suppressed++; return null; }
            if (!ZonesStillOk(c)) { Ineligible++; return null; }
            if (BeyondHoldsDevice(c)) { CalmRefused++; return null; }
            return Place(c, tier, card, card);
        }

        // Card 2: the floor's card at this zone's tier (forceCard for tools), placed at the first length that fits (FitOrder).
        // Nothing spends the floor at once; a card that fits nowhere here (or only into a zone beyond with a decided device,
        // card 3) waits for the floor's next eligible zone.
        FrontRoomsRunRoom FromDeck(GridCoord zoneId, int tier)
        {
            if (deck.Dealt(Floor)) { Spent++; return null; }
            RunCard top, played;
            string why;
            if (rules.forceCard.IsNothing) played = deck.Peek(Floor, tier, rules, out top, out why);
            else
            {
                top = played = rules.forceCard;
                why = "forced (tools)";
            }
            if (played.IsNothing)
            {
                var none = deck.Commit(Floor, tier, top, played, RunCard.Nothing, why, null, 0);
                Dealt?.Invoke(none);
                return null;
            }
            var refused = false;
            foreach (var card in FitOrder(played))
            {
                var c = Candidate(zoneId, card.cells);
                if (c == null || !ZonesStillOk(c)) continue;
                if (BeyondHoldsDevice(c)) { refused = true; continue; }
                var room = Place(c, tier, card, played);
                var deal = deck.Commit(Floor, tier, top, played, card, why, card == played ? null : "fitted as " + card, room.Id);
                Dealt?.Invoke(deal);
                return room;
            }
            if (refused) CalmRefused++; else NoFit++;
            return null;
        }

        /// <summary>
        /// Card 3: a foiled window or an alarm door is already decided on an edge touching the zone beyond the candidate's goal
        /// door (FrontRoomsFoil.Touches, FrontRoomsAlarmDoors.Touches). That zone may have been walked before the host zone was
        /// met, its device built and armed; placed, the room would make the zone calm under a device that still trips, or (were it
        /// disarmed) leave a harmless detector prop, which RELAY 6 forbids. So the room is not placed there. The host zone needs
        /// no such test: a chunk build meets both zones of every edge first, so no edge touching it is built before it is
        /// decided. False with the calm rule off or with neither device planner.
        /// </summary>
        public bool BeyondHoldsDevice(FrontRoomsRunRoom c)
        {
            if (!rules.calm || c == null || cache == null || gen == null) return false;
            if (cache.Foil == null && cache.AlarmDoors == null) return false;
            var beyond = gen.ZoneOf(c.Beyond).id;
            return (cache.Foil != null && cache.Foil.Touches(beyond)) || (cache.AlarmDoors != null && cache.AlarmDoors.Touches(beyond));
        }

        /// <summary>
        /// CR-5 for every zone the candidate's cells touch, as the run stands now: none is the arrival zone, and each was either
        /// not met yet (it will be met at today's tier or later, after the first send) or met at minTier+ after the first send.
        /// </summary>
        bool ZonesStillOk(FrontRoomsRunRoom c)
        {
            foreach (var z in c.Zones)
            {
                if (HasStart && z == ArrivalZone) return false;
                if (z == c.Zone || !metTier.TryGetValue(z, out var t)) continue;
                if (!metOpen.Contains(z)) return false;
                if (!rules.force && t < rules.minTier) return false;
            }
            return true;
        }

        /// <summary>The tier a zone was met at and whether the first send had gone out then (false: not met yet).</summary>
        public bool MetAt(GridCoord zoneId, out int tier, out bool afterFirstSend)
        {
            afterFirstSend = metOpen.Contains(zoneId);
            return metTier.TryGetValue(zoneId, out tier);
        }

        bool Eligible(GridCoord zoneId, int tier)
        {
            if (!rules.force && (!Open || tier < rules.minTier)) return false;
            if (HasStart && zoneId == ArrivalZone) return false;
            if (!Competes(zoneId)) return false;
            // V7a: one device per slot: a zone whose slot another device holds gets no room (a foil only ever takes a Tall zone's).
            if (budget != null && budget.HolderOf(zoneId) != TriggerDeviceKind.None) return false;
            return Roll(zoneId) < ChanceAt(tier);
        }

        // The pure half of eligibility: Standard, Level 0 (or Office with call 5), and a roll under the best chance at any tier.
        bool Competes(GridCoord zoneId)
        {
            var zone = gen.Zone(zoneId);
            if (zone.height != ZoneHeight.Standard) return false;
            if (zone.theme == ZoneTheme.Office && !rules.office) return false;
            // A Run! is a room inside a Level 0 or Office zone (71 s.3.4): never in a pool family zone.
            if (zone.theme != ZoneTheme.Level0 && zone.theme != ZoneTheme.Office) return false;
            return Roll(zoneId) < MaxChance;
        }

        /// <summary>Card 1's fit check (the validator): the zone competes for a Run! room at all (Standard, an allowed theme, its roll under the best chance).</summary>
        public bool CompetesForTools(GridCoord zoneId) => gen != null && Competes(zoneId);

        /// <summary>A higher-priority zone whose candidate shares a chunk with <paramref name="c"/>, or null (per zone only).</summary>
        FrontRoomsRunRoom SuppressedBy(FrontRoomsRunRoom c)
        {
            var mine = Priority(c.Zone);
            var seen = new HashSet<GridCoord>();
            foreach (var chunk in new[] { c.ChunkA, c.ChunkB })
            {
                var o = MapGrid.ChunkOrigin(chunk);
                for (var j = 0; j < N; j++)
                for (var i = 0; i < N; i++)
                {
                    var other = gen.ZoneOf(new GridCoord(o.x + i, o.y + j)).id;
                    if (other == c.Zone || !seen.Add(other)) continue;
                    var oc = Candidate(other, c.LengthCells);
                    if (oc == null || (oc.ChunkA != c.ChunkA && oc.ChunkA != c.ChunkB && oc.ChunkB != c.ChunkA && oc.ChunkB != c.ChunkB)) continue;
                    var theirs = Priority(other);
                    if (theirs < mine || (theirs == mine && (other.x < c.Zone.x || (other.x == c.Zone.x && other.y < c.Zone.y)))) return oc;
                }
            }
            return null;
        }

        FrontRoomsRunRoom Place(FrontRoomsRunRoom c, int tier, RunCard card, RunCard dealt)
        {
            var room = new FrontRoomsRunRoom
            {
                Id = rooms.Count + 1, Zone = c.Zone, Zones = c.Zones, Cells = c.Cells, AxisX = c.AxisX, AxisY = c.AxisY, Outside = c.Outside, Beyond = c.Beyond,
                ChunkA = c.ChunkA, ChunkB = c.ChunkB, Tier = tier, BeyondHeight = c.BeyondHeight, Theme = c.Theme,
                Variant = card.variant, Card = dealt, Floor = Floor, DoorTrip = rules.doorTripDepth,
                SeatCells = card.variant == RunVariant.Recede ? SeatsOf(card.from, card.cells) : null,
            };
            rooms.Add(room);
            if (room.Receding) receding++;
            // V7a: the room takes its zone's slot in the shared budget's ledger.
            budget?.Take(room.Zone, TriggerDeviceKind.Run);
            foreach (var cell in room.Cells) byCell[cell] = room;
            Index(room.ChunkA, room);
            if (room.ChunkB != room.ChunkA) Index(room.ChunkB, room);
            for (var k = 0; k + 1 < room.LengthCells; k++)
            {
                var a = room.Cells[k];
                var b = room.Cells[k + 1];
                if (MapGrid.ChunkOf(a) != MapGrid.ChunkOf(b)) borderOverride[MapGrid.EdgeId(a, b)] = EdgeKind.Open;
            }
            if (room.EntranceDoor) entranceDoors[MapGrid.EdgeId(room.Outside, room.Cells[0])] = room;
            else archEdges[MapGrid.EdgeId(room.Outside, room.Cells[0])] = room;
            goalEdges[MapGrid.EdgeId(room.GoalCell, room.Beyond)] = room;
            // Card 3: the calm zones (the host's, the zone beyond the goal door).
            calmZones.Add(room.Zone);
            calmZones.Add(gen.ZoneOf(room.Beyond).id);
            Planned?.Invoke(room);
            return room;
        }

        void Index(GridCoord chunk, FrontRoomsRunRoom room)
        {
            if (!byChunk.TryGetValue(chunk, out var list)) byChunk[chunk] = list = new List<FrontRoomsRunRoom>();
            list.Add(room);
        }

        // ---------- The candidate (CR-1, card 1; pure in the seed, the zone, the length, the rules and Blocked) ----------

        /// <summary>The zone's one 9-cell candidate straight (memoised), or null: none fits, or the zone does not compete.</summary>
        public FrontRoomsRunRoom Candidate(GridCoord zoneId) => Candidate(zoneId, Length);

        /// <summary>The zone's one candidate straight of <paramref name="cells"/> (7 .. 14; memoised), or null: none fits, the zone
        /// does not compete, or the length is out of range. At 9 cells the same pick as before the rethink.</summary>
        public FrontRoomsRunRoom Candidate(GridCoord zoneId, int cells)
        {
            if (cells < MinCells || cells > CapCells) return null;
            var memo = candidates[cells] ?? (candidates[cells] = new Dictionary<GridCoord, FrontRoomsRunRoom>());
            if (memo.TryGetValue(zoneId, out var known)) return known;
            FrontRoomsRunRoom result = null;
            if (Competes(zoneId))
            {
                var zoneCells = new List<GridCoord>();
                KeyCellOf(zoneId, zoneCells);
                var found = new List<FrontRoomsRunRoom>();
                // The entrance's outside cell is in the host zone: every straight starts one step from one of its cells.
                foreach (var o in zoneCells)
                {
                    Consider(zoneId, new GridCoord(o.x + 1, o.y), 1, 0, false, cells, found);
                    Consider(zoneId, new GridCoord(o.x, o.y + 1), 0, 1, false, cells, found);
                    Consider(zoneId, new GridCoord(o.x - cells, o.y), 1, 0, true, cells, found);
                    Consider(zoneId, new GridCoord(o.x, o.y - cells), 0, 1, true, cells, found);
                }
                if (found.Count > 0)
                    result = found[(int)(MapHash.Hash(gen.Seed, zoneId.x, zoneId.y, PickSalt, cells == Length ? 0 : cells) % (uint)found.Count)];
            }
            memo[zoneId] = result;
            return result;
        }

        static int Mod(int v) => ((v % N) + N) % N;

        bool IsBlocked(GridCoord cell) => Blocked != null && Blocked(cell);

        int StartDistance(GridCoord cell) => Math.Abs(cell.x - StartDoor.x) + Math.Abs(cell.y - StartDoor.y);

        // The straight of <cells> whose low cell is l along (ax, ay); entered at its low end (from l - axis) or, reversed, at its
        // high end (from l + cells axis). Adds it when it passes.
        void Consider(GridCoord zoneId, GridCoord l, int ax, int ay, bool reversed, int cells, List<FrontRoomsRunRoom> found)
        {
            // Long sides inside a chunk; both ends off a chunk border (chunk-local 1..6 along the axis) with the high end in the
            // next chunk along, so the straight crosses exactly one chunk border (71 s.3.2 (a): 4 .. 14 cells).
            var perp = ax == 1 ? Mod(l.y) : Mod(l.x);
            var along = ax == 1 ? Mod(l.x) : Mod(l.y);
            if (perp < 1 || perp > N - 2 || along < 1 || along > N - 2) return;
            var reach = along + cells - 1;
            if (reach < N || reach >= 2 * N || Mod(reach) < 1 || Mod(reach) > N - 2) return;
            var host = gen.Zone(zoneId);
            var straight = new GridCoord[cells];
            var zones = new List<GridCoord> { zoneId };
            for (var k = 0; k < cells; k++)
            {
                var c = new GridCoord(l.x + ax * k, l.y + ay * k);
                var z = gen.ZoneOf(c);
                // One space (as a carved room across zones): every cell Standard and of the host's theme.
                if (z.height != ZoneHeight.Standard || z.theme != host.theme || IsBlocked(c)) return;
                // RELAY 6: no zone's key lies in it.
                if (KeyCell(z.id) == c) return;
                if (HasStart && StartDistance(c) < rules.minStartCells) return;
                // No glass and no C33 arch on a long side: a Tall neighbour there would be walled.
                var s1 = new GridCoord(c.x + ay, c.y + ax);
                var s2 = new GridCoord(c.x - ay, c.y - ax);
                if (gen.HeightOf(s1) == ZoneHeight.Tall || gen.HeightOf(s2) == ZoneHeight.Tall) return;
                if (!zones.Contains(z.id)) zones.Add(z.id);
                straight[k] = c;
            }
            var low = new GridCoord(l.x - ax, l.y - ay);
            var high = new GridCoord(l.x + ax * cells, l.y + ay * cells);
            var outside = reversed ? high : low;
            var beyond = reversed ? low : high;
            if (gen.ZoneOf(outside).id != zoneId || IsBlocked(outside)) return;
            if (HasStart && StartDistance(outside) < rules.minStartCells) return;
            if (IsBlocked(beyond)) return;
            var far = gen.ZoneOf(beyond);
            if (far.height == ZoneHeight.Tall) return;
            // Call 2 on (6a): the forced door opens onto any Standard or Low cell. Off (6b): a Standard-to-Low border only.
            if (!rules.goalDoorForced && far.height != ZoneHeight.Low) return;
            var ordered = new GridCoord[cells];
            for (var k = 0; k < cells; k++) ordered[k] = reversed ? straight[cells - 1 - k] : straight[k];
            var chunkA = MapGrid.ChunkOf(ordered[0]);
            var chunkB = MapGrid.ChunkOf(ordered[cells - 1]);
            // Both chunks hold a host cell, so neither is generated before the host zone is met (and decided): chunk A holds the
            // entrance's outside cell; chunk B is checked.
            if (!HoldsZone(chunkB, zoneId)) return;
            found.Add(new FrontRoomsRunRoom
            {
                Zone = zoneId, Zones = zones.ToArray(), Cells = ordered, AxisX = reversed ? -ax : ax, AxisY = reversed ? -ay : ay, Outside = outside, Beyond = beyond,
                ChunkA = chunkA, ChunkB = chunkB, BeyondHeight = far.height, Theme = host.theme,
            });
        }

        bool HoldsZone(GridCoord chunk, GridCoord zoneId)
        {
            var o = MapGrid.ChunkOrigin(chunk);
            for (var j = 0; j < N; j++)
            for (var i = 0; i < N; i++)
                if (gen.ZoneOf(new GridCoord(o.x + i, o.y + j)).id == zoneId) return true;
            return false;
        }

        /// <summary>A zone's key cell (memoised; a Tall zone, which has no key, reads a cell no corridor can hold).</summary>
        public GridCoord KeyCell(GridCoord zoneId)
        {
            if (keyCells.TryGetValue(zoneId, out var key)) return key;
            key = gen.Zone(zoneId).height == ZoneHeight.Tall ? new GridCoord(int.MinValue, int.MinValue) : KeyCellOf(zoneId, null);
            keyCells[zoneId] = key;
            return key;
        }

        /// <summary>
        /// The zone's key cell (PlaceKey's search: the zone cell nearest its site over the 5 x 5 chunks round it, the first in
        /// scan order on a tie), or the carve's move for it (A26); fills <paramref name="zoneCells"/> (when given) with every cell of the zone
        /// in that window, in scan order.
        /// </summary>
        GridCoord KeyCellOf(GridCoord zoneId, List<GridCoord> zoneCells)
        {
            var zone = gen.Zone(zoneId);
            var best = float.MaxValue;
            var key = new GridCoord(int.MinValue, int.MinValue);
            for (var y = (zoneId.y - 2) * N; y < (zoneId.y + 3) * N; y++)
            for (var x = (zoneId.x - 2) * N; x < (zoneId.x + 3) * N; x++)
            {
                var cell = new GridCoord(x, y);
                if (gen.ZoneOf(cell).id != zoneId) continue;
                zoneCells?.Add(cell);
                var ex = (x + .5f) * MapGrid.CellSize - zone.siteX;
                var ez = (y + .5f) * MapGrid.CellSize - zone.siteZ;
                var d = ex * ex + ez * ez;
                if (d >= best) continue;
                best = d;
                key = cell;
            }
            if (cache != null && cache.CarveKeyMove(zoneId, out var moved)) key = moved;
            return key;
        }

        // ---------- Apply (FrontRoomsMapCache.Get, after the carve) ----------

        /// <summary>Write every room planned through this freshly generated chunk into it (see the class summary).</summary>
        public void Apply(MapChunk chunk)
        {
            if (chunk == null || !byChunk.TryGetValue(chunk.coord, out var list) || list.Count == 0) return;
            var origin = chunk.Origin;
            var mine = new bool[N * N];
            foreach (var room in list)
                foreach (var c in room.Cells)
                {
                    int i = c.x - origin.x, j = c.y - origin.y;
                    if (i >= 0 && i < N && j >= 0 && j < N) mine[MapGrid.LocalIndex(i, j)] = true;
                }
            DropRooms(chunk, mine);
            foreach (var room in list)
            {
                for (var k = 0; k < room.LengthCells; k++)
                {
                    var c = room.Cells[k];
                    // The long sides.
                    var px = room.AxisY != 0 ? 1 : 0;
                    var py = room.AxisX != 0 ? 1 : 0;
                    SetEdge(chunk, c, new GridCoord(c.x + px, c.y + py), EdgeKind.Wall);
                    SetEdge(chunk, c, new GridCoord(c.x - px, c.y - py), EdgeKind.Wall);
                    if (k + 1 < room.LengthCells) SetEdge(chunk, c, room.Cells[k + 1], EdgeKind.Open);
                    int i = c.x - origin.x, j = c.y - origin.y;
                    if (i >= 0 && i < N && j >= 0 && j < N) chunk.lamp[MapGrid.LocalIndex(i, j)] = ModuleLamp.Off;
                }
                // Card 5: a Run door room's entrance is a door (RN-F + F5 by the world's member hook); every other room's the arch.
                SetEdge(chunk, room.Outside, room.Cells[0], room.EntranceDoor ? EdgeKind.Door : EdgeKind.Arch);
                SetEdge(chunk, room.GoalCell, room.Beyond, EdgeKind.Door);
            }
            Reconnect(chunk, mine);
        }

        // Write an edge's kind into this chunk's copy of it (its east/north arrays, or west/south for a border it shares).
        static void SetEdge(MapChunk chunk, GridCoord a, GridCoord b, EdgeKind kind)
        {
            if (b.x < a.x || b.y < a.y) { var t = a; a = b; b = t; }
            var east = b.x == a.x + 1;
            var o = chunk.Origin;
            int ai = a.x - o.x, aj = a.y - o.y, bi = b.x - o.x, bj = b.y - o.y;
            if (ai >= 0 && ai < N && aj >= 0 && aj < N)
            {
                if (east) chunk.east[MapGrid.LocalIndex(ai, aj)] = kind;
                else chunk.north[MapGrid.LocalIndex(ai, aj)] = kind;
            }
            else if (bi >= 0 && bi < N && bj >= 0 && bj < N)
            {
                if (east && bi == 0) chunk.west[bj] = kind;
                else if (!east && bj == 0) chunk.south[bi] = kind;
            }
        }

        // Every room over a corridor cell leaves rooms[] (as the carve's roomDrop): columns and the key spot again, and a dropped
        // module room's lamps go back to the last module room left over the cell, else Auto (the corridor's own go Off after).
        void DropRooms(MapChunk chunk, bool[] mine)
        {
            List<CellRect> kept = null;
            List<RoomModuleData> keptModules = null;
            List<CellRect> droppedModules = null;
            for (var r = 0; r < chunk.rooms.Length; r++)
            {
                var rect = chunk.rooms[r];
                var hit = false;
                for (var j = rect.y; j < rect.y + rect.h && !hit; j++)
                for (var i = rect.x; i < rect.x + rect.w && !hit; i++)
                    hit = mine[MapGrid.LocalIndex(i, j)];
                if (hit && kept == null)
                {
                    kept = new List<CellRect>();
                    keptModules = new List<RoomModuleData>();
                    droppedModules = new List<CellRect>();
                    for (var k = 0; k < r; k++) { kept.Add(chunk.rooms[k]); keptModules.Add(chunk.ModuleOf(k)); }
                }
                if (hit) { if (chunk.ModuleOf(r) != null) droppedModules.Add(rect); }
                else if (kept != null) { kept.Add(rect); keptModules.Add(chunk.ModuleOf(r)); }
            }
            if (kept == null) return;
            chunk.rooms = kept.ToArray();
            chunk.roomModules = keptModules.ToArray();
            gen.PlaceColumns(chunk, chunk.revision);
            gen.PlaceKeySpot(chunk);
            foreach (var rect in droppedModules)
                for (var j = rect.y; j < rect.y + rect.h; j++)
                for (var i = rect.x; i < rect.x + rect.w; i++)
                    chunk.lamp[MapGrid.LocalIndex(i, j)] = KeptLamp(chunk, i, j);
        }

        static ModuleLamp KeptLamp(MapChunk chunk, int i, int j)
        {
            for (var r = chunk.rooms.Length - 1; r >= 0; r--)
            {
                var module = chunk.ModuleOf(r);
                var rect = chunk.rooms[r];
                if (module != null && rect.Contains(i, j)) return module.LampAt(i - rect.x, j - rect.y);
            }
            return ModuleLamp.Auto;
        }

        /// <summary>
        /// Join the chunk's other cells into one piece without the corridor (RELAY 6: a trigger room is never the only way
        /// round): each round opens the first generated wall (scan order) between two pieces, glass last; between two heights it
        /// opens as the map's exit (a door, or a window next to a tall zone). The corridor's own edges are never opened. Always
        /// possible: the corridor stays a chunk-local 1..6 off its long sides and leaves a free row past each end in its chunk.
        /// </summary>
        void Reconnect(MapChunk chunk, bool[] mine)
        {
            var parent = new int[N * N];
            for (var round = 0; round < N * N; round++)
            {
                for (var k = 0; k < parent.Length; k++) parent[k] = k;
                int Find(int k) { while (parent[k] != k) k = parent[k] = parent[parent[k]]; return k; }
                for (var j = 0; j < N; j++)
                for (var i = 0; i < N; i++)
                {
                    var k = MapGrid.LocalIndex(i, j);
                    if (mine[k]) continue;
                    if (i < N - 1 && !mine[k + 1] && MapGrid.Passable(chunk.east[k])) parent[Find(k)] = Find(k + 1);
                    if (j < N - 1 && !mine[k + N] && MapGrid.Passable(chunk.north[k])) parent[Find(k)] = Find(k + N);
                }
                var root = -1;
                var joined = true;
                for (var k = 0; k < parent.Length && joined; k++)
                {
                    if (mine[k]) continue;
                    if (root < 0) root = Find(k);
                    else joined = Find(k) == root;
                }
                if (joined) return;
                var best = -1;
                var bestGlass = true;
                for (var j = 0; j < N && (best < 0 || bestGlass); j++)
                for (var i = 0; i < N && (best < 0 || bestGlass); i++)
                {
                    var k = MapGrid.LocalIndex(i, j);
                    if (mine[k]) continue;
                    for (var e = 0; e < 2; e++)
                    {
                        var other = e == 0 ? (i < N - 1 ? k + 1 : -1) : (j < N - 1 ? k + N : -1);
                        if (other < 0 || mine[other] || Find(k) == Find(other)) continue;
                        if ((e == 0 ? chunk.east[k] : chunk.north[k]) != EdgeKind.Wall) continue;
                        var glass = Kind(chunk, k, e == 0) == EdgeKind.Window;
                        if (best < 0 || (bestGlass && !glass)) { best = e == 0 ? k : N * N + k; bestGlass = glass; }
                    }
                }
                if (best < 0) return;
                var isEast = best < N * N;
                var a = best % (N * N);
                if (isEast) chunk.east[a] = Kind(chunk, a, true); else chunk.north[a] = Kind(chunk, a, false);
            }
        }

        // The map's exit between two side-by-side cells of the chunk: an arch at one height, else a door, or a window next to a tall zone.
        EdgeKind Kind(MapChunk chunk, int index, bool east)
        {
            var a = chunk.Cell(index % N, index / N);
            var b = east ? new GridCoord(a.x + 1, a.y) : new GridCoord(a.x, a.y + 1);
            var ha = gen.HeightOf(a);
            var hb = gen.HeightOf(b);
            return ha == hb ? EdgeKind.Arch : ha == ZoneHeight.Tall || hb == ZoneHeight.Tall ? EdgeKind.Window : EdgeKind.Door;
        }

        // ---------- The module, as data (CR-1, CR-2, CR-3, CR-4, CR-6) ----------

        /// <summary>
        /// Run_AlarmCorridor_1x9 as module data: width 1, depth 9, Standard, the host zone's theme; a Corridor (planned by the
        /// map, never placed into a carved room), trigger Alarm, finish Run, no columns, no fill, every lamp Off (the rig's),
        /// the south edge an Arch (the entrance), the north edge a Door (the goal), the long sides Wall. The planner builds
        /// exactly this; it is not in any profile's pool.
        /// </summary>
        public static RoomModuleData CorridorModule(ZoneTheme theme = ZoneTheme.Level0) => CorridorModule(theme, Length, false);

        /// <summary>Cards 1 and 5: the corridor at <paramref name="cells"/> (7 .. 14), its south edge a Run door when <paramref name="entranceDoor"/>.</summary>
        public static RoomModuleData CorridorModule(ZoneTheme theme, int cells, bool entranceDoor)
        {
            cells = Math.Max(MinCells, Math.Min(CapCells, cells));
            var m = new RoomModuleData
            {
                width = 1, depth = cells, height = ZoneHeight.Standard, theme = theme, placement = ModulePlacement.Corridor,
                trigger = ModuleTrigger.Alarm, finish = ModuleFinish.Run, columns = ModuleColumns.None, fill = ModuleFill.None,
                allowRotate = true, weight = 0f,
            };
            m.Normalize();
            m.south[0] = entranceDoor ? ModuleEdge.Door : ModuleEdge.Arch;
            m.north[0] = ModuleEdge.Door;
            for (var j = 0; j < cells; j++) m.lamps[j] = ModuleLamp.Off;
            return m;
        }
    }
}
