#if FRONTROOMS_RELAY_V2
using FrontRooms.Map;
using UnityEngine;

/// <summary>What the Relay did with the last noise made (FrontRoomsMapHunter.LastHeard, tools and level-design research's report).</summary>
public enum FrontRoomsRelayHeard : byte
{
    /// <summary>No noise yet.</summary>
    None,
    /// <summary>Deaf: off the map, in a chase with sight, FADE's one look spent, or a noise made inside the stairwell.</summary>
    Deaf,
    /// <summary>Heard test failed: path d over loudness x hearing.</summary>
    OutOfRange,
    /// <summary>In a door break: held until the break ends, then taken like any noise.</summary>
    Queued,
    /// <summary>A new link (none was current).</summary>
    Linked,
    /// <summary>It replaced the current link (a heard re-target).</summary>
    Retargeted,
    /// <summary>A repeat that re-targeted: k halved, the look upgraded, the estimate drawn again.</summary>
    Repeat,
    /// <summary>A repeat inside the 1.5 s limit: k halved, the look upgraded, the estimate left where it was.</summary>
    RepeatHeld,
    /// <summary>Lost to the current link: its margin under the link's, decayed.</summary>
    Outweighed,
    /// <summary>Lost to the 1.5 s re-target limit.</summary>
    Limited,
}

/// <summary>RELAY 7's sense link as a read-out (FrontRoomsMapHunter.SenseLink): what the Relay holds of a heard noise.</summary>
public readonly struct FrontRoomsRelaySenseLink
{
    /// <summary>A link is held; default otherwise.</summary>
    public readonly bool Active;
    /// <summary>The estimate: its cell, and the point it walks to (the noise's own point at k 0).</summary>
    public readonly GridCoord Cell;
    public readonly Vector3 Point;
    /// <summary>The error in cells the estimate carries now (a repeat halves it).</summary>
    public readonly int K;
    /// <summary>Metres of margin when heard (loudness x hearing - d), that margin decayed to now, and its age in seconds.</summary>
    public readonly float Margin, MarginNow, Age;
    /// <summary>Repeats heard on this link.</summary>
    public readonly int Repeats;
    /// <summary>The look at the estimate is a cautious one (false: a repeat upgraded it, or no heard look is due).</summary>
    public readonly bool Cautious;

    public FrontRoomsRelaySenseLink(GridCoord cell, Vector3 point, int k, float margin, float marginNow, float age, int repeats, bool cautious)
    {
        Active = true;
        Cell = cell;
        Point = point;
        K = k;
        Margin = margin;
        MarginNow = marginNow;
        Age = age;
        Repeats = repeats;
        Cautious = cautious;
    }

    public override string ToString() => !Active ? "no link"
        : "link " + Cell + " k " + K + ", margin " + MarginNow.ToString("0.0") + " m of " + Margin.ToString("0.0") + ", " + Age.ToString("0.0") + " s old, "
            + Repeats + " repeats, " + (Cautious ? "cautious look" : "full search");
}

/// <summary>
/// Relay v2 step V4a (RELAY_PURSUIT_REDESIGN sections 5, 6, 7, 11), compiled only with FRONTROOMS_RELAY_V2: hearing.
/// Noise(point, radius) keeps its signature and its heard test (path d at most loudness x hearing: F when the source is
/// the player's cell and the range is inside F's 45 m, else a one-off source field rooted at an opening's two cells), and
/// what it heard is now a sense link {estimate cell, time, margin = loudness x hearing - d} instead of a walk to the
/// exact point:
/// - Hearing is the tier's (TierV2.hearing, 1.0 to 1.3, base 1.0), never the scene's FrontRoomsHunterTuning.hearing.
/// - The estimate carries k = round(2d / (L h)) cells of error (half rounds up), +1 through a shut opening past half
///   range, at most 3 (EstimateErrorCells). k 0 is the exact point, so radius-1000 test noises stay exact.
/// - Side-correct: a one-off Relay-rooted field (earFieldV2) keeps the estimate on the source's side of every opening
///   between them (same opening cost from the Relay as the source), and puts a noise made on an opening's own line on
///   the far side of it from the Relay. Reading of RELAY 7 (no more detail exists; flagged in the V4a NOTE).
/// - Which noise wins: a new one replaces the link only if its margin is at least the link's less 1.5 m/s of its age; at
///   most one re-target per 1.5 s. Sight drops the link. The give-up beat, Withdraw, a dispatch and Away end it.
/// - Repeat: a second noise within 6 s and 2 cells of the estimate keeps the link fresh and upgrades a cautious look to the
///   full search, at once. It halves k and moves the estimate only as a re-target, so at most once per 1.5 s (ruled
///   2026-10-30: the 3.3 Hz sprint cadence must not sharpen it).
/// - The look: a lone heard noise and a dispatch end in a cautious look (V4b: RELAY 8's 1.5 s, one hidden spot, 1.2 s); a
///   lost chase in the full search.
/// - BreakDoor queues what it hears (one slot, the same margin rule) until the break ends. A chase without sight moves
///   only the last known point (never the lose meter, the state or ListenPoint). Deaf in Away, Arrive and a chase with
///   sight. FADE's one cautious look and Withdraw's half range (V3a's HearsV2, HeardV2) are kept.
/// - Seams: V5's door verbs (the game's RelayDoorLoudness); V4b's LoseTrack (HeardDoorSinceSight, the door it heard move
///   after losing sight).
/// </summary>
public sealed partial class FrontRoomsMapHunter
{
    // ---------- RELAY 7's numbers (SP) ----------

    /// <summary>Which noise wins: a link's margin fades this many metres a second; at most one re-target per RetargetSeconds.</summary>
    public const float LinkDecayPerSecond = 1.5f, RetargetSeconds = 1.5f;
    /// <summary>Repeat: a noise within this long of the link and this many cells (Chebyshev) of its estimate.</summary>
    public const float RepeatSeconds = 6f;
    public const int RepeatCells = 2;
    /// <summary>The estimate's error never passes this many cells.</summary>
    public const int MaxEstimateErrorCells = 3;
    /// <summary>On the way out it hears at this share of the range (RELAY 11).</summary>
    public const float WithdrawHearingShare = .5f;
    // Opening costs are whole metres: two cells are on one side when theirs differ by less than this.
    const float SideSlack = .5f;

    struct SenseV2
    {
        public Vector3 point;
        public GridCoord cell;
        // A noise on an opening's own line (a door, a window), and the opening's other cell.
        public bool onEdge;
        public GridCoord edgeCell;
        public int k;
        public float margin, time;
    }

    // The link.
    bool linkActiveV2;
    GridCoord linkCellV2;
    Vector3 linkPointV2;
    int linkKV2, linkRepeatsV2;
    float linkMarginV2, linkTimeV2, retargetAtV2 = float.NegativeInfinity;
    // The look at the estimate is a cautious one (a lone heard noise), not the full search.
    bool cautiousLookV2;
    // A noise moved the last known point since sight was lost (LoseTrack goes there, not through the door it saw).
    bool lastKnownHeardV2;
    // BreakDoor's slot.
    bool senseQueuedV2;
    SenseV2 queuedSenseV2;
    // V4b's seam: the last door it heard move after losing sight.
    FrontRoomsMapWorld.Door heardDoorV2;
    float heardDoorAtV2;
    // The one-off Relay-rooted field (SourceFieldCap: as far as a heard noise reaches). relayField stays the way cue's.
    readonly FrontRoomsMapPathField earFieldV2 = new FrontRoomsMapPathField(SourceFieldCap);

    /// <summary>The ears' multiplier on the v2 loudness: the tier's (1.0 to 1.3), never the scene's 1.4.</summary>
    public float HearingV2 => TierV2 != null ? TierV2.hearing : 1f;
    /// <summary>The sense link now (default: none).</summary>
    public FrontRoomsRelaySenseLink SenseLink => linkActiveV2
        ? new FrontRoomsRelaySenseLink(linkCellV2, linkPointV2, linkKV2, linkMarginV2, linkMarginV2 - LinkDecayPerSecond * (clock - linkTimeV2), clock - linkTimeV2, linkRepeatsV2, cautiousLookV2)
        : default;
    /// <summary>Where it would go if it lost the player now: where it last saw them, or the estimate of a noise heard in a chase without sight.</summary>
    public Vector3 LastKnownPoint => lastSeen;
    /// <summary>Tools: what it did with the last noise made.</summary>
    public FrontRoomsRelayHeard LastHeard { get; private set; }
    /// <summary>Tools: the last noise that passed the heard test: path metres, range (loudness x hearing, halved in Withdraw), margin, and its own k.</summary>
    public float LastHeardMetres { get; private set; }
    public float LastHeardRange { get; private set; }
    public float LastHeardMargin { get; private set; }
    public int LastHeardK { get; private set; }
    /// <summary>A noise waits in BreakDoor's slot.</summary>
    public bool SenseQueued => senseQueuedV2;
    /// <summary>Links taken this encounter (the first included), from its dispatch to the next (a handoff keeps counting); stays readable after the Away.</summary>
    public int HeardLinks { get; private set; }
    /// <summary>Heard re-targets this encounter (a noise that replaced a current link), counted as HeardLinks.</summary>
    public int HeardRetargets { get; private set; }
    /// <summary>Heard re-targets this run (Park keeps them).</summary>
    public int HeardRetargetsTotal { get; private set; }
    /// <summary>V4b's LoseTrack: the last door it heard move after losing sight (null: none since it last saw the player), and how long ago.</summary>
    public FrontRoomsMapWorld.Door HeardDoorSinceSight => heardDoorV2;
    public float HeardDoorSeconds => heardDoorV2 != null ? clock - heardDoorAtV2 : float.PositiveInfinity;

    /// <summary>
    /// RELAY 7's error in cells for a noise heard <paramref name="metres"/> along the path with <paramref name="range"/> = L x h:
    /// round(2d / range), half rounds up; +1 through a shut opening past half range; at most MaxEstimateErrorCells.
    /// </summary>
    public static int EstimateErrorCells(float metres, float range, bool throughShut)
    {
        if (range <= 0f) return 0;
        var k = Mathf.FloorToInt(2f * metres / range + .5f);
        if (throughShut && metres > range * .5f) k++;
        return Mathf.Clamp(k, 0, MaxEstimateErrorCells);
    }

    /// <summary>Tools and tests: DebugPlace, facing <paramref name="facing"/> (flat); RELAY 7: it clears the link, the meter, the gate, the stage holds and the lock-on, and the head looks forward.</summary>
    public void DebugPlace(Vector3 feet, Vector3 facing)
    {
        DebugPlace(feet);
        var flat = Flat(facing);
        if (flat.sqrMagnitude > 1e-6f) Heading = flat.normalized;
    }

    // ---------- Hearing ----------

    /// <summary>
    /// Noise under the define: the heard test on the path field, then the link, BreakDoor's slot or (a door it heard move)
    /// a BREAK licence. V1's rules for a noise from the stairwell are kept: one made inside X is never heard; the EXIT
    /// door's own is N's.
    /// </summary>
    void HearV2(Vector3 source, float loudness)
    {
        LastHeard = FrontRoomsRelayHeard.Deaf;
        if (world == null || !Released) return;
        var chasing = State == HunterState.Chase;
        var breaking = State == HunterState.BreakDoor;
        // Deaf in a chase with sight, and in the lock-on's hold; ruled for V4b: a door it sees move there counts as heard (RELAY 5).
        // RELAY 4, 11: on the way out and under FADE, one cautious look at most.
        if (LockingOn || (chasing && SightNowV2()))
        {
            SawDoorMoveV2(source);
            return;
        }
        if (!chasing && !breaking && !HearsV2()) return;
        var range = Mathf.Max(0f, loudness * HearingV2);
        if (State == HunterState.Withdraw) range *= WithdrawHearingShare;
        var sourceCell = world.CellOf(source);
        // C22 (plan v2 D3): X is no map to the Relay; the stairwell is a safe zone (Noise's v1 text).
        var fromExit = world.InExitArea(sourceCell);
        if (fromExit)
        {
            var front = world.ExitFrontCell;
            var into = Flat(world.CellCenter(sourceCell) - world.CellCenter(front)).normalized;
            if (Vector3.Dot(Flat(source - world.CrossingPoint(front, sourceCell)), into) > ExitLineSlack) return;
            sourceCell = front;
        }
        var myCell = world.CellOf(position);
        // From X the noise is N's alone: the EXIT door's other side is the stairwell, no map to root a field in. Only a noise
        // on an opening's own line (a door, a window) is rooted in both its cells (RELAY 7), never a step beside one.
        GridCoord? edgeCell = null;
        if (!fromExit) TrySourceEdge(source, sourceCell, out edgeCell);
        if (edgeCell.HasValue && Flat(world.CrossingPoint(sourceCell, edgeCell.Value) - source).magnitude > HeardDoorMetres) edgeCell = null;
        float distance, opening;
        if (range > SourceFieldCap)
        {
            // Very large-radius tool probes are intentionally broad, and exact.
            distance = opening = 0f;
        }
        else if (playerFieldReady && sourceCell == playerFieldRoot && playerFieldRevision == world.PassageRevision && range <= PlayerFieldCap)
        {
            distance = playerField.DistanceTo(myCell);
            opening = playerField.OpeningCostTo(myCell);
        }
        else
        {
            sourceField.Build(world, sourceCell, edgeCell);
            distance = sourceField.DistanceTo(myCell);
            opening = sourceField.OpeningCostTo(myCell);
        }
        if (distance > range)
        {
            LastHeard = FrontRoomsRelayHeard.OutOfRange;
            return;
        }
        var sense = new SenseV2
        {
            point = source,
            cell = sourceCell,
            onEdge = edgeCell.HasValue,
            edgeCell = edgeCell ?? sourceCell,
            k = EstimateErrorCells(distance, range, opening >= FrontRoomsMapPathField.ShutOpening),
            margin = range - distance,
            time = clock,
        };
        LastHeardMetres = distance;
        LastHeardRange = range;
        LastHeardMargin = sense.margin;
        LastHeardK = sense.k;
        // RELAY 5: a door it hears move is a BREAK licence, taken now, in a chase and a break too, won or lost.
        LicenseHeardDoorV2(source, sourceCell);
        if (breaking) QueueSenseV2(sense);
        else TakeSenseV2(sense);
    }

    // It sees the player now: the last tick's sight, and the door shut since then has not come between them.
    bool SightNowV2() => SeesPlayer && Sees(sightEye);

    /// <summary>A heard noise against the current link: RELAY 7's "which noise wins", the repeat, the re-target limit; then the walk (or, in a chase, the last known point).</summary>
    void TakeSenseV2(SenseV2 s)
    {
        var had = linkActiveV2;
        var age = had ? Mathf.Max(0f, s.time - linkTimeV2) : 0f;
        var current = had ? linkMarginV2 - LinkDecayPerSecond * age : float.NegativeInfinity;
        var repeat = had && age <= RepeatSeconds && CellsApartV2(s.cell, linkCellV2) <= RepeatCells;
        var limited = had && clock - retargetAtV2 + TimeSlack < RetargetSeconds;
        var k = s.k;
        if (repeat)
        {
            // It confirms the link, loud or faint: the link is fresh, whether or not the estimate may move yet.
            linkMarginV2 = Mathf.Max(current, s.margin);
            linkTimeV2 = s.time;
            linkRepeatsV2++;
            if (limited)
            {
                // Ruled 2026-10-30: k halves at most once per re-target, so k and the estimate wait; only the look upgrades at once.
                UpgradeLookV2();
                LastHeard = FrontRoomsRelayHeard.RepeatHeld;
                return;
            }
            k = Mathf.Min(linkKV2, s.k) / 2;
            linkKV2 = k;
        }
        else if (had)
        {
            if (s.margin < current)
            {
                LastHeard = FrontRoomsRelayHeard.Outweighed;
                return;
            }
            if (limited)
            {
                LastHeard = FrontRoomsRelayHeard.Limited;
                return;
            }
        }
        DrawEstimateV2(s, k, out var cell, out var point);
        if (!repeat)
        {
            linkMarginV2 = s.margin;
            linkTimeV2 = s.time;
            linkRepeatsV2 = 0;
            linkKV2 = k;
        }
        linkActiveV2 = true;
        linkCellV2 = cell;
        linkPointV2 = point;
        retargetAtV2 = clock;
        LastHeard = repeat ? FrontRoomsRelayHeard.Repeat : had ? FrontRoomsRelayHeard.Retargeted : FrontRoomsRelayHeard.Linked;
        HeardLinks++;
        if (had)
        {
            HeardRetargets++;
            HeardRetargetsTotal++;
        }
        if (State == HunterState.Chase)
        {
            // RELAY 7: a chase without sight moves the last known point and nothing else.
            lastSeen = point;
            lastKnownHeardV2 = true;
            return;
        }
        // RELAY 11: on the way out or under FADE this is the one cautious look.
        HeardV2();
        cautiousLookV2 = !repeat;
        // RELAY 5: its way to the sound is licensed now, from where the hunt's plan will start (HuntToward). A heard noise
        // is an investigation again, never a search after a sighting.
        LicenseRouteV2(RouteCell(world.CellOf(position)), cell);
        sightSearchV2 = false;
        ListenPoint = k <= 0 ? s.point : point;
        lastContactTime = clock;
        HuntToward(cell, point);
    }

    /// <summary>
    /// The estimate for <paramref name="s"/> with <paramref name="k"/> cells of error. k 0 and not on an opening's line: the
    /// noise's own point. Otherwise the one-off Relay-rooted field decides the side (RELAY 7): a noise on an opening's line
    /// lands in the cell on the far side of it from the Relay (it came through, or from beyond); and the error is a random
    /// cell within k steps of the source with the source's opening cost from the Relay, so it stays on the source's side of
    /// every opening between them. With no such cell, the source's own.
    /// </summary>
    void DrawEstimateV2(SenseV2 s, int k, out GridCoord cell, out Vector3 point)
    {
        cell = s.cell;
        point = s.point;
        if (k <= 0 && !s.onEdge) return;
        earFieldV2.Build(world, RouteCell(world.CellOf(position)));
        if (s.onEdge)
        {
            var near = earFieldV2.DistanceTo(s.cell);
            var far = earFieldV2.DistanceTo(s.edgeCell);
            if (EstimateCellOkV2(s.edgeCell) && !float.IsPositiveInfinity(far) && (far > near || float.IsPositiveInfinity(near))) cell = s.edgeCell;
            // The middle of that side: a body cannot stand on the opening's line (a shut door, an intact pane).
            point = world.CellCenter(cell);
        }
        if (k <= 0) return;
        var side = earFieldV2.OpeningCostTo(cell);
        if (float.IsPositiveInfinity(side)) return;
        Search(cell, k, true);
        candidates.Clear();
        foreach (var pair in depth)
            if (pair.Value > 0 && EstimateCellOkV2(pair.Key) && Mathf.Abs(earFieldV2.OpeningCostTo(pair.Key) - side) < SideSlack) candidates.Add(pair.Key);
        if (candidates.Count == 0) return;
        cell = candidates[(int)(Next() % (uint)candidates.Count)];
        point = world.CellCenter(cell);
    }

    static int CellsApartV2(GridCoord a, GridCoord b) => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

    // A repeat while it is already looking: it searches on in full where it stands (under FADE the look stays one spot).
    void UpgradeLookV2()
    {
        if (!cautiousLookV2) return;
        cautiousLookV2 = false;
        if (State == HunterState.Search && !fadingV2) BeginSearch(world.CellOf(position));
    }

    // BreakDoor's slot: the same margin rule among what it heard during the break.
    void QueueSenseV2(SenseV2 s)
    {
        if (senseQueuedV2 && s.margin < queuedSenseV2.margin - LinkDecayPerSecond * (s.time - queuedSenseV2.time))
        {
            LastHeard = FrontRoomsRelayHeard.Outweighed;
            return;
        }
        queuedSenseV2 = s;
        senseQueuedV2 = true;
        LastHeard = FrontRoomsRelayHeard.Queued;
    }

    /// <summary>Each tick: the break is over: what it queued is taken as it would have been, as of when it was heard. A chase that has sight is deaf to it.</summary>
    void FlushSenseV2()
    {
        if (!senseQueuedV2 || State == HunterState.BreakDoor) return;
        senseQueuedV2 = false;
        if (!Released) return;
        var deaf = State == HunterState.Chase ? SightNowV2() : !HearsV2();
        if (!deaf) TakeSenseV2(queuedSenseV2);
    }

    // V4b's seam: a door it heard move after losing sight (it has seen the player this encounter and does not see them now).
    void NoteHeardDoorV2(FrontRoomsMapWorld.Door door)
    {
        if (lastSeenTime <= float.MinValue || SightNowV2()) return;
        heardDoorV2 = door;
        heardDoorAtV2 = clock;
    }

    /// <summary>Sight replaces whatever it heard: the link, the slot, the last known point's noise, the door heard since it last saw them.</summary>
    void SightClearsSenseV2()
    {
        ClearLinkV2();
        heardDoorV2 = null;
    }

    void ClearLinkV2()
    {
        linkActiveV2 = false;
        linkKV2 = linkRepeatsV2 = 0;
        linkMarginV2 = 0f;
        retargetAtV2 = float.NegativeInfinity;
        cautiousLookV2 = lastKnownHeardV2 = senseQueuedV2 = false;
    }

    /// <summary>V4a's part of the encounter reset (Park, DebugPlace, Arm, Away) and of a dispatch.</summary>
    void ResetSenseV4a()
    {
        ClearLinkV2();
        heardDoorV2 = null;
    }
}
#endif
