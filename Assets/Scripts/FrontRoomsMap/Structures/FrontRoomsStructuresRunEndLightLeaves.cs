using System.Text;
using UnityEngine;

/// <summary>
/// SP7 treatment #3, THE LIGHT LEAVES (research/level_space/72_unreachable_end.md section 3.3; Red 2026-11-04: make each of
/// the five in scene 4 to look at). Built against look B, the agreed one; every number names its source below.
///   Armed: today's room (the soffit track is the tell, as #1).
///   t 0, the trip at FD1 (9 m): behind the walker the room trips as L's look trips it (B: the band from the entrance, the
///     red strike, the heads rise). Ahead the white strips go out one by one, a front running to the door at 12 m/s, each
///     with Run! B's own mains drop and lens afterglow; in 1.5 s the corridor ahead is black but for the door's own light
///     (the goal head on its bracket and C42 in the lite, never dimmed: rule 7). The fog turns near black.
///   0 - 0.25 s: the tell (the magnets let go: a 15 mm jolt, two twitches of the goal head on its bracket).
///   0.25 - 2.6 s: the end (SP7's T1 head, no blocks) glides back: 0.5 s up to 8 m/s, the cruise, 0.5 s braking; the cells
///     it uncovers are drawn dark, so nobody sees them appear. The goal head nods with the acceleration (off with F).
///   2.6 - 3.0 s: the heavy seat (the cells open quietly, still dark).
///   While running: each strip strikes red as the walker comes within 1.5 m of it, the heads beside it rise, and they stay
///     on behind: a moving pocket of red, the floor under the walker always lit.
///   About 3.0 s: the runway: from the walker's feet every dark strip strikes red toward the door at 20 m/s, the heads rise
///     with it; B's pulses start 1.0 s after the seat, as agreed.
///   T: today's white re-strike from the entrance on every lamp (the fog fades back in its dark phase); a recede already
///     moving finishes. Y, V, X: the room as built.
/// N: the SWALLOWED comparison (72 section 3.3's harsher variant, not chosen: it breaks rule 7): the front also takes the
/// door's light, the end moves only after that, unseen, and the runway finds it farther, its light coming back there.
/// F (Reduce Flashing): every front a 0.5 s cross-fade, no strip changes twice inside 1.0 s (72 section 3.3), no nod.
/// Nothing new is built (72 section 3.3: "Corridor: unchanged"; no model, no light, no material of its own); no sound.
/// </summary>
public sealed class FrontRoomsStructuresRunEndLightLeaves : FrontRoomsStructuresRunEnd
{
    // ================================================================ identity (the plate, the placard, the G panel)

    public override int Number => 3;
    public override string Name => "光走了";
    public override string TryLine => "冲过 FD1，前面的灯一盏盏熄灭，只剩门的光往后飘；跟着头顶变红的灯跑，门停下后红灯铺到门口。";

    public override string OptionLine
    {
        get
        {
            var line = swallowedNext
                ? "N：现在是“吞没”对照（门的光也被吞掉，破了规则 7）；再按 N 回到推荐的那种。"
                : "N：换成“吞没”对照：门的光也被吞掉，到更远处才重新亮起（破了规则 7，只作对照）。";
            return swallowedNext != swallowed ? line + "（下次待触发时生效）" : line;
        }
    }

    public override string Status(FrontRoomsStructuresRunEndRoom room)
    {
        if (room == null) return "";
        var phase = room.Phase;
        if (phase == FrontRoomsStructuresRunEndPhase.Resetting) return Moving ? "门还在退完" : "";
        if (phase == FrontRoomsStructuresRunEndPhase.Tripped && passive) return "已经退过：只有警报灯";
        switch (beat)
        {
            case Beat.Gate:
                return swallowRun ? "灯在灭，门的光也要灭" : "等你看向尽头";
            case Beat.Tell:
            case Beat.Travel:
            case Beat.Seat:
                return swallowRun ? "门在黑里退" : "门的光在飘走";
            case Beat.Seated:
                if (phase != FrontRoomsStructuresRunEndPhase.Tripped) return "";
                return room.Clock < runwayEnd ? "红灯铺向门口" : "门停下了";
        }
        if (phase == FrontRoomsStructuresRunEndPhase.Armed && !room.Receded) return swallowed ? "吞没对照 · L 只管身后的灯" : "L 只管身后的灯";
        return "";
    }

    // ================================================================ numbers (each with its source)

    /// <summary>72 3.3, the leave: "a front running away toward the door at 12 m/s; in 1.5 s the corridor ahead is black"
    /// (72 section 5: proposed there, not agreed).</summary>
    const float FrontSpeed = 12f;
    /// <summary>72 3.3, the carry: "each strip strikes red as the player comes within 1.5 m of passing under it" (the heads
    /// beside it "rise with their strips").</summary>
    const float CarryLead = 1.5f;
    /// <summary>72 3.3, the runway: "the strips strike red one after another toward the door at 20 m/s" (look B's band
    /// speed, 71 section 6.4: 0.15 s a cell).</summary>
    const float RunwaySpeed = 20f;
    /// <summary>72 3.3 (the glide of #2, 72 3.2): "a 0.25 s tell, 0.5 s up to 8 m/s, cruise, braking, a heavy seat".</summary>
    const float Cruise = 8f, RampUp = .5f, RampDown = .5f, TellTime = .25f;
    /// <summary>The tell's 15 mm jolt toward the walker and back, and the heavy final seat (0.35 s, 30 mm): today's SP7
    /// (FrontRoomsStructuresRunRecede.BuildSchedule and Advance, 71 section 4.5); 72 3.3: "about 2.6-3.0 s: the light
    /// stops; a heavy thud in the dark".</summary>
    const float TellJolt = .015f, SeatTime = .35f, SeatAmp = .03f;
    /// <summary>72 3.3 Build: "SP7's gaze gate (35 degrees, fallback at 18 m)" (room.GazeDegrees; 71 section 4.3).</summary>
    const float GateFallback = 18f;
    /// <summary>No strip strikes red sooner than 0.35 s after its white went out: look B's own white-to-red interval
    /// (RunRecede.StripLevels: the white drops at 0.15 c, the red strikes at 0.35 + 0.15 c; the rig's headDelay 0.35), so no
    /// strip here changes faster than in the agreed look.</summary>
    const float MinDark = .35f;
    /// <summary>F: 72 3.3 Risks, "never more than one change per strip in a second": a strip the walker reaches within 1.0 s
    /// is spared by the front (one cross-fade, white to red, when reached), and a strike waits until 1.0 s after a dark. A
    /// spared strip goes out after all when the walker stops (0.5 s of slack, so it never flips).</summary>
    const float MinDarkReduced = 1f, SpareHorizon = 1f, SpareSlack = .5f;
    /// <summary>72 3.3 Look: "the tripped fog colour must be near black, or the black end reads as grey haze" (the build
    /// proposal). The colour (gamma: a maroon black, so the red pocket's distance goes dark, not grey) and the density
    /// (today's 0.014 raised, so the leaving light sinks: 70 % of the door's light is left at 33 m) are this build's, for the
    /// walk to judge. In over the leave's own 1.5 s ((27 - 9) / 12, 72 3.3); out in the re-strike's dark phase (the rig's
    /// resetRiseStart, 0.45 s).</summary>
    static readonly Color DarkFog = new Color(.030f, .016f, .014f);
    const float DarkFogDensity = .018f, FogIn = 1.5f;
    /// <summary>SWALLOWED (72 3.3's harsher variant): the end starts 0.1 s after the front has taken the door's light (this
    /// build's); the goal unit is 1.2 m out from the end wall (RunRecede.BuildHead).</summary>
    const float SwallowPause = .1f, GoalUnitOut = 1.2f;
    /// <summary>The goal head's nod on its bracket, this build's numbers (by eye, for the walk): a 3.2 Hz spring with damping
    /// 0.3, leaning 0.11 degrees per m/s2 of the end's acceleration (16 m/s2 on the ramps: about 1.8 degrees); the two clacks
    /// and the seat's thud kick it (degrees per second). Off with F (FRAME rule 4: no swing faster than slow).</summary>
    const float NodHz = 3.2f, NodDamping = .3f, NodGain = .11f, NodLimit = 4f, ClackKick = 20f, ThudKick = 35f;
    /// <summary>Seconds after which today's drive of a strip stands still (room.TrippedSince: its look's front has passed it),
    /// plus this margin.</summary>
    const float BandMargin = .05f;

    // ================================================================ state

    enum Beat { Idle, Gate, Tell, Travel, Seat, Seated, Off }

    Beat beat = Beat.Idle;
    float beatClock;
    bool secondClack;
    // The lamps: live from the first frame after the trip until T's re-strike (or a reset); passive on a trip that moves
    // nothing (after the recede, until Y): today's look then.
    bool lampsLive, passive;
    float frontFrom;
    // Per strip: driven here (ahead of the walker at the trip), when its white goes out (+inf kept white, -inf never lit: a
    // hidden cell), when its red strike starts (+inf not yet), a strike from white (one cross-fade), spared by F's front,
    // the front has passed it, and what it showed at the trip (the armed white, the stutter tube's level included).
    bool[] ahead, turn, spared, frontSeen;
    float[] leaveAt, strikeAt;
    FrontRoomsStructuresRunEndStrip[] white;
    // Per head lamp: driven here (held dark until carried or the runway), when it rises.
    bool[] headAhead;
    float[] headStrikeAt;
    float runwayAt = -1f, runwayFrom, runwayEnd = float.PositiveInfinity;
    bool runwayShown;
    // SWALLOWED: picked (N) and in force for this run; when the front takes the door's light and when the runway gives it back.
    bool swallowed, swallowedNext, swallowRun;
    float takenAt = float.PositiveInfinity, backAt = float.PositiveInfinity;
    float liteShown = 1f, liteResetFrom, liteResetAt;
    bool liteDriven, liteResetting;
    // The fog (mine while set): in over the leave, out in the re-strike's dark phase.
    bool fogMine, fogOut;
    float fogLevel;
    // The goal unit's nod.
    Transform goalUnit;
    Quaternion goalUnitRest = Quaternion.identity;
    float nod, nodVel, headAccel;
    bool nodSet;

    bool Moving => beat == Beat.Tell || beat == Beat.Travel || beat == Beat.Seat;

    // ================================================================ hooks: build, enter, reset, exit

    public override void Build(FrontRoomsStructuresRunEndRoom room)
    {
        // Nothing to build: 72 3.3 keeps the corridor, the head (T1 only: no blocks) and the lamps as they are. The lamp
        // tables only, and the goal unit the nod turns.
        EnsureArrays(room);
        goalUnit = room.GoalUnit;
    }

    public override void Reset(FrontRoomsStructuresRunEndRoom room)
    {
        EnsureArrays(room);
        lampsLive = passive = false;
        beat = Beat.Idle;
        beatClock = 0f;
        swallowed = swallowedNext;
        swallowRun = false;
        takenAt = backAt = float.PositiveInfinity;
        runwayAt = -1f;
        runwayShown = false;
        runwayEnd = float.PositiveInfinity;
        // The frame cleared the fog, the lite and every pose before this (its own reset).
        fogMine = fogOut = false;
        fogLevel = 0f;
        liteDriven = liteResetting = false;
        liteShown = 1f;
        nod = nodVel = headAccel = 0f;
        nodSet = false;
        if (goalUnit == null) goalUnit = room.GoalUnit;
        if (goalUnit != null) goalUnitRest = goalUnit.localRotation;
        for (var i = 0; i < ahead.Length; i++) ClearStrip(i);
        for (var i = 0; i < headAhead.Length; i++)
        {
            headAhead[i] = false;
            headStrikeAt[i] = float.PositiveInfinity;
        }
    }

    public override void Exit(FrontRoomsStructuresRunEndRoom room)
    {
        // The frame puts back the head, the lite, the fog and the lamps; only this file's own state goes.
        lampsLive = passive = false;
        beat = Beat.Idle;
        if (goalUnit != null && nodSet) goalUnit.localRotation = goalUnitRest;
        nodSet = false;
        nod = nodVel = 0f;
    }

    // ================================================================ hooks: the trip, the frame, the re-strike

    public override void Trip(FrontRoomsStructuresRunEndRoom room)
    {
        // A trip that moves nothing (at the entrance once the end has receded, until Y; or while a recede still finishes):
        // today's look only.
        if (room.Receded || Moving || Mathf.Abs(room.HeadZ - room.RestLine) > .01f)
        {
            passive = true;
            room.Dirty();
            return;
        }
        passive = false;
        swallowRun = swallowed;
        takenAt = backAt = float.PositiveInfinity;
        runwayAt = -1f;
        runwayShown = false;
        runwayEnd = float.PositiveInfinity;
        // The lamps armed at the trip itself, from the walker's feet (room.Feet).
        ArmLamps(room, room.Feet.z);
        fogMine = true;
        fogOut = false;
        room.Waiting = true;
        SetBeat(room, Beat.Gate);
    }

    public override void Tick(FrontRoomsStructuresRunEndRoom room, FrontRoomsStructuresRunEndFrame frame)
    {
        headAccel = 0f;
        var tripped = room.Phase == FrontRoomsStructuresRunEndPhase.Tripped;
        if (lampsLive && tripped) Carry(room, frame);
        Glide(room, frame);
        if (!runwayShown && runwayAt >= 0f && room.Clock >= runwayEnd)
        {
            // The plate's state line: the runway has reached the door.
            runwayShown = true;
            room.Dirty();
        }
        DriveFog(room);
        DriveLite(room);
        Nod(room, frame.dt);
    }

    public override void ResetStarted(FrontRoomsStructuresRunEndRoom room)
    {
        // Today's white re-strike walks from the entrance on every lamp (the frame released every hold): this file's lamps let
        // go now. A recede waiting for its look is called off with the alarm; one already moving finishes (never stops midway).
        lampsLive = passive = false;
        if (beat == Beat.Gate) SetBeat(room, Beat.Off);
        if (fogMine) fogOut = true;
        // The door's light (SWALLOWED) comes back with the white sweep, at the end's cell.
        if (liteDriven && liteShown < .999f)
        {
            liteResetting = true;
            liteResetFrom = liteShown;
            var cell = FrontRoomsStructuresRunEndRoom.CellOf(Mathf.Max(0f, room.FinalLine - .1f));
            liteResetAt = room.Rig.resetRiseStart + room.Rig.resetCellSeconds * cell;
        }
        swallowRun = false;
        room.Dirty();
    }

    public override void Rearmed(FrontRoomsStructuresRunEndRoom room)
    {
        lampsLive = passive = false;
        if (beat == Beat.Gate || beat == Beat.Off || beat == Beat.Seated) beat = Beat.Idle;
        if (fogMine)
        {
            room.ClearFog();
            fogMine = fogOut = false;
            fogLevel = 0f;
        }
        if (liteDriven || liteResetting) SetLiteLevel(room, 1f);
        liteDriven = liteResetting = false;
        swallowed = swallowedNext;
        room.Dirty();
    }

    // ================================================================ the lamps (72 3.3: leave, carry, runway)

    // At the trip: the walker's feet are where the front starts. Behind them today's look; ahead every lit strip goes out as
    // the front reaches it, the hidden cells' strips stay dark, the heads ahead are held dark.
    void ArmLamps(FrontRoomsStructuresRunEndRoom room, float feetZ)
    {
        EnsureArrays(room);
        frontFrom = Mathf.Clamp(feetZ, 0f, FrontRoomsStructuresRunEndRoom.Length);
        for (var i = 0; i < ahead.Length; i++)
        {
            ClearStrip(i);
            var z = room.StripZ(i);
            white[i] = room.ShownStrip(i);
            if (!room.CellOpen(room.StripCell(i)))
            {
                // A hidden cell's strip: never lit until the runway (or the carry) strikes it: one change only.
                ahead[i] = true;
                leaveAt[i] = float.NegativeInfinity;
                frontSeen[i] = true;
            }
            else if (z >= frontFrom)
            {
                ahead[i] = true;
                leaveAt[i] = (z - frontFrom) / FrontSpeed;
            }
        }
        for (var i = 0; i < headAhead.Length; i++)
        {
            headStrikeAt[i] = float.PositiveInfinity;
            // The goal heads are today's (rule 7); the heads beside and behind the walker rise as the look raises them.
            headAhead[i] = !room.IsGoalHead(i) && !(room.CellOpen(room.HeadCell(i)) && room.HeadLampZ(i) <= frontFrom + CarryLead);
        }
        // SWALLOWED: the front reaches the door's line and takes its light too.
        takenAt = swallowRun ? Mathf.Max(0f, room.RestLine - frontFrom) / FrontSpeed : float.PositiveInfinity;
        lampsLive = true;
    }

    // Every tripped frame: F's spare test where the front arrives, the carry (the walker within the lead).
    void Carry(FrontRoomsStructuresRunEndRoom room, FrontRoomsStructuresRunEndFrame frame)
    {
        var t = room.Clock;
        var feet = frame.feet.z;
        var reduce = room.Reduce;
        var wall = room.HeadZ;
        // The lead: the strike's own rise (0.35 at once, full 0.40 s later; F a 0.5 s cross-fade) taken at the walker's own
        // forward pace on top of the 1.5 m, so a strip is fully red by the time the walker comes within 1.5 m of it; standing,
        // it is the 1.5 m. Render stage 11-04 (renders/scene4/run_ends2): with the bare 1.5 m the floor just ahead fell to luma
        // 8 between strips at a sprint and 12-16 at a walk through FD2, where 72 3.3 has "the floor just ahead is always lit"
        // ("my own light keeps one cell ahead of me"). Each strip still changes once.
        var rig = room.Rig;
        var lead = CarryLead + Mathf.Max(0f, frame.own.z) * (reduce ? rig.reducedEnvelope : rig.resetRise);
        for (var i = 0; i < ahead.Length; i++)
        {
            if (!ahead[i] || strikeAt[i] < float.PositiveInfinity) continue;
            var z = room.StripZ(i);
            if (!frontSeen[i] && t >= leaveAt[i])
            {
                frontSeen[i] = true;
                if (reduce && Reaches(frame, z, SpareHorizon, lead))
                {
                    spared[i] = true;
                    leaveAt[i] = float.PositiveInfinity;
                }
            }
            else if (spared[i] && (!reduce || !Reaches(frame, z, SpareHorizon + SpareSlack, lead)))
            {
                // The walker stopped short of a spared strip (or F went off): it goes out now, after all.
                spared[i] = false;
                leaveAt[i] = t;
            }
            // Only lamps in front of the end wall (behind it the cells stay hidden).
            if (feet >= z - lead && z < wall) Trigger(room, i, t);
        }
        for (var i = 0; i < headAhead.Length; i++)
        {
            if (!headAhead[i] || headStrikeAt[i] < float.PositiveInfinity) continue;
            var z = room.HeadLampZ(i);
            if (feet >= z - lead && z < wall) headStrikeAt[i] = t;
        }
    }

    // The walker will come within the carry's lead of z inside the horizon at its own forward pace.
    static bool Reaches(FrontRoomsStructuresRunEndFrame frame, float z, float horizon, float lead) =>
        frame.feet.z + Mathf.Max(0f, frame.own.z) * horizon >= z - lead;

    // A strip's strike wanted at a time: from dark, never sooner than MinDark after its white went out; from white (the walker
    // there before the front, or a strip F spared), one cross-fade and the front's dark called off.
    void Trigger(FrontRoomsStructuresRunEndRoom room, int i, float at)
    {
        if (strikeAt[i] < float.PositiveInfinity) return;
        var leave = leaveAt[i];
        if (float.IsNegativeInfinity(leave))
        {
            strikeAt[i] = at;
            turn[i] = false;
            return;
        }
        if (leave <= at)
        {
            strikeAt[i] = Mathf.Max(at, leave + (room.Reduce ? MinDarkReduced : MinDark));
            turn[i] = false;
            return;
        }
        strikeAt[i] = at;
        turn[i] = true;
        leaveAt[i] = float.PositiveInfinity;
        frontSeen[i] = true;
        spared[i] = false;
    }

    // The runway, from the final seat: from the walker's feet every strip still dark strikes as a front reaches it at 20 m/s,
    // the heads with it; SWALLOWED's door light comes back as the front reaches the goal head.
    void StartRunway(FrontRoomsStructuresRunEndRoom room)
    {
        runwayAt = room.Clock;
        runwayShown = false;
        runwayFrom = Mathf.Clamp(room.Feet.z, 0f, FrontRoomsStructuresRunEndRoom.Length);
        // To the door only: at R 9 the cells past 36 m stay hidden behind the end wall, their lamps untouched.
        var wall = room.HeadZ;
        for (var i = 0; i < ahead.Length; i++)
            if (ahead[i] && strikeAt[i] == float.PositiveInfinity && room.StripZ(i) < wall)
                Trigger(room, i, runwayAt + Mathf.Max(0f, room.StripZ(i) - runwayFrom) / RunwaySpeed);
        for (var i = 0; i < headAhead.Length; i++)
            if (headAhead[i] && headStrikeAt[i] == float.PositiveInfinity && room.HeadLampZ(i) < wall)
                headStrikeAt[i] = runwayAt + Mathf.Max(0f, room.HeadLampZ(i) - runwayFrom) / RunwaySpeed;
        var rig = room.Rig;
        runwayEnd = runwayAt + Mathf.Max(0f, room.FinalLine - runwayFrom) / RunwaySpeed + (room.Reduce ? rig.reducedEnvelope : rig.resetRise);
        if (swallowRun) backAt = runwayAt + Mathf.Max(0f, room.HeadZ - GoalUnitOut - runwayFrom) / RunwaySpeed;
        room.Dirty();
    }

    public override void Strip(FrontRoomsStructuresRunEndRoom room, int i, ref FrontRoomsStructuresRunEndStrip s)
    {
        if (!lampsLive || ahead == null || i < 0 || i >= ahead.Length || !ahead[i]) return;
        var t = room.Clock;
        var reduce = room.Reduce;
        var rig = room.Rig;
        var shown = Base(rig, reduce, i, t, s);
        var at = strikeAt[i];
        if (t >= at)
        {
            var k = StrikeCurve(rig, reduce, t - at, turn[i]);
            shown = FrontRoomsStructuresRunEndStrip.Lerp(shown, Target(room, rig, reduce, i, t, s), k);
        }
        shown.open = s.open;
        s = shown;
    }

    // A strip before (and under) its strike: the white it showed at the trip until the front reaches it, then Run! B's mains
    // drop (0.06 s, linear) with the lens's afterglow (0.03, out over a 0.5 s tau) as today's look drops it (RunRecede
    // WhiteDrop / Afterglow); F: a 0.5 s cross-fade (the rig's reducedEnvelope). A hidden cell's strip: dark.
    FrontRoomsStructuresRunEndStrip Base(RunRigLayout rig, bool reduce, int i, float t, FrontRoomsStructuresRunEndStrip s)
    {
        var at = leaveAt[i];
        if (float.IsNegativeInfinity(at)) return s.AsDark();
        var w = white[i];
        if (t < at) return w;
        var u = t - at;
        float drop, end;
        if (reduce)
        {
            end = rig.reducedEnvelope;
            drop = 1f - Smooth(u / Mathf.Max(.01f, end));
        }
        else
        {
            end = rig.mainsDrop;
            drop = Mathf.Clamp01(1f - u / Mathf.Max(.001f, end));
        }
        var glow = u <= end ? rig.afterglow : rig.afterglow * Mathf.Exp(-(u - end) / Mathf.Max(.01f, rig.afterglowTau));
        var d = w;
        d.white = w.white * drop;
        d.lens = w.lens * Mathf.Lerp(glow, 1f, drop);
        d.red = w.red * drop;
        d.tube = w.tube * drop;
        return d;
    }

    // What a struck strip turns into: today's drive once its look's own front has passed it (room.TrippedSince: so B's pulses
    // run on it after the seat), before that the look's own tripped strip (room.TrippedStrip: A, B, C the room's red and the
    // tube at 1; today's look the tube alone, no red light). A strip of a cell not open yet (drawn, the head still moving)
    // keeps the look's tripped strip.
    static FrontRoomsStructuresRunEndStrip Target(FrontRoomsStructuresRunEndRoom room, RunRigLayout rig, bool reduce, int i, float t, FrontRoomsStructuresRunEndStrip s)
    {
        if (s.open && t >= room.TrippedSince(i) + BandMargin) return s;
        return room.TrippedStrip(i);
    }

    // The red strike (RunRecede.RedRise, 71 section 6.3): from dark 0 -> 0.35 at once, then a smoothstep to 1 over 0.40 s
    // (the rig's resetRise); from white one cross-fade over the same 0.40 s; F: a 0.5 s cross-fade either way.
    static float StrikeCurve(RunRigLayout rig, bool reduce, float u, bool fromWhite)
    {
        if (u < 0f) return 0f;
        if (reduce) return Smooth(u / Mathf.Max(.01f, rig.reducedEnvelope));
        if (fromWhite) return Smooth(u / Mathf.Max(.01f, rig.resetRise));
        return .35f + .65f * Smooth(u / Mathf.Max(.01f, rig.resetRise));
    }

    public override void Head(FrontRoomsStructuresRunEndRoom room, int i, ref FrontRoomsStructuresRunEndHead h)
    {
        if (!lampsLive || headAhead == null || i < 0 || i >= headAhead.Length) return;
        if (room.IsGoalHead(i))
        {
            // Rule 7: the goal heads stay today's; only the SWALLOWED comparison takes them with the front.
            if (swallowRun) h.level *= DoorLight(room, room.Clock);
            return;
        }
        if (!headAhead[i]) return;
        var u = room.Clock - headStrikeAt[i];
        if (u < 0f)
        {
            h.level = 0f;
            return;
        }
        // The rig's head rise (RunRecede.HeadLevel: an ease-in square over 0.18 s; F: a 0.5 s smoothstep), to the head's own
        // level (heads hold steady under every look).
        var rig = room.Rig;
        h.level = room.Reduce ? Smooth(u / Mathf.Max(.01f, rig.reducedEnvelope)) : Square(Mathf.Clamp01(u / Mathf.Max(.01f, rig.headRise)));
    }

    // SWALLOWED: the door's light (the goal heads and C42) as the front takes it (Run! B's mains drop, F a 0.5 s fade) and as
    // the runway gives it back (the head's rise).
    float DoorLight(FrontRoomsStructuresRunEndRoom room, float t)
    {
        if (t < takenAt) return 1f;
        var rig = room.Rig;
        var reduce = room.Reduce;
        var u = t - takenAt;
        var off = reduce ? 1f - Smooth(u / Mathf.Max(.01f, rig.reducedEnvelope)) : Mathf.Clamp01(1f - u / Mathf.Max(.001f, rig.mainsDrop));
        if (t < backAt) return off;
        var v = t - backAt;
        var on = reduce ? Smooth(v / Mathf.Max(.01f, rig.reducedEnvelope)) : Square(Mathf.Clamp01(v / Mathf.Max(.01f, rig.headRise)));
        return Mathf.Max(off, on);
    }

    float Drop(FrontRoomsStructuresRunEndRoom room) => room.Reduce ? room.Rig.reducedEnvelope : room.Rig.mainsDrop;

    // ================================================================ the end: gate, tell, glide, heavy seat

    void Glide(FrontRoomsStructuresRunEndRoom room, FrontRoomsStructuresRunEndFrame frame)
    {
        var rest = room.RestLine;
        var final = room.FinalLine;
        switch (beat)
        {
            case Beat.Gate:
            {
                if (room.Phase != FrontRoomsStructuresRunEndPhase.Tripped) break;
                bool go;
                // SWALLOWED: the end leaves only once its light is gone (unseen). Otherwise SP7's gaze gate: a look down the
                // corridor, or the walker at 18 m.
                if (swallowRun) go = room.Clock >= takenAt + Drop(room) + SwallowPause;
                else go = frame.axisAngle <= FrontRoomsStructuresRunEndRoom.GazeDegrees || frame.feet.z >= GateFallback;
                if (go) StartTell(room);
                break;
            }
            case Beat.Tell:
            {
                beatClock += frame.dt;
                var u = Mathf.Clamp01(beatClock / TellTime);
                // The magnets let go: 15 mm toward the walker and back (never into a walker standing against the wall).
                var clear = Mathf.Clamp01((rest - .08f - .35f - frame.feet.z) / .5f);
                room.PlaceHead(rest - TellJolt * clear * Mathf.Sin(Mathf.PI * u), false);
                if (!secondClack && u >= .5f)
                {
                    secondClack = true;
                    Kick(room, ClackKick);
                }
                if (beatClock >= TellTime)
                {
                    beatClock -= TellTime;
                    room.PlaceHead(rest, true);
                    // The line it leaves keeps its ghost band (lit only when the runway gets there).
                    room.Leaving(rest);
                    SetBeat(room, Beat.Travel);
                }
                break;
            }
            case Beat.Travel:
            {
                beatClock += frame.dt;
                var length = final - rest;
                var total = MoveTime(length, Cruise, RampUp, RampDown);
                room.PlaceHead(rest + Move(beatClock, length, Cruise, RampUp, RampDown), true);
                headAccel = MoveAccel(beatClock, length, Cruise, RampUp, RampDown);
                if (beatClock >= total)
                {
                    beatClock -= total;
                    room.PlaceHead(final, false);
                    // Today's seat order: the cells open at the end of the travel, quietly (revealed in the dark: their
                    // strips and heads stay dark here until the runway), the hall's blind panel goes at 42; then the bump.
                    room.Seat(rest, final, false);
                    Kick(room, ThudKick);
                    SetBeat(room, Beat.Seat);
                }
                break;
            }
            case Beat.Seat:
            {
                beatClock += frame.dt;
                var u = Mathf.Clamp01(beatClock / SeatTime);
                room.PlaceHead(final + SeatAmp * Mathf.Sin(Mathf.PI * u) * Mathf.Exp(-3f * u), false);
                if (beatClock >= SeatTime)
                {
                    room.PlaceHead(final, false);
                    var live = lampsLive && room.Phase == FrontRoomsStructuresRunEndPhase.Tripped;
                    room.Done();
                    SetBeat(room, Beat.Seated);
                    if (live) StartRunway(room);
                }
                break;
            }
        }
    }

    void StartTell(FrontRoomsStructuresRunEndRoom room)
    {
        room.Waiting = false;
        room.HeadMoving = true;
        // The cells it uncovers drawn, dark (72 3.3: "the hidden cells are revealed in the dark, so nobody sees them appear").
        room.ShowBetween(room.RestLine, room.FinalLine);
        beatClock = 0f;
        secondClack = false;
        Kick(room, -ClackKick);
        SetBeat(room, Beat.Tell);
    }

    // The acceleration of FrontRoomsStructuresRunEnd.Move at t (m/s2): the ramp up, the cruise, the brake (both ramps
    // scaled down the same way when the move is too short to reach the cruise).
    static float MoveAccel(float t, float length, float cruise, float accel, float brake)
    {
        if (t <= 0f || length <= 0f) return 0f;
        cruise = Mathf.Max(.01f, cruise);
        accel = Mathf.Max(0f, accel);
        brake = Mathf.Max(0f, brake);
        var ramps = .5f * cruise * (accel + brake);
        if (ramps > length)
        {
            var k = Mathf.Sqrt(length / ramps);
            cruise *= k;
            accel *= k;
            brake *= k;
            ramps = length;
        }
        var cruiseTime = (length - ramps) / cruise;
        if (t < accel) return accel > 1e-4f ? cruise / accel : 0f;
        if (t < accel + cruiseTime) return 0f;
        if (t < accel + cruiseTime + brake) return brake > 1e-4f ? -cruise / brake : 0f;
        return 0f;
    }

    // ================================================================ the goal head's nod (model animation, this build's)

    void Nod(FrontRoomsStructuresRunEndRoom room, float dt)
    {
        if (goalUnit == null) return;
        if (room.Reduce)
        {
            if (nodSet) goalUnit.localRotation = goalUnitRest;
            nodSet = false;
            nod = nodVel = 0f;
            return;
        }
        // Leaning against the end's acceleration: away from the walker as it pulls away, forward as it brakes.
        var target = -NodGain * headAccel;
        if (!nodSet && Mathf.Abs(target) < 1e-4f && Mathf.Abs(nodVel) < 1e-3f) return;
        var w0 = 2f * Mathf.PI * NodHz;
        var steps = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(0f, dt) * 240f), 1, 16);
        var h = Mathf.Max(0f, dt) / steps;
        for (var k = 0; k < steps; k++)
        {
            nodVel += (w0 * w0 * (target - nod) - 2f * NodDamping * w0 * nodVel) * h;
            nod += nodVel * h;
        }
        nod = Mathf.Clamp(nod, -NodLimit, NodLimit);
        if (Mathf.Abs(target) < 1e-4f && Mathf.Abs(nod) < .003f && Mathf.Abs(nodVel) < .03f)
        {
            nod = nodVel = 0f;
            goalUnit.localRotation = goalUnitRest;
            nodSet = false;
            return;
        }
        goalUnit.localRotation = goalUnitRest * Quaternion.Euler(nod, 0f, 0f);
        nodSet = true;
    }

    void Kick(FrontRoomsStructuresRunEndRoom room, float degreesPerSecond)
    {
        if (goalUnit == null || room.Reduce) return;
        nodVel += degreesPerSecond;
        nodSet = true;
    }

    // ================================================================ the fog and the lite

    void DriveFog(FrontRoomsStructuresRunEndRoom room)
    {
        if (!fogMine) return;
        var phase = room.Phase;
        if (!fogOut)
        {
            if (phase != FrontRoomsStructuresRunEndPhase.Tripped) return;
            fogLevel = Smooth(room.Clock / FogIn);
            SetFog(room, fogLevel);
            return;
        }
        // T's re-strike (its clock from 0): back to the preset's fog over the dark phase, so the white walks in the room's fog.
        var span = Mathf.Max(.01f, room.Rig.resetRiseStart);
        var back = phase == FrontRoomsStructuresRunEndPhase.Resetting ? Smooth(room.Clock / span) : 1f;
        if (back >= 1f)
        {
            room.ClearFog();
            fogMine = fogOut = false;
            fogLevel = 0f;
            return;
        }
        SetFog(room, fogLevel * (1f - back));
    }

    // The preset's own fog (room.PresetFogColour / PresetFogDensity) toward the near black, by k.
    static void SetFog(FrontRoomsStructuresRunEndRoom room, float k) =>
        room.Fog(Color.Lerp(room.PresetFogColour, DarkFog, k), Mathf.Lerp(room.PresetFogDensity, DarkFogDensity, k));

    void DriveLite(FrontRoomsStructuresRunEndRoom room)
    {
        if (liteResetting)
        {
            var rig = room.Rig;
            var u = room.Clock - liteResetAt;
            var k = room.Phase == FrontRoomsStructuresRunEndPhase.Resetting
                ? (u <= 0f ? 0f : Smooth(u / Mathf.Max(.01f, room.Reduce ? rig.reducedEnvelope : rig.resetRise)))
                : 1f;
            SetLiteLevel(room, Mathf.Lerp(liteResetFrom, 1f, k));
            if (k >= 1f)
            {
                liteResetting = false;
                liteDriven = false;
            }
            return;
        }
        if (!swallowRun || !lampsLive) return;
        SetLiteLevel(room, DoorLight(room, room.Clock));
    }

    void SetLiteLevel(FrontRoomsStructuresRunEndRoom room, float v)
    {
        if (liteDriven && Mathf.Abs(v - liteShown) < 1e-4f) return;
        liteShown = v;
        liteDriven = true;
        room.SetLite(v);
    }

    // ================================================================ N, the G panel

    public override bool Option(FrontRoomsStructuresRunEndRoom room, out string message)
    {
        swallowedNext = !swallowedNext;
        var armed = room.Phase == FrontRoomsStructuresRunEndPhase.Armed && !Moving;
        if (armed) swallowed = swallowedNext;
        message = (swallowedNext
            ? "光走了：换成“吞没”对照（门的光也被吞掉，到更远处才重新亮起；破了规则 7，只作对照）"
            : "光走了：回到推荐的那种（门的光一直亮着）")
            + (armed ? "" : "（下次待触发时生效）");
        room.Dirty();
        return true;
    }

    public override void Guide(FrontRoomsStructuresRunEndRoom room, StringBuilder sb)
    {
        sb.AppendLine("3 光走了（72 第 3.3 节）：冲过 FD1，身后的灯按 L 的样子变红；前面的白灯从你脚下起每秒 12 米一盏盏灭到门口，只剩门上的应急灯和小窗的光；门先顿两下（门上的灯跟着一抖），0.5 秒加速到每秒 8 米往后滑，重重落定；雾变成近黑。你跑到一盏灯 1.5 米内它才变红，身后的一直亮着；门停下后，红灯从你脚下每秒 20 米铺到门口，B 的脉冲照常开始。");
        sb.AppendLine("这一种改了 B 在触发那一刻的样子：L 只管身后的灯和门停下以后。F：每一下都是 0.5 秒的渐变，一盏灯一秒内不会变两次，门上的灯不晃。N：“吞没”对照（门的光也灭，破了规则 7）。没有新加的模型和灯，全是走廊、端头和灯本身。");
    }

    // ================================================================ helpers

    void SetBeat(FrontRoomsStructuresRunEndRoom room, Beat b)
    {
        if (beat == b) return;
        beat = b;
        room.Dirty();
    }

    void EnsureArrays(FrontRoomsStructuresRunEndRoom room)
    {
        var n = Mathf.Max(0, room.StripCount);
        if (ahead == null || ahead.Length != n)
        {
            ahead = new bool[n];
            turn = new bool[n];
            spared = new bool[n];
            frontSeen = new bool[n];
            leaveAt = new float[n];
            strikeAt = new float[n];
            white = new FrontRoomsStructuresRunEndStrip[n];
            for (var i = 0; i < n; i++) ClearStrip(i);
        }
        var m = Mathf.Max(0, room.HeadCount);
        if (headAhead == null || headAhead.Length != m)
        {
            headAhead = new bool[m];
            headStrikeAt = new float[m];
            for (var i = 0; i < m; i++) headStrikeAt[i] = float.PositiveInfinity;
        }
    }

    void ClearStrip(int i)
    {
        ahead[i] = turn[i] = spared[i] = frontSeen[i] = false;
        leaveAt[i] = float.PositiveInfinity;
        strikeAt[i] = float.PositiveInfinity;
    }

    static float Square(float x) => x * x;
}
