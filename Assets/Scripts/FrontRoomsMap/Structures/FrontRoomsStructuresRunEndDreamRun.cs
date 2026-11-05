using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// SP7 treatment #5, THE DREAM RUN (research/level_space/72_unreachable_end.md section 3.5, Little Nightmares II's own way):
/// the door never moves (the head stays on its rest line, 27 m: no recede, no track). From FD1's frame to the door's threshold
/// the floor is a machine: tile-faced pallets, one VCT course (0.30 m) each, butt-joined on the tiles' own joints, between two
/// galvanised comb plates with safety-yellow comb segments. At the trip the pallets come off their locks with a clack (10 mm
/// up, the comb teeth rise level with them) and the floor runs back toward the entrance, eased up to 1.0 m/s (9 -> 12) or
/// 1.25 m/s (9 -> 14) in 1.0 s. It carries the walker (room.Push while grounded on it, blended in and out over the comb
/// plates): a sprint nets 4.25-4.5 m/s, a walk 1.95-2.2, Shift's slow gait 0.19-0.44; standing still it brings the walker
/// back onto FD1's comb plate and stops there; walking back is quicker. The door only grows. T's re-strike: the belt eases to
/// a stop before the white reaches its first cell, then each course settles flush with a clack as the white walks over it,
/// the teeth go down, and the floor is a floor again. The tiles are the corridor's own VCT (the dress's band materials, in
/// mesh-UV copies so the tiles travel with the pallets); the dress's VCT over the belt is hidden and laid again, unchanged,
/// outside it. No light of its own: looks A/B/C, the band, the pulses and the re-strike are the frame's. Greybox: the comb
/// plates, the pallets' steel and the edge trims are this file's geometry in the game's own materials (no production asset
/// exists); no sound (the game would want the clunk, the motor, the pallets' rattle, the comb ticks, the lock). N: the same
/// belt at 0.7 of 72's speed.
/// </summary>
public sealed class FrontRoomsStructuresRunEndDreamRun : FrontRoomsStructuresRunEnd
{
    public override int Number => 5;
    public override string Name => "梦里跑不动";
    public override string TryLine => "过 FD1 地板往回走：冲刺、走路、Shift 慢走各跑一次，看几秒到门口。";
    // The door never moves and is never locked (C38): at 27 m it opens onto the frame's lit stand-in room (scene 4 has its hall
    // only behind 42 m).
    public override bool OpensShort => true;
    // FD2's mark: nothing recedes here, so no gaze gate's fallback.
    public override string Mark(FrontRoomsStructuresRunEndRoom room, int i, string today) => i == 3 ? "FD2 · 18 米" : today;

    public override string OptionLine
    {
        get
        {
            var next = pendingSet ? pendingGentle : gentle;
            var s = gentle ? "N：传送带速度，现在慢三成（再按回到 72 的）" : "N：传送带速度，现在是 72 的（按一下慢三成）";
            if (pendingSet && next != gentle) s += "，下次待触发时换成" + (next ? "慢三成" : "72 的");
            return s;
        }
    }

    // ---------------------------------------------------------------- the belt's numbers (each with its source)

    // 72 section 3.5 ("belt 1.0 m/s at T4, 1.25 m/s at T5; proposed here", not agreed: 72 section 5); FRAME.txt #5 maps them on
    // the receding variants (4 = 9 -> 12, 5 = 9 -> 14). Both are well under the walk (3.2 m/s) and under Shift's slow gait
    // (1.44 m/s; FrontRoomsStructuresWalker), so every gait still gains on the door.
    const float BeltT4 = 1.0f, BeltT5 = 1.25f;
    // This build, N: the same belt at 0.7 of 72's speed (0.70 / 0.875 m/s), for Red to feel how much of it is frustration.
    const float GentleShare = .7f;
    // 72 section 3.5 ("reaching 1.0 m/s in 1.0 s"), FRAME.txt ("up in 1.0 s"): a smoothstep in time (no step in speed; peak
    // 1.9 m/s2 at 1.25 m/s), starting once the pallets are up (0.10 s after the trip, this build).
    const float RampDelay = .10f, RampTime = 1f;
    // T's re-strike (72 section 3.5: "the belt slows and locks"): a smoothstep down over at least 0.9 s (this build: never
    // harsher than the ramp up), over before the settle begins.
    const float StopMin = .9f;
    // 72 section 3.5 ("the tile courses lift 10 mm"), FRAME.txt ("pallets 10 mm proud ... lifted at the trip"). This build's
    // clack: up to 13 mm in 0.05 s, back to 10 mm in 0.10 s (off the locks), 0.01 s after the comb teeth start up.
    const float Lift = .010f, LiftPeak = .013f, LiftDelay = .01f, LiftUp = .05f, LiftSettle = .10f;
    // 72 section 3.5 ("the pallets settle flush with one clack each course"): this build drops a course to 1.2 mm under flush
    // in 0.045 s and back in 0.075 s, 0.10 s after the white starts rising over it (FRAME.txt: Rig.resetRiseStart +
    // Rig.resetCellSeconds * cell, taken per metre so every course has its own clack: a wave at 20 m/s).
    const float SettleDip = .0012f, SettleDrop = .045f, SettleRebound = .075f, SettleAfterWhite = .10f;

    // ---------------------------------------------------------------- the machine's geometry (district metres)

    // FD1's casing stands at z 9 +- 0.10 (the frame's BuildFireFrames); the head's wall face at 27 - 0.08 with its casing at
    // 26.90 and cove at 26.9165 (BuildHead): the near comb plate starts at FD1's casing, the far one stops 7 mm short of the
    // head's. FRAME.txt #5: "Comb plates at FD1 (z 9.2) and before the door; pallets ... from 9.2 to 26.9".
    const float NearPlate0 = 9.10f, NearHinge = 9.50f, WinNear = 9.58f, NearTeethTip = 9.68f;
    const float FarTeethTip = 26.32f, WinFar = 26.42f, FarHinge = 26.50f, FarPlate1 = 26.893f;
    // The dress's VCT laid again outside the belt, ending and starting under the comb plates.
    const float StaticNearEnd = 9.30f, StaticFarStart = 26.70f;
    // One VCT course (FrontRoomsRunDress: Grid 0.30; RunB_VCT: a 1.2 m world tile of 4 x 4) per pallet, so the seams fall on
    // the tiles' own joints and the pattern runs on unbroken (the texture repeats every 1.2 m).
    const float Pitch = .30f, TextureRepeat = 1.2f;
    // The pallets' top edges: 3 mm chamfers, butt-joined (no gap to see through, so the moving seams stay low-contrast: rule
    // 11, a moving regular pattern); 1.5 mm with Reduce Flashing (fainter still). Their steel reaches the VCT slab's depth.
    const float Bevel = .003f, BevelReduced = .0015f, PalletBottom = -.02f;
    // The skirt (this build): 3 mm from the pallets to a 30 mm galvanised edge trim 2 mm proud on the dress's wax band.
    const float SkirtGap = .003f, TrimWidth = .03f, TrimProud = .002f;
    // The comb plates (this build; no kit exists): a galvanised landing plate 20 mm proud, a 30 mm nosing from 3 mm toward
    // FD1, a 60 mm band of safety yellow before the comb, the comb segment (yellow) 80 mm deep hinged on the plate's edge,
    // its teeth 100 mm long on a 25 mm pitch, 13 mm wide at the root and 7 mm at the tip, 4 mm over the lifted pallets (1 mm
    // at the clack's peak).
    const float PlateHalf = 1.40f, CombTop = .020f, NoseLow = .003f, Nose = .03f, PaintBand = .06f, CombBase = .08f;
    const float TeethLength = .10f, TeethPitch = .025f, ToothRoot = .0065f, ToothTip = .0035f, ToothBottom = .014f, ToothTipTop = .018f;
    // Armed the comb segment rests 4 degrees down, so its teeth's tips sit 1.5 mm over the flush pallets; it rises level at
    // the trip (0.05 s) and lowers once its end of the belt has settled (0.08 s).
    const float CombArmed = 4f, CombRise = .05f, CombLower = .08f;
    // The push (FRAME.txt: room.Push while grounded on the belt): blended over 0.5 m at each comb plate (this build: about the
    // walker's 0.6 m footprint, CharacterController r 0.3, so the carried speed never jumps), kept 0.15 s over a lost contact.
    const float PushBlend = .5f, GroundGrace = .15f;
    // Where a walker "reaches the door": off the belt onto the door's comb plate (the leaf itself is 0.5 m on).
    const float ArriveZ = WinFar;

    static readonly Vector3 Hidden = new Vector3(0f, -.05f, WinNear);
    const MeshUpdateFlags Quiet = MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices;
    static readonly CultureInfo Num = CultureInfo.InvariantCulture;

    // ---------------------------------------------------------------- state

    enum Belt { Locked, Running, Stopping }

    Belt state = Belt.Locked;
    float speed, target, speedAtStop, travel, stopTime = StopMin, settleT0 = 1f, cellSeconds = .15f, cellLength = 3f;
    bool gentle, pendingSet, pendingGentle;
    float groundGrace;
    // The run as the walker made it (for the arrival line and the plate).
    bool onBelt, arrived;
    float tripZ = float.NaN, legs, lastArrive = -1f, lastPlain = -1f;

    // ---------------------------------------------------------------- built things

    bool fallback;
    float baseY;
    readonly List<Renderer> dressVct = new List<Renderer>();
    float bx0 = -1.27f, bx1 = 1.27f, px0, px1, iz0 = .15f, iz1 = 41.85f, phaseX, phaseZ;
    int stripCount, vctSubs;
    float[] sx0, sx1;
    int[] sSub;
    Material[] sSrc, beltMats;
    Material galv, yellow, pit, bolt;
    Mesh beltMesh;
    Vector3[] bv;
    Vector2[] buv;
    int slots, perSlot;
    bool beltDirty = true;
    float lastBevel = -1f, colliderLift = float.NaN, nearAngle = float.NaN, farAngle = float.NaN;
    Transform belt, beltTop, nearComb, farComb;

    // ================================================================ hooks

    public override void Build(FrontRoomsStructuresRunEndRoom room)
    {
        galv = FrontRoomsPoolMaterials.Surface("RunB_GalvSteel", new Color(.60f, .61f, .60f));
        yellow = FrontRoomsPoolMaterials.Surface("RunB_BucketYellow", new Color(.85f, .68f, .08f));
        pit = FrontRoomsPoolMaterials.Surface("Plenum_Dark", new Color(.055f, .052f, .048f));
        bolt = FrontRoomsPoolMaterials.Steel;
        phaseX = Mathf.Repeat(room.Site.x, TextureRepeat);
        phaseZ = Mathf.Repeat(room.Site.z, TextureRepeat);
        ReadDress(room);
        BuildFloor(room);
        BuildPlates(room);
        nearComb = BuildComb(room, "SP7 #5 comb FD1 (safety yellow, hinged)", NearHinge, 1f);
        farComb = BuildComb(room, "SP7 #5 comb door (safety yellow, hinged)", FarHinge, -1f);
        BuildBelt(room);
        // The belt's top for the walker: one box over the window, moved up and down with the pallets (it never moves along z;
        // the carrying is room.Push). The dress's floor slabs stay under it.
        beltTop = room.NewObject(this, "SP7 #5 belt top (collider, rises 10 mm with the pallets)");
        var box = beltTop.gameObject.AddComponent<BoxCollider>();
        box.center = new Vector3((bx0 + bx1) * .5f, -.01f, (WinNear + WinFar) * .5f);
        box.size = new Vector3(bx1 - bx0, .02f, WinFar - WinNear);
    }

    public override void Enter(FrontRoomsStructuresRunEndRoom room)
    {
        lastArrive = lastPlain = -1f;
    }

    public override void Reset(FrontRoomsStructuresRunEndRoom room)
    {
        if (pendingSet) { gentle = pendingGentle; pendingSet = false; }
        state = Belt.Locked;
        speed = target = speedAtStop = 0f;
        travel = 0f;
        groundGrace = 0f;
        onBelt = arrived = false;
        tripZ = float.NaN;
        legs = 0f;
        // The armed room: the dress's VCT over the belt is off (laid again outside it in the same materials); no track and no
        // guide slot (the end never moves, 72 section 3.5: "no head, no track, no reserved length").
        if (!fallback)
            for (var i = 0; i < dressVct.Count; i++)
                if (dressVct[i] != null) dressVct[i].enabled = false;
        if (room.Track != null) room.Track.gameObject.SetActive(false);
        // With no track the head's trolleys and hangers would hang free in the soffit corners: the head's machine goes too (the
        // frame shows it again on its next reset).
        if (room.HeadMachine != null) room.HeadMachine.gameObject.SetActive(false);
        beltDirty = true;
        Pose(room, 0f);
        room.Dirty();
    }

    public override void Trip(FrontRoomsStructuresRunEndRoom room)
    {
        state = Belt.Running;
        target = BeltSpeed(room);
        onBelt = arrived = false;
        tripZ = float.NaN;
        legs = 0f;
        beltDirty = true;
        room.Dirty();
    }

    public override void Tick(FrontRoomsStructuresRunEndRoom room, FrontRoomsStructuresRunEndFrame frame)
    {
        var dt = Mathf.Clamp(frame.dt, 0f, .1f);
        var clock = frame.clock;
        switch (state)
        {
            case Belt.Running:
                speed = target * Smooth((clock - RampDelay) / RampTime);
                break;
            case Belt.Stopping:
                speed = speedAtStop * (1f - Smooth(clock / stopTime));
                if (clock >= FarCombDown + CombLower + .02f)
                {
                    // Every course is down and both combs are lowered: locked, a floor again (Rearmed follows).
                    state = Belt.Locked;
                    speed = 0f;
                    beltDirty = true;
                    room.Dirty();
                }
                break;
            default:
                speed = 0f;
                break;
        }
        travel += speed * dt;
        // Keep the float small; 1200 m is a whole number of texture repeats, so nothing on the belt moves.
        if (travel > 1200f) travel -= 1200f;
        Carry(room, frame, dt);
        Follow(room, frame, dt);
        Pose(room, clock);
    }

    public override void ResetStarted(FrontRoomsStructuresRunEndRoom room)
    {
        if (state != Belt.Running) return;
        state = Belt.Stopping;
        speedAtStop = speed;
        // The frame's re-strike (room.RestrikeAt: the white rises over a cell from its own time, F included). The belt stops
        // before it reaches the belt's first cell; the courses settle as it walks over them (per metre, at the rig's 0.15 s a
        // 3 m cell), never while the belt still moves.
        var rig = room.Rig;
        cellSeconds = rig != null ? rig.resetCellSeconds : .15f;
        cellLength = rig != null ? Mathf.Max(.1f, rig.resetCellLength) : 3f;
        var white = room.RestrikeAt(0f);
        var whiteAtBelt = room.RestrikeAt(WinNear);
        stopTime = Mathf.Max(StopMin, whiteAtBelt - .05f);
        settleT0 = Mathf.Max(white + cellSeconds * WinNear / cellLength + SettleAfterWhite, stopTime + .05f);
        room.Dirty();
    }

    public override void Rearmed(FrontRoomsStructuresRunEndRoom room)
    {
        if (pendingSet) { gentle = pendingGentle; pendingSet = false; }
        state = Belt.Locked;
        speed = speedAtStop = 0f;
        beltDirty = true;
        Pose(room, 0f);
        room.Dirty();
    }

    public override void Lines(FrontRoomsStructuresRunEndRoom room, List<float> lines)
    {
        // The door never leaves its rest line.
        lines.Add(room.RestLine);
    }

    public override bool Option(FrontRoomsStructuresRunEndRoom room, out string message)
    {
        var armed = room.Phase == FrontRoomsStructuresRunEndPhase.Armed;
        var next = !(pendingSet ? pendingGentle : gentle);
        if (armed)
        {
            gentle = next;
            pendingSet = false;
        }
        else
        {
            pendingGentle = next;
            pendingSet = true;
        }
        message = "传送带速度：" + (next
            ? "慢三成（" + F(BeltT4 * GentleShare, "0.0#") + " / " + F(BeltT5 * GentleShare, "0.0#") + " 米/秒）"
            : "72 的（" + F(BeltT4, "0.0#") + " / " + F(BeltT5, "0.0#") + " 米/秒）") + "，会退 9→12 / 9→14 格" + (armed ? "" : "（下次待触发时生效）");
        room.Dirty();
        return true;
    }

    public override string Status(FrontRoomsStructuresRunEndRoom room)
    {
        string s;
        switch (state)
        {
            case Belt.Running: s = "传送带往回走 " + F(target, "0.0#") + " 米/秒"; break;
            case Belt.Stopping: s = "传送带停下、锁回去"; break;
            default: s = "地板锁着" + (gentle ? "（慢三成）" : ""); break;
        }
        if (lastArrive >= 0f) s += " · 上次到门口 " + F(lastArrive, "0.0") + " 秒";
        return s;
    }

    public override void Guide(FrontRoomsStructuresRunEndRoom room, StringBuilder sb)
    {
        sb.AppendLine("做法 5 梦里跑不动：门不动也不退（一直在 27 米，所以顶上没有轨道）。FD1 门框后到门口的地板，是一节一节贴着走廊地砖的传送带，两头各有一块带黄色梳齿的钢板；过 FD1 触发时地板“咔”地抬起 10 毫米，1 秒内加速往回走：会退 9→12 格 1.0 米/秒，9→14 格 1.25 米/秒（N 换成慢三成）。");
        sb.AppendLine("满体力冲刺约 3.7 / 3.9 秒到门口（地板不动 3.2 秒），走路约 7.6 / 8.5 秒（5.5 秒），Shift 慢走约 37 / 85 秒（12 秒）；站着不动会被送回 FD1 的梳齿板上停住，往回走更快，谁都不会被困住。到门口时提示报用时。T 恢复：传送带慢下来停住，白光走过时一节一节落平锁死，又是地板。");
        sb.AppendLine("走上门前的梳齿板就算到了；门不锁，E 推开，门后是一间亮着的占位小房间（场景 4 的大厅只在 42 米，游戏里门后是下一个房间）。传送带、梳齿板和边条是用游戏自己的地砖和钢材搭的，没有正式模型；没有声音（游戏里要有咔哒、电机、齿板和锁死的声音）；Relay 替身不动，游戏里它也会被带着走。");
    }

    // ================================================================ the belt, each frame

    float BeltSpeed(FrontRoomsStructuresRunEndRoom room) => (room.Variant == 4 ? BeltT4 : BeltT5) * (gentle ? GentleShare : 1f);

    // The walker on the belt is carried with it (FRAME.txt: once a frame, velocity * dt, while grounded on it). The share is 0
    // on the comb plates and eases to 1 over 0.5 m of belt at both ends: a standing walker is brought back onto FD1's comb plate
    // and comes to rest there; one stepping on or off never feels a jump in speed.
    void Carry(FrontRoomsStructuresRunEndRoom room, FrontRoomsStructuresRunEndFrame frame, float dt)
    {
        groundGrace = frame.grounded ? GroundGrace : groundGrace - dt;
        if (speed < 1e-4f || groundGrace <= 0f || dt <= 0f) return;
        var p = frame.feet;
        if (p.y > baseY + .15f || p.y < -.2f || Mathf.Abs(p.x) > FrontRoomsStructuresRunEndRoom.HalfWidth) return;
        var share = PushShare(p.z);
        if (share <= 0f) return;
        room.Push(new Vector3(0f, 0f, -speed * share * dt));
    }

    static float PushShare(float z)
    {
        if (z <= WinNear || z >= WinFar) return 0f;
        return Mathf.Min(Smooth((z - WinNear) / PushBlend), Smooth((WinFar - z) / PushBlend));
    }

    // The run, for the arrival line: the walker's own legs (frame.own, the pushes taken out) from the trip to the door's comb
    // plate, and what the same legs would have taken on a floor that stays put.
    void Follow(FrontRoomsStructuresRunEndRoom room, FrontRoomsStructuresRunEndFrame frame, float dt)
    {
        if (frame.phase != FrontRoomsStructuresRunEndPhase.Tripped || arrived) return;
        var z = frame.feet.z;
        if (float.IsNaN(tripZ)) tripZ = z;
        legs += frame.own.z * dt;
        if (!onBelt && z >= WinNear && z < WinFar) onBelt = true;
        // Only a walker who came off the belt onto the door's plate (a jump to the hall stop is no arrival).
        if (!onBelt || z < ArriveZ || z > ArriveZ + 1f || Mathf.Abs(frame.feet.x) > FrontRoomsStructuresRunEndRoom.HalfWidth) return;
        arrived = true;
        lastArrive = frame.clock;
        var mean = frame.clock > .05f ? legs / frame.clock : 0f;
        lastPlain = mean > .3f && tripZ < WinNear - .3f ? (ArriveZ - tripZ) / mean : -1f;
        var line = "到门口了：触发后 " + F(lastArrive, "0.0") + " 秒";
        if (lastPlain > 0f) line += "；同样的腿速，地板不动约 " + F(lastPlain, "0.0") + " 秒";
        room.Flash(line + "（E 推开门：场景 4 里门后是一间占位的小房间）");
        room.Dirty();
    }

    // The clack at the trip (all courses at once), the settle at the re-strike (course by course as the white passes).
    float PalletLift(float zc, float clock)
    {
        switch (state)
        {
            case Belt.Running: return Clack(clock - LiftDelay);
            case Belt.Stopping: return Settle(clock - SettleAt(zc));
            default: return 0f;
        }
    }

    static float Clack(float t)
    {
        if (t <= 0f) return 0f;
        if (t < LiftUp) return LiftPeak * Smooth(t / LiftUp);
        if (t < LiftUp + LiftSettle) return Mathf.Lerp(LiftPeak, Lift, Smooth((t - LiftUp) / LiftSettle));
        return Lift;
    }

    static float Settle(float t)
    {
        if (t <= 0f) return Lift;
        if (t < SettleDrop) return Mathf.Lerp(Lift, -SettleDip, Smooth(t / SettleDrop));
        if (t < SettleDrop + SettleRebound) return Mathf.Lerp(-SettleDip, 0f, Smooth((t - SettleDrop) / SettleRebound));
        return 0f;
    }

    float SettleAt(float z) => settleT0 + cellSeconds * (z - WinNear) / cellLength;
    float NearCombDown => SettleAt(WinNear + Pitch) + .03f;
    float FarCombDown => SettleAt(WinFar + Pitch * .5f) + .03f;

    // The comb segment's tilt (degrees down at its teeth): armed down, level from the trip, down again once its end has settled.
    float CombAngle(bool far, float clock)
    {
        switch (state)
        {
            case Belt.Running: return Mathf.Lerp(CombArmed, 0f, Smooth(clock / CombRise));
            case Belt.Stopping: return Mathf.Lerp(0f, CombArmed, Smooth((clock - (far ? FarCombDown : NearCombDown)) / CombLower));
            default: return CombArmed;
        }
    }

    // The walker's floor follows the pallets without the clack's overshoot (a 10 mm rise over 0.15 s, a fall over the wave).
    float ColliderLift(float clock)
    {
        switch (state)
        {
            case Belt.Running: return Lift * Smooth((clock - LiftDelay) / (LiftUp + LiftSettle));
            case Belt.Stopping:
                var a = SettleAt(WinNear);
                return Lift * (1f - Smooth((clock - a) / Mathf.Max(.05f, SettleAt(WinFar) - a + SettleDrop)));
            default: return 0f;
        }
    }

    void Pose(FrontRoomsStructuresRunEndRoom room, float clock)
    {
        var bevel = room.Reduce ? BevelReduced : Bevel;
        if (state != Belt.Locked || beltDirty || bevel != lastBevel)
        {
            FillBelt(clock, bevel);
            beltDirty = false;
            lastBevel = bevel;
        }
        Tilt(nearComb, CombAngle(false, clock), 1f, ref nearAngle);
        Tilt(farComb, CombAngle(true, clock), -1f, ref farAngle);
        var lift = ColliderLift(clock);
        if (beltTop != null && lift != colliderLift)
        {
            colliderLift = lift;
            beltTop.localPosition = new Vector3(0f, baseY + lift, 0f);
        }
    }

    static void Tilt(Transform pivot, float degrees, float dir, ref float shown)
    {
        if (pivot == null || degrees == shown) return;
        shown = degrees;
        pivot.localRotation = Quaternion.Euler(dir * degrees, 0f, 0f);
    }

    // The window's pallets, from the seams: a seam wherever the belt's material coordinate (z + travel, in the world's tile
    // phase) is a whole course, so the tiles' pattern is continuous by construction and nothing wraps in view. Every slot has a
    // fixed set of vertices (one quad per tile face and chamfer per band, two steel ends); slots outside the window collapse.
    void FillBelt(float clock, float bevel)
    {
        if (beltMesh == null) return;
        var phase = Mathf.Repeat(travel + phaseZ, TextureRepeat);
        var first = WinNear - Mathf.Repeat(WinNear + phase, Pitch);
        for (var k = 0; k < slots; k++)
        {
            var s = first + k * Pitch;
            var e = s + Pitch;
            var o = k * perSlot;
            if (e <= WinNear + 1e-4f || s >= WinFar - 1e-4f)
            {
                Collapse(o, perSlot);
                continue;
            }
            var top = baseY + PalletLift(s + Pitch * .5f, clock);
            var c0 = Mathf.Max(s, WinNear);
            var c1 = Mathf.Min(e, WinFar);
            for (var i = 0; i < stripCount; i++, o += 12)
            {
                float x0 = sx0[i], x1 = sx1[i];
                // The tile face.
                var a0 = Mathf.Max(c0, s + bevel);
                var a1 = Mathf.Min(c1, e - bevel);
                if (a1 > a0 + 1e-5f) Slope(o, x0, x1, a0, top, a1, top, phase);
                else Collapse(o, 4);
                // The chamfer toward the entrance: from (s, top - bevel) up to (s + bevel, top).
                var b0 = Mathf.Max(c0, s);
                var b1 = Mathf.Min(c1, s + bevel);
                if (b1 > b0 + 1e-5f) Slope(o + 4, x0, x1, b0, top - bevel + (b0 - s), b1, top - bevel + (b1 - s), phase);
                else Collapse(o + 4, 4);
                // The chamfer toward the door: from (e - bevel, top) down to (e, top - bevel).
                var n0 = Mathf.Max(c0, e - bevel);
                var n1 = Mathf.Min(c1, e);
                if (n1 > n0 + 1e-5f) Slope(o + 8, x0, x1, n0, top - (n0 - e + bevel), n1, top - (n1 - e + bevel), phase);
                else Collapse(o + 8, 4);
            }
            // The steel ends under the tiles: seen only where two courses stand at different heights (the settle).
            if (s >= WinNear - 1e-5f) End(o, s, top - bevel, true);
            else Collapse(o, 4);
            if (e <= WinFar + 1e-5f) End(o + 4, e, top - bevel, false);
            else Collapse(o + 4, 4);
        }
        beltMesh.SetVertices(bv, 0, bv.Length, Quiet);
        beltMesh.SetUVs(0, buv, 0, buv.Length, Quiet);
    }

    // A quad a (x0, yA, zA), b (x0, yB, zB), c (x1, yB, zB), d (x1, yA, zA): its front is Cross(b - a, d - a), up (and toward the
    // falling side for a chamfer). UVs in metres as the surface shader's world projection reads a floor: (x, -z), the belt's
    // travel and the world's tile phase added, so the tiles go with the pallets.
    void Slope(int o, float x0, float x1, float zA, float yA, float zB, float yB, float phase)
    {
        bv[o] = new Vector3(x0, yA, zA);
        bv[o + 1] = new Vector3(x0, yB, zB);
        bv[o + 2] = new Vector3(x1, yB, zB);
        bv[o + 3] = new Vector3(x1, yA, zA);
        float u0 = x0 + phaseX, u1 = x1 + phaseX, vA = -(zA + phase), vB = -(zB + phase);
        buv[o] = new Vector2(u0, vA);
        buv[o + 1] = new Vector2(u0, vB);
        buv[o + 2] = new Vector2(u1, vB);
        buv[o + 3] = new Vector2(u1, vA);
    }

    void End(int o, float z, float yTop, bool south)
    {
        if (south)
        {
            bv[o] = new Vector3(px0, PalletBottom, z);
            bv[o + 1] = new Vector3(px0, yTop, z);
            bv[o + 2] = new Vector3(px1, yTop, z);
            bv[o + 3] = new Vector3(px1, PalletBottom, z);
        }
        else
        {
            bv[o] = new Vector3(px0, PalletBottom, z);
            bv[o + 1] = new Vector3(px1, PalletBottom, z);
            bv[o + 2] = new Vector3(px1, yTop, z);
            bv[o + 3] = new Vector3(px0, yTop, z);
        }
    }

    void Collapse(int o, int count)
    {
        for (var i = 0; i < count; i++) bv[o + i] = Hidden;
    }

    // ================================================================ build

    // The dress's VCT bands (field, accents, lane) are long-shell slabs from z 0.15 to 41.85, top at 0, 20 mm thick: their x,
    // their materials (the pallets wear the same bands) and the renderers to hide over the belt. The wax band along the walls
    // stays as it is. Without the dress (the fallback shell) the belt lies 2 mm over the shell's floor in its material.
    void ReadDress(FrontRoomsStructuresRunEndRoom room)
    {
        dressVct.Clear();
        var site = room.Site;
        var found = new List<Band>();
        var shell = room.LongShell;
        if (room.Dress != null && shell != null)
            for (var i = 0; i < shell.Count; i++)
            {
                var r = shell[i];
                if (r == null || r.sharedMaterial == null) continue;
                var n = r.gameObject.name;
                if (!n.StartsWith("run b vct", System.StringComparison.Ordinal) || n.IndexOf("wax", System.StringComparison.Ordinal) >= 0) continue;
                var bounds = WorldBounds(r);
                var top = bounds.max.y - site.y;
                if (top > .01f || top < -.01f || bounds.size.x < .01f) continue;
                found.Add(new Band { renderer = r, bounds = bounds });
            }
        found.Sort(ByX);
        var mats = new List<Material>();
        if (found.Count == 0)
        {
            fallback = true;
            baseY = .002f;
            stripCount = 1;
            sx0 = new[] { -FrontRoomsStructuresRunEndRoom.HalfWidth + .15f };
            sx1 = new[] { FrontRoomsStructuresRunEndRoom.HalfWidth - .15f };
            sSrc = new[] { room.FloorMat };
            sSub = new[] { 0 };
            mats.Add(room.FloorMat);
            FrontRoomsPoolMaterials.LogOnce("SP7 #5 dress", "SP7 #5: no Run! B VCT in the dress; the belt lies 2 mm over the fallback floor.");
        }
        else
        {
            fallback = false;
            baseY = 0f;
            stripCount = found.Count;
            sx0 = new float[stripCount];
            sx1 = new float[stripCount];
            sSrc = new Material[stripCount];
            sSub = new int[stripCount];
            iz0 = float.MaxValue;
            iz1 = float.MinValue;
            for (var i = 0; i < stripCount; i++)
            {
                var r = found[i].renderer;
                var b = found[i].bounds;
                sx0[i] = b.min.x - site.x;
                sx1[i] = b.max.x - site.x;
                iz0 = Mathf.Min(iz0, b.min.z - site.z);
                iz1 = Mathf.Max(iz1, b.max.z - site.z);
                sSrc[i] = r.sharedMaterial;
                var m = mats.IndexOf(r.sharedMaterial);
                if (m < 0)
                {
                    m = mats.Count;
                    mats.Add(r.sharedMaterial);
                }
                sSub[i] = m;
                dressVct.Add(r);
            }
        }
        bx0 = float.MaxValue;
        bx1 = float.MinValue;
        for (var i = 0; i < stripCount; i++)
        {
            bx0 = Mathf.Min(bx0, sx0[i]);
            bx1 = Mathf.Max(bx1, sx1[i]);
        }
        px0 = bx0 + SkirtGap;
        px1 = bx1 - SkirtGap;
        // The pallets' bands: the dress's, cut to the pallets' edges.
        for (var i = 0; i < stripCount; i++)
        {
            sx0[i] = Mathf.Max(sx0[i], px0);
            sx1[i] = Mathf.Min(sx1[i], px1);
        }
        vctSubs = mats.Count;
        beltMats = new Material[vctSubs + 1];
        for (var i = 0; i < vctSubs; i++) beltMats[i] = Travelling(room, mats[i]);
        beltMats[vctSubs] = galv;
        for (var i = 0; i < beltMats.Length; i++) if (beltMats[i] == null) beltMats[i] = FrontRoomsPoolMaterials.Steel;
    }

    sealed class Band
    {
        public Renderer renderer;
        public Bounds bounds;
    }

    static int ByX(Band a, Band b) => a.bounds.min.x.CompareTo(b.bounds.min.x);

    // A slab's world box, from its mesh when the renderer is off (a disabled renderer's bounds are not to be trusted); the
    // dress's slabs stand unrotated.
    static Bounds WorldBounds(Renderer r)
    {
        if (r.enabled && r.gameObject.activeInHierarchy) return r.bounds;
        var mf = r.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return r.bounds;
        var local = mf.sharedMesh.bounds;
        var t = r.transform;
        var size = Vector3.Scale(local.size, t.lossyScale);
        return new Bounds(t.TransformPoint(local.center), new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z)));
    }

    // A copy of the dress's band that reads its tiles from the mesh's UVs (metres) instead of the world, so they ride the
    // pallets (FrontRoomsSurface.shader: _FR_MESH_UV, a multi_compile_local). Its world-space macro dirt would stand still while
    // the tiles move under it, so the copy has none (the tone's +-2 % stays). Everything else is the dress's own.
    static Material Travelling(FrontRoomsStructuresRunEndRoom room, Material src)
    {
        if (src == null) return null;
        var m = new Material(src) { name = "Structures / SP7 #5 pallets " + src.name, hideFlags = HideFlags.DontSave };
        m.EnableKeyword("_FR_MESH_UV");
        if (m.HasProperty("_MeshUV")) m.SetFloat("_MeshUV", 1f);
        if (m.HasProperty("_MacroDirt")) m.SetFloat("_MacroDirt", 0f);
        room.Own(m);
        return m;
    }

    // The corridor's own VCT laid again where the belt is not (the dress's bands hidden from 0.15 to 41.85 in Reset): the same
    // materials, world-projected, so it reads exactly as before; the dress's colliders stay under it. The pit under the belt
    // (dark, seen only down the skirt gaps) and the two galvanised edge trims on the wax band.
    void BuildFloor(FrontRoomsStructuresRunEndRoom room)
    {
        var k = room.Kit;
        if (!fallback)
        {
            room.BeginPart(this, "SP7 #5 corridor VCT outside the belt (the dress's own bands)");
            k.Shell(FrontRoomsStructuresRunEndRoom.Ceiling, 0f);
            for (var i = 0; i < stripCount; i++)
            {
                var a = sx0[i] <= px0 + 1e-4f ? bx0 : sx0[i];
                var b = sx1[i] >= px1 - 1e-4f ? bx1 : sx1[i];
                k.Box(a, -.02f, iz0, b, 0f, StaticNearEnd, sSrc[i], false);
                k.Box(a, -.02f, StaticFarStart, b, 0f, iz1, sSrc[i], false);
            }
            room.EndPart();
        }
        room.BeginPart(this, "SP7 #5 belt pit and edge trims");
        k.Shell(FrontRoomsStructuresRunEndRoom.Ceiling, 0f);
        k.Mesh.CastShadows = false;
        k.Box(bx0 - .006f, -.07f, StaticNearEnd, bx1 + .006f, -.025f, StaticFarStart, pit, false);
        k.Box(bx0 - TrimWidth, baseY, WinNear - .06f, bx0, baseY + TrimProud, WinFar + .06f, galv, false);
        k.Box(bx1, baseY, WinNear - .06f, bx1 + TrimWidth, baseY + TrimProud, WinFar + .06f, galv, false);
        // The trims' countersunk screws every 0.6 m: still marks beside the moving seams, so the eye reads the belt's travel.
        for (var z = WinNear + .3f; z < WinFar - .1f; z += .6f)
            foreach (var x in new[] { bx0 - TrimWidth * .5f, bx1 + TrimWidth * .5f })
                k.Box(x - .004f, baseY + TrimProud, z - .004f, x + .004f, baseY + TrimProud + .0006f, z + .004f, bolt, false);
        room.EndPart();
    }

    // The two landing plates (static): FD1's with a nosing toward the frame, the door's up to the head's casing; each runs on
    // beside the belt to the teeth's line over the wax band, carries a 60 mm yellow band before its comb, two section seams and
    // sixteen bolt heads, and one collision box (top 20 mm) over the plate and its teeth.
    void BuildPlates(FrontRoomsStructuresRunEndRoom room)
    {
        var k = room.Kit;
        room.BeginPart(this, "SP7 #5 comb plate FD1 (landing plate)");
        k.Shell(FrontRoomsStructuresRunEndRoom.Ceiling, 0f);
        k.Box(-PlateHalf, -.07f, NearPlate0, PlateHalf, NoseLow, NearPlate0 + Nose, galv, false);
        var n0 = new Vector3(-PlateHalf, NoseLow, NearPlate0);
        var n1 = new Vector3(PlateHalf, NoseLow, NearPlate0);
        var n2 = new Vector3(PlateHalf, CombTop, NearPlate0 + Nose);
        var n3 = new Vector3(-PlateHalf, CombTop, NearPlate0 + Nose);
        k.QuadPlanar(n0, n1, n2, n3, new Vector3(0f, Nose, -(CombTop - NoseLow)), galv, false);
        k.QuadPlanar(n0, n3, new Vector3(-PlateHalf, NoseLow, NearPlate0 + Nose), new Vector3(-PlateHalf, NoseLow, NearPlate0 + Nose), Vector3.left, galv, false);
        k.QuadPlanar(n1, new Vector3(PlateHalf, NoseLow, NearPlate0 + Nose), n2, n2, Vector3.right, galv, false);
        k.Box(-PlateHalf, -.07f, NearPlate0 + Nose, PlateHalf, CombTop, NearHinge, galv, false);
        k.Box(-PlateHalf, -.07f, NearHinge, bx0 - .0005f, CombTop, NearTeethTip, galv, false);
        k.Box(bx1 + .0005f, -.07f, NearHinge, PlateHalf, CombTop, NearTeethTip, galv, false);
        k.Box(-PlateHalf, CombTop, NearHinge - PaintBand, PlateHalf, CombTop + .0006f, NearHinge, yellow, false);
        PlateDetail(k, NearPlate0 + Nose, NearHinge - PaintBand);
        k.Mesh.Collider(new Vector3(0f, (CombTop - .02f) * .5f, (NearPlate0 + NearTeethTip) * .5f), new Vector3(2f * PlateHalf, CombTop + .02f, NearTeethTip - NearPlate0));
        room.EndPart();

        room.BeginPart(this, "SP7 #5 comb plate door (landing plate)");
        k.Shell(FrontRoomsStructuresRunEndRoom.Ceiling, 0f);
        k.Box(-PlateHalf, -.07f, FarHinge, PlateHalf, CombTop, FarPlate1, galv, false);
        k.Box(-PlateHalf, -.07f, FarTeethTip, bx0 - .0005f, CombTop, FarHinge, galv, false);
        k.Box(bx1 + .0005f, -.07f, FarTeethTip, PlateHalf, CombTop, FarHinge, galv, false);
        k.Box(-PlateHalf, CombTop, FarHinge, PlateHalf, CombTop + .0006f, FarHinge + PaintBand, yellow, false);
        PlateDetail(k, FarHinge + PaintBand, FarPlate1);
        k.Mesh.Collider(new Vector3(0f, (CombTop - .02f) * .5f, (FarTeethTip + FarPlate1) * .5f), new Vector3(2f * PlateHalf, CombTop + .02f, FarPlate1 - FarTeethTip));
        room.EndPart();
    }

    // Two section seams (x +-0.47, dark, 3 mm) and two rows of eight bolt heads (14 mm, 1.5 mm proud) between z0 and z1.
    void PlateDetail(FrontRoomsStructuresKit k, float z0, float z1)
    {
        foreach (var x in new[] { -.47f, .47f })
            k.Box(x - .0015f, CombTop, z0 + .004f, x + .0015f, CombTop + .0005f, z1 - .004f, pit, false);
        var rows = new[] { z0 + .05f, z1 - .05f };
        var cols = new[] { -1.32f, -1.02f, -.70f, -.24f, .24f, .70f, 1.02f, 1.32f };
        foreach (var z in rows)
            foreach (var x in cols)
                k.Box(x - .007f, CombTop, z - .007f, x + .007f, CombTop + .0015f, z + .007f, bolt, false);
    }

    // A comb segment: the yellow base from the hinge toward the belt (down into the pit, so it hides the belt's window edge)
    // and its teeth over the belt; built in district metres, then hung under a pivot on the hinge line, so tilting the pivot
    // about x lifts and lowers the teeth (dir +1: teeth toward +z, FD1's; -1: toward -z, the door's).
    Transform BuildComb(FrontRoomsStructuresRunEndRoom room, string name, float hinge, float dir)
    {
        var pivot = room.NewObject(this, name + " hinge");
        pivot.localPosition = new Vector3(0f, CombTop, hinge);
        var part = room.BeginPart(this, name);
        var k = room.Kit;
        k.Shell(FrontRoomsStructuresRunEndRoom.Ceiling, 0f);
        var front = hinge + dir * CombBase;
        k.Box(bx0, -.03f, Mathf.Min(hinge, front), bx1, CombTop, Mathf.Max(hinge, front), yellow, false);
        var tip = front + dir * TeethLength;
        var count = Mathf.Max(1, Mathf.FloorToInt((bx1 - bx0 - 2f * ToothRoot) / TeethPitch) + 1);
        var x = (bx0 + bx1) * .5f - (count - 1) * .5f * TeethPitch;
        for (var i = 0; i < count; i++, x += TeethPitch) Tooth(k, x, front, tip, dir);
        room.EndPart();
        part.SetParent(pivot, true);
        return pivot;
    }

    // One tooth: a tapered prism, its top sloping 2 mm down to the tip, flat underneath (never seen).
    void Tooth(FrontRoomsStructuresKit k, float xc, float zr, float zt, float dir)
    {
        var rw = new Vector3(xc - ToothRoot, ToothBottom, zr);
        var re = new Vector3(xc + ToothRoot, ToothBottom, zr);
        var tw = new Vector3(xc - ToothTip, ToothBottom, zt);
        var te = new Vector3(xc + ToothTip, ToothBottom, zt);
        var rwT = new Vector3(xc - ToothRoot, CombTop, zr);
        var reT = new Vector3(xc + ToothRoot, CombTop, zr);
        var twT = new Vector3(xc - ToothTip, ToothTipTop, zt);
        var teT = new Vector3(xc + ToothTip, ToothTipTop, zt);
        k.QuadPlanar(rwT, twT, teT, reT, Vector3.up, yellow, false);
        k.QuadPlanar(rw, tw, twT, rwT, Vector3.left, yellow, false);
        k.QuadPlanar(re, reT, teT, te, Vector3.right, yellow, false);
        k.QuadPlanar(tw, te, teT, twT, new Vector3(0f, 0f, dir), yellow, false);
    }

    // The belt's renderer: one dynamic mesh, a submesh per VCT band material (the travelling copies) and one for the steel ends.
    void BuildBelt(FrontRoomsStructuresRunEndRoom room)
    {
        belt = room.NewObject(this, "SP7 #5 belt (tile-faced pallets, one VCT course each)");
        slots = Mathf.CeilToInt((WinFar - WinNear) / Pitch) + 2;
        perSlot = stripCount * 12 + 8;
        var n = slots * perSlot;
        bv = new Vector3[n];
        buv = new Vector2[n];
        var nrm = new Vector3[n];
        var tan = new Vector4[n];
        var subs = vctSubs + 1;
        var tris = new List<int>[subs];
        for (var s = 0; s < subs; s++) tris[s] = new List<int>(slots * 36);
        var chamferSouth = new Vector3(0f, 1f, -1f).normalized;
        var chamferNorth = new Vector3(0f, 1f, 1f).normalized;
        // Tangent along +x with w +1: the surface shader's mesh-UV frame then matches its world projection on a floor.
        var along = new Vector4(1f, 0f, 0f, 1f);
        for (var k = 0; k < slots; k++)
        {
            var o = k * perSlot;
            for (var i = 0; i < stripCount; i++, o += 12)
            {
                Normals(nrm, tan, o, Vector3.up, along);
                Normals(nrm, tan, o + 4, chamferSouth, along);
                Normals(nrm, tan, o + 8, chamferNorth, along);
                var t = tris[sSub[i]];
                Quad(t, o);
                Quad(t, o + 4);
                Quad(t, o + 8);
            }
            Normals(nrm, tan, o, Vector3.back, along);
            Normals(nrm, tan, o + 4, Vector3.forward, along);
            Quad(tris[vctSubs], o);
            Quad(tris[vctSubs], o + 4);
        }
        for (var i = 0; i < n; i++) bv[i] = Hidden;
        beltMesh = new Mesh { name = "SP7 #5 belt", hideFlags = HideFlags.DontSave };
        beltMesh.MarkDynamic();
        beltMesh.SetVertices(bv);
        beltMesh.SetNormals(nrm);
        beltMesh.SetTangents(tan);
        beltMesh.SetUVs(0, buv);
        beltMesh.subMeshCount = subs;
        for (var s = 0; s < subs; s++) beltMesh.SetTriangles(tris[s], s, false);
        beltMesh.bounds = new Bounds(new Vector3((bx0 + bx1) * .5f, 0f, (WinNear + WinFar) * .5f), new Vector3(bx1 - bx0 + .1f, .2f, WinFar - WinNear + .4f));
        room.Own(beltMesh);
        belt.gameObject.AddComponent<MeshFilter>().sharedMesh = beltMesh;
        var mr = belt.gameObject.AddComponent<MeshRenderer>();
        mr.sharedMaterials = beltMats;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = true;
        beltDirty = true;
    }

    static void Normals(Vector3[] nrm, Vector4[] tan, int o, Vector3 n, Vector4 t)
    {
        for (var i = 0; i < 4; i++)
        {
            nrm[o + i] = n;
            tan[o + i] = t;
        }
    }

    // (0, 1, 2) (0, 2, 3): with Cross(b - a, d - a) along the front, a, b, c, d run clockwise seen from the front (as the kit).
    static void Quad(List<int> t, int o)
    {
        t.Add(o);
        t.Add(o + 1);
        t.Add(o + 2);
        t.Add(o);
        t.Add(o + 2);
        t.Add(o + 3);
    }

    static string F(float v, string format) => v.ToString(format, Num);
}
