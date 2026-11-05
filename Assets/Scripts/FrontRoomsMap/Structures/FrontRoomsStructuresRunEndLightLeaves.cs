using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UrpVignette = UnityEngine.Rendering.Universal.Vignette;
using UrpLensDistortion = UnityEngine.Rendering.Universal.LensDistortion;
using UrpChromaticAberration = UnityEngine.Rendering.Universal.ChromaticAberration;
using UrpCameraData = UnityEngine.Rendering.Universal.UniversalAdditionalCameraData;

/// <summary>
/// SP7 treatment #3, THE LIGHT LEAVES, deepened (Red 2026-11-04, choosing among the five ends built in SP7, on #3's sprint
/// film: "deepen this one"; the design: W/level_space/drafts/run/light_leaves_deepen.md, for research/level_space/
/// 72_unreachable_end.md section 8). N cycles 20 rows, Shift+N back (light_leaves_deepen 6.1): the REC LEAVING HARBOUR, the two
/// alternatives THE LONE LIGHT and THE HAUL, the version Red picked (as built 11-04 20:11, c61562d4: the same behaviour), one
/// row per lever variant on the REC, the SWALLOWED comparison (rule 7 broken, labelled), and the REC with the camera layers
/// of research/level_space/74 section 6 (rows 19 and 20, lever g below).
///
/// Six levers (light_leaves_deepen section 3), each row one choice of each:
///   (a) how the light leaves: CUT (the film: a front from the runner's feet to the door at 12 m/s, Run! B's 0.06 s mains drop
///       and lens afterglow) / BROWNOUT (the same front; a far lamp sags to 30 % and yellows over 0.45 s, its arc drops, the
///       ends of its tubes glow orange like embers for about 2 s; a lamp the front reaches with the runner within 6 m dies in
///       0.16 s, so the floor ahead stays lit) / TIDE (the front starts at the last lit lamp and runs toward the runner, stopping
///       at the runner's own red light).
///   (b) how the door's light drifts: GLIDE (the film: a 0.25 s tell, 0.5 s up to 8 m/s, the cruise, a 0.5 s brake, the heavy
///       seat) / HAUL (today's stepped recede under #3's lamps: UsesTodayRecede, the frame's gate, steps and keep-away glide) /
///       BREATH (three surges, R 9 two, each a 0.25 s pull and a 0.6 s ease to 1.5 m/s, never stopping until the seat).
///   (c) Red's model animation, inside the door's own light: RIGID (the film) / LET GO (the wall's blocks lift out of their
///       beds at the clacks, top courses first, loosen into a lit cloud round the door as it pulls away, and fold back into
///       courses in the brake, bottom first; the door, its frame and its lamp stay whole) / RE-STACK PER STEP (with HAUL: the
///       frame's T2 body at every step, B on T2) / SPARKS (the soffit trolleys and the floor pin throw sparks that stay behind).
///   (d) the lamp over the runner: CARRY (the film: each strip red as the feet come within 1.5 m + pace x 0.40 s) / LONG CARRY
///       (6 m + pace x 0.40 s: two lamps ahead) / WARM GUIDE (the battery heads ahead warm within 6 m, the strips red overhead).
///   (e) the runway: SWEEP (the film: red from the feet to the door at the rig's 20 m/s at the final seat) / OVERTAKE (B's
///       first pulse strikes every dark lamp it reaches) / COUNTED (one lamp every 0.40 s) / NONE (the carry only).
///   (f) the reset: AS BUILT (the fog lifts in the re-strike's 0.45 s dark phase) / WHITE WASH (the fog thins with the white).
///   (g) the lens (74 section 6.3; rows 19 and 20 only, on the REC): NONE / TUNNEL (L1 THE DARK CLOSES IN: as the lamps ahead
///       die, the frame's edges dim toward the door, Run_B's vignette 0.32 / 0.20 -> 0.38 / 0.28, the door kept brightest;
///       the runway opens them again) / TUNNEL + FLINCH (and L2 THE LENS FLINCHES: at the first clack the picture draws in
///       toward the door once, lens distortion to -0.20 and chromatic aberration to 0.15, back within 1 s; x the game's
///       Camera Motion; at 0, or with F, only a short deepening of the edges). The whole-lens warp (74's L3, the classic
///       stretch Red ruled out) is not built. Two local URP volumes over SP7 alone, with their own runtime profiles.
///   Every row but the as-built two adds the door's light in the dark air: a faint beam under the goal head (the rig's own
///   RunBeamCone shader, faded with the fog) and, but on THE LONE LIGHT, a drop of grit from the soffit seam at the tell (a
///   puff at every seat under HAUL), lit only while the goal lamp is near it.
/// Rules kept: the goal heads and C42 never dip, pulse or change colour (rule 7; only the SWALLOWED row, labelled); the door
/// is never locked; nothing reads the Relay; no cut, teleport or FOV change; the floor never moves; everything that moves
/// stays ahead of the walker (the glide outruns a sprinter; under HAUL the frame's keep-away); every strip changes at most
/// twice a trip (one fall, one red strike) at least 0.35 s apart (F 1.0 s); B's pulses and every red level are the rig's.
/// F (Reduce Flashing) calms every effect: 0.5 s cross-fades, the brownout one 0.8 s sag, no let-go, no nod, no surges,
/// no sparks, the overtaking runway a sweep, no lens flinch (L1's slow edge dim stays: it is not a flash). Nothing is played
/// (no AudioSource) and no Light is added.
/// </summary>
public sealed class FrontRoomsStructuresRunEndLightLeaves : FrontRoomsStructuresRunEnd
{
    // ================================================================ the rows (N; light_leaves_deepen 6.1 and 4)

    enum Leave { Cut, Brownout, Tide }
    enum Motion { Glide, Haul, Breath }
    enum Model { Rigid, LetGo, Restack, Sparks }
    enum Pocket { Carry, Long, Warm }
    enum Reveal { Sweep, Overtake, Counted, None }
    enum Restore { AsBuilt, Wash }
    // (g) the lens, 74 section 6.3's g0 / g1 / g2 (g3, the whole-lens warp, is not built).
    enum Lens { None, Tunnel, TunnelFlinch }

    struct Pick
    {
        public Leave leave;
        public Motion motion;
        public Model model;
        public Pocket pocket;
        public Reveal reveal;
        public Restore restore;
        public Lens lens;
        // halo: the beam under the goal head; grit: the drop at the tell (a puff a seat under HAUL); swallow: SWALLOWED;
        // asBuilt: the film's own version (c61562d4), nothing of the deepening.
        public bool halo, grit, swallow, asBuilt;
        public string name, shortName, tryLine;
    }

    static Pick Row(Leave a, Motion b, Model c, Pocket d, Reveal e, Restore f, bool halo, bool grit, bool swallow, bool asBuilt, string name, string shortName, string tryLine, Lens g = Lens.None) =>
        new Pick { leave = a, motion = b, model = c, pocket = d, reveal = e, restore = f, lens = g, halo = halo, grit = grit, swallow = swallow, asBuilt = asBuilt, name = name, shortName = shortName, tryLine = tryLine };

    static readonly Pick[] Rows =
    {
        Row(Leave.Brownout, Motion.Glide, Model.LetGo, Pocket.Carry, Reveal.Sweep, Restore.AsBuilt, true, true, false, false,
            "推荐「离港」", "离港", "冲过 FD1：前面的灯像余烬一样熄灭，墙散开跟着门的光漂走，在远处重新砌好，红灯再铺到门口。"),
        Row(Leave.Tide, Motion.Glide, Model.Rigid, Pocket.Carry, Reveal.Overtake, Restore.AsBuilt, true, false, false, false,
            "备选「孤灯」", "孤灯", "冲过 FD1：黑暗从门那头涌过来，只剩门的一盏光漂走；门停下后，等警报脉冲从身后追上来。"),
        Row(Leave.Cut, Motion.Haul, Model.Restack, Pocket.Carry, Reveal.Sweep, Restore.AsBuilt, true, true, false, false,
            "备选「拖走」", "拖走", "冲过 FD1：灯一刀切灭，门被一格一格拖走，每一步墙都在门的光里拆开又砌回。"),
        Row(Leave.Cut, Motion.Glide, Model.Rigid, Pocket.Carry, Reveal.Sweep, Restore.AsBuilt, false, false, false, true,
            "原版（你选的那段影片）", "原版", "和你看过的影片一样：前面的灯一盏盏灭，门的光往后飘，门停下后红灯从脚下铺到门口。"),
        Row(Leave.Cut, Motion.Glide, Model.LetGo, Pocket.Carry, Reveal.Sweep, Restore.AsBuilt, true, true, false, false,
            "灯：一刀切灭（像影片）", "一刀切灭", "冲过 FD1：前面的灯一刀切灭，墙散开跟着门的光漂走，在远处砌好，红灯再铺到门口。"),
        Row(Leave.Tide, Motion.Glide, Model.LetGo, Pocket.Carry, Reveal.Sweep, Restore.AsBuilt, true, true, false, false,
            "灯：黑暗从门那头涌过来", "黑暗涌来", "冲过 FD1：先灭门那头的灯，黑暗朝你涌来停在你的红灯前；墙散开跟着门走，远处砌好。"),
        Row(Leave.Brownout, Motion.Haul, Model.LetGo, Pocket.Carry, Reveal.Sweep, Restore.AsBuilt, true, true, false, false,
            "门：一格一格被拖走", "一格一格拖", "冲过 FD1：灯像余烬一样熄灭，门被一格一格拖走，墙一直散着跟着它，最后一下才砌回。"),
        Row(Leave.Brownout, Motion.Breath, Model.LetGo, Pocket.Carry, Reveal.Sweep, Restore.AsBuilt, true, true, false, false,
            "门：分三口气退走", "三口气退", "冲过 FD1：灯像余烬一样熄灭，门分三口气往后退，墙散开跟着走，远处砌好，红灯铺到门口。"),
        Row(Leave.Brownout, Motion.Glide, Model.Rigid, Pocket.Carry, Reveal.Sweep, Restore.AsBuilt, true, true, false, false,
            "墙：不散开，整面滑走", "整面滑走", "冲过 FD1：灯像余烬一样熄灭，墙不散开，整面带着门滑走，远处落定，红灯铺到门口。"),
        Row(Leave.Brownout, Motion.Haul, Model.Restack, Pocket.Carry, Reveal.Sweep, Restore.AsBuilt, true, true, false, false,
            "墙：每一步拆了重砌（门也分步退）", "每步拆砌", "冲过 FD1：灯像余烬一样熄灭，门一格一格退，每一步墙都在门的光里拆开又砌回。"),
        Row(Leave.Brownout, Motion.Glide, Model.Sparks, Pocket.Carry, Reveal.Sweep, Restore.AsBuilt, true, true, false, false,
            "墙：轨道上擦出火花", "擦出火花", "冲过 FD1：灯像余烬一样熄灭，墙整面滑走，顶上轨道和地上的槽一路擦出火花。"),
        Row(Leave.Brownout, Motion.Glide, Model.LetGo, Pocket.Long, Reveal.Sweep, Restore.AsBuilt, true, true, false, false,
            "头顶：红光提前两格亮", "提前两格", "冲过 FD1：灯像余烬一样熄灭，你头顶的红光提前两格亮起，前面看得更远，没那么怕。"),
        Row(Leave.Brownout, Motion.Glide, Model.LetGo, Pocket.Warm, Reveal.Sweep, Restore.AsBuilt, true, true, false, false,
            "头顶：前面暖灯，身后红灯", "前暖后红", "冲过 FD1：灯像余烬一样熄灭，前面的应急灯先暖暖地照亮地面，灯管到你头顶才变红。"),
        Row(Leave.Brownout, Motion.Glide, Model.LetGo, Pocket.Carry, Reveal.Overtake, Restore.AsBuilt, true, true, false, false,
            "跑道：警报脉冲从身后追上来铺", "脉冲追铺", "冲过 FD1：门停下后前面先黑着，等警报脉冲从身后追上来、越过你，一路把灯点红到门口。"),
        Row(Leave.Brownout, Motion.Glide, Model.LetGo, Pocket.Carry, Reveal.Counted, Restore.AsBuilt, true, true, false, false,
            "跑道：一盏一盏数着亮", "数着亮", "冲过 FD1：门停下后，红灯从你脚下一盏一盏慢慢亮到门口，能数出来有多远。"),
        Row(Leave.Brownout, Motion.Glide, Model.LetGo, Pocket.Carry, Reveal.None, Restore.AsBuilt, true, true, false, false,
            "跑道：不铺，走到哪亮到哪", "不铺", "冲过 FD1：门停下后不铺红灯，前面一直黑着，走到哪盏灯，哪盏才亮。"),
        Row(Leave.Brownout, Motion.Glide, Model.LetGo, Pocket.Carry, Reveal.Sweep, Restore.Wash, true, true, false, false,
            "复位：白光一路把黑雾洗掉", "白光洗雾", "冲过 FD1，看完以后按 T：白光从入口一格一格亮回来，黑雾跟着白光一路散开。"),
        Row(Leave.Cut, Motion.Glide, Model.Rigid, Pocket.Carry, Reveal.Sweep, Restore.AsBuilt, false, false, true, true,
            "对照：吞没（门的光也灭，破规则 7）", "吞没对照", "对照：门的光也被黑暗吞掉，门在看不见的时候退，红灯铺过去时它已经更远（破规则 7）。"),
        // The REC (row 1's levers, untouched) with 74's camera layers: L1 alone, then L1 + L2 (Red decides on both).
        Row(Leave.Brownout, Motion.Glide, Model.LetGo, Pocket.Carry, Reveal.Sweep, Restore.AsBuilt, true, true, false, false,
            "离港 + 暗收", "离港+暗收", "冲过 FD1：和离港一样，只是前面的灯熄灭时，画面四周跟着暗下来收向门的光，红灯铺开再亮回来。", Lens.Tunnel),
        Row(Leave.Brownout, Motion.Glide, Model.LetGo, Pocket.Carry, Reveal.Sweep, Restore.AsBuilt, true, true, false, false,
            "离港 + 暗收 + 一颤", "离港+暗收+一颤", "冲过 FD1：门动的第一声咔嗒，画面朝门那边颤一下、一秒内回来；四周也跟着灯熄灭暗下来。", Lens.TunnelFlinch),
    };

    // ================================================================ identity (the plate, the placard, the G panel)

    public override int Number => 3;
    public override string Name => "光走了";
    public override string TryLine => Rows[pickIndex].tryLine;

    public override string OptionLine
    {
        get
        {
            var line = "N：" + (pickIndex + 1) + "/" + Rows.Length + " · " + Rows[pickIndex].name + "；N 换下一种，Shift+N 往回";
            if (pickNext != pickIndex) line += "（已选 " + (pickNext + 1) + "/" + Rows.Length + " · " + Rows[pickNext].name + "，下次待触发时生效）";
            return line;
        }
    }

    /// <summary>HAUL rows layer on today's stepped recede (the frame reads this at each trip): its gaze gate, flinch, steps,
    /// keep-away glide and seats; B then sets its pace and, with RE-STACK PER STEP, its body (T2 by default).</summary>
    public override bool UsesTodayRecede => pick.motion == Motion.Haul;
    public override bool UsesBody => pick.motion == Motion.Haul;

    /// <summary>The magnets: the rest and final lines, and under HAUL the frame's step lines (BuildSchedule: the flinch to
    /// rest + 6, then a cell at a time).</summary>
    public override void Lines(FrontRoomsStructuresRunEndRoom room, List<float> lines)
    {
        lines.Add(room.RestLine);
        if (pick.motion == Motion.Haul)
            for (var line = room.RestLine + 6f; line < room.FinalLine - .01f; line += FrontRoomsStructuresRunEndRoom.CellLength) lines.Add(line);
        if (Mathf.Abs(room.FinalLine - room.RestLine) > .01f) lines.Add(room.FinalLine);
    }

    public override string Status(FrontRoomsStructuresRunEndRoom room)
    {
        if (room == null) return "";
        var phase = room.Phase;
        if (phase == FrontRoomsStructuresRunEndPhase.Resetting) return IsMoving(room) ? "门还在退完" : "";
        if (phase == FrontRoomsStructuresRunEndPhase.Tripped && passive) return "已经退过：只有警报灯";
        switch (beat)
        {
            case Beat.Gate:
                return swallowRun ? "灯在灭，门的光也要灭" : "等你看向尽头";
            case Beat.Haul:
                return room.HeadMoving ? "门被一格一格拖走" : "等你看向尽头";
            case Beat.Tell:
            case Beat.Travel:
            case Beat.Seat:
                if (swallowRun) return "门在黑里退";
                return breathRun ? "门的光一口一口退走" : "门的光在飘走";
            case Beat.Seated:
                if (phase != FrontRoomsStructuresRunEndPhase.Tripped) return "";
                if (runwayAt < 0f) return run.reveal == Reveal.None ? "走到哪亮到哪" : "门停下了";
                if (runwayReveal == Reveal.None) return "走到哪亮到哪";
                if (room.Clock < runwayFirst) return runwayReveal == Reveal.Overtake ? "等警报脉冲追上来" : "门停下了";
                if (room.Clock < runwayEnd)
                    return runwayReveal == Reveal.Overtake ? "警报脉冲铺向门口" : runwayReveal == Reveal.Counted ? "红灯一盏盏亮向门口" : "红灯铺向门口";
                return "门停下了";
        }
        if (phase == FrontRoomsStructuresRunEndPhase.Armed && !room.Receded)
        {
            var p = Rows[pickIndex];
            if (p.model == Model.Restack && !room.BodyT2) return p.shortName + " · 按 B 换 T2 才拆砖";
            if (p.lens == Lens.TunnelFlinch && !FlinchAllowed(room)) return p.shortName + " · 一颤换成四周暗一下";
            return p.shortName + " · L 只管身后的灯";
        }
        return "";
    }

    // ================================================================ numbers (each with its source)

    // ---- as built (c61562d4; 72 section 3.3; log_LightLeaves.txt; log_render.txt 19:32), unchanged

    /// <summary>72 3.3, the leave: "a front running away toward the door at 12 m/s; in 1.5 s the corridor ahead is black".</summary>
    const float FrontSpeed = 12f;
    /// <summary>72 3.3, the carry: "each strip strikes red as the player comes within 1.5 m of passing under it"; the render
    /// stage's fix of 11-04 19:32 adds the walker's own forward pace x the strike's rise (Leads).</summary>
    const float CarryLead = 1.5f;
    /// <summary>72 3.3 (the glide of #2, 72 3.2): "a 0.25 s tell, 0.5 s up to 8 m/s, cruise, braking, a heavy seat".</summary>
    const float Cruise = 8f, RampUp = .5f, RampDown = .5f, TellTime = .25f;
    /// <summary>The tell's 15 mm jolt and the heavy final seat (0.35 s, 30 mm): today's SP7 (RunRecede.BuildSchedule, 71 4.5).</summary>
    const float TellJolt = .015f, SeatTime = .35f, SeatAmp = .03f;
    /// <summary>72 3.3 Build: "SP7's gaze gate (35 degrees, fallback at 18 m)" (room.GazeDegrees; 71 section 4.3).</summary>
    const float GateFallback = 18f;
    /// <summary>F: 72 3.3 Risks, "never more than one change per strip in a second": a strip the walker reaches within 1.0 s is
    /// spared by the front (one cross-fade, white to red, when reached), a strike waits until 1.0 s after a fall began; a
    /// spared strip goes out after all when the walker stops (0.5 s of slack, so it never flips). The dark interval without F
    /// is the rig's headDelay (0.35 s, look B's own white-to-red interval).</summary>
    const float MinDarkReduced = 1f, SpareHorizon = 1f, SpareSlack = .5f;
    // Render stage 11-05 (W/level_space/light_leaves_render_log.txt): with F a spared strip that stayed white until the runner
    // reached it left a white lamp 4-7 m ahead of a sprinter while the lamps round the runner were red, so the walls ahead
    // flipped white / red at the lamp-passing rate (~1.8 Hz): the project's flash check (Tools/lookdev/flash_check.py, the area
    // rule) counted 3.5 a second after the trip with F on against 2.0 with F off. Every row but the as-built ones (rows 4 and
    // 18 keep c61562d4's F) now turns a spared strip red as the front reaches it, in F's one 0.5 s cross-fade: it still changes
    // once and never goes dark, and the lamps the runner will reach are red before it gets there.
    /// <summary>72 3.3 Look: the tripped fog near black (a maroon black, gamma), ExpSq .018 (from .014): 70 % of the door's
    /// light left at 33 m; in over the leave's 1.5 s, out in the re-strike's dark phase (the rig's resetRiseStart).</summary>
    static readonly Color DarkFog = new Color(.030f, .016f, .014f);
    const float DarkFogDensity = .018f, FogIn = 1.5f;
    /// <summary>SWALLOWED: the end starts 0.1 s after the front has taken the door's light; the goal unit is 1.2 m out.</summary>
    const float SwallowPause = .1f, GoalUnitOut = 1.2f;
    /// <summary>The goal head's nod on its bracket (a 3.2 Hz spring, damping 0.3, 0.11 degrees per m/s2 of the end's
    /// acceleration, kicks at the clacks and the seat); off with F.</summary>
    const float NodHz = 3.2f, NodDamping = .3f, NodGain = .11f, NodLimit = 4f, ClackKick = 20f, ThudKick = 35f;
    /// <summary>Seconds after which today's drive of a strip stands still (room.TrippedSince), plus this margin.</summary>
    const float BandMargin = .05f;

    // ---- (a) BROWNOUT (light_leaves_deepen 3.a)

    /// <summary>The front reaches a strip with the runner this close: a quick death (sag 1 -> 0.30 over 0.12 s, the arc out in
    /// 0.04 s), so the floor ahead stays lit; farther: the brownout (sag over 0.45 s while the light yellows, out in 0.08 s).
    /// F: one smooth 0.8 s sag to 0, no drop step.</summary>
    const float QuickReach = 6f, SagLevel = .30f, QuickSag = .12f, QuickDrop = .04f, BrownSag = .45f, BrownDrop = .08f, ReducedSag = .8f;
    /// <summary>Render stage 11-05 (W/level_space/light_leaves_render_log.txt): the reach is measured QuickPace seconds ahead of
    /// the runner's own pace. With the bare 6 m the sprinter's strip at 19.8 m sat on the line (5.94 m at 60 fps, 6.03 m at
    /// 30 fps, where the decision falls one frame later): at 30 fps it browned out, and the floor just ahead went near black for
    /// 0.37 s at trip + 1.6..2.0 s (luma 16-18 against the as-built's 36). The margin keeps every row's tables as designed
    /// (sprinter: 19.8 quick, 22.8 and 25.8 brown; walker: 16.8 quick, 19.8-25.8 brown; standing: 6 m) with 0.5 m or more
    /// either side of the line, at any frame rate.</summary>
    const float QuickPace = .10f;
    /// <summary>The brownout's colours: the strip Light toward amber (typed 1.00, .62, .32, gamma), the lens toward (1.0, .55,
    /// .25) x the rig's lens peak.</summary>
    static readonly Color AmberLight = new Color(1f, .62f, .32f);
    static readonly Color AmberLens = new Color(1f, .55f, .25f);
    /// <summary>The embers: the tube ends (rapid-start cathodes) glow HDR (1.0, .30, .05) x 1.6 from the arc's drop (a 0.10 s
    /// rise; F 0.3 s), decaying exp(-u / 0.8 s), off below 2 % (about 3.1 s). Sleeves 40 mm long round the 38 mm T12s.</summary>
    static readonly Color EmberHdr = new Color(1.6f, .48f, .08f);
    const float EmberTau = .8f, EmberOff = .02f, EmberRise = .10f, EmberRiseReduced = .30f, SleeveLength = .040f, SleeveRadius = .020f;

    // ---- (b) BREATH (light_leaves_deepen 3.b): each surge a 0.25 s pull to the peak and a 0.6 s ease to 1.5 m/s (the last to
    // rest); three surges at R 15 (peak 10.8 m/s, the end stops 2.80 s after the trip), two at R 9 (peak 9.8 m/s); a 20 deg/s
    // nod kick at each pull. F: the GLIDE.
    const float BreathPull = .25f, BreathEase = .6f, BreathLow = 1.5f, BreathKick = 20f;

    // ---- (c) LET GO (light_leaves_deepen 3.c c2)

    /// <summary>The lift at the clacks: 20-40 mm up and 1-4 degrees, each block 0.10 s, starting (1 - row) x 0.15 s into the tell.</summary>
    const float LiftMin = .020f, LiftMax = .040f, TiltMin = 1f, TiltMax = 4f, LiftStagger = .15f, LiftTime = .10f;
    /// <summary>The cloud over the ramp-up: a depth in -0.8 .. +1.4 m round the wall plane (the blocks round the door only
    /// behind it, +0.16 .. +1.4, clear of the goal bracket and the goal unit), a turn of 3-12 degrees, a spread of 6 / 5 cm.</summary>
    const float DepthNear = -.8f, DepthFar = 1.4f, DoorColumn = .80f, DoorColumnDepth = .16f, TurnMin = 3f, TurnMax = 12f, SpreadX = .06f, SpreadY = .05f;
    /// <summary>The lag behind the head's travel (0.06 + 0.06 x row s: at 8 m/s under 1 m of stretch), the bob (15 mm at
    /// 1.2-1.8 Hz) and a slow shimmer of 1.5 degrees at 0.6 Hz.</summary>
    const float LagBase = .06f, LagRow = .06f, BobAmp = .015f, BobHzMin = 1.2f, BobHzMax = 1.8f, ShimmerDeg = 1.5f, ShimmerHz = .6f;
    /// <summary>Never inside |x| &lt; 0.71 m and y &lt; 2.35 m (the casing, HeadCasingHalf 0.56, + 0.15 m: the leaf and the lite stay
    /// clear at any depth); inside the corridor's section |x| &lt;= 1.38 m, y &lt;= 2.86 m.</summary>
    const float WindowX = .71f, WindowY = 2.35f, SectionX = 1.38f, SectionY = 2.86f;
    /// <summary>The landing in the brake, bottom courses first: a block is home at the brake's start + 0.10 + 0.40 x row s (each
    /// over 0.25 s), 5 mm high, and settles over 0.12 s. Under HAUL the brake is when the head comes within 1.2 m of the final
    /// line.</summary>
    const float LandLead = .15f, LandBase = .10f, LandRow = .40f, SettleDrop = .005f, SettleTime = .12f, HaulLandDistance = 1.2f;
    /// <summary>F switched on while the cloud is up: every block folds back home together, its lag too, over 1.0 s (slow).</summary>
    const float CalmTime = 1f;

    // ---- (c) SPARKS (light_leaves_deepen 3.c c4): emitting while the end runs faster than 3 m/s, 30 a second at each soffit
    // trolley (head-local x +-1.25, y 2.86), 15 at the floor pin (x 0.95, y 0.01); 4 x 30-60 mm streaks, HDR (1.0, .45, .10) x 3
    // cooling to dark red over 0.35 s, 1 g, in world space (they stay behind).
    const float SparkSpeed = 3f, SparkRateTrolley = 30f, SparkRatePin = 15f, SparkLife = .35f, SparkWidth = .004f, SparkLenMin = .030f, SparkLenMax = .060f;
    static readonly Color SparkHot = new Color(3f, 1.35f, .30f);
    static readonly Color SparkCold = new Color(.35f, .03f, 0f);

    // ---- (d) the pockets (light_leaves_deepen 3.d)
    const float LongLead = 6f, WarmHeadLead = 6f, WarmStripLead = .5f, WarmStripLeadReduced = 1.5f;

    // ---- (e) COUNTED: one strip every 0.40 s (7.5 m/s).
    const float CountedSeconds = .40f;

    // ---- the door's light in the dark air (light_leaves_deepen 3.b, "what all three share")

    /// <summary>The beam: a cone from the goal heads' lenses to the floor before the leaf (head-local (0, 0, -0.45)), 0.9 m
    /// across at the floor, core 0.05 (two faces of 0.025), fogged, in with the fog over 1.5 s and out with it.</summary>
    const float BeamCore = .025f, BeamBase = .9f, BeamLens = .05f;
    /// <summary>Render stage 11-05: the cone's _Gain (the rig's own head beams run 16). At 1 the beam rendered invisible (+1.5 /
    /// 255 under the head at 3 m, nothing from the corridor); the REC rendered at 1 / 4 / 8 / 16 (renders/scene4/ll_beamtest):
    /// 8 reads as a soft shaft of the door's light in the air at the door and a glow under the head from afar, half the rig's
    /// own beam, so it stays fainter than the lamps' own cones in Run! B. Red's to judge (the design: if it reads as the
    /// door's light changing, it goes).</summary>
    const float BeamGain = 8f;
    static readonly Vector3 BeamAim = new Vector3(0f, 0f, -.45f);
    /// <summary>The grit: about 40 soft motes from the soffit seam at the rest line during the tell (24 at each seat under HAUL),
    /// falling 0.6 m over 1.2 s and drifting on, lit only by what lights that place: the strip just in front of the line (its
    /// Light 92 mm under the soffit, a 172 degree downlight; at the tell the last white lamp, which browns out at 1.4-1.9 s, so
    /// the grit glows amber and goes dark as the light leaves) and goal head 0 (only in front of it, 1 / (1 + (d / 1.2 m)^2);
    /// the goal heads come up only 0.46-0.64 s after the trip, by when the goal lamp has passed the tell's drop).</summary>
    const int GritPerDrop = 40, GritPerPuff = 24, GritPool = 96, SparkPool = 64;
    const float GritFall = .6f, GritFallTime = 1.2f, GritDrift = .12f, GritLife = 2.6f, GritAlpha = .25f, GritGain = 1f, GritFalloff = 1.2f;
    const float GritStripGain = 1.6f, StripLightDrop = .092f;

    // ---- (g) the lens (research/level_space/74 sections 4.1, 4.6, 5.4 and 6.1; rows 19 and 20)

    /// <summary>L1's vignette: Run_B's 0.32 / 0.20 (black, not rounded) to 0.38 / 0.28 (74 6.1: the side walls keep 0.58 of
    /// their light instead of 0.77, the corners 0.27 instead of 0.54, the floor in the lower fifth 0.87 instead of 0.92, the
    /// door 1.00). The override is the target; the volume's weight is the amount (URP lerps from the stack's own value).</summary>
    const float DimVignette = .38f, DimSmooth = .28f;
    /// <summary>L1's centre on the door (the goal leaf's middle, head-local (0, 1.05, 0)), clamped to 0.5 +- 0.05 of the
    /// screen "so it never swings" (74 6.1), eased over 0.25 s.</summary>
    const float LensCentreClamp = .05f, LensCentreTau = .25f;
    static readonly Vector3 DoorMiddle = new Vector3(0f, 1.05f, 0f);
    /// <summary>L1 follows the share of the light ahead that has left with a 0.12 s lag, at most 2.0 a second (no step when a
    /// strip leaves the count); it opens again over the runway (first strike to last + the rise), never faster than 0.8 s
    /// (74 6.1: "0.8 s at a sprint, 1.2 s at a walk"), or over 0.8 s from T.</summary>
    const float DimLag = .12f, DimRate = 2f, DimOutMin = .8f;
    /// <summary>L2, the one shudder at the first clack (74 6.1): lens distortion from the stack's rest (the film look's -0.04 in
    /// SP7) to -0.20 and chromatic aberration 0.06 -> 0.15, an ease-out up over 0.12 s and a smoothstep back, the whole of it
    /// 1.00 s ("back within 1 s"), x Camera Motion. F switched on during it folds the rest out over 0.3 s.</summary>
    const float FlinchDistortion = -.20f, FlinchChroma = .15f, FlinchUp = .12f, FlinchTotal = 1f, FlinchFold = .3f;
    /// <summary>L2's still cue at Camera Motion 0 or with F (74 5.5 / 6.1: "L1's edges deepen by 0.04 for 0.3 s and return"):
    /// on L1's volume, weight a + (1 - a) x 0.35 x cue and intensity 0.38 + 0.04 x cue x a (a = L1's amount): at a = 0 the
    /// side walls go 0.77 -> 0.708, what +0.04 alone gives; at a = 1 exactly +0.04. Up 0.12 s, held to 0.30 s, back over 0.40 s;
    /// with F 0.5 s up and 0.5 s back (F's fronts).</summary>
    const float CueWeight = .35f, CueDepth = .04f, CueUp = .12f, CueHold = .30f, CueDown = .40f, CueCalm = .5f;
    /// <summary>Above SP7's Run_B grade (1), the film look (0) and the structures' pool grade (100).</summary>
    const float LensPriority = 120f;

    // ================================================================ state

    enum Beat { Idle, Gate, Tell, Travel, Seat, Seated, Off, Haul }
    enum Death : byte { None, Quick, Brown, Reduced }
    const byte StrikeDark = 0, StrikeTurn = 1, StrikePulse = 2;

    // The row in force (the plate, UsesTodayRecede), the row N chose, and the row this trip runs.
    int pickIndex, pickNext;
    Pick pick = Rows[0];
    Pick run = Rows[0];

    Beat beat = Beat.Idle;
    float beatClock;
    bool secondClack;
    // The lamps: live from the trip until T's re-strike (or a reset); passive on a trip that moves nothing.
    bool lampsLive, passive;
    float frontFrom;
    // Per strip: driven here, spared by F's front, the front has passed it; when its white starts to go (+inf kept white,
    // -inf never lit: a hidden cell), how it died (BROWNOUT) and when it was dark, when its red strike starts and how.
    bool[] ahead, spared, frontSeen;
    float[] leaveAt, darkAt, strikeAt;
    Death[] death;
    byte[] strikeHow;
    FrontRoomsStructuresRunEndStrip[] white;
    // Per head lamp: driven here (held dark until carried or the runway), when it rises.
    bool[] headAhead;
    float[] headStrikeAt;
    float runwayAt = -1f, runwayFrom, runwayEnd = float.PositiveInfinity, runwayFirst = float.PositiveInfinity;
    bool runwayShown, runwayBegun;
    Reveal runwayReveal;
    // SWALLOWED: when the front takes the door's light and when the runway gives it back.
    bool swallowRun;
    float takenAt = float.PositiveInfinity, backAt = float.PositiveInfinity;
    float liteShown = 1f, liteResetFrom, liteResetAt;
    bool liteDriven, liteResetting;
    // The fog (mine while set) and the level it shows (the beam fades with it).
    bool fogMine, fogOut;
    float fogLevel, fogShown;
    // The goal unit's nod; the end's speed and acceleration this frame.
    Transform goalUnit;
    Quaternion goalUnitRest = Quaternion.identity;
    float nod, nodVel, headAccel, headSpeed;
    bool nodSet;
    // The motion: BREATH this run (F off at the tell), its last kicked surge; HAUL (the frame moves the end).
    bool breathRun, sparksRun;
    int breathKicked;
    bool haulRun, haulMoving, haulDone, pendingPuff;
    float haulLastSeat, pendingPuffZ;

    // The head's z over the last frames (the let-go's lag behind its travel).
    const int HistLength = 64;
    readonly float[] histTime = new float[HistLength];
    readonly float[] histZ = new float[HistLength];
    int histCount, histNext;

    // The let-go: the frame's blocks, their seats and their own numbers; the cloud's times (frame.time).
    Transform[] blk;
    float[] bSeatX, bSeatY, bHalfW, bHalfH, bRow, bLift, bTilt, bDepth, bTurn, bSpreadX, bSpreadY, bBobHz, bPhase;
    Vector3[] bTiltAxis, bTurnAxis;
    bool cloudOn;
    float cloudTell, cloudTravel, cloudLand = -1f, cloudHide = -1f, cloudCalm = -1f;

    // The embers: one sleeve renderer per strip, the level each shows, the level at T's re-strike.
    Renderer[] sleeves;
    float[] emberShown, emberFrom;

    // The beam under the goal head: its renderer and pose in the goal unit's frame.
    Renderer beam;
    Transform beamT;
    Vector3 beamApex, beamAxis = Vector3.down;

    // The grit motes and the sparks (pools, built once).
    Transform[] gT;
    Renderer[] gR;
    bool[] gLive;
    int[] gStrip;
    float[] gT0, gLife, gSize0, gSize1, gSpin, gFall;
    Vector3[] gP0, gDrift;
    Transform[] sT;
    Renderer[] sR;
    bool[] sLive;
    float[] sAge, sLife, sLen;
    Vector3[] sP, sV;
    float creditA, creditB, creditPin;
    int seed;

    // The lens (rows 19 and 20): the object with the trigger box and the two local volumes, their runtime profiles and the
    // overrides driven; lensLive while a trip runs it. L1: each counted strip's held fall, the share held, the amount in,
    // the release (its start and length on frame.time, the amount frozen there). L2: how the first clack is marked (the lens
    // shudder, the still cue, the calm cue), when, its size (Camera Motion) and F's fold.
    enum Shudder : byte { None, Lens, Cue, CueCalm }
    GameObject lensRoot;
    Volume dimVolume, flinchVolume;
    VolumeProfile dimProfile, flinchProfile;
    UrpVignette dimVignette;
    UrpLensDistortion flinchDistortion;
    UrpChromaticAberration flinchChroma;
    bool lensLive, dimOutPending, dimFrozenSet;
    bool[] dimCount;
    float[] dimFall;
    float dimHeld, dimIn, dimFrozen, dimOutAt = -1f, dimOutTime = DimOutMin;
    Shudder shudder;
    float shudderAt = -1f, shudderSize, shudderFold = -1f;
    Vector2 lensCentre = new Vector2(.5f, .5f);

    MaterialPropertyBlock mpb;
    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int DensityId = Shader.PropertyToID("_Density");

    bool IsMoving(FrontRoomsStructuresRunEndRoom room) =>
        beat == Beat.Tell || beat == Beat.Travel || beat == Beat.Seat || (beat == Beat.Haul && room != null && room.HeadMoving);

    // ================================================================ hooks: build, reset, exit

    public override void Build(FrontRoomsStructuresRunEndRoom room)
    {
        // The corridor, the head and the lamps stay the frame's (72 3.3); this file builds only the deepening's small things,
        // each on its own so one failing leaves the rest (logged once).
        EnsureArrays(room);
        goalUnit = room.GoalUnit;
        if (mpb == null) mpb = new MaterialPropertyBlock();
        try { BuildEmbers(room); }
        catch (System.Exception e) { sleeves = null; FrontRoomsPoolMaterials.LogOnce("SP7 #3 embers", "SP7 #3: the ember sleeves failed and are left out: " + e.Message); }
        try { BuildBeam(room); }
        catch (System.Exception e) { beam = null; FrontRoomsPoolMaterials.LogOnce("SP7 #3 beam", "SP7 #3: the beam under the goal head failed and is left out: " + e.Message); }
        try { BuildGrit(room); }
        catch (System.Exception e) { gT = null; FrontRoomsPoolMaterials.LogOnce("SP7 #3 grit", "SP7 #3: the grit failed and is left out: " + e.Message); }
        try { BuildSparks(room); }
        catch (System.Exception e) { sT = null; FrontRoomsPoolMaterials.LogOnce("SP7 #3 sparks", "SP7 #3: the sparks failed and are left out: " + e.Message); }
        try { BuildLens(room); }
        catch (System.Exception e)
        {
            // Rows 19 and 20 then run as row 1 (the REC without a camera layer).
            DropLens();
            FrontRoomsPoolMaterials.LogOnce("SP7 #3 lens", "SP7 #3: the lens volumes (rows 19-20) failed and are left out: " + e.Message);
        }
    }

    public override void Reset(FrontRoomsStructuresRunEndRoom room)
    {
        EnsureArrays(room);
        if (mpb == null) mpb = new MaterialPropertyBlock();
        ApplyPick();
        run = pick;
        lampsLive = passive = false;
        beat = Beat.Idle;
        beatClock = 0f;
        swallowRun = false;
        takenAt = backAt = float.PositiveInfinity;
        runwayAt = -1f;
        runwayShown = runwayBegun = false;
        runwayEnd = runwayFirst = float.PositiveInfinity;
        // The frame cleared the fog, the lite, the blocks and every pose before this (its own reset).
        fogMine = fogOut = false;
        fogLevel = fogShown = 0f;
        liteDriven = liteResetting = false;
        liteShown = 1f;
        nod = nodVel = headAccel = headSpeed = 0f;
        nodSet = false;
        breathRun = sparksRun = false;
        haulRun = haulMoving = haulDone = pendingPuff = false;
        haulLastSeat = room.RestLine;
        cloudOn = false;
        cloudLand = cloudHide = cloudCalm = -1f;
        if (goalUnit == null) goalUnit = room.GoalUnit;
        if (goalUnit != null) goalUnitRest = goalUnit.localRotation;
        for (var i = 0; i < ahead.Length; i++) ClearStrip(i);
        for (var i = 0; i < headAhead.Length; i++)
        {
            headAhead[i] = false;
            headStrikeAt[i] = float.PositiveInfinity;
        }
        HistoryClear(0f, room.HeadZ);
        HideOwn();
        // The lens: weights 0, volumes off, its object inactive (no trigger) until a row-19/20 trip.
        LensOff();
    }

    public override void Exit(FrontRoomsStructuresRunEndRoom room)
    {
        // The frame puts back the head, the blocks, the lite, the fog and the lamps; this file's own things hide with Own.
        lampsLive = passive = false;
        beat = Beat.Idle;
        cloudOn = false;
        if (goalUnit != null && nodSet) goalUnit.localRotation = goalUnitRest;
        nodSet = false;
        nod = nodVel = 0f;
        HideOwn();
        LensOff();
    }

    public override void Release(FrontRoomsStructuresRunEndRoom room)
    {
        // The scene stops: the lens's two runtime profiles and their overrides are ScriptableObjects that neither the Own root
        // nor room.Own frees (everything else of this file is under Own or owned by the room).
        DropLens();
    }

    // ================================================================ hooks: the trip, the frame, the seats, the re-strike

    public override void Trip(FrontRoomsStructuresRunEndRoom room)
    {
        // A trip that moves nothing (at the entrance once the end has receded, until Y; or while a recede still finishes):
        // today's look only.
        if (room.Receded || IsMoving(room) || Mathf.Abs(room.HeadZ - room.RestLine) > .01f)
        {
            passive = true;
            room.Dirty();
            return;
        }
        passive = false;
        run = pick;
        swallowRun = run.swallow;
        haulRun = run.motion == Motion.Haul;
        haulMoving = haulDone = pendingPuff = false;
        haulLastSeat = room.RestLine;
        breathRun = sparksRun = false;
        takenAt = backAt = float.PositiveInfinity;
        runwayAt = -1f;
        runwayShown = runwayBegun = false;
        runwayEnd = runwayFirst = float.PositiveInfinity;
        // The lamps armed at the trip itself, from the walker's feet (room.Feet).
        ArmLamps(room, room.Feet.z);
        // The lens (rows 19 and 20) counts the strips the front will take from here.
        LensArm(room);
        fogMine = true;
        fogOut = false;
        if (haulRun)
        {
            // Today's schedule waits for its own gaze gate (the frame set it pending before this hook).
            SetBeat(room, Beat.Haul);
            return;
        }
        room.Waiting = true;
        SetBeat(room, Beat.Gate);
    }

    public override void Tick(FrontRoomsStructuresRunEndRoom room, FrontRoomsStructuresRunEndFrame frame)
    {
        headAccel = headSpeed = 0f;
        var tripped = room.Phase == FrontRoomsStructuresRunEndPhase.Tripped;
        if (lampsLive && tripped) Carry(room, frame);
        Glide(room, frame);
        HaulTrack(room, frame);
        HistoryPush(frame.time, room.HeadZ);
        if (haulRun && haulMoving) headAccel = HistoryAccel();
        PlateBeats(room);
        DriveFog(room);
        DriveLite(room);
        Nod(room, frame.dt);
        LetGo(room, frame);
        Embers(room);
        DriveBeam(room);
        DriveGrit(room, frame);
        DriveSparks(room, frame);
        // Last: the first clack may have come in Glide this frame.
        DriveLens(room, frame);
    }

    public override void Seated(FrontRoomsStructuresRunEndRoom room, float from, float to)
    {
        // Only today's seats under HAUL (this file's own glide seats once, at the end, and needs nothing here): a puff of grit
        // from the soffit seam where the wall lands (spawned on the next frame), and the goal lamp shudders.
        if (!haulRun) return;
        haulLastSeat = to;
        if (run.grit)
        {
            pendingPuff = true;
            pendingPuffZ = to;
        }
        Kick(room, ThudKick * .6f);
    }

    public override void Done(FrontRoomsStructuresRunEndRoom room)
    {
        // Today's final seat under HAUL: the runway (this file's own glide starts it after its own room.Done).
        if (!haulRun) return;
        haulDone = true;
        var live = lampsLive && room.Phase == FrontRoomsStructuresRunEndPhase.Tripped;
        SetBeat(room, Beat.Seated);
        if (live) StartRunway(room);
    }

    public override void ResetStarted(FrontRoomsStructuresRunEndRoom room)
    {
        // Today's white re-strike walks from the entrance on every lamp (the frame released every hold): this file's lamps let
        // go now. A recede waiting for its look is called off with the alarm; one already moving finishes (never stops midway).
        lampsLive = passive = false;
        if (beat == Beat.Gate) SetBeat(room, Beat.Off);
        if (beat == Beat.Haul && !room.HeadMoving) SetBeat(room, Beat.Off);
        if (fogMine) fogOut = true;
        // L1 opens over 0.8 s from here if the runway has not opened it already (a shudder still running ends on its own).
        if (lensLive) dimOutPending = true;
        // The embers fade out over the re-strike's dark phase, from where they stand.
        if (emberShown != null)
            for (var i = 0; i < emberShown.Length; i++) emberFrom[i] = emberShown[i];
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
        if (beat == Beat.Gate || beat == Beat.Off || beat == Beat.Seated || (beat == Beat.Haul && !room.HeadMoving)) beat = Beat.Idle;
        if (fogMine)
        {
            room.ClearFog();
            fogMine = fogOut = false;
            fogLevel = fogShown = 0f;
        }
        if (liteDriven || liteResetting) SetLiteLevel(room, 1f);
        liteDriven = liteResetting = false;
        LensOff();
        ApplyPick();
        room.Dirty();
    }

    // ================================================================ the lamps (72 3.3 and light_leaves_deepen 3.a, 3.d, 3.e)

    // At the trip: the walker's feet are where the front starts (CUT, BROWNOUT; TIDE: the last lit strip ahead). Behind them
    // today's look; ahead every lit strip goes as the front reaches it, the hidden cells' strips stay dark, the heads ahead
    // are held dark.
    void ArmLamps(FrontRoomsStructuresRunEndRoom room, float feetZ)
    {
        EnsureArrays(room);
        frontFrom = Mathf.Clamp(feetZ, 0f, FrontRoomsStructuresRunEndRoom.Length);
        var tideFrom = frontFrom;
        if (run.leave == Leave.Tide)
            for (var i = 0; i < ahead.Length; i++)
            {
                var z = room.StripZ(i);
                if (room.CellOpen(room.StripCell(i)) && z >= frontFrom && z < room.HeadZ && z > tideFrom) tideFrom = z;
            }
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
                leaveAt[i] = run.leave == Leave.Tide ? (tideFrom - z) / FrontSpeed : (z - frontFrom) / FrontSpeed;
            }
        }
        // The goal heads are today's (rule 7); the heads beside and behind the walker rise as the look raises them.
        var near = run.pocket == Pocket.Long ? LongLead : run.pocket == Pocket.Warm ? WarmHeadLead : CarryLead;
        for (var i = 0; i < headAhead.Length; i++)
        {
            headStrikeAt[i] = float.PositiveInfinity;
            headAhead[i] = !room.IsGoalHead(i) && !(room.CellOpen(room.HeadCell(i)) && room.HeadLampZ(i) <= frontFrom + near);
        }
        // SWALLOWED: the front reaches the door's line and takes its light too.
        takenAt = swallowRun ? Mathf.Max(0f, room.RestLine - frontFrom) / FrontSpeed : float.PositiveInfinity;
        lampsLive = true;
    }

    // The leads by the pocket (d): the strips' and the heads' (the walker's own forward pace taken at the strike's rise).
    void Leads(RunRigLayout rig, bool reduce, float pace, out float lead, out float headLead)
    {
        switch (run.pocket)
        {
            case Pocket.Long:
                lead = LongLead + pace * (reduce ? rig.reducedEnvelope : rig.redRise);
                headLead = lead;
                return;
            case Pocket.Warm:
                // 0.5 m (F 1.5 m) as designed, plus the strike's rise at the walker's pace (as the carry's 19:32 fix): the strip
                // is fully red as the walker comes under it. The bare 0.5 m left the nearest lit strip up to 4.5 m from a
                // sprinter's feet for 1.2 s (this stage's sim), against "the floor under the walker always lit".
                lead = (reduce ? WarmStripLeadReduced : WarmStripLead) + pace * (reduce ? rig.reducedEnvelope : rig.redRise);
                headLead = WarmHeadLead + pace * (reduce ? rig.reducedEnvelope : rig.headRise);
                return;
            default:
                // Render stage 11-04 (renders/scene4/run_ends2): with the bare 1.5 m the floor just ahead fell to luma 8 between
                // strips at a sprint; the strike's rise taken at the walker's pace keeps one cell ahead lit.
                lead = CarryLead + pace * (reduce ? rig.reducedEnvelope : rig.redRise);
                headLead = lead;
                return;
        }
    }

    // Every tripped frame: F's spare test and BROWNOUT's death where the front arrives, the carry (the walker within the lead).
    void Carry(FrontRoomsStructuresRunEndRoom room, FrontRoomsStructuresRunEndFrame frame)
    {
        var t = room.Clock;
        var feet = frame.feet.z;
        var reduce = room.Reduce;
        var wall = room.HeadZ;
        var rig = room.Rig;
        var pace = Mathf.Max(0f, frame.own.z);
        Leads(rig, reduce, pace, out var lead, out var headLead);
        for (var i = 0; i < ahead.Length; i++)
        {
            if (!ahead[i] || strikeAt[i] <= t) continue;
            var z = room.StripZ(i);
            if (!frontSeen[i] && t >= leaveAt[i])
            {
                frontSeen[i] = true;
                if (reduce && Reaches(frame, z, SpareHorizon, lead))
                {
                    spared[i] = true;
                    leaveAt[i] = float.PositiveInfinity;
                    // F (render stage 11-05, see SpareHorizon): red now, one cross-fade from white, not when reached.
                    if (!run.asBuilt && z < wall) Trigger(room, i, t, StrikeTurn);
                }
                else Die(i, z - feet, pace, reduce);
            }
            else if (spared[i] && (!reduce || !Reaches(frame, z, SpareHorizon + SpareSlack, lead)))
            {
                // The walker stopped short of a spared strip (or F went off): it goes out now, after all.
                spared[i] = false;
                leaveAt[i] = t;
                Die(i, z - feet, pace, reduce);
            }
            // Only lamps in front of the end wall (behind it the cells stay hidden).
            if (feet >= z - lead && z < wall) Trigger(room, i, t, StrikeDark);
        }
        for (var i = 0; i < headAhead.Length; i++)
        {
            if (!headAhead[i] || headStrikeAt[i] <= t) continue;
            var z = room.HeadLampZ(i);
            if (feet >= z - headLead && z < wall) headStrikeAt[i] = t;
        }
    }

    // The walker will come within the lead of z inside the horizon at its own forward pace.
    static bool Reaches(FrontRoomsStructuresRunEndFrame frame, float z, float horizon, float lead) =>
        frame.feet.z + Mathf.Max(0f, frame.own.z) * horizon >= z - lead;

    // The front has reached strip i: how it dies (BROWNOUT; the others drop as the rig drops them) and when it is dark. The
    // reach is the runner's distance QuickPace seconds on at its own pace (the 11-05 render stage's margin, see QuickPace).
    void Die(int i, float distance, float pace, bool reduce)
    {
        if (run.leave != Leave.Brownout)
        {
            death[i] = Death.None;
            darkAt[i] = leaveAt[i];
            return;
        }
        if (reduce)
        {
            death[i] = Death.Reduced;
            darkAt[i] = leaveAt[i] + ReducedSag;
        }
        else if (distance - pace * QuickPace <= QuickReach)
        {
            death[i] = Death.Quick;
            darkAt[i] = leaveAt[i] + QuickSag + QuickDrop;
        }
        else
        {
            death[i] = Death.Brown;
            darkAt[i] = leaveAt[i] + BrownSag + BrownDrop;
        }
    }

    // The earliest a strip that went dark may strike red: the rig's headDelay after the fall began (CUT, TIDE, as built) or
    // after the arc dropped (BROWNOUT); F: 1.0 s after the fall began (BROWNOUT's 0.8 s sag: 0.2 s dark).
    float EarliestStrike(FrontRoomsStructuresRunEndRoom room, int i)
    {
        var reduce = room.Reduce;
        if (death[i] != Death.None)
            return death[i] == Death.Reduced || reduce ? leaveAt[i] + Mathf.Max(MinDarkReduced, ReducedSag + .2f) : darkAt[i] + room.Rig.headDelay;
        return leaveAt[i] + (reduce ? MinDarkReduced : room.Rig.headDelay);
    }

    // A strike wanted at a time (the carry now, the runway at its own time): from dark never sooner than the dark interval;
    // from white (the walker there before the front, or a strip F spared) one cross-fade, the front's dark called off. An
    // earlier wish replaces a later one that has not started (the carry overtakes a slow runway).
    void Trigger(FrontRoomsStructuresRunEndRoom room, int i, float at, byte how)
    {
        if (strikeAt[i] <= room.Clock) return;
        var leave = leaveAt[i];
        float when;
        byte kind;
        if (float.IsNegativeInfinity(leave))
        {
            when = at;
            kind = how;
        }
        else if (leave <= at)
        {
            when = Mathf.Max(at, EarliestStrike(room, i));
            kind = how == StrikePulse && when > at + 1e-4f ? StrikeDark : how;
        }
        else
        {
            when = at;
            kind = StrikeTurn;
        }
        if (when >= strikeAt[i]) return;
        strikeAt[i] = when;
        strikeHow[i] = kind;
        if (kind == StrikeTurn)
        {
            leaveAt[i] = float.PositiveInfinity;
            frontSeen[i] = true;
            spared[i] = false;
            death[i] = Death.None;
        }
    }

    // The runway, from the final seat: every strip still dark ahead strikes by the row's reveal (SWEEP from the feet at the
    // rig's frontSpeed; COUNTED one lamp every 0.40 s; OVERTAKE when B's first pulse reaches it, falling back to the sweep with
    // F or a look without pulses; NONE: the carry alone); the heads with it; SWALLOWED's light comes back as the front passes.
    void StartRunway(FrontRoomsStructuresRunEndRoom room)
    {
        var rig = room.Rig;
        runwayAt = room.Clock;
        runwayShown = runwayBegun = false;
        runwayFrom = Mathf.Clamp(room.Feet.z, 0f, FrontRoomsStructuresRunEndRoom.Length);
        runwayReveal = run.reveal;
        if (runwayReveal == Reveal.Overtake && (room.Reduce || room.Look != 2)) runwayReveal = Reveal.Sweep;
        runwayFirst = runwayEnd = float.PositiveInfinity;
        if (runwayReveal == Reveal.None)
        {
            room.Dirty();
            return;
        }
        // B's first pulse leaves the entrance strip at the frame's PulseFirst and runs at 0.15 s a cell (the rig's frontSpeed).
        var first = Mathf.Max(rig.pulseFirst, room.FinalSeatTime + 1f);
        var z0 = room.StripCount > 0 ? room.StripZ(0) : 1.8f;
        var speed = Mathf.Max(.1f, rig.frontSpeed);
        var counted = FrontRoomsStructuresRunEndRoom.CellLength / Mathf.Max(.05f, CountedSeconds);
        // To the door only: at R 9 the cells past 36 m stay hidden behind the end wall, their lamps untouched.
        var wall = room.HeadZ;
        for (var i = 0; i < ahead.Length; i++)
        {
            var z = room.StripZ(i);
            if (!ahead[i] || strikeAt[i] <= runwayAt || z >= wall) continue;
            float at;
            var how = StrikeDark;
            if (runwayReveal == Reveal.Overtake)
            {
                // The strip joins the pulse at its floor (the dip's 0.35, then B's own rise): one step from dark, as a strike.
                at = first + (z - z0) / speed + rig.pulseFall;
                how = StrikePulse;
            }
            else if (runwayReveal == Reveal.Counted) at = runwayAt + Mathf.Max(0f, z - runwayFrom) / counted;
            else at = runwayAt + Mathf.Max(0f, z - runwayFrom) / speed;
            Trigger(room, i, at, how);
            if (strikeAt[i] < runwayFirst) runwayFirst = strikeAt[i];
        }
        for (var i = 0; i < headAhead.Length; i++)
        {
            var z = room.HeadLampZ(i);
            if (!headAhead[i] || headStrikeAt[i] <= runwayAt || z >= wall) continue;
            float at;
            if (runwayReveal == Reveal.Overtake) at = first + (z - z0) / speed;
            else if (runwayReveal == Reveal.Counted) at = runwayAt + Mathf.Max(0f, z - runwayFrom) / counted;
            else at = runwayAt + Mathf.Max(0f, z - runwayFrom) / speed;
            if (at < headStrikeAt[i]) headStrikeAt[i] = at;
        }
        var rise = room.Reduce ? rig.reducedEnvelope : rig.redRise;
        var far = Mathf.Max(0f, room.FinalLine - runwayFrom);
        if (runwayReveal == Reveal.Overtake) runwayEnd = first + Mathf.Max(0f, room.FinalLine - z0) / speed + rig.pulseFall + rig.pulseHold + rig.pulseRise;
        else if (runwayReveal == Reveal.Counted) runwayEnd = runwayAt + far / counted + rise;
        else runwayEnd = runwayAt + far / speed + rise;
        if (runwayFirst > runwayEnd) runwayFirst = runwayAt;
        if (swallowRun) backAt = runwayAt + Mathf.Max(0f, room.HeadZ - GoalUnitOut - runwayFrom) / speed;
        room.Dirty();
    }

    // The plate's state line as the runway begins and reaches the door.
    void PlateBeats(FrontRoomsStructuresRunEndRoom room)
    {
        if (runwayAt < 0f) return;
        var t = room.Clock;
        if (!runwayBegun && t >= runwayFirst)
        {
            runwayBegun = true;
            room.Dirty();
        }
        if (!runwayShown && t >= runwayEnd)
        {
            runwayShown = true;
            room.Dirty();
        }
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
            var k = StrikeCurve(rig, reduce, t - at, strikeHow[i]);
            shown = FrontRoomsStructuresRunEndStrip.Lerp(shown, Target(room, i, t, s), k);
        }
        shown.open = s.open;
        s = shown;
    }

    // A strip before (and under) its strike: the white it showed at the trip until the front reaches it, then its fall. CUT
    // and TIDE: Run! B's mains drop (0.06 s, linear) with the lens's afterglow (0.03, a 0.5 s tau), as today's look drops it;
    // F a 0.5 s cross-fade. BROWNOUT: Brown. A hidden cell's strip: dark.
    FrontRoomsStructuresRunEndStrip Base(RunRigLayout rig, bool reduce, int i, float t, FrontRoomsStructuresRunEndStrip s)
    {
        var at = leaveAt[i];
        if (float.IsNegativeInfinity(at)) return s.AsDark();
        var w = white[i];
        if (t < at) return w;
        var u = t - at;
        if (death[i] != Death.None) return Brown(rig, i, u, w);
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

    // BROWNOUT's death u seconds after the front: the sag (the Light and the lens toward amber as they fall), the arc's drop,
    // the lens's afterglow after it (the embers on the tube ends are the sleeves, Embers).
    FrontRoomsStructuresRunEndStrip Brown(RunRigLayout rig, int i, float u, FrontRoomsStructuresRunEndStrip w)
    {
        float level, amber;
        var kind = death[i];
        if (kind == Death.Reduced)
        {
            amber = Smooth(u / ReducedSag);
            level = 1f - amber;
        }
        else
        {
            var sag = kind == Death.Quick ? QuickSag : BrownSag;
            var drop = kind == Death.Quick ? QuickDrop : BrownDrop;
            if (u < sag)
            {
                amber = Smooth(u / sag);
                level = 1f - (1f - SagLevel) * amber;
            }
            else
            {
                amber = 1f;
                level = SagLevel * Mathf.Clamp01(1f - (u - sag) / drop);
            }
        }
        var end = Mathf.Max(.01f, darkAt[i] - leaveAt[i]);
        var glow = u <= end ? rig.afterglow : rig.afterglow * Mathf.Exp(-(u - end) / Mathf.Max(.01f, rig.afterglowTau));
        var d = w;
        d.white = w.white * level;
        d.lens = w.lens * Mathf.Lerp(glow, 1f, level);
        d.red = w.red * level;
        d.tube = w.tube * level;
        d.whiteColour = Color.Lerp(w.whiteColour, AmberLight.linear, amber);
        d.lensColour = Color.Lerp(w.lensColour, AmberLens * rig.stripLensEmission.maxColorComponent, amber);
        return d;
    }

    // What a struck strip turns into: today's drive once its look's own front has passed it (room.TrippedSince: so B's pulses
    // run on it after the seat), before that the look's own tripped strip (room.TrippedStrip).
    static FrontRoomsStructuresRunEndStrip Target(FrontRoomsStructuresRunEndRoom room, int i, float t, FrontRoomsStructuresRunEndStrip s)
    {
        if (s.open && t >= room.TrippedSince(i) + BandMargin) return s;
        return room.TrippedStrip(i);
    }

    // The red strike, the rig's: from dark 0 -> redStrikeLevel at once, then a smoothstep to 1 over redRise; from white one
    // cross-fade over redRise; with B's pulse (OVERTAKE) at once onto the pulse's own floor and rise; F a 0.5 s cross-fade.
    static float StrikeCurve(RunRigLayout rig, bool reduce, float u, byte how)
    {
        if (u < 0f) return 0f;
        if (reduce) return Smooth(u / Mathf.Max(.01f, rig.reducedEnvelope));
        if (how == StrikeTurn) return Smooth(u / Mathf.Max(.01f, rig.redRise));
        if (how == StrikePulse) return 1f;
        return rig.redStrikeLevel + (1f - rig.redStrikeLevel) * Smooth(u / Mathf.Max(.01f, rig.redRise));
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
        // The rig's head rise (an ease-in square over 0.18 s; F: a 0.5 s smoothstep), to the head's own level.
        var rig = room.Rig;
        h.level = room.Reduce ? Smooth(u / Mathf.Max(.01f, rig.reducedEnvelope)) : Square(Mathf.Clamp01(u / Mathf.Max(.01f, rig.headRise)));
    }

    // SWALLOWED: the door's light (the goal heads and C42) as the front takes it and as the runway gives it back.
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

    // ================================================================ the end: gate, tell, glide or breath, heavy seat

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
                if (go) StartTell(room, frame.time);
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
                    // The line it leaves keeps its ghost band.
                    room.Leaving(rest);
                    SetBeat(room, Beat.Travel);
                }
                break;
            }
            case Beat.Travel:
            {
                beatClock += frame.dt;
                var length = final - rest;
                var total = TravelTime(length);
                room.PlaceHead(rest + TravelAt(room, beatClock, length), true);
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

    void StartTell(FrontRoomsStructuresRunEndRoom room, float now)
    {
        room.Waiting = false;
        room.HeadMoving = true;
        // The cells it uncovers drawn, dark (72 3.3: "the hidden cells are revealed in the dark, so nobody sees them appear").
        room.ShowBetween(room.RestLine, room.FinalLine);
        beatClock = 0f;
        secondClack = false;
        Kick(room, -ClackKick);
        SetBeat(room, Beat.Tell);
        // The motion is fixed for the run here (F at the tell: no surges, no let-go, no sparks).
        var reduce = room.Reduce;
        breathRun = run.motion == Motion.Breath && !reduce;
        breathKicked = -1;
        sparksRun = run.model == Model.Sparks && !reduce;
        creditA = creditB = creditPin = 0f;
        var length = room.FinalLine - room.RestLine;
        var total = TravelTime(length);
        var brake = breathRun ? BreathEase : RampDown;
        if (run.model == Model.LetGo && !reduce)
            StartCloud(room, now, now + TellTime, now + TellTime + total - brake, now + TellTime + total + SeatTime);
        // One drop of grit from the soffit seam at the rest line, in the goal lamp's light (left behind as it goes).
        if (run.grit) SpawnGrit(room, room.RestLine, now, GritPerDrop, .35f);
        // L2 (row 20): the first clack is marked once, now: the lens shudder x Camera Motion, or (Camera Motion 0, or F) the
        // still cue on L1's edges.
        if (lensLive && run.lens == Lens.TunnelFlinch) StartShudder(room, now);
    }

    float TravelTime(float length) => breathRun ? BreathTime(length) : MoveTime(length, Cruise, RampUp, RampDown);

    // The end's travel at t: GLIDE (Move) or BREATH (the surges), with its speed and acceleration (the nod, the sparks).
    float TravelAt(FrontRoomsStructuresRunEndRoom room, float t, float length)
    {
        if (breathRun)
        {
            var d = BreathAt(t, length, out headSpeed, out headAccel);
            var surge = Mathf.FloorToInt(Mathf.Max(0f, t) / (BreathPull + BreathEase));
            if (surge > breathKicked && surge < BreathSurges(length))
            {
                breathKicked = surge;
                Kick(room, -BreathKick);
            }
            return d;
        }
        headAccel = MoveAccel(t, length, Cruise, RampUp, RampDown);
        headSpeed = MoveSpeed(t, length, Cruise, RampUp, RampDown);
        return Move(t, length, Cruise, RampUp, RampDown);
    }

    // The ramps of FrontRoomsStructuresRunEnd.Move (both scaled down when the move is too short to reach the cruise).
    static void Ramps(float length, ref float cruise, ref float accel, ref float brake, out float cruiseTime)
    {
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
        cruiseTime = (length - ramps) / cruise;
    }

    // The acceleration of Move at t (m/s2): the ramp up, the cruise, the brake.
    static float MoveAccel(float t, float length, float cruise, float accel, float brake)
    {
        if (t <= 0f || length <= 0f) return 0f;
        Ramps(length, ref cruise, ref accel, ref brake, out var cruiseTime);
        if (t < accel) return accel > 1e-4f ? cruise / accel : 0f;
        if (t < accel + cruiseTime) return 0f;
        if (t < accel + cruiseTime + brake) return brake > 1e-4f ? -cruise / brake : 0f;
        return 0f;
    }

    // The speed of Move at t (m/s).
    static float MoveSpeed(float t, float length, float cruise, float accel, float brake)
    {
        if (t <= 0f || length <= 0f) return 0f;
        Ramps(length, ref cruise, ref accel, ref brake, out var cruiseTime);
        if (t < accel) return accel > 1e-4f ? cruise * t / accel : cruise;
        if (t < accel + cruiseTime) return cruise;
        if (t < accel + cruiseTime + brake) return brake > 1e-4f ? cruise * (1f - (t - accel - cruiseTime) / brake) : 0f;
        return 0f;
    }

    // BREATH: n surges (each a pull from the low speed to the peak, then an ease back; the first from rest, the last to rest),
    // speed ramps in smoothsteps (a ramp covers its mean speed x its time), the peak chosen so the surges cover the length.
    static int BreathSurges(float length) => length >= 12f ? 3 : 2;
    static float BreathTime(float length) => BreathSurges(length) * (BreathPull + BreathEase);

    static float BreathPeak(float length)
    {
        var n = BreathSurges(length);
        var fixedPart = .5f * (BreathPull + BreathEase) * BreathLow * (n - 1);
        return Mathf.Max(BreathLow + .1f, (length - fixedPart) / (.5f * n * (BreathPull + BreathEase)));
    }

    static float BreathAt(float t, float length, out float speed, out float accel)
    {
        speed = accel = 0f;
        if (t <= 0f || length <= 0f) return 0f;
        var n = BreathSurges(length);
        var peak = BreathPeak(length);
        var d = 0f;
        for (var j = 0; j < 2 * n; j++)
        {
            var surge = j / 2;
            float a, b, dur;
            if ((j & 1) == 0)
            {
                a = surge == 0 ? 0f : BreathLow;
                b = peak;
                dur = BreathPull;
            }
            else
            {
                a = peak;
                b = surge == n - 1 ? 0f : BreathLow;
                dur = BreathEase;
            }
            if (t < dur)
            {
                var x = t / dur;
                speed = a + (b - a) * x * x * (3f - 2f * x);
                accel = (b - a) / dur * 6f * x * (1f - x);
                return Mathf.Min(length, d + a * t + (b - a) * dur * (x * x * x - .5f * x * x * x * x));
            }
            d += .5f * (a + b) * dur;
            t -= dur;
        }
        return length;
    }

    // HAUL: today's schedule moves the end (its gate, steps, keep-away glide, seats); this file follows it for the let-go,
    // the grit puffs and the status.
    void HaulTrack(FrontRoomsStructuresRunEndRoom room, FrontRoomsStructuresRunEndFrame frame)
    {
        if (!haulRun) return;
        if (pendingPuff)
        {
            pendingPuff = false;
            SpawnGrit(room, pendingPuffZ, frame.time, GritPerPuff, .2f);
        }
        if (haulDone)
        {
            if (cloudOn)
            {
                if (cloudLand < 0f) cloudLand = frame.time - .35f;
                if (cloudHide < 0f) cloudHide = frame.time;
            }
            haulMoving = false;
            return;
        }
        var moving = room.HeadMoving;
        if (moving && !haulMoving)
        {
            // The frame's gaze gate opened: its first tell (0.25 s, the flinch's) starts now.
            haulMoving = true;
            room.Dirty();
            if (run.model == Model.LetGo && !room.Reduce) StartCloud(room, frame.time, frame.time + TellTime, -1f, -1f);
        }
        // The brake of the last approach: the head within 1.2 m of the final line, moving.
        if (cloudOn && cloudLand < 0f && moving && room.HeadZ > haulLastSeat + .01f && room.FinalLine - room.HeadZ < HaulLandDistance)
            cloudLand = frame.time;
    }

    // ================================================================ the head's recent travel (the let-go's lag; HAUL's nod)

    void HistoryClear(float time, float z)
    {
        histCount = histNext = 0;
        HistoryPush(time, z);
    }

    void HistoryPush(float time, float z)
    {
        if (histCount > 0)
        {
            var last = (histNext - 1 + HistLength) % HistLength;
            if (time <= histTime[last] + 1e-6f)
            {
                histZ[last] = z;
                return;
            }
        }
        histTime[histNext] = time;
        histZ[histNext] = z;
        histNext = (histNext + 1) % HistLength;
        if (histCount < HistLength) histCount++;
    }

    // The head's z at an earlier time (linear between frames).
    float HistoryZ(float time)
    {
        if (histCount == 0) return 0f;
        var newest = (histNext - 1 + HistLength) % HistLength;
        if (time >= histTime[newest]) return histZ[newest];
        for (var n = 1; n < histCount; n++)
        {
            var i = (newest - n + HistLength) % HistLength;
            if (time < histTime[i]) continue;
            var j = (i + 1) % HistLength;
            var span = histTime[j] - histTime[i];
            return Mathf.Lerp(histZ[i], histZ[j], span > 1e-6f ? (time - histTime[i]) / span : 1f);
        }
        return histZ[(newest - (histCount - 1) + HistLength) % HistLength];
    }

    // The head's acceleration over the last three frames (the frame's own steps under HAUL), clamped.
    float HistoryAccel()
    {
        if (histCount < 3) return 0f;
        var a = (histNext - 1 + HistLength) % HistLength;
        var b = (a - 1 + HistLength) % HistLength;
        var c = (b - 1 + HistLength) % HistLength;
        var v1 = (histZ[b] - histZ[c]) / Mathf.Max(1e-4f, histTime[b] - histTime[c]);
        var v2 = (histZ[a] - histZ[b]) / Mathf.Max(1e-4f, histTime[a] - histTime[b]);
        return Mathf.Clamp((v2 - v1) / Mathf.Max(1e-4f, .5f * (histTime[a] - histTime[c])), -40f, 40f);
    }

    // ================================================================ the let-go (light_leaves_deepen 3.c c2 and 6.3)

    // The frame's ~90 T2 blocks (0.40 x 0.20 m, 0.156 m deep, district space under the district root, parked hidden). The
    // frame keeps their seats to itself (REQUEST: room.BlockSeat / BlockRow); today's T2 path at t 0 puts every block on its
    // seat at a line, so the seats are read back from there once.
    bool ReadBlocks(FrontRoomsStructuresRunEndRoom room)
    {
        if (blk != null) return blk.Length > 0;
        var root = room.Blocks;
        var n = root != null ? root.childCount : 0;
        blk = new Transform[n];
        bSeatX = new float[n];
        bSeatY = new float[n];
        bHalfW = new float[n];
        bHalfH = new float[n];
        bRow = new float[n];
        bLift = new float[n];
        bTilt = new float[n];
        bDepth = new float[n];
        bTurn = new float[n];
        bSpreadX = new float[n];
        bSpreadY = new float[n];
        bBobHz = new float[n];
        bPhase = new float[n];
        bTiltAxis = new Vector3[n];
        bTurnAxis = new Vector3[n];
        if (n == 0) return false;
        var line = room.HeadZ;
        room.FlyBlocks(0f, 3f, line, line);
        for (var j = 0; j < n; j++)
        {
            var t = root.GetChild(j);
            blk[j] = t;
            var p = t.localPosition;
            var s = t.localScale;
            bSeatX[j] = p.x;
            bSeatY[j] = p.y;
            bHalfW[j] = .5f * Mathf.Abs(s.x);
            bHalfH[j] = .5f * Mathf.Abs(s.y);
            bRow[j] = Mathf.Clamp01(p.y / FrontRoomsStructuresRunEndRoom.HeadTop);
            bLift[j] = Mathf.Lerp(LiftMin, LiftMax, Hash01(p.x, p.y, 1));
            bTilt[j] = Mathf.Lerp(TiltMin, TiltMax, Hash01(p.x, p.y, 2)) * (Hash01(p.x, p.y, 3) < .5f ? -1f : 1f);
            bTiltAxis[j] = new Vector3(Hash01(p.x, p.y, 4) - .5f, Hash01(p.x, p.y, 5) - .5f, .25f * (Hash01(p.x, p.y, 6) - .5f)).normalized;
            bDepth[j] = Mathf.Abs(p.x) < DoorColumn
                ? Mathf.Lerp(DoorColumnDepth, DepthFar, Hash01(p.x, p.y, 7))
                : Mathf.Lerp(DepthNear, DepthFar, Hash01(p.x, p.y, 7));
            bTurn[j] = Mathf.Lerp(TurnMin, TurnMax, Hash01(p.x, p.y, 8)) * (Hash01(p.x, p.y, 9) < .5f ? -1f : 1f);
            bTurnAxis[j] = new Vector3(Hash01(p.x, p.y, 10) - .5f, Hash01(p.x, p.y, 11) - .5f, Hash01(p.x, p.y, 12) - .5f).normalized;
            if (bTiltAxis[j].sqrMagnitude < .5f) bTiltAxis[j] = Vector3.right;
            if (bTurnAxis[j].sqrMagnitude < .5f) bTurnAxis[j] = Vector3.up;
            bSpreadX[j] = (Hash01(p.x, p.y, 13) - .5f) * 2f * SpreadX;
            bSpreadY[j] = (Hash01(p.x, p.y, 14) - .5f) * 2f * SpreadY;
            bBobHz[j] = Mathf.Lerp(BobHzMin, BobHzMax, Hash01(p.x, p.y, 15));
            bPhase[j] = 2f * Mathf.PI * Hash01(p.x, p.y, 16);
        }
        return true;
    }

    // The cloud's times on frame.time: the tell (the lift), the travel's start (the loosening over the ramp-up), the brake
    // (the landing; -1 not known yet under HAUL), the end of the seat (the solid wall back; -1 under HAUL until Done).
    void StartCloud(FrontRoomsStructuresRunEndRoom room, float tell, float travel, float land, float hide)
    {
        if (!ReadBlocks(room)) return;
        cloudOn = true;
        cloudTell = tell;
        cloudTravel = travel;
        cloudLand = land;
        cloudHide = hide;
        cloudCalm = -1f;
        room.ShowBlocks(true);
        PoseBlocks(tell);
    }

    void LetGo(FrontRoomsStructuresRunEndRoom room, FrontRoomsStructuresRunEndFrame frame)
    {
        if (!cloudOn || blk == null) return;
        var t = frame.time;
        // F on while the cloud is up: no let-go under F, so it folds home slowly (CalmTime) and the solid wall comes back.
        if (room.Reduce && cloudCalm < 0f) cloudCalm = t;
        var home = cloudLand >= 0f && cloudHide >= 0f && t >= cloudHide && t >= cloudLand + LandBase + LandRow + SettleTime + LagBase + LagRow;
        if (home || (cloudCalm >= 0f && t >= cloudCalm + CalmTime))
        {
            // Every course home: the solid wall again (its renderers at the head's own pose).
            room.ShowBlocks(false);
            cloudOn = false;
            return;
        }
        // Today's T2 steps (HAUL with B on T2) hide the blocks at each seat; this frame shows them again before the render.
        room.ShowBlocks(true);
        PoseBlocks(t);
    }

    void PoseBlocks(float t)
    {
        var calm = cloudCalm >= 0f ? 1f - Smooth((t - cloudCalm) / CalmTime) : 1f;
        for (var j = 0; j < blk.Length; j++)
        {
            var b = blk[j];
            if (b == null) continue;
            var r = bRow[j];
            var z0 = HistoryZ(t - (LagBase + LagRow * r) * calm);
            // The lift at the clacks, top courses first.
            var l = Smooth((t - cloudTell - (1f - r) * LiftStagger) / LiftTime);
            // The cloud over the ramp-up.
            var k = Smooth((t - cloudTravel) / RampUp);
            // The landing in the brake, bottom courses first, then the settle.
            var m = 0f;
            var lift = bLift[j] * l;
            if (cloudLand >= 0f)
            {
                var we = cloudLand + LandBase + LandRow * r;
                var ws = we - LandBase - LandLead;
                m = Smooth((t - ws) / Mathf.Max(.01f, we - ws));
                lift = t <= we ? Mathf.Lerp(lift, SettleDrop, m) : SettleDrop * (1f - Smooth((t - we) / SettleTime));
            }
            var c = k * (1f - m) * calm;
            var tilt = l * (1f - m) * calm;
            lift *= calm;
            var x = bSeatX[j] + bSpreadX[j] * c;
            var y = bSeatY[j] + lift + bSpreadY[j] * c + BobAmp * c * Mathf.Sin(2f * Mathf.PI * bBobHz[j] * t + bPhase[j]);
            if (c > 0f)
            {
                var hw = bHalfW[j];
                var hh = bHalfH[j];
                // The door's window (the leaf, the casing and the lite clear at any depth), blended in with the cloud.
                if (Mathf.Abs(x) < WindowX + hw && y < WindowY + hh)
                {
                    var px = WindowX + hw - Mathf.Abs(x);
                    var py = WindowY + hh - y;
                    if (px <= py) x = (bSeatX[j] < 0f ? -1f : 1f) * (Mathf.Abs(x) + px * c);
                    else y += py * c;
                }
                // Inside the corridor's section.
                x = Mathf.Lerp(x, Mathf.Clamp(x, -(SectionX - hw), SectionX - hw), c);
                y = Mathf.Lerp(y, Mathf.Clamp(y, hh + .005f, SectionY - hh), c);
            }
            b.localPosition = new Vector3(x, y, z0 + bDepth[j] * c - .01f * tilt);
            var shimmer = ShimmerDeg * c * Mathf.Sin(2f * Mathf.PI * ShimmerHz * t + 1.7f * bPhase[j]);
            b.localRotation = Quaternion.AngleAxis(bTilt[j] * tilt, bTiltAxis[j]) * Quaternion.AngleAxis(bTurn[j] * c + shimmer, bTurnAxis[j]);
        }
    }

    // ================================================================ the embers (light_leaves_deepen 3.a a2; 5.1 item 4)

    // Four 40 mm sleeves per strip round the ends of its two white T12s (where a rapid-start tube's cathodes glow on after the
    // arc drops), one mesh for all, one renderer per strip on the pool's emissive material (a property block each). STAND-IN
    // for the strip kit's own ember ends (the lamp-fit line's kit in the game, light_leaves_deepen 5.1 item 4).
    void BuildEmbers(FrontRoomsStructuresRunEndRoom room)
    {
        var n = room.StripCount;
        sleeves = new Renderer[n];
        emberShown = new float[n];
        emberFrom = new float[n];
        if (!LensBox(room, out var c, out var e))
        {
            // The kit's tubes as built (FrontRoomsLampKit: a 140 mm channel, two white T12s either side of the red one).
            c = new Vector3(0f, -.07f, 0f);
            e = new Vector3(.064f, .019f, .61f);
        }
        var radius = Mathf.Clamp(e.y, .012f, .025f);
        var xTube = Mathf.Max(.02f, e.x - radius);
        var zEnd = Mathf.Max(.1f, e.z - .5f * SleeveLength);
        var mesh = SleeveMesh(c.x, xTube, c.y, c.z, zEnd, Mathf.Max(SleeveRadius, radius + .001f), SleeveLength, 12);
        room.Own(mesh);
        var mat = FrontRoomsPoolMaterials.Emissive("SP7 #3 ember sleeve (tube end)", new Color(.24f, .21f, .19f), .25f);
        var root = room.NewObject(this, "STAND-IN SP7 #3 ember sleeves (the strip kit's tube ends)");
        for (var i = 0; i < n; i++)
        {
            var sr = room.StripRoot(i);
            if (sr == null) continue;
            var go = new GameObject("ember sleeves " + i);
            go.transform.SetParent(root, false);
            go.transform.localPosition = room.Local(sr.position);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            mr.enabled = false;
            sleeves[i] = mr;
        }
    }

    // The white tubes' box in the strip root's frame, from strip 0's lens (RunRecede.Strip names it "SP7 strip tubes"; the
    // stand-in "STAND-IN strip lens"). REQUEST: room.StripLens(i).
    static bool LensBox(FrontRoomsStructuresRunEndRoom room, out Vector3 centre, out Vector3 extents)
    {
        centre = extents = Vector3.zero;
        if (room.StripCount <= 0) return false;
        var sr = room.StripRoot(0);
        if (sr == null) return false;
        var t = sr.Find("SP7 strip tubes");
        if (t == null) t = sr.Find("STAND-IN strip lens");
        var mf = t != null ? t.GetComponent<MeshFilter>() : null;
        if (mf == null || mf.sharedMesh == null) return false;
        var b = mf.sharedMesh.bounds;
        centre = sr.InverseTransformPoint(t.TransformPoint(b.center));
        var s = t.lossyScale;
        extents = new Vector3(Mathf.Abs(b.extents.x * s.x), Mathf.Abs(b.extents.y * s.y), Mathf.Abs(b.extents.z * s.z));
        return extents.z > .2f && extents.x > .01f;
    }

    // Four open cylinders along z (strip-root space): at x = cx +- xTube, y = cy, centred at z = cz +- zEnd.
    static Mesh SleeveMesh(float cx, float xTube, float cy, float cz, float zEnd, float radius, float length, int sides)
    {
        const int count = 4;
        var ring = sides + 1;
        var verts = new Vector3[count * 2 * ring];
        var normals = new Vector3[verts.Length];
        var tris = new int[count * sides * 6];
        var v = 0;
        var w = 0;
        for (var s = 0; s < count; s++)
        {
            var x = cx + (s % 2 == 0 ? -xTube : xTube);
            var zc = cz + (s < 2 ? -zEnd : zEnd);
            var first = v;
            for (var r = 0; r < 2; r++)
            {
                var z = zc + (r == 0 ? -.5f : .5f) * length;
                for (var k = 0; k <= sides; k++)
                {
                    var a = k / (float)sides * Mathf.PI * 2f;
                    var nrm = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                    verts[v] = new Vector3(x, cy, z) + nrm * radius;
                    normals[v] = nrm;
                    v++;
                }
            }
            for (var k = 0; k < sides; k++)
            {
                var v00 = first + k;
                var v01 = first + k + 1;
                var v10 = first + ring + k;
                var v11 = first + ring + k + 1;
                tris[w++] = v00;
                tris[w++] = v01;
                tris[w++] = v10;
                tris[w++] = v01;
                tris[w++] = v11;
                tris[w++] = v10;
            }
        }
        var mesh = new Mesh { name = "SP7 #3 ember sleeves" };
        mesh.vertices = verts;
        mesh.normals = normals;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        return mesh;
    }

    // A dying strip's ember level at t (trip clock): from the arc's drop (F: from 60 % of the sag) a rise, then the decay.
    float EmberLevel(int i, float t)
    {
        var kind = death[i];
        if (kind == Death.None) return 0f;
        var dark = darkAt[i];
        float start, rise;
        if (kind == Death.Reduced)
        {
            start = leaveAt[i] + .6f * ReducedSag;
            rise = EmberRiseReduced;
        }
        else
        {
            start = dark - (kind == Death.Quick ? QuickDrop : BrownDrop);
            rise = EmberRise;
        }
        if (t < start) return 0f;
        var up = Smooth((t - start) / rise);
        return t <= dark ? up : up * Mathf.Exp(-(t - dark) / EmberTau);
    }

    void Embers(FrontRoomsStructuresRunEndRoom room)
    {
        if (sleeves == null || death == null) return;
        var phase = room.Phase;
        var fade = phase == FrontRoomsStructuresRunEndPhase.Resetting ? 1f - Smooth(room.Clock / Mathf.Max(.01f, room.Rig.resetRiseStart)) : 1f;
        for (var i = 0; i < sleeves.Length && i < death.Length; i++)
        {
            var mr = sleeves[i];
            if (mr == null) continue;
            var e = 0f;
            if (phase == FrontRoomsStructuresRunEndPhase.Tripped && lampsLive) e = EmberLevel(i, room.Clock);
            else if (phase == FrontRoomsStructuresRunEndPhase.Resetting) e = emberFrom[i] * fade;
            if (e <= EmberOff)
            {
                if (mr.enabled) mr.enabled = false;
                emberShown[i] = 0f;
                continue;
            }
            if (!mr.enabled) mr.enabled = true;
            if (Mathf.Abs(e - emberShown[i]) < .002f) continue;
            emberShown[i] = e;
            mpb.Clear();
            mpb.SetColor(EmissionId, EmberHdr * e);
            mr.SetPropertyBlock(mpb);
        }
    }

    // ================================================================ the door's light in the dark air: the beam

    // An additive cone from the goal heads' lenses to the floor before the leaf, on the rig's own visible-beam shader
    // (Resources/Rendering/RunBeamCone: rim and axial falloff, a soft depth fade where it meets the floor, fogged), its pose
    // kept in the goal unit's frame so it rocks with the nod. The air thickening, never the lamp changing (rule 7).
    void BuildBeam(FrontRoomsStructuresRunEndRoom room)
    {
        var shader = Resources.Load<Shader>("Rendering/RunBeamCone");
        if (shader == null || goalUnit == null || room.Head == null) return;
        var l0 = room.HeadCount > 1 ? room.HeadLight(0) : null;
        var l1 = room.HeadCount > 1 ? room.HeadLight(1) : null;
        var apex = l0 != null && l1 != null
            ? .5f * (l0.transform.position + l1.transform.position)
            : room.Head.TransformPoint(new Vector3(0f, 2.45f, -GoalUnitOut));
        var axis = room.Head.TransformPoint(BeamAim) - apex;
        var length = axis.magnitude;
        if (length < .5f) return;
        beamApex = goalUnit.InverseTransformPoint(apex);
        beamAxis = goalUnit.InverseTransformDirection(axis / length);
        var mesh = ConeMesh(length, BeamLens, (BeamBase - BeamLens) / length, 32);
        room.Own(mesh);
        var mat = new Material(shader) { name = "SP7 #3 the door's beam in the dark air", renderQueue = 3000 };
        mat.SetColor("_Color", room.Rig.floodColor);
        mat.SetFloat("_Density", 0f);
        mat.SetFloat("_DiffuseCoefficient", 1f);
        mat.SetFloat("_Length", length);
        mat.SetFloat("_SoftDepth", .35f);
        mat.SetFloat("_Gain", BeamGain);
        room.Own(mat);
        beamT = room.NewObject(this, "SP7 #3 beam under the goal head");
        beamT.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = beamT.gameObject.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = LightProbeUsage.Off;
        r.reflectionProbeUsage = ReflectionProbeUsage.Off;
        r.enabled = false;
        beam = r;
    }

    // The rig's beam-cone mesh (FrontRoomsRunRig.ConeMesh, private there): rings along +z from the lens, uv.y 0 at the lens.
    static Mesh ConeMesh(float length, float r0, float slope, int seg)
    {
        const int rings = 6;
        var verts = new Vector3[(rings + 1) * (seg + 1)];
        var normals = new Vector3[verts.Length];
        var uvs = new Vector2[verts.Length];
        var tris = new int[rings * seg * 6];
        var k = 0;
        for (var j = 0; j <= rings; j++)
        {
            var s = j / (float)rings;
            var zc = s * length;
            var r = r0 + slope * zc;
            for (var i = 0; i <= seg; i++)
            {
                var a = i / (float)seg * Mathf.PI * 2f;
                var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                verts[k] = d * r + Vector3.forward * zc;
                normals[k] = (d - Vector3.forward * slope).normalized;
                uvs[k] = new Vector2(i / (float)seg, s);
                k++;
            }
        }
        var t = 0;
        for (var j = 0; j < rings; j++)
            for (var i = 0; i < seg; i++)
            {
                var a = j * (seg + 1) + i;
                var b = a + seg + 1;
                tris[t++] = a;
                tris[t++] = b;
                tris[t++] = a + 1;
                tris[t++] = a + 1;
                tris[t++] = b;
                tris[t++] = b + 1;
            }
        var mesh = new Mesh { name = "SP7 #3 beam cone" };
        mesh.vertices = verts;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        return mesh;
    }

    float GoalLevel(FrontRoomsStructuresRunEndRoom room) => room.HeadCount > 0 ? Mathf.Clamp01(room.ShownHead(0).level) : 0f;

    void DriveBeam(FrontRoomsStructuresRunEndRoom room)
    {
        if (beam == null || beamT == null) return;
        var level = run.halo && fogMine && goalUnit != null ? BeamCore * fogShown * GoalLevel(room) : 0f;
        var on = level > 1e-5f;
        if (beam.enabled != on) beam.enabled = on;
        if (!on) return;
        beamT.position = goalUnit.TransformPoint(beamApex);
        var dir = goalUnit.TransformDirection(beamAxis);
        beamT.rotation = Quaternion.LookRotation(dir, Mathf.Abs(dir.y) > .95f ? Vector3.forward : Vector3.up);
        mpb.Clear();
        mpb.SetFloat(DensityId, level);
        beam.SetPropertyBlock(mpb);
    }

    // ================================================================ the grit (light_leaves_deepen 3.b: the drop at the tell)

    // Soft additive motes (FarShore's rust-puff method: a soft round sprite on its own material), lit by the goal lamp alone.
    void BuildGrit(FrontRoomsStructuresRunEndRoom room)
    {
        var sprite = SoftSprite("SP7 #3 grit sprite", 64, 64, false);
        room.Own(sprite);
        var mat = AdditiveMaterial(room, "SP7 #3 grit (soft sprite, additive)", sprite);
        if (mat == null) return;
        var root = room.NewObject(this, "SP7 #3 grit in the door's light");
        gT = new Transform[GritPool];
        gR = new Renderer[GritPool];
        gLive = new bool[GritPool];
        gStrip = new int[GritPool];
        gT0 = new float[GritPool];
        gLife = new float[GritPool];
        gSize0 = new float[GritPool];
        gSize1 = new float[GritPool];
        gSpin = new float[GritPool];
        gFall = new float[GritPool];
        gP0 = new Vector3[GritPool];
        gDrift = new Vector3[GritPool];
        for (var k = 0; k < GritPool; k++)
        {
            var go = Billboard(root, "grit " + k, mat);
            gT[k] = go.transform;
            gR[k] = go.GetComponent<Renderer>();
        }
    }

    void SpawnGrit(FrontRoomsStructuresRunEndRoom room, float line, float now, int count, float spread)
    {
        if (gT == null) return;
        // The strip just in front of the line (strip i at 1.8 + 3 i): the lamp that lights this place.
        var strip = Mathf.Clamp(Mathf.FloorToInt((line - .05f - 1.8f) / FrontRoomsStructuresRunEndRoom.CellLength), 0, Mathf.Max(0, room.StripCount - 1));
        var made = 0;
        for (var k = 0; k < gT.Length && made < count; k++)
        {
            if (gLive[k] || gT[k] == null) continue;
            var h = ++seed;
            gLive[k] = true;
            gStrip[k] = room.StripCount > 0 ? strip : -1;
            gT0[k] = now + spread * Hash01(h, 1);
            gLife[k] = GritLife * (.8f + .4f * Hash01(h, 2));
            gP0[k] = new Vector3((Hash01(h, 3) - .5f) * 2.4f, FrontRoomsStructuresRunEndRoom.HeadTop - .02f - .04f * Hash01(h, 4),
                line - FrontRoomsStructuresRunEndRoom.HeadWallHalf - .015f - .05f * Hash01(h, 5));
            gDrift[k] = new Vector3((Hash01(h, 6) - .5f) * .08f, 0f, -(.02f + .05f * Hash01(h, 7)));
            gFall[k] = GritFall * (.7f + .6f * Hash01(h, 8));
            gSize0[k] = .035f + .03f * Hash01(h, 9);
            gSize1[k] = .09f + .07f * Hash01(h, 10);
            gSpin[k] = 360f * Hash01(h, 11);
            made++;
        }
    }

    void DriveGrit(FrontRoomsStructuresRunEndRoom room, FrontRoomsStructuresRunEndFrame frame)
    {
        if (gT == null) return;
        // The lamp it catches: goal head 0's Light (it rides the head and the nod), district metres.
        var lamp = room.HeadCount > 0 ? room.HeadLight(0) : null;
        var g = Vector3.zero;
        var aim = Vector3.forward;
        var level = 0f;
        if (lamp != null)
        {
            g = room.Local(lamp.transform.position);
            aim = lamp.transform.forward;
            level = GoalLevel(room);
        }
        var cam = room.View != null ? room.View.transform.forward : Vector3.forward;
        var face = Quaternion.LookRotation(cam.sqrMagnitude > 1e-6f ? cam : Vector3.forward, Vector3.up);
        var flood = room.Rig.floodColor;
        for (var k = 0; k < gT.Length; k++)
        {
            if (!gLive[k] || gT[k] == null) continue;
            var go = gT[k].gameObject;
            var age = frame.time - gT0[k];
            if (age < 0f)
            {
                if (go.activeSelf) go.SetActive(false);
                continue;
            }
            if (age > gLife[k])
            {
                gLive[k] = false;
                go.SetActive(false);
                continue;
            }
            if (!go.activeSelf) go.SetActive(true);
            var u = Mathf.Min(1f, age / GritFallTime);
            var p = gP0[k] + gDrift[k] * age;
            p.y -= gFall[k] * (1f - (1f - u) * (1f - u)) + GritDrift * Mathf.Max(0f, age - GritFallTime);
            var size = Mathf.Lerp(gSize0[k], gSize1[k], Smooth(age / gLife[k]));
            gT[k].localPosition = p;
            gT[k].localRotation = face * Quaternion.Euler(0f, 0f, gSpin[k] + 25f * age);
            gT[k].localScale = new Vector3(size, size, 1f);
            // The strip in front of its line, as it shows now (white, sagging amber, dark, or struck red): a downlight, so the
            // grit lights up as it falls under it and goes dark with it.
            var col = Color.black;
            var si = gStrip[k];
            if (si >= 0 && si < room.StripCount)
            {
                var s = room.ShownStrip(si);
                var ds = p - new Vector3(0f, FrontRoomsStructuresRunEndRoom.Ceiling - StripLightDrop, room.StripZ(si));
                var dn = ds.magnitude;
                if (dn > 1e-3f)
                {
                    var fall = Smooth((-ds.y / dn - .07f) / .40f) / (1f + dn * dn);
                    var c = (s.whiteColour * Mathf.Max(0f, s.white) + s.redColour * (.6f * Mathf.Max(0f, s.red))).gamma;
                    col += c * (GritStripGain * fall);
                }
            }
            // Goal head 0: near and in front of it only, so it goes dark as the lamp leaves it behind.
            var d = p - g;
            var dist = d.magnitude;
            var facing = dist > 1e-4f ? Vector3.Dot(aim, d / dist) : 1f;
            var q = dist / GritFalloff;
            col += flood * (level * GritGain / (1f + q * q) * Smooth((facing + .2f) / 1.2f));
            var alpha = GritAlpha * Smooth(age / .12f) * (1f - Smooth((age - (gLife[k] - .8f)) / .8f));
            mpb.Clear();
            mpb.SetColor(BaseColorId, new Color(col.r, col.g, col.b, alpha));
            gR[k].SetPropertyBlock(mpb);
        }
    }

    // ================================================================ the sparks (light_leaves_deepen 3.c c4)

    void BuildSparks(FrontRoomsStructuresRunEndRoom room)
    {
        var sprite = SoftSprite("SP7 #3 spark streak", 16, 64, true);
        room.Own(sprite);
        var mat = AdditiveMaterial(room, "SP7 #3 sparks (streak, additive)", sprite);
        if (mat == null) return;
        var root = room.NewObject(this, "SP7 #3 sparks from the trolleys and the pin");
        sT = new Transform[SparkPool];
        sR = new Renderer[SparkPool];
        sLive = new bool[SparkPool];
        sAge = new float[SparkPool];
        sLife = new float[SparkPool];
        sLen = new float[SparkPool];
        sP = new Vector3[SparkPool];
        sV = new Vector3[SparkPool];
        for (var k = 0; k < SparkPool; k++)
        {
            var go = Billboard(root, "spark " + k, mat);
            sT[k] = go.transform;
            sR[k] = go.GetComponent<Renderer>();
        }
    }

    void DriveSparks(FrontRoomsStructuresRunEndRoom room, FrontRoomsStructuresRunEndFrame frame)
    {
        if (sT == null) return;
        var dt = Mathf.Clamp(frame.dt, 0f, .05f);
        if (sparksRun && !room.Reduce && beat == Beat.Travel && headSpeed > SparkSpeed)
        {
            creditA += SparkRateTrolley * dt;
            creditB += SparkRateTrolley * dt;
            creditPin += SparkRatePin * dt;
            for (; creditA >= 1f; creditA -= 1f) EmitSpark(room, new Vector3(-1.25f, 2.86f, -.16f), -1f, false);
            for (; creditB >= 1f; creditB -= 1f) EmitSpark(room, new Vector3(1.25f, 2.86f, -.16f), 1f, false);
            for (; creditPin >= 1f; creditPin -= 1f) EmitSpark(room, new Vector3(.95f, .012f, .02f), 1f, true);
        }
        var view = room.View;
        var camP = view != null ? room.Local(view.transform.position) : Vector3.zero;
        for (var k = 0; k < sT.Length; k++)
        {
            if (!sLive[k] || sT[k] == null) continue;
            sAge[k] += dt;
            if (sAge[k] > sLife[k] || room.Reduce)
            {
                sLive[k] = false;
                sT[k].gameObject.SetActive(false);
                continue;
            }
            var v = sV[k];
            v.y -= 9.81f * dt;
            var p = sP[k] + v * dt;
            if (p.y < .004f)
            {
                p.y = .004f;
                v.y = -.3f * v.y;
                v.x *= .5f;
                v.z *= .5f;
            }
            p.x = Mathf.Clamp(p.x, -1.40f, 1.40f);
            sP[k] = p;
            sV[k] = v;
            var u = sAge[k] / sLife[k];
            var col = Color.Lerp(SparkHot, SparkCold, Smooth(u * 1.4f)) * (1f - Smooth((u - .55f) / .45f));
            var toCam = p - camP;
            var vd = toCam.sqrMagnitude > 1e-6f ? toCam.normalized : Vector3.forward;
            var speed = v.magnitude;
            var vdir = speed > 1e-4f ? v / speed : Vector3.down;
            var up = vdir - vd * Vector3.Dot(vdir, vd);
            if (up.sqrMagnitude < 1e-6f) up = Vector3.up;
            sT[k].localPosition = p;
            sT[k].localRotation = Quaternion.LookRotation(vd, up.normalized);
            sT[k].localScale = new Vector3(SparkWidth, sLen[k] + .012f * speed, 1f);
            if (!sT[k].gameObject.activeSelf) sT[k].gameObject.SetActive(true);
            mpb.Clear();
            mpb.SetColor(BaseColorId, new Color(col.r, col.g, col.b, 1f));
            sR[k].SetPropertyBlock(mpb);
        }
    }

    // One spark at an emitter (head-local x, y, z): dragged along a little by the end, thrown back and sprayed, then 1 g.
    void EmitSpark(FrontRoomsStructuresRunEndRoom room, Vector3 local, float side, bool pin)
    {
        for (var k = 0; k < sT.Length; k++)
        {
            if (sLive[k] || sT[k] == null) continue;
            var h = ++seed;
            sLive[k] = true;
            sAge[k] = 0f;
            sLife[k] = SparkLife * (.7f + .6f * Hash01(h, 1));
            sLen[k] = Mathf.Lerp(SparkLenMin, SparkLenMax, Hash01(h, 2));
            sP[k] = new Vector3(local.x + (Hash01(h, 3) - .5f) * .03f, local.y, room.HeadZ + local.z);
            var spray = pin
                ? new Vector3((Hash01(h, 4) - .5f) * 1.2f, .3f + 1.2f * Hash01(h, 5), -(.5f + 1.5f * Hash01(h, 6)))
                : new Vector3(-side * (.2f + Hash01(h, 4)), -(.2f + 1.3f * Hash01(h, 5)), -(.5f + 1.5f * Hash01(h, 6)));
            sV[k] = new Vector3(0f, 0f, headSpeed * (.15f + .3f * Hash01(h, 7))) + spray;
            return;
        }
    }

    // ================================================================ shared bits: sprites, the additive material, billboards

    // A white sprite whose alpha is a soft round core with a slightly lumpy rim (FarShore's PuffSprite), or a soft streak
    // (narrow across, tapered along) for the sparks.
    static Texture2D SoftSprite(string name, int w, int h, bool streak)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, true, false)
        {
            name = name,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave,
        };
        var px = new Color32[w * h];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var u = (x + .5f) / w * 2f - 1f;
                var v = (y + .5f) / h * 2f - 1f;
                float a;
                if (streak) a = Mathf.Exp(-5f * u * u) * Smooth((1f - Mathf.Abs(u)) / .3f) * Smooth((1f - Mathf.Abs(v)) / .4f);
                else
                {
                    var phi = Mathf.Atan2(v, u);
                    var r = Mathf.Sqrt(u * u + v * v) * (1f + .10f * Mathf.Sin(3f * phi + .7f) + .06f * Mathf.Sin(5f * phi + 2.1f));
                    a = r >= 1f ? 0f : Mathf.Exp(-3.2f * r * r) * Smooth((1f - r) / .35f);
                }
                px[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.Clamp(Mathf.RoundToInt(a * 255f), 0, 255));
            }
        tex.SetPixels32(px);
        tex.Apply(true, false);
        return tex;
    }

    // A copy of the pool's unlit glow (URP Unlit) made transparent and additive (SrcAlpha, One), the sprite as its base map:
    // it adds _BaseColor x the sprite's alpha, so a mote the lamp does not reach adds nothing.
    static Material AdditiveMaterial(FrontRoomsStructuresRunEndRoom room, string name, Texture2D sprite)
    {
        var glow = FrontRoomsPoolMaterials.Glow("SP7 #3 additive base", Color.white);
        if (glow == null) return null;
        var m = new Material(glow) { name = name };
        SetFloat(m, "_Surface", 1f);
        SetFloat(m, "_Blend", 2f);
        SetFloat(m, "_SrcBlend", (float)BlendMode.SrcAlpha);
        SetFloat(m, "_DstBlend", (float)BlendMode.One);
        SetFloat(m, "_SrcBlendAlpha", (float)BlendMode.One);
        SetFloat(m, "_DstBlendAlpha", (float)BlendMode.One);
        SetFloat(m, "_ZWrite", 0f);
        SetFloat(m, "_Cull", 0f);
        SetFloat(m, "_AlphaClip", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.DisableKeyword("_ALPHATEST_ON");
        m.renderQueue = (int)RenderQueue.Transparent;
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", sprite);
        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", sprite);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
        room.Own(m);
        return m;
    }

    static void SetFloat(Material m, string property, float value)
    {
        if (m.HasProperty(property)) m.SetFloat(property, value);
    }

    static GameObject Billboard(Transform root, string name, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        UnityEngine.Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(root, false);
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = LightProbeUsage.Off;
        r.reflectionProbeUsage = ReflectionProbeUsage.Off;
        go.SetActive(false);
        return go;
    }

    void HideOwn()
    {
        if (sleeves != null)
            for (var i = 0; i < sleeves.Length; i++)
            {
                if (sleeves[i] != null) sleeves[i].enabled = false;
                emberShown[i] = emberFrom[i] = 0f;
            }
        if (beam != null) beam.enabled = false;
        if (gT != null)
            for (var k = 0; k < gT.Length; k++)
            {
                gLive[k] = false;
                if (gT[k] != null) gT[k].gameObject.SetActive(false);
            }
        if (sT != null)
            for (var k = 0; k < sT.Length; k++)
            {
                sLive[k] = false;
                if (sT[k] != null) sT[k].gameObject.SetActive(false);
            }
    }

    // ================================================================ the lens (74 section 6: L1 THE DARK CLOSES IN, L2 THE LENS FLINCHES)

    // Two LOCAL URP volumes on one object under the Own root, one trigger box over SP7's own space (the vestibule, the
    // corridor and the release hall), each with its own runtime profile (never an asset): L1's overrides only the Vignette,
    // L2's only Lens Distortion and Chromatic Aberration. A volume's weight is its envelope: URP lerps every overridden value
    // from what the stack holds under it (in SP7 Run_B's vignette 0.32 / 0.20, the film look's lens -0.04, CA 0.06) to the
    // override, so weight 0 is the room as today and weight 1 is 74's numbers, whatever the profiles below are tuned to.
    // Local, so it acts on no camera outside SP7 even if a hook were missed (FrontRoomsPostStack.EnsureZoneVolume's pattern:
    // SP7's own Run_B grade is such a trigger box); no renderer feature, no new pass; the lens stays non-zero (-0.04 to -0.20)
    // and CA above 0, so the uber shader's keywords never change (no variant switch, no hitch: FrontRoomsInspectLook's rule).
    void BuildLens(FrontRoomsStructuresRunEndRoom room)
    {
        var go = room.NewObject(this, "SP7 #3 lens (local volumes: L1 edge dim, L2 flinch; rows 19-20)").gameObject;
        go.SetActive(false);
        lensRoot = go;
        go.layer = LensLayer(room);
        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        var lo = new Vector3(FrontRoomsStructuresRunEndRoom.HallX0 - .1f, -.5f, FrontRoomsStructuresRunEndRoom.VestibuleZ0 - .2f);
        var hi = new Vector3(FrontRoomsStructuresRunEndRoom.HallX1 + .1f, FrontRoomsStructuresRunEndRoom.HallCeiling + .5f, FrontRoomsStructuresRunEndRoom.HallZ1 + .2f);
        box.center = .5f * (lo + hi);
        box.size = hi - lo;
        dimProfile = NewLensProfile("SP7 #3 L1 the dark closes in (runtime)");
        dimVignette = dimProfile.Add<UrpVignette>(false);
        dimVignette.hideFlags |= HideFlags.DontSave;
        dimVignette.intensity.Override(DimVignette);
        dimVignette.smoothness.Override(DimSmooth);
        dimVignette.color.Override(Color.black);
        dimVignette.center.Override(new Vector2(.5f, .5f));
        flinchProfile = NewLensProfile("SP7 #3 L2 the lens flinches (runtime)");
        flinchDistortion = flinchProfile.Add<UrpLensDistortion>(false);
        flinchDistortion.hideFlags |= HideFlags.DontSave;
        flinchDistortion.intensity.Override(FlinchDistortion);
        flinchDistortion.center.Override(new Vector2(.5f, .5f));
        flinchChroma = flinchProfile.Add<UrpChromaticAberration>(false);
        flinchChroma.hideFlags |= HideFlags.DontSave;
        flinchChroma.intensity.Override(FlinchChroma);
        dimVolume = NewLensVolume(go, dimProfile, LensPriority);
        flinchVolume = NewLensVolume(go, flinchProfile, LensPriority + 1f);
    }

    static VolumeProfile NewLensProfile(string name)
    {
        var p = ScriptableObject.CreateInstance<VolumeProfile>();
        p.name = name;
        p.hideFlags = HideFlags.DontSave;
        return p;
    }

    // A local volume (the collider is on the object already), off and at weight 0 until a trip drives it.
    static Volume NewLensVolume(GameObject go, VolumeProfile profile, float priority)
    {
        var v = go.AddComponent<Volume>();
        v.isGlobal = false;
        v.blendDistance = 0f;
        v.priority = priority;
        v.weight = 0f;
        v.sharedProfile = profile;
        v.enabled = false;
        return v;
    }

    // The layer the walker's camera reads volumes on (URP's default mask: Default), read from its camera data, never changed.
    static int LensLayer(FrontRoomsStructuresRunEndRoom room)
    {
        var view = room.View;
        var mask = 1;
        if (view != null && view.TryGetComponent<UrpCameraData>(out var data)) mask = data.volumeLayerMask.value;
        if ((mask & 1) != 0) return 0;
        for (var l = 1; l < 32; l++)
            if ((mask & (1 << l)) != 0) return l;
        return 0;
    }

    // Weights 0, both volumes off, the object inactive (no trigger, nothing registered); the trip's state cleared.
    void LensOff()
    {
        lensLive = dimOutPending = dimFrozenSet = false;
        dimHeld = dimIn = dimFrozen = 0f;
        dimOutAt = -1f;
        dimOutTime = DimOutMin;
        shudder = Shudder.None;
        shudderAt = shudderFold = -1f;
        shudderSize = 0f;
        SetLensWeight(dimVolume, 0f);
        SetLensWeight(flinchVolume, 0f);
        if (lensRoot != null && lensRoot.activeSelf) lensRoot.SetActive(false);
    }

    // Release (and a failed build): off, then both profiles and their overrides destroyed.
    void DropLens()
    {
        LensOff();
        DestroyLensProfile(dimProfile);
        DestroyLensProfile(flinchProfile);
        dimProfile = flinchProfile = null;
        dimVignette = null;
        flinchDistortion = null;
        flinchChroma = null;
        if (dimVolume != null) dimVolume.sharedProfile = null;
        if (flinchVolume != null) flinchVolume.sharedProfile = null;
        dimVolume = flinchVolume = null;
        lensRoot = null;
    }

    static void DestroyLensProfile(VolumeProfile p)
    {
        if (p == null) return;
        var list = p.components;
        for (var i = 0; i < list.Count; i++)
            if (list[i] != null) UnityEngine.Object.Destroy(list[i]);
        list.Clear();
        UnityEngine.Object.Destroy(p);
    }

    static void SetLensWeight(Volume v, float w)
    {
        if (v == null) return;
        w = Mathf.Clamp01(w);
        var on = w > 1e-4f;
        if (v.enabled != on) v.enabled = on;
        v.weight = on ? w : 0f;
    }

    // At the trip (after ArmLamps): the strips L1 counts are the ones lit ahead of the feet (the front will reach them).
    void LensArm(FrontRoomsStructuresRunEndRoom room)
    {
        LensOff();
        if (run.lens == Lens.None || lensRoot == null || dimVolume == null || ahead == null) return;
        var n = ahead.Length;
        if (dimCount == null || dimCount.Length != n)
        {
            dimCount = new bool[n];
            dimFall = new float[n];
        }
        for (var i = 0; i < n; i++)
        {
            var leave = leaveAt[i];
            dimCount[i] = ahead[i] && !float.IsInfinity(leave);
            dimFall[i] = 0f;
        }
        lensCentre = LensDoor(room);
        lensLive = true;
        lensRoot.SetActive(true);
    }

    // The first clack (row 20): the shudder x Camera Motion, or the still cue when it is 0 or F is on (SP7's F or the game's
    // Reduce Flashing). Once per trip.
    void StartShudder(FrontRoomsStructuresRunEndRoom room, float now)
    {
        shudderAt = now;
        shudderFold = -1f;
        shudderSize = CameraMotion();
        shudder = Calm(room) ? Shudder.CueCalm : shudderSize <= .001f ? Shudder.Cue : Shudder.Lens;
    }

    // The game's Camera Motion (0, 0.5 or 1), read only, as its camera rig reads it. Scene 4 never loads or writes the game's
    // settings: it reads 1 here unless the game scene loaded the player's settings earlier in the same session.
    static float CameraMotion() => Mathf.Clamp01(FrontRoomsSettings.CameraMotionScale);

    static bool Calm(FrontRoomsStructuresRunEndRoom room) => room.Reduce || FrontRoomsSettings.ReduceFlashing;

    bool FlinchAllowed(FrontRoomsStructuresRunEndRoom room) => !Calm(room) && CameraMotion() > .001f;

    // L2's weight u seconds after the first clack: an ease-out up over 0.12 s, a smoothstep back, nothing after 1.0 s.
    static float FlinchAt(float u)
    {
        if (u <= 0f || u >= FlinchTotal) return 0f;
        if (u < FlinchUp)
        {
            var x = 1f - u / FlinchUp;
            return 1f - x * x;
        }
        return 1f - Smooth((u - FlinchUp) / (FlinchTotal - FlinchUp));
    }

    // The still cue u seconds after the first clack: up 0.12 s, held to 0.30 s, back over 0.40 s; calm (F): 0.5 s up and back.
    static float CueAt(float u, bool calm)
    {
        if (u <= 0f) return 0f;
        if (calm) return u < CueCalm ? Smooth(u / CueCalm) : 1f - Smooth((u - CueCalm) / CueCalm);
        if (u < CueUp) return Smooth(u / CueUp);
        if (u < CueHold) return 1f;
        return 1f - Smooth((u - CueHold) / CueDown);
    }

    // The share of the light ahead that has left (L1): over the strips lit ahead of the feet at the trip, each one's fall
    // (held: 1 - its shown white / its white at the trip, once the front has reached it) over those falling plus those still
    // white ahead. A strip the walker turned red before the front came (the carry), or that F spared and the walker then
    // reached, leaves the count: it did not die, its red is the walker's own pocket.
    float LensShare(FrontRoomsStructuresRunEndRoom room)
    {
        var t = room.Clock;
        var n = 0;
        var gone = 0f;
        var count = Mathf.Min(dimCount.Length, ahead.Length);
        for (var i = 0; i < count; i++)
        {
            if (!dimCount[i]) continue;
            var leave = leaveAt[i];
            if (float.IsPositiveInfinity(leave))
            {
                // Turned (or spared): out of the count once its red strike has begun; a fall it had stays counted.
                if (strikeAt[i] <= t && dimFall[i] <= 0f) continue;
                n++;
                gone += dimFall[i];
                continue;
            }
            n++;
            if (float.IsNegativeInfinity(leave) || t < leave) continue;
            var w0 = white[i].white;
            var f = w0 > 1e-4f ? Mathf.Clamp01(1f - room.ShownStrip(i).white / w0) : 1f;
            if (f > dimFall[i]) dimFall[i] = f;
            gone += dimFall[i];
        }
        return n > 0 ? gone / n : 0f;
    }

    // The door's point on the screen (viewport units, 0..1 from the bottom left: the uv the uber pass centres the vignette
    // and the lens on), held within 0.45..0.55; the screen's centre while the door is behind the camera.
    Vector2 LensDoor(FrontRoomsStructuresRunEndRoom room)
    {
        var view = room.View;
        var head = room.Head;
        if (view == null || head == null) return new Vector2(.5f, .5f);
        var v = view.WorldToViewportPoint(head.TransformPoint(DoorMiddle));
        if (v.z < .1f) return new Vector2(.5f, .5f);
        return new Vector2(Mathf.Clamp(v.x, .5f - LensCentreClamp, .5f + LensCentreClamp), Mathf.Clamp(v.y, .5f - LensCentreClamp, .5f + LensCentreClamp));
    }

    void DriveLens(FrontRoomsStructuresRunEndRoom room, FrontRoomsStructuresRunEndFrame frame)
    {
        if (!lensLive || lensRoot == null || dimVolume == null || dimVignette == null) return;
        var now = frame.time;
        var dt = Mathf.Clamp(frame.dt, 0f, .1f);
        // L1 opens again: over the runway (from its first strike to its last + the rise, never under 0.8 s), or over 0.8 s
        // from T (ResetStarted) when no runway has opened it yet.
        if (dimOutPending)
        {
            dimOutPending = false;
            if (dimOutAt < 0f || now < dimOutAt)
            {
                dimOutAt = now;
                dimOutTime = DimOutMin;
            }
        }
        else if (dimOutAt < 0f && runwayAt >= 0f && !float.IsInfinity(runwayFirst) && !float.IsInfinity(runwayEnd))
        {
            dimOutAt = now + Mathf.Max(0f, runwayFirst - room.Clock);
            dimOutTime = Mathf.Max(DimOutMin, runwayEnd - runwayFirst);
        }
        var opening = dimOutAt >= 0f && now >= dimOutAt;
        // L1 closes in: the share held (never lower while the light leaves), smoothstepped, followed.
        if (!opening && lampsLive && room.Phase == FrontRoomsStructuresRunEndPhase.Tripped)
        {
            dimHeld = Mathf.Max(dimHeld, LensShare(room));
            var step = (Smooth(dimHeld) - dimIn) * (1f - Mathf.Exp(-dt / DimLag));
            dimIn += Mathf.Clamp(step, 0f, DimRate * dt);
        }
        float a;
        if (opening)
        {
            if (!dimFrozenSet)
            {
                dimFrozen = dimIn;
                dimFrozenSet = true;
            }
            a = dimFrozen * (1f - Smooth((now - dimOutAt) / Mathf.Max(.01f, dimOutTime)));
        }
        else a = dimIn;
        // L2 or its cue.
        var cue = 0f;
        var flinch = 0f;
        if (shudderAt >= 0f)
        {
            var u = now - shudderAt;
            if (shudder == Shudder.Lens)
            {
                if (shudderFold < 0f && Calm(room)) shudderFold = now;
                flinch = FlinchAt(u) * shudderSize;
                if (shudderFold >= 0f) flinch *= 1f - Smooth((now - shudderFold) / FlinchFold);
            }
            else if (shudder != Shudder.None) cue = CueAt(u, shudder == Shudder.CueCalm);
            if (u > FlinchTotal + 2f * CueCalm)
            {
                shudderAt = -1f;
                shudder = Shudder.None;
            }
        }
        // The centre on the door, eased; L2's lens centre maps it as the shader does (centre 2c - 0.5).
        var door = LensDoor(room);
        lensCentre += (door - lensCentre) * (1f - Mathf.Exp(-dt / LensCentreTau));
        dimVignette.center.value = lensCentre;
        dimVignette.intensity.value = DimVignette + CueDepth * cue * a;
        SetLensWeight(dimVolume, a + (1f - a) * CueWeight * cue);
        if (flinchDistortion != null) flinchDistortion.center.value = new Vector2(.5f * (lensCentre.x + .5f), .5f * (lensCentre.y + .5f));
        SetLensWeight(flinchVolume, flinch);
        // All done (opened, no shudder left): off until the next trip.
        if (opening && now - dimOutAt >= dimOutTime && shudderAt < 0f) LensOff();
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
        if (!fogMine)
        {
            fogShown = 0f;
            return;
        }
        var phase = room.Phase;
        if (!fogOut)
        {
            if (phase != FrontRoomsStructuresRunEndPhase.Tripped) return;
            fogLevel = Smooth(room.Clock / FogIn);
            fogShown = fogLevel;
            SetFog(room, fogLevel);
            return;
        }
        // T's re-strike (its clock from 0): back to the preset's fog over the dark phase (AS BUILT), or in step with the white
        // front from the entrance to the end's cell (WHITE WASH).
        var rig = room.Rig;
        float back;
        if (phase != FrontRoomsStructuresRunEndPhase.Resetting) back = 1f;
        else if (run.restore == Restore.Wash)
        {
            var a = room.RestrikeAt(0f);
            var b = room.RestrikeAt(Mathf.Max(0f, room.HeadZ - .1f)) + (room.Reduce ? rig.reducedEnvelope : rig.resetRise);
            back = Smooth((room.Clock - a) / Mathf.Max(.01f, b - a));
        }
        else back = Smooth(room.Clock / Mathf.Max(.01f, rig.resetRiseStart));
        if (back >= 1f)
        {
            room.ClearFog();
            fogMine = fogOut = false;
            fogLevel = fogShown = 0f;
            return;
        }
        fogShown = fogLevel * (1f - back);
        SetFog(room, fogShown);
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
        // N the next row, Shift+N the one before (as the frame's X and Shift+X).
        var k = UnityEngine.InputSystem.Keyboard.current;
        var back = k != null && (k.leftShiftKey.isPressed || k.rightShiftKey.isPressed);
        pickNext = ((pickNext + (back ? -1 : 1)) % Rows.Length + Rows.Length) % Rows.Length;
        var armed = room.Phase == FrontRoomsStructuresRunEndPhase.Armed && !IsMoving(room);
        if (armed)
        {
            ApplyPick();
            run = pick;
        }
        var p = Rows[pickNext];
        message = "光走了 " + (pickNext + 1) + "/" + Rows.Length + " · " + p.name + "：" + p.tryLine + (armed ? "" : "（下次待触发时生效）");
        room.Dirty();
        return true;
    }

    public override void Guide(FrontRoomsStructuresRunEndRoom room, StringBuilder sb)
    {
        var p = Rows[pickIndex];
        sb.AppendLine("3 光走了 · N " + (pickIndex + 1) + "/" + Rows.Length + " " + p.name + "：" + Describe(p) + "。");
        sb.AppendLine("试：从到达点冲刺过 FD1，或在“FD1 门框前”往前走；也站在 FD1 不动看完全程，走到门口再回头看。N 下一种，Shift+N 上一种；F 减少闪烁（每一下都变慢，墙不散、灯不晃、没有火花）；T 白光复位，Y 把门放回原位。");
        if (p.asBuilt && !p.swallow) sb.AppendLine("这一行就是你选的那段影片：没有余火、光柱、灰和砖块，和 11 月 4 日 20:11 的版本一样。");
        else if (p.swallow) sb.AppendLine("吞没对照：门的光也被吞掉，破了规则 7，只用来对照。");
        else if (p.lens == Lens.Tunnel) sb.AppendLine("暗收只是画面四周变暗（门始终最亮，脚下照样有光），不闪，开 F 也一样；和第1种「离港」来回对比着看。");
        else if (p.lens == Lens.TunnelFlinch)
            sb.AppendLine("一颤跟着游戏的“镜头晃动”设置（现在 " + Mathf.RoundToInt(100f * CameraMotion()) + "%）：为 0 或开 F 时不颤，只在那一声四周暗一下；和第19种「离港 + 暗收」对比着看。");
        else if (p.motion == Motion.Haul) sb.AppendLine("这一种用今天的分步后退：B 选 T2 时砖块" + (p.model == Model.LetGo ? "也照样散成一团跟着走" : "每一步拆开砌回") + "，选 T1 时走得快一点；冲到离门 12 米内它就一口气滑到底。换到这一行后按一次 Y，墙上的磁铁才对上分步的位置。");
    }

    static string Describe(Pick p)
    {
        var sb = new StringBuilder(160);
        sb.Append(p.leave == Leave.Cut ? "灯一刀切灭" : p.leave == Leave.Brownout ? "灯像余烬一样慢慢死，灯管两头留着余火" : "黑暗从门那头朝你涌来，停在你的红灯前");
        sb.Append("；").Append(p.motion == Motion.Glide ? "门一口气滑走" : p.motion == Motion.Haul ? "门一格一格被拖走" : "门分三口气退走");
        sb.Append("；").Append(p.model == Model.Rigid ? "墙整面跟着门走" : p.model == Model.LetGo ? "墙在门的光里散开跟着走，刹车时从下往上砌回" : p.model == Model.Restack ? "每一步墙都拆开再砌回" : "墙整面滑走，顶上轨道和地上的槽擦出火花");
        sb.Append("；").Append(p.pocket == Pocket.Carry ? "红光跟着你，到灯前才亮" : p.pocket == Pocket.Long ? "红光提前两格亮" : "前面的应急灯先暖暖亮起，灯管到头顶才变红");
        sb.Append("；").Append(p.reveal == Reveal.Sweep ? "门停下后红灯从脚下铺到门口" : p.reveal == Reveal.Overtake ? "门停下后等警报脉冲从身后追上来铺" : p.reveal == Reveal.Counted ? "红灯一盏一盏数着亮到门口" : "不铺，走到哪亮到哪");
        sb.Append("；").Append(p.restore == Restore.AsBuilt ? "复位和现在一样" : "复位时白光一路把黑雾洗掉");
        if (p.halo) sb.Append("；门灯下一道淡淡的光柱");
        if (p.grit) sb.Append(p.motion == Motion.Haul ? "；每一步落下一撮灰" : "；墙原位落下一撮灰");
        if (p.swallow) sb.Append("；门的光也被吞掉（破规则 7）");
        if (p.lens != Lens.None) sb.Append("；前面的灯熄灭时画面四周暗下来、收向门的光，红灯铺开时再打开（暗收）");
        if (p.lens == Lens.TunnelFlinch) sb.Append("；门动的第一声咔嗒，画面朝门一颤，一秒内回来（一颤）");
        return sb.ToString();
    }

    // ================================================================ helpers

    void ApplyPick()
    {
        pickIndex = Mathf.Clamp(pickNext, 0, Rows.Length - 1);
        pick = Rows[pickIndex];
    }

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
            spared = new bool[n];
            frontSeen = new bool[n];
            leaveAt = new float[n];
            darkAt = new float[n];
            strikeAt = new float[n];
            death = new Death[n];
            strikeHow = new byte[n];
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
        ahead[i] = spared[i] = frontSeen[i] = false;
        leaveAt[i] = darkAt[i] = float.PositiveInfinity;
        strikeAt[i] = float.PositiveInfinity;
        death[i] = Death.None;
        strikeHow[i] = StrikeDark;
    }

    static float Square(float x) => x * x;

    // A hash in 0..1 of a block's seat (as the frame hashes its blocks), and of a spawn counter.
    static float Hash01(float x, float y, int k)
    {
        var s = Mathf.Sin(x * 12.9898f + y * 78.233f + k * 37.719f) * 43758.547f;
        return s - Mathf.Floor(s);
    }

    static float Hash01(int a, int b)
    {
        unchecked
        {
            var h = (uint)(a * 73856093) ^ (uint)(b * 19349663) ^ 0x9e3779b9u;
            h ^= h >> 16;
            h *= 0x7feb352du;
            h ^= h >> 15;
            h *= 0x846ca68bu;
            h ^= h >> 16;
            return (h & 0xffffffu) / 16777216f;
        }
    }
}
