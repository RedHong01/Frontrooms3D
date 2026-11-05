using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// One family room of the title corridor (line game visual, title_stream Build, 2026-11-04; Red 11-04 09:13:40 in this line's
/// chat: "...我希望你在这个无限流房间的里增加上这些不同的房间作为最开始无限流房间的设计"). A vignette is built once, kept in the
/// stream's bank and moved into a sealed stream slot when the title programme gives that slot its family
/// (FrontRoomsRoomStream.TitleRooms). It is drawn at the stream's own module (11.5 m between wall centres, 12 m, 2.9 m; the
/// slot keeps its shell, its walnut pair at the far end and its door wall) and brings what makes the family: its dress, its
/// lamps (or the slot's L03 with its own odds), the finish the slot's walls, floor and ceiling take while it stands there, its
/// zone grade and its reflection zone. Nothing here ticks: the stream drives every lamp through its lamp logic (strike, rise,
/// flicker, dead), so the reveal, the lights-off behind a shut door and the 1.2 s fade at the start door work for every family.
/// Room space: x 0 on the corridor's axis, z 0 on the entrance door line, z 12 on the far door line, y 0 on the floor.
/// Every object name avoids floor, carpet, ceiling, wall, seal and partition (RoomStream.RefreshRoomMaterials).
/// </summary>
public sealed class FrontRoomsTitleVignette
{
    /// <summary>What the stream writes to a lamp's second renderer: nothing, the lens's own block (the pendant's lit ceiling
    /// patch, as the map writes it), or the Run! strip's red tube (its coating lit by the two white tubes, armed).</summary>
    public enum SecondDrive : byte { None, LensBlock, RedTube }

    public FrontRoomsRoomStream.TitleFamily family;
    public GameObject root;
    /// <summary>The vignette brings its own lamps (the slot's twelve L03 are hidden while it stands there).</summary>
    public bool ownLamps;
    /// <summary>Own lamps, but the slot's L03 stay up, dark and dead (the dark pool's overheads).</summary>
    public bool keepSlotFixtures;
    /// <summary>Own lamps in lamp-logic order; <see cref="shadowCount"/> of them cast soft shadows (row 4: the lamps over the
    /// furniture, as tight cones; Run!'s strips all, as its rig).</summary>
    public Light[] lights = new Light[0];
    public float[] baseIntensity = new float[0];
    public Renderer[] lens = new Renderer[0];
    public Renderer[] second = new Renderer[0];
    public Renderer[] halo = new Renderer[0];
    public int shadowCount;
    public SecondDrive secondDrive;
    /// <summary>The lamp colour (the Lights) and the lens's emission at full level (lensColor * lensPeak, as the map drives it).</summary>
    public Color lampColor = new Color(1f, .96f, .88f);
    public Color lensColor = new Color(1f, .96f, .88f);
    public float lensPeak = FrontRoomsTrofferKit.LensPeak;
    /// <summary>RedTube: the coating's emission at the white tubes' full level (FrontRoomsRunRig's redTubeBounce, armed).</summary>
    public Color secondColor = new Color(.40f, .045f, .05f);
    /// <summary>The slot's L03 while the vignette rides them (no own lamps): colour above, intensity and range.</summary>
    public float slotIntensity = 5.2f, slotRange = 10f;
    /// <summary>Per-lamp odds of a dead or a failing ballast (the stream's LampOdds for this family).</summary>
    public float deadChance = .02f, unstableChance = .10f;
    /// <summary>The lamps strike from the entrance toward the far door, one cell at a time (Run!'s RESET re-strike).</summary>
    public bool chase;
    /// <summary>The slot shell's finish while the vignette stands in it; null keeps the Level 0 finish.</summary>
    public Material wall, floor, ceiling;
    /// <summary>The vignette lays its own floor (the pool's sunken floor): the slot's carpet box is put away.</summary>
    public bool hideShellFloor;
    /// <summary>The slot's two baseboards are put away (tile rooms).</summary>
    public bool hideBaseboards;
    /// <summary>FrontRoomsLook.ReflectionZone by name for the title camera in this room (a pool zone exists only with the pool look).</summary>
    public string reflectionZone = "Level0";
    /// <summary>The furniture that matters for shadows (room space, plan footprints 0.25-2.45 m tall): row 4 picks the lamps over
    /// it. A family riding the slot's L03 hands it to the stream (ApplyShadowRoles); one with lamps of its own used it at build.</summary>
    public Bounds[] occluders = new Bounds[0];
    /// <summary>Row 4's pick among the slot's twelve L03 for a family riding them (made once by the stream).</summary>
    public int[] slotShadowPicks;
    /// <summary>Components on only while the room can be seen (the pool's water and its mirror cost).</summary>
    public Behaviour[] seenOnly = new Behaviour[0];
    /// <summary>The stream sequence it stands in, -1 while it waits in the bank.</summary>
    public int inSequence = -1;
    public int Count => lights.Length;
}

/// <summary>
/// The builders of the title's family rooms (FrontRoomsTitleVignette), each a vignette of a family designed and landed elsewhere,
/// never a new design of it:
///   Office    O-C (Red 10-31 "办公室选择O-C"): twelve lensed-indirect pendants on the map's cell centres (x -3, 0, +3; z 1.5 + 3k,
///             the map's 0.3 m troffer offset), the Office finish, the Office kit's planned cubicle farm (four pods of four
///             stations round its 1.6 m chase lane on the axis, dark CRTs, W2 chairs; OfficeDress), outlets, the Office grade and cube.
///   Run       Run! B armed (R-B strips with the red tube, Red 11-04 06:22 "加，用更亮档"): a 2.84 m corridor on the axis (faces x +-1.42,
///             the map's width; Red 10-31: a corridor stays a corridor), FrontRoomsRunDress over a 4-cell layout (VCT and lane,
///             lower tone, cove, bumper rails, sprinkler main, tray, EMT, the fire gear by the entrance, one extinguisher cabinet,
///             the ELECTRICAL ROOM side door, the janitor cart), a strip every cell with its soffit halo, all four shadowed,
///             the T-A unit dark; the strips strike from the entrance toward the far door one cell at a time; no EXIT sign (C41).
///   Waiting   a B-floor night post (BS4; walls "这个版本可以" 10-22): the Post Room finish (eggshell, 6 in crash rail, VCT with its
///             dark border), the slot's twelve L03, two tandem rows facing across the lane (T-B slate), the magazine rack, the
///             coat stand, the smoking stand, the guard's counter with his jacket on the chair, a wall clock, a dark wall TV.
///   Pool      a pool-level room (bathroom_rooms D1, the pool look's PL_Chain as built in W/proj_unlock 11-04: floor -0.15, ankle
///             water -0.03, the band at floor + 1.2 .. 1.5, tile everywhere, "瓷白"): dry tiled sills at both doors (the walnut
///             pair never stands in water, C11), the slot's L03 with one in three dead. Its materials, grade, zone look and water
///             come from the pool look when it is in the project (soft references: nothing here needs its code to compile);
///             without it, main's stand-ins (FrontRoomsPoolMaterials).
///   Low       a Low stock room (Red 10-31 "低矮区按推荐"): a 2.4 m soffit stepping up to the door head 1.2 m from each end wall,
///             eight 4 ft wraps on it, steel-and-ply storage along the walls.
///   DarkPool  the pool room with every overhead dead and four wet niches glowing under the water (P2-A is still Red's pick;
///             these are stand-in niches, a lit face in the tile, no kit).
/// Not built: the club (no locker or bench kits exist: it waits on the pool look's club dress), the stairwell (a 3 m safe zone),
/// the Tall and pool halls (5.4 m: RoomHeight is a const).
/// Shadows (技术美术调研 row 4, FrontRoomsRoomStream.FamilyShadows): each family casts only from the lamps over its furniture
/// (PickShadowLamps), as tight cones; Run! keeps its rig's strips. Every lamp gets URP's per-light data at creation with its
/// soft-shadow filter set explicitly: the map's FrontRoomsMapWorld.ShippedShadowFilter (row 16, option D: Low), so the filter
/// every lamp ships with lives in one place.
/// </summary>
public static class FrontRoomsTitleVignettes
{
    const float W = FrontRoomsRoomStream.RoomWidth, L = FrontRoomsRoomStream.RoomLength, H = FrontRoomsRoomStream.RoomHeight;
    /// <summary>The slot's side walls' inner faces (0.26 m walls on x +-5.75).</summary>
    public const float SideFace = 5.62f;
    /// <summary>The end walls' faces (the 0.04 m door walls on z 0 and z 12).</summary>
    public const float EndFace = .02f;
    /// <summary>The door opening and its casing, half width (the walnut kit: 1.2 m + lining 0.019 + reveal 0.012 + face 0.038).</summary>
    public const float DoorCasingHalf = 1.27f;
    /// <summary>The door's head casing top (2.66 m + 0.069).</summary>
    public const float DoorCasingTop = 2.73f;
    const float LampDrop = .06f;        // FrontRooms.Map.ModuleUnits.LampDrop
    const float CellOffsetZ = .3f;      // FrontRooms.Map.ModuleUnits.TrofferOffsetZ
    const float Cell = 3f;

    // ---------------------------------------------------------------------------------------------------------- entry points
    /// <summary>True when the family can be built in this project (its kits and finishes are present).</summary>
    public static bool Available(FrontRoomsRoomStream.TitleFamily family)
    {
        switch (family)
        {
            case FrontRoomsRoomStream.TitleFamily.Office: return FrontRoomsLampKit.Available(FrontRoomsLampKit.Family.Pendant);
            case FrontRoomsRoomStream.TitleFamily.Run: return FrontRoomsLampKit.Available(FrontRoomsLampKit.Family.Strip) && FrontRoomsSurfaces.TryGet("RunB_Floor") != null;
            case FrontRoomsRoomStream.TitleFamily.Waiting: return FrontRoomsSurfaces.TryGet("Station_Wall") != null && FrontRoomsKitLibrary.Has("Kit_TandemBeam3");
            case FrontRoomsRoomStream.TitleFamily.Low: return FrontRoomsLampKit.Available(FrontRoomsLampKit.Family.Wrap);
            case FrontRoomsRoomStream.TitleFamily.Pool:
            case FrontRoomsRoomStream.TitleFamily.DarkPool: return true;
            default: return false;
        }
    }

    /// <summary>Builds the family's vignette, inactive, under <paramref name="bank"/>. Null for Level 0 and for a family this
    /// project cannot build (Available).</summary>
    public static FrontRoomsTitleVignette Build(FrontRoomsRoomStream.TitleFamily family, Transform bank, int seed)
    {
        if (!Available(family)) return null;
        switch (family)
        {
            case FrontRoomsRoomStream.TitleFamily.Office: return Office(bank, seed);
            case FrontRoomsRoomStream.TitleFamily.Run: return Run(bank);
            case FrontRoomsRoomStream.TitleFamily.Waiting: return Waiting(bank);
            case FrontRoomsRoomStream.TitleFamily.Pool: return Pool(bank, false);
            case FrontRoomsRoomStream.TitleFamily.DarkPool: return Pool(bank, true);
            case FrontRoomsRoomStream.TitleFamily.Low: return Low(bank);
            default: return null;
        }
    }

    static FrontRoomsTitleVignette Begin(FrontRoomsRoomStream.TitleFamily family, Transform bank, string label)
    {
        var go = new GameObject("Title room / " + label);
        go.SetActive(false);
        go.transform.SetParent(bank, false);
        return new FrontRoomsTitleVignette { family = family, root = go };
    }

    static bool Phone => FrontRoomsTrofferKit.PhoneTier;

    // ---------------------------------------------------------------------------------------------------------- Office (O-C)
    static FrontRoomsTitleVignette Office(Transform bank, int seed)
    {
        var v = Begin(FrontRoomsRoomStream.TitleFamily.Office, bank, "Office O-C");
        var t = v.root.transform;
        v.wall = FrontRoomsSurfaces.Room(RoomRule.Office, FrontRoomsSurfaces.Slot.Wall);
        v.floor = FrontRoomsSurfaces.Room(RoomRule.Office, FrontRoomsSurfaces.Slot.Floor);
        v.ceiling = FrontRoomsSurfaces.Room(RoomRule.Office, FrontRoomsSurfaces.Slot.Ceiling);
        v.ownLamps = true;
        v.secondDrive = FrontRoomsTitleVignette.SecondDrive.LensBlock;
        // The map's Office lamp (.90, .96, 1) at 6; O-C's lens as seen: the zone lens tint at PendantLensPeak (LampKit.LensDrive).
        v.lampColor = new Color(.90f, .96f, 1f);
        v.lensColor = Normalised(new Color(.90f, .96f, 1f));
        v.lensPeak = FrontRoomsLampKit.PendantLensPeak;
        v.deadChance = .03f;
        v.unstableChance = .06f;
        v.reflectionZone = "Office";
        // Twelve pendants on the map's cell centres, the centre column first; which of them cast is row 4's pick (below).
        var columns = new[] { 0f, -Cell, Cell };
        var count = columns.Length * 4;
        Alloc(v, count, true, false);
        var phone = Phone;
        for (var c = 0; c < columns.Length; c++)
        for (var r = 0; r < 4; r++)
        {
            var i = c * 4 + r;
            var frame = new GameObject("office pendant " + i).transform;
            frame.SetParent(t, false);
            frame.localPosition = new Vector3(columns[c], H, Cell * .5f + Cell * r + CellOffsetZ);
            MeshRenderer glow;
            v.lens[i] = FrontRoomsLampKit.Build(FrontRoomsLampKit.Family.Pendant, frame, Vector3.zero, Quaternion.identity, "office pendant lens " + i, phone, out glow);
            v.second[i] = glow;
            // "fluorescent light N": the sound line's fixture hum finds it by name (lensed-indirect T8 tubes); the stream keys
            // its own troffers by their lens names, so the name changes nothing else here.
            v.lights[i] = DownLight(frame, "fluorescent light " + i, LampDrop, 162f, 96f, v.lampColor, 6f, 10f, false);
            v.baseIntensity[i] = 6f;
        }
        // Outlets on the Office faces (the stream's planner, the Office class), then the bullpen: the Office kit's own planned
        // cubicle farm (FrontRoomsOfficeKit.Dress) with its chase lane on the corridor's axis (OfficeDress).
        var outlets = new GameObject("office outlets").transform;
        outlets.SetParent(t, false);
        FrontRoomsWallFixtures.InstallLocal(outlets, FrontRoomsRoomStream.StreamFaces(RoomRule.Office), seed,
            FrontRoomsWallFixtures.StreamClass(RoomRule.Office), v.wall);
        var dress = OfficeDress(t, OfficeDressSeed);
        // Dark CRTs (the O-C reference's monitors are dark; the T6 set is the title's only screen): the desk screens the kit
        // attaches go before they ever wake (the vignette is built inactive), so each monitor shows the kit's own dark face.
        var screens = dress.GetComponentsInChildren<FrontRoomsScreenVideo>(true);
        for (var i = 0; i < screens.Length; i++) DropComponent(screens[i]);
        // Row 4: the pendants over the pods and desks cast, tight; the rest light unshadowed at 162 deg.
        v.occluders = Occluders(t, dress);
        PickOwnShadows(v, 162f, 96f, 10f);
        Grade(t, "Office", new Vector3(0f, H * .5f, L * .5f), new Vector3(W - .3f, H, L - .3f), 1f);
        return v;
    }

    /// <summary>The Office dress's seed: the kit's planner with this seed fills the module with four pods round the chase lane
    /// and keeps its wall units on the end walls (chosen from seeds 0-399 by FrontRoomsTitleStreamSmoke.OfficeSeeds, 11-04).</summary>
    public const int OfficeDressSeed = 285;

    /// <summary>The title Office's bullpen: the Office kit's planned cubicle farm (FrontRoomsOfficeKit.Dress, the map's own
    /// planner and pods) on the room's floor, its 1.6 m chase lane centred on the corridor's axis (the planner centres its pod
    /// grid, so the lane between the two pod columns falls on x 0; a 0.5 m keepClear strip holds it), clear approaches at both
    /// doors (the leaves swing 1.2 m into the room), no columns (the stream module has none). Public for the seed search.</summary>
    public static Transform OfficeDress(Transform room, int seed)
    {
        var dress = new GameObject("office dress").transform;
        dress.SetParent(room, false);
        var area = new Rect(-SideFace, .35f, SideFace * 2f, L - .7f);
        var keep = new[]
        {
            // The lane strip is 0.5 m: the planner reserves 0.25 m round it on its 0.25 m cells, so a wider strip takes the cell
            // under the pods' inner edge (x +-0.80) and every pod is refused (seed search, 11-04: 1 m strips gave 2 pods at best).
            new Rect(-.25f, .35f, .5f, L - .7f),
            new Rect(-DoorCasingHalf - .03f, .35f, (DoorCasingHalf + .03f) * 2f, 1.2f),
            new Rect(-DoorCasingHalf - .03f, L - .35f - 1.2f, (DoorCasingHalf + .03f) * 2f, 1.2f),
        };
        FrontRoomsOfficeKit.Dress(dress, area, H, seed, keep, new Rect[0]);
        return dress;
    }

    // ---------------------------------------------------------------------------------------------------------- Run! B, armed
    /// <summary>The corridor's half width (the map's Run! corridor: faces at x +-1.42, clear 2.84 m).</summary>
    public const float RunHalf = 1.42f;

    static FrontRoomsTitleVignette Run(Transform bank)
    {
        var v = Begin(FrontRoomsRoomStream.TitleFamily.Run, bank, "Run B armed");
        var t = v.root.transform;
        v.ownLamps = true;
        v.secondDrive = FrontRoomsTitleVignette.SecondDrive.RedTube;
        // The rig's armed strips: wrapColor (.90, .95, 1) at wrapIntensity 5.5, range 10, outer 172 / inner 124; the bare tubes'
        // lens at stripLensEmission (.90, .95, 1) x 2.6; the red tube's coating at redTubeBounce x their level.
        v.lampColor = new Color(.90f, .95f, 1f);
        v.lensColor = new Color(.90f, .95f, 1f);
        v.lensPeak = 2.6f;
        v.secondColor = new Color(.40f, .045f, .05f);
        v.deadChance = 0f;
        v.unstableChance = 0f;
        v.chase = true;
        v.reflectionZone = "Level0";
        // The end walls inside the corridor are the stream's door reveals (beside the walnut casing, 0.15 m a side, and the
        // 0.17 m over its head): they wear the upper tone while the vignette stands in the slot (RefreshRoomMaterials).
        v.wall = FrontRoomsSurfaces.TryGet("RunB_BlockUpper");
        // The corridor's structure: two block walls on the axis's sides (colliders, both faces), the end walls are the stream's.
        var block = FrontRoomsSurfaces.TryGet("RunB_BlockUpper") ?? FrontRoomsSurfaces.Room(RoomRule.Run, FrontRoomsSurfaces.Slot.Wall);
        var sideZ0 = EndFace;
        var sideZ1 = L - EndFace;
        for (var s = -1; s <= 1; s += 2)
            Slab(t, s < 0 ? "run b corridor block left" : "run b corridor block right", new Vector3(s * (RunHalf + .08f), H * .5f, (sideZ0 + sideZ1) * .5f),
                new Vector3(.16f, H, sideZ1 - sideZ0), block, true, ShadowCastingMode.On);
        // The dress over a 4-cell layout: the map's rules for the finishes, the fire gear and the services, skinned on the
        // corridor's faces (2 mm, 5 mm clear of the stream's door reveals), the VCT 1 mm over the slot's floor.
        var l = new RunRigLayout
        {
            name = "TitleRun4",
            halfWidth = RunHalf, length = L, height = H,
            skinShell = true, buildShell = false,
            floorX0 = -RunHalf, floorX1 = RunHalf, floorZ0 = 0f, floorZ1 = L,
            laneHalf = .45f, accentRowsX = new[] { -.6f, .6f },
            // The services ride 0.18 / 0.06 m higher than the map's (main 2.62, tray 2.70): over the walnut pair's head casing
            // (2.73 m), where the map's corridor has its 2.2 m arch.
            mainX = .55f, mainY = 2.80f, trayX = -.60f, trayY = 2.76f, emtWallX = -RunHalf, runZ0 = EndFace, runZ1 = L - EndFace,
            goalPoint = new Vector3(0f, 1.05f, L), goalHalf = FrontRoomsDoubleDoorKit.Width * .5f, goalTop = FrontRoomsDoubleDoorKit.HeadBottom,
        };
        const float faceInset = .005f;
        // The ELECTRICAL ROOM door 2 m into cell 1 on the right (Corridor's rule); the cabinet on the left at z 7.2.
        const float doorZ = 5f, cabinetZ = 7.2f;
        const float a0 = EndFace + faceInset;
        var left = new RunWallFace { start = new Vector3(-RunHalf, 0f, a0), along = Vector3.forward, length = L - 2f * a0, normal = Vector3.right, gaps = new[] { new Vector2(cabinetZ - .16f - a0, cabinetZ + .16f - a0) } };
        var right = new RunWallFace { start = new Vector3(RunHalf, 0f, a0), along = Vector3.forward, length = L - 2f * a0, normal = Vector3.left, gaps = new[] { new Vector2(doorZ - .545f - a0, doorZ + .545f - a0) } };
        // Only the long faces: an end face would grow an arch reveal through the walnut frame (RunDress.Reveal); the end walls'
        // upper tone is the reveals' (above), their lower tone and cove the four strips below.
        l.walls = new[] { left, right };
        l.sideDoors = new[] { new RunSideDoor { z = doorZ, side = 1f, face = "electrical" } };
        l.wallItems = new[]
        {
            new RunWallItem { kit = "Kit_PullStation", point = new Vector3(RunHalf, 1.2f - .09f, 1f), normal = Vector3.left },
            new RunWallItem { kit = "Kit_FireBell", point = new Vector3(RunHalf, 2.3f, 1f), normal = Vector3.left },
            new RunWallItem { kit = "Kit_ExtinguisherCabinet", point = new Vector3(-RunHalf, 1.1f - .305f, cabinetZ), normal = Vector3.right },
            new RunWallItem { kit = "Kit_AlarmZoneCard", point = new Vector3(-RunHalf, 1.55f, .9f), normal = Vector3.right },
        };
        // One obstacle, in a pool of light (darkness is never cover): the janitor cart against the left wall between strips 2 and 3.
        l.obstacles = new[] { new RunObstacle { kit = "Kit_JanitorCart", position = new Vector3(-RunHalf + .37f, 0f, 9.3f), yaw = 90f } };
        FrontRoomsRunDress.Build(t, l);
        var lower = FrontRoomsSurfaces.TryGet("RunB_BlockLower");
        var cove = FrontRoomsSurfaces.TryGet("RunB_Cove");
        var strip = RunHalf - DoorCasingHalf;
        for (var e = 0; e < 2; e++)
        for (var s = -1; s <= 1; s += 2)
        {
            var z = e == 0 ? EndFace + .006f : L - EndFace - .006f;
            var x = s * (DoorCasingHalf + strip * .5f);
            if (lower != null) Slab(t, "run b end lower tone " + e + (s < 0 ? " left" : " right"), new Vector3(x, .6f, z), new Vector3(strip, 1.2f, .002f), lower, false, ShadowCastingMode.Off);
            if (cove != null) Slab(t, "run b end cove " + e + (s < 0 ? " left" : " right"), new Vector3(x, .051f, z + (e == 0 ? .0015f : -.0015f)), new Vector3(strip, .102f, .003f), cove, false, ShadowCastingMode.Off);
        }
        // The T-A unit by the entrance, dark while armed (its heads come with the trip, which the title never plays).
        WallKit(t, "Kit_EmergencyTwinHead", new Vector3(RunHalf, 2.45f - .1f, 3f), Vector3.left, "run b twin head unit");
        // A strip every cell (z 1.8 + 3k), its channel on the soffit, its long axis along the corridor; each Light shadowed (F7).
        var count = 4;
        Alloc(v, count, true, true);
        var phone = Phone || FrontRoomsRunRig.WebGLTier;
        var haloMaterial = FrontRoomsRunRig.CreateHaloMaterial(new Color(.45f, .47f, .50f));
        for (var k = 0; k < count; k++)
        {
            var frame = new GameObject("run b strip " + k).transform;
            frame.SetParent(t, false);
            frame.localPosition = new Vector3(0f, H, 1.8f + Cell * k);
            MeshRenderer red;
            v.lens[k] = FrontRoomsLampKit.Build(FrontRoomsLampKit.Family.Strip, frame, Vector3.zero, Quaternion.identity, "run b strip tubes " + k, phone, out red);
            v.second[k] = red;
            v.lights[k] = DownLight(frame, "run b wrap light " + k, .092f, 172f, 124f, v.lampColor, 5.5f, 10f, true);
            v.baseIntensity[k] = 5.5f;
            v.halo[k] = FrontRoomsRunRig.BuildHalo(frame, new Vector3(0f, -.003f, 0f), haloMaterial);
        }
        v.shadowCount = count;
        Grade(t, "Run_B", new Vector3(0f, H * .5f, L * .5f), new Vector3(RunHalf * 2f + .16f, H, L - .3f), 1f);
        return v;
    }

    // ---------------------------------------------------------------------------------------------------------- the waiting room
    static FrontRoomsTitleVignette Waiting(Transform bank)
    {
        var v = Begin(FrontRoomsRoomStream.TitleFamily.Waiting, bank, "Waiting night post");
        var t = v.root.transform;
        // Post Room: eggshell walls, VCT with its dark border, the Level 0 ceiling and its twelve L03 in the Level 0 colour.
        v.wall = FrontRoomsSurfaces.TryGet("Station_Wall");
        v.floor = FrontRoomsSurfaces.TryGet("Station_Floor");
        v.ceiling = null;
        v.hideBaseboards = true;
        v.reflectionZone = "Level0";
        v.slotIntensity = 5.2f;
        v.deadChance = .04f;
        v.unstableChance = .10f;
        var rail = FrontRoomsSurfaces.TryGet("Station_CrashRail");
        var border = FrontRoomsSurfaces.TryGet("Station_FloorBorder");
        var cove = FrontRoomsSurfaces.TryGet("Station_Base") ?? FrontRoomsSurfaces.CoveBase;
        const float railBottom = .78f, railTop = .93f, railProud = .025f, borderWidth = .30f;
        var zMid = L * .5f;
        var zLen = L - 2f * EndFace;
        for (var s = -1; s <= 1; s += 2)
        {
            var x = s * SideFace;
            // the 6 in crash rail along both long walls; the cove base; the floor's dark border 1 mm over the VCT
            if (rail != null) Slab(t, s < 0 ? "post crash rail left" : "post crash rail right", new Vector3(x - s * railProud * .5f, (railBottom + railTop) * .5f, zMid),
                new Vector3(railProud, railTop - railBottom, zLen - .02f), rail, false, ShadowCastingMode.On);
            Slab(t, s < 0 ? "post cove left" : "post cove right", new Vector3(x - s * .004f, .05f, zMid), new Vector3(.008f, .1f, zLen), cove, false, ShadowCastingMode.Off);
            if (border != null) Slab(t, s < 0 ? "post vct border left" : "post vct border right", new Vector3(x - s * borderWidth * .5f, .0005f, zMid),
                new Vector3(borderWidth, .001f, zLen), border, false, ShadowCastingMode.Off);
        }
        if (border != null)
            for (var e = 0; e < 2; e++)
            {
                var z = e == 0 ? EndFace + borderWidth * .5f : L - EndFace - borderWidth * .5f;
                var span = SideFace - DoorCasingHalf - borderWidth;
                for (var s = -1; s <= 1; s += 2)
                    Slab(t, "post vct border end " + e + (s < 0 ? " left" : " right"), new Vector3(s * (DoorCasingHalf + span * .5f), .0005f, z), new Vector3(span, .001f, borderWidth), border, false, ShadowCastingMode.Off);
            }
        // Two tandem rows facing across the lane (T-B slate: Kit_TandemBeam3, Red's pick still open), front edges 1.2 m off the axis.
        const float rowX = 1.55f;
        FloorKit(t, "Kit_TandemBeam3", new Vector3(-(rowX + .30f), 0f, 4.05f), 90f, "post tandem left a");
        FloorKit(t, "Kit_TandemBeam3", new Vector3(-(rowX + .30f), 0f, 6.12f), 90f, "post tandem left b");
        FloorKit(t, "Kit_TandemBeam3", new Vector3(rowX + .30f, 0f, 4.05f), 270f, "post tandem right a");
        FloorKit(t, "Kit_TandemBeam2Table", new Vector3(rowX + .30f, 0f, 6.12f), 270f, "post tandem right b");
        FloorKit(t, "Kit_SmokingStand", new Vector3(-(rowX + .30f), 0f, 7.55f), 0f, "post smoking stand");
        FloorKit(t, "Kit_TrashBin", new Vector3(-(rowX + .40f), 0f, 2.65f), 0f, "post bin");
        // The coat stand by the entrance, the magazine rack and the notice board on the left wall, the water cooler at the far end.
        FloorKit(t, "Kit_HatStand", new Vector3(-(SideFace - .45f), 0f, 1.05f), 0f, "post coat stand");
        WallKit(t, "Kit_MagazineRack", new Vector3(-SideFace, 0f, 2.45f), Vector3.right, "post magazine rack");
        WallKit(t, "Kit_NoticeBoard", new Vector3(-SideFace, 0f, 5.1f), Vector3.right, "post notice board");
        FloorKit(t, "Kit_WaterCooler", new Vector3(-(SideFace - .215f), 0f, 9.4f), 90f, "post water cooler");
        // The guard's counter in the far right corner, along the wall, his jacket on the chair behind it; the radio and the book.
        const float counterX = 4.3f;
        FloorKit(t, "Kit_StationCounter_End", new Vector3(counterX, 0f, 8.2875f), 270f, "post counter end a");
        FloorKit(t, "Kit_StationCounter_Run075", new Vector3(counterX, 0f, 8.65f), 270f, "post counter run a");
        FloorKit(t, "Kit_StationCounter_Run120", new Vector3(counterX, 0f, 9.625f), 270f, "post counter run b");
        FloorKit(t, "Kit_StationCounter_End", new Vector3(counterX, 0f, 10.2125f), 270f, "post counter end b");
        FloorKit(t, "Kit_RegisterBook", new Vector3(counterX - .15f, 1.07f, 9.575f), 270f, "post register");
        FloorKit(t, "Kit_TentCard", new Vector3(counterX - .15f, 1.07f, 8.925f), 270f, "post tent card");
        FloorKit(t, "Kit_DeskBlotter", new Vector3(counterX + .02f, .74f, 9.325f), 90f, "post blotter");
        FloorKit(t, "Kit_CoffeeCup", new Vector3(counterX + .18f, .7505f, 9.125f), 90f, "post coffee cup");
        FloorKit(t, "Kit_DeskPhone_Corded", new Vector3(counterX + .10f, .74f, 8.645f), 110f, "post desk phone");
        FloorKit(t, "Kit_RadioCharger", new Vector3(counterX + .12f, .74f, 8.405f), 90f, "post radio");
        FloorKit(t, "Kit_TaskChair_Charcoal", new Vector3(counterX + .72f, 0f, 9.35f), 245f, "post guard chair");
        FloorKit(t, "Kit_JacketDraped", new Vector3(counterX + .72f, 0f, 9.35f), 245f, "post guard jacket");
        // The wall clock over the rows; the wall TV high on the right wall, dark (the T6 set is the title's only screen).
        WallKit(t, "Kit_WallClock", new Vector3(SideFace, 2.2f, 6.1f), Vector3.left, "post clock");
        WallTv(t, new Vector3(SideFace, 0f, 2.7f), Vector3.left);
        // Row 4: the slot's L03 over the rows and the counter cast, tight (the stream picks them: ApplyShadowRoles).
        v.occluders = Occluders(t, t);
        return v;
    }

    /// <summary>The station's wall TV (Kit_TVWallBracket + Kit_CRTTV_Station on its set_mount, 20 deg down), dark.</summary>
    static void WallTv(Transform parent, Vector3 wallPoint, Vector3 normal)
    {
        var bracketInfo = FrontRoomsKitLibrary.GetInfo("Kit_TVWallBracket");
        var setInfo = FrontRoomsKitLibrary.GetInfo("Kit_CRTTV_Station");
        if (bracketInfo == null || setInfo == null || !bracketInfo.TryAnchor("set_mount", out var mount)) return;
        var holder = new GameObject("post wall tv").transform;
        holder.SetParent(parent, false);
        holder.localPosition = wallPoint + normal * .001f;
        holder.localRotation = Quaternion.LookRotation(normal, Vector3.up);
        FrontRoomsKitLibrary.Spawn("Kit_TVWallBracket", holder, Vector3.zero, 0f, null, false, "post tv bracket", false);
        var pitch = Quaternion.Euler(20f, 0f, 0f);
        FrontRoomsKitLibrary.Spawn("Kit_CRTTV_Station", holder, mount - pitch * new Vector3(0f, setInfo.Min.y, setInfo.Min.z), pitch, null, false, "post tv set", false);
    }

    // ---------------------------------------------------------------------------------------------------------- the pool level
    /// <summary>The pool look's PL_Chain values (W/proj_unlock FrontRoomsPoolLook.Spaces, 11-04): floor, water, band.</summary>
    public const float PoolFloor = -.15f, PoolWater = -.03f, PoolBand0 = 1.2f, PoolBand1 = 1.5f;
    /// <summary>The dry tiled sill in front of each door (the walnut pair never stands in water, C11).</summary>
    public const float PoolSill = .9f;

    static FrontRoomsTitleVignette Pool(Transform bank, bool dark)
    {
        var v = Begin(dark ? FrontRoomsRoomStream.TitleFamily.DarkPool : FrontRoomsRoomStream.TitleFamily.Pool, bank, dark ? "Pool dark" : "Pool level");
        var t = v.root.transform;
        var field = PoolTile("Field", FrontRoomsPoolMaterials.Tile);
        var course = PoolTile("Course", FrontRoomsPoolMaterials.Band);
        var mosaic = PoolTile("Mosaic", FrontRoomsPoolMaterials.Tile);
        var deck = PoolTile("Deck", FrontRoomsPoolMaterials.Tile);
        v.wall = field;
        v.ceiling = field;
        v.floor = null;
        v.hideShellFloor = true;
        v.hideBaseboards = true;
        // The level's troffers (the map's pool pass: (.86, .96, 1)); one in three dead (PL_OfficeCeiling's rule). Dark: every one dead.
        v.lampColor = new Color(.86f, .96f, 1f);
        v.lensColor = Normalised(new Color(.86f, .96f, 1f));
        v.lensPeak = FrontRoomsTrofferKit.LensPeak;
        v.slotIntensity = 5.2f;
        v.deadChance = dark ? 1f : .34f;
        v.unstableChance = dark ? 0f : .06f;
        v.reflectionZone = dark ? "PoolDead" : "PoolLevel";
        // The sunken floor (mosaic) between the dry sills; the sills (deck tile) at the doors, full width; their risers.
        var basinZ0 = PoolSill;
        var basinZ1 = L - PoolSill;
        Slab(t, "pool basin mosaic", new Vector3(0f, PoolFloor - .1f, (basinZ0 + basinZ1) * .5f), new Vector3(SideFace * 2f, .2f, basinZ1 - basinZ0), mosaic, true, ShadowCastingMode.On);
        for (var e = 0; e < 2; e++)
        {
            var z0 = e == 0 ? -.02f : basinZ1;
            var z1 = e == 0 ? basinZ0 : L + .02f;
            Slab(t, e == 0 ? "pool sill entry" : "pool sill far", new Vector3(0f, -.12f, (z0 + z1) * .5f), new Vector3(SideFace * 2f, .24f, z1 - z0), deck, true, ShadowCastingMode.On);
        }
        // Tile from the sunken floor up to the slot's walls (their paper starts at 0), along both long walls over the basin.
        for (var s = -1; s <= 1; s += 2)
            Slab(t, s < 0 ? "pool skirt left" : "pool skirt right", new Vector3(s * (SideFace - .004f), PoolFloor * .5f, (basinZ0 + basinZ1) * .5f),
                new Vector3(.008f, -PoolFloor + .002f, basinZ1 - basinZ0), field, false, ShadowCastingMode.Off);
        // The band (the level's one accent): floor + 1.2 .. 1.5 on every face, 2 mm proud, round the doors' casings.
        var b0 = PoolFloor + PoolBand0;
        var b1 = PoolFloor + PoolBand1;
        for (var s = -1; s <= 1; s += 2)
            Slab(t, s < 0 ? "pool band left" : "pool band right", new Vector3(s * (SideFace - .001f), (b0 + b1) * .5f, L * .5f), new Vector3(.002f, b1 - b0, L - 2f * EndFace), course, false, ShadowCastingMode.Off);
        for (var e = 0; e < 2; e++)
        {
            var z = e == 0 ? EndFace + .006f : L - EndFace - .006f;
            var span = SideFace - DoorCasingHalf;
            for (var s = -1; s <= 1; s += 2)
                Slab(t, "pool band end " + e + (s < 0 ? " left" : " right"), new Vector3(s * (DoorCasingHalf + span * .5f), (b0 + b1) * .5f, z), new Vector3(span, b1 - b0, .002f), course, false, ShadowCastingMode.Off);
        }
        // The ankle water over the basin, from sill to sill.
        var water = Water(t, new Rect(-SideFace, basinZ0, SideFace * 2f, basinZ1 - basinZ0), PoolWater, H - PoolWater);
        if (water != null) v.seenOnly = new[] { water };
        // The dark pool: four wet niches in the long walls under the water, the room's only light (P2-A is still Red's pick: a lit
        // face in the tile, no kit), struck as the room is revealed.
        if (dark)
        {
            v.ownLamps = true;
            v.keepSlotFixtures = true;
            v.lampColor = new Color(.62f, .92f, 1f);
            v.lensColor = new Color(.62f, .92f, 1f);
            v.lensPeak = 3.2f;
            v.deadChance = 0f;
            v.unstableChance = 0f;
            Alloc(v, 4, false, false);
            var face = FrontRoomsPoolMaterials.Emissive("title niche face", new Color(.62f, .92f, 1f));
            for (var k = 0; k < 4; k++)
            {
                var s = k % 2 == 0 ? -1f : 1f;
                var z = k < 2 ? 3.6f : 8.4f;
                var frame = new GameObject("pool niche " + k).transform;
                frame.SetParent(t, false);
                frame.localPosition = new Vector3(s * (SideFace - .003f), PoolFloor + .07f, z);
                frame.localRotation = Quaternion.LookRotation(new Vector3(-s, 0f, 0f), Vector3.up);
                var lensGo = Slab(frame, "pool niche lens " + k, Vector3.zero, new Vector3(.30f, .07f, .004f), face, false, ShadowCastingMode.Off);
                v.lens[k] = lensGo.GetComponent<Renderer>();
                var go = new GameObject("pool niche light " + k);
                go.transform.SetParent(frame, false);
                go.transform.localPosition = new Vector3(0f, .02f, .12f);
                var light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = v.lampColor;
                light.range = 4.5f;
                light.intensity = 1.6f;
                light.shadows = LightShadows.None;
                FrontRoomsMapWorld.ShippedShadowFilter(light);
                v.lights[k] = light;
                v.baseIntensity[k] = 1.6f;
            }
            v.shadowCount = 0;
        }
        var zone = dark ? "PoolDark" : "PoolLevel";
        var box = new Vector3(SideFace * 2f, H - PoolFloor, L - .3f);
        var centre = new Vector3(0f, (H + PoolFloor) * .5f, L * .5f);
        Grade(t, zone, centre, box, 1f);
        ZoneLook(t, zone, centre, box, 1f, PoolWater);
        return v;
    }

    // ---------------------------------------------------------------------------------------------------------- the Low stock room
    /// <summary>The Low soffit (the map's Low class) and how far it stays off each end wall (under it the door head would show).</summary>
    public const float LowSoffit = 2.4f, LowStep = 1.2f;

    static FrontRoomsTitleVignette Low(Transform bank)
    {
        var v = Begin(FrontRoomsRoomStream.TitleFamily.Low, bank, "Low stock room");
        var t = v.root.transform;
        v.ownLamps = true;
        v.secondDrive = FrontRoomsTitleVignette.SecondDrive.None;
        v.lampColor = new Color(1f, .96f, .88f);
        v.lensColor = Normalised(new Color(1f, .96f, .88f));
        v.lensPeak = FrontRoomsLampKit.WrapLensPeak;
        v.deadChance = .03f;
        v.unstableChance = .12f;
        v.reflectionZone = "Level0";
        var tile = FrontRoomsSurfaces.Room(RoomRule.Lobby, FrontRoomsSurfaces.Slot.Ceiling);
        var paper = FrontRoomsSurfaces.Room(RoomRule.Lobby, FrontRoomsSurfaces.Slot.Wall);
        // The dropped soffit (the 2'x2' print, world-projected, so it lines up with the slot's ceiling) and its two bulkhead faces.
        var z0 = LowStep;
        var z1 = L - LowStep;
        Slab(t, "low soffit", new Vector3(0f, (LowSoffit + H) * .5f, (z0 + z1) * .5f), new Vector3(SideFace * 2f + .02f, H - LowSoffit, z1 - z0), tile, true, ShadowCastingMode.On);
        Slab(t, "low bulkhead entry", new Vector3(0f, (LowSoffit + H) * .5f, z0 - .001f), new Vector3(SideFace * 2f, H - LowSoffit, .002f), paper, false, ShadowCastingMode.Off);
        Slab(t, "low bulkhead far", new Vector3(0f, (LowSoffit + H) * .5f, z1 + .001f), new Vector3(SideFace * 2f, H - LowSoffit, .002f), paper, false, ShadowCastingMode.Off);
        // Eight 4 ft wraps on the soffit, two columns off the lane; column +x first (lamps 0..3 cast).
        var count = 8;
        Alloc(v, count, false, false);
        var phone = Phone;
        for (var c = 0; c < 2; c++)
        for (var r = 0; r < 4; r++)
        {
            var i = c * 4 + r;
            var frame = new GameObject("low wrap " + i).transform;
            frame.SetParent(t, false);
            frame.localPosition = new Vector3(c == 0 ? 2.1f : -2.1f, LowSoffit, 2.4f + 2.4f * r);
            v.lens[i] = FrontRoomsLampKit.Build(FrontRoomsLampKit.Family.Wrap, frame, Vector3.zero, Quaternion.identity, "low wrap lens " + i, phone);
            v.lights[i] = DownLight(frame, "fluorescent light " + i, .09f, 162f, 96f, v.lampColor, 5f, 9f, false);
            v.baseIntensity[i] = 5f;
        }
        // Storage along both walls, the lane clear (Low_Storage_2x3's kits).
        WallFloorKit(t, "Kit_Bookcase", -1f, 2.6f, "low bookcase");
        WallFloorKit(t, "Kit_PlyCabinet", -1f, 5.0f, "low ply cabinet");
        WallFloorKit(t, "Kit_RollingCabinet", -1f, 6.2f, "low rolling cabinet");
        WallFloorKit(t, "Kit_Bookcase", -1f, 8.9f, "low bookcase b");
        FloorKit(t, "Kit_Pallet", new Vector3(4.55f, 0f, 2.9f), 0f, "low pallet");
        FloorKit(t, "Kit_Crate", new Vector3(4.6f, .144f, 2.9f), 12f, "low crate");
        FloorKit(t, "Kit_HandTruck_Cartons", new Vector3(3.6f, 0f, 6.4f), 200f, "low hand truck");
        FloorKit(t, "Kit_StepStool", new Vector3(4.95f, 0f, 8.3f), 75f, "low step stool");
        FloorKit(t, "Kit_Crate", new Vector3(4.7f, 0f, 9.8f), -6f, "low crate b");
        // Row 4: the wraps over the shelving and the stock cast, tight; the rest light unshadowed.
        v.occluders = Occluders(t, t);
        PickOwnShadows(v, 162f, 96f, 9f);
        return v;
    }

    // ---------------------------------------------------------------------------------------------------------- helpers
    static void Alloc(FrontRoomsTitleVignette v, int n, bool second, bool halo)
    {
        v.lights = new Light[n];
        v.baseIntensity = new float[n];
        v.lens = new Renderer[n];
        v.second = second ? new Renderer[n] : new Renderer[0];
        v.halo = halo ? new Renderer[n] : new Renderer[0];
    }

    static Color Normalised(Color c)
    {
        var m = Mathf.Max(1e-4f, c.maxColorComponent);
        return new Color(c.r / m, c.g / m, c.b / m, 1f);
    }

    /// <summary>A downward spot under a lamp (the map's and the stream's: shadows soft on the casters).</summary>
    static Light DownLight(Transform frame, string name, float drop, float outer, float inner, Color color, float intensity, float range, bool shadows)
    {
        var go = new GameObject(name);
        go.transform.SetParent(frame, false);
        go.transform.localPosition = new Vector3(0f, -drop, 0f);
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var light = go.AddComponent<Light>();
        light.type = LightType.Spot;
        light.spotAngle = outer;
        light.innerSpotAngle = inner;
        light.color = color;
        light.range = range;
        light.intensity = intensity;
        light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
        light.shadowStrength = .92f;
        light.shadowNearPlane = .1f;
        FrontRoomsMapWorld.ShippedShadowFilter(light);
        return light;
    }

    // The soft-shadow filter of every vignette lamp (技术美术调研 row 16, option D; Red 2026-11-04 14:14 in that chat, "按推荐"):
    // FrontRoomsMapWorld.ShippedShadowFilter at creation (DownLight, the pool niches), the map's one place for it: URP's per-light
    // data with Low set, biases left on the pipeline's settings, so the Editor, the clones and every player draw what ships.

    // ---------------------------------------------------------------------------------------------------------- row 4 shadows
    /// <summary>How far round the point under a lamp its furniture counts (metres, plan).</summary>
    const float ShadowReach = 2.2f;
    /// <summary>Plan area (m2, weighted by height) under a lamp below which it is not worth a shadow map.</summary>
    const float MinShadowScore = .2f;

    /// <summary>The furniture under <paramref name="from"/> as room-space plan boxes: shadow-casting LOD0 meshes 0.25-2.45 m tall
    /// and smaller than 6 m across (no shell piece, no lamp, no rail or border strip). From the meshes, not Renderer.bounds: the
    /// vignette is built inactive, where a renderer's bounds are empty.</summary>
    static Bounds[] Occluders(Transform roomRoot, Transform from)
    {
        var list = new System.Collections.Generic.List<Bounds>(64);
        var filters = from.GetComponentsInChildren<MeshFilter>(true);
        var origin = roomRoot.position;
        for (var i = 0; i < filters.Length; i++)
        {
            var f = filters[i];
            var mesh = f != null ? f.sharedMesh : null;
            var r = f != null ? f.GetComponent<MeshRenderer>() : null;
            if (mesh == null || r == null || r.shadowCastingMode == ShadowCastingMode.Off) continue;
            var n = f.gameObject.name;
            if (n.IndexOf("_LOD", StringComparison.Ordinal) >= 0 && n.IndexOf("_LOD0", StringComparison.Ordinal) < 0) continue;
            var m = f.transform.localToWorldMatrix;
            var lb = mesh.bounds;
            var b = new Bounds(m.MultiplyPoint3x4(lb.center), Vector3.zero);
            for (var c = 0; c < 8; c++)
                b.Encapsulate(m.MultiplyPoint3x4(lb.center + Vector3.Scale(lb.extents, new Vector3((c & 1) == 0 ? -1f : 1f, (c & 2) == 0 ? -1f : 1f, (c & 4) == 0 ? -1f : 1f))));
            if (b.size.y < .25f || b.max.y - origin.y > 2.45f || b.size.x > 6f || b.size.z > 6f) continue;
            list.Add(new Bounds(b.center - origin, b.size));
        }
        return list.ToArray();
    }

    /// <summary>Row 4: the lamps (room-space points) over the furniture that matters, best first, at most <paramref name="max"/>.
    /// A lamp's score is the plan area of furniture within ShadowReach of the point under it, weighted by height (a desk
    /// counts more than a bin); lamps under MinShadowScore are left out.</summary>
    public static int[] PickShadowLamps(Vector3[] lamps, Bounds[] occluders, int max)
    {
        if (lamps == null || occluders == null || max <= 0) return new int[0];
        var score = new float[lamps.Length];
        for (var i = 0; i < lamps.Length; i++)
        {
            if (lamps[i].y < 0f) { score[i] = -1f; continue; }
            float x0 = lamps[i].x - ShadowReach, x1 = lamps[i].x + ShadowReach, z0 = lamps[i].z - ShadowReach, z1 = lamps[i].z + ShadowReach;
            for (var k = 0; k < occluders.Length; k++)
            {
                var b = occluders[k];
                var dx = Mathf.Min(b.max.x, x1) - Mathf.Max(b.min.x, x0);
                var dz = Mathf.Min(b.max.z, z1) - Mathf.Max(b.min.z, z0);
                if (dx <= 0f || dz <= 0f) continue;
                score[i] += dx * dz * Mathf.Clamp01(b.size.y / 1.2f);
            }
        }
        var picks = new System.Collections.Generic.List<int>(max);
        while (picks.Count < max)
        {
            var best = -1;
            for (var i = 0; i < score.Length; i++)
                if (score[i] >= MinShadowScore && !picks.Contains(i) && (best < 0 || score[i] > score[best])) best = i;
            if (best < 0) break;
            picks.Add(best);
        }
        return picks.ToArray();
    }

    /// <summary>A lamp's row-4 role: shadowed as a tight cone, or unshadowed at the family's wide cone and range.</summary>
    public static void SetShadowRole(Light light, bool tight, float wideOuter, float wideInner, float wideRange)
    {
        if (light == null) return;
        light.shadows = tight ? LightShadows.Soft : LightShadows.None;
        light.spotAngle = tight ? FrontRoomsRoomStream.TightShadowCone : wideOuter;
        light.innerSpotAngle = tight ? FrontRoomsRoomStream.TightShadowInner : wideInner;
        light.range = tight ? FrontRoomsRoomStream.TightShadowRange : wideRange;
    }

    /// <summary>Row 4 for a family's own lamps: pick over its occluders, set every lamp's role, count the shadowed.</summary>
    static void PickOwnShadows(FrontRoomsTitleVignette v, float wideOuter, float wideInner, float wideRange)
    {
        var at = new Vector3[v.lights.Length];
        for (var i = 0; i < at.Length; i++)
            at[i] = v.lights[i] != null ? v.lights[i].transform.position - v.root.transform.position : new Vector3(0f, -100f, 0f);
        var picks = FrontRoomsRoomStream.FamilyShadows ? PickShadowLamps(at, v.occluders, FrontRoomsRoomStream.FamilyShadowLamps) : new[] { 0, 1, 2, 3 };
        v.shadowCount = 0;
        for (var i = 0; i < v.lights.Length; i++)
        {
            var tight = Array.IndexOf(picks, i) >= 0;
            if (FrontRoomsRoomStream.FamilyShadows) SetShadowRole(v.lights[i], tight, wideOuter, wideInner, wideRange);
            else if (v.lights[i] != null) v.lights[i].shadows = tight ? LightShadows.Soft : LightShadows.None;
            if (tight) v.shadowCount++;
        }
    }

    static GameObject Slab(Transform parent, string name, Vector3 centre, Vector3 size, Material material, bool collider, ShadowCastingMode shadows)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = centre;
        go.AddComponent<MeshFilter>().sharedMesh = FrontRoomsFilmMesh.GetPlanarBox(size);
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = shadows;
        if (collider) go.AddComponent<BoxCollider>().size = size;
        return go;
    }

    /// <summary>Removes a component of the inactive vignette at once: a deferred Destroy would let it wake first when the
    /// vignette is attached in the same frame (Initialize), and a desk screen would leave its picture quad behind with its
    /// material gone (magenta, 11-04 captures/office_farm).</summary>
    static void DropComponent(Component c)
    {
        if (c != null) UnityEngine.Object.DestroyImmediate(c);
    }

    static GameObject FloorKit(Transform parent, string kit, Vector3 position, float yaw, string label)
    {
        return FrontRoomsKitLibrary.Spawn(kit, parent, position, yaw, null, true, label, false);
    }

    /// <summary>A wall-mounted kit: its back (local z 0) on the wall point, its front along the wall's inward normal.</summary>
    static GameObject WallKit(Transform parent, string kit, Vector3 wallPoint, Vector3 normal, string label)
    {
        return FrontRoomsKitLibrary.Spawn(kit, parent, wallPoint + normal * .001f, Quaternion.LookRotation(normal, Vector3.up), null, false, label, false);
    }

    /// <summary>A floor kit with its back to a long wall (side -1 left, +1 right) at z, its front toward the lane.</summary>
    static GameObject WallFloorKit(Transform parent, string kit, float side, float z, string label)
    {
        var info = FrontRoomsKitLibrary.GetInfo(kit);
        var back = info != null ? -info.Min.z : .25f;
        var x = side * (SideFace - back - .01f);
        return FloorKit(parent, kit, new Vector3(x, 0f, z), side < 0f ? 90f : 270f, label);
    }

    /// <summary>A zone's grade (Resources/Rendering/FrontRoomsPost_&lt;zone&gt;) over the room, blending in at its doors. Null, and
    /// nothing built, when the project has no such profile (a pool zone in a project without the pool look).</summary>
    static void Grade(Transform parent, string zone, Vector3 centre, Vector3 size, float blend)
    {
        FrontRoomsPostStack.EnsureZoneVolume(parent, zone, new Bounds(centre, size), blend);
    }

    // ---------------------------------------------------------------------------------------------------------- the pool look, soft
    // The pool look (the game visual line's pool look run) landed in main on 2026-11-04 (pool_zone_look, pool_tile, pool_water).
    // Everything it brings is still reached by name, so this file compiles and runs if that code moves or is absent: its tile
    // (Resources/Surfaces/Pool/Prop_PoolTile_<kind>[_Phone], world-planar), its zone look (FrontRoomsZoneLookVolume.Ensure), its
    // water (FrontRooms/Water + FrontRoomsWaterSurface). Direct calls are a follow-up (one hunk, game visual).
    static Material PoolTile(string kind, Material fallback)
    {
        Material m = null;
        if (Phone) m = Resources.Load<Material>("Surfaces/Pool/Prop_PoolTile_" + kind + "_Phone");
        if (m == null) m = Resources.Load<Material>("Surfaces/Pool/Prop_PoolTile_" + kind);
        return m != null ? m : fallback;
    }

    /// <summary>True when the pool look's tile is in the project (else the vignette wears main's stand-in tile).</summary>
    public static bool PoolLookPresent => Resources.Load<Material>("Surfaces/Pool/Prop_PoolTile_Field") != null;

    static void ZoneLook(Transform parent, string zone, Vector3 centre, Vector3 size, float blend, float waterY)
    {
        var type = FindType("FrontRoomsZoneLookVolume");
        if (type == null) return;
        var ensure = type.GetMethod("Ensure", BindingFlags.Public | BindingFlags.Static, null,
            new[] { typeof(Transform), typeof(string), typeof(Bounds), typeof(float), typeof(float) }, null);
        if (ensure == null) return;
        try { ensure.Invoke(null, new object[] { parent, zone, new Bounds(centre, size), blend, waterY }); }
        catch (Exception e) { Debug.LogWarning("[FrontRooms3D] title pool: the pool look's zone look refused " + zone + " (" + e.GetBaseException().Message + ")"); }
    }

    /// <summary>The water over a rectangle (room space) at y: the pool look's FrontRooms/Water and FrontRoomsWaterSurface (an ankle
    /// body) when present, else main's stand-in water. Returns the component the stream switches with the room's visibility.</summary>
    static Behaviour Water(Transform parent, Rect area, float y, float roomHeight)
    {
        var go = new GameObject("pool water");
        go.SetActive(false);
        go.transform.SetParent(parent, false);
        var mesh = new Mesh { name = "Title pool water", hideFlags = HideFlags.DontSave };
        mesh.vertices = new[]
        {
            new Vector3(area.xMin, y, area.yMin), new Vector3(area.xMin, y, area.yMax),
            new Vector3(area.xMax, y, area.yMax), new Vector3(area.xMax, y, area.yMin)
        };
        mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
        mesh.uv = new[] { new Vector2(area.xMin, area.yMin), new Vector2(area.xMin, area.yMax), new Vector2(area.xMax, area.yMax), new Vector2(area.xMax, area.yMin) };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateBounds();
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        Behaviour surface = null;
        var shader = Shader.Find("FrontRooms/Water");
        var type = FindType("FrontRoomsWaterSurface");
        if (shader != null && shader.isSupported && type != null)
        {
            r.sharedMaterial = new Material(shader) { name = "Title pool water (FrontRooms/Water)", renderQueue = (int)RenderQueue.Transparent, hideFlags = HideFlags.DontSave };
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            surface = go.AddComponent(type) as Behaviour;
            SetField(surface, "roomHeight", roomHeight);
            var kind = type.GetNestedType("Kind");
            if (kind != null) SetField(surface, "kind", Enum.ToObject(kind, 1));     // Ankle
            if (surface != null) surface.enabled = false;
        }
        else
        {
            r.sharedMaterial = FrontRoomsPoolMaterials.WaterOf(FrontRoomsPoolMaterials.Water.Level);
            // The stand-in has nothing to switch: the renderer itself goes with the room.
            surface = null;
        }
        go.SetActive(true);
        return surface;
    }

    /// <summary>A type of the game's own assembly by name (the pool look's code lives there too), or null.</summary>
    static Type FindType(string name)
    {
        return Type.GetType(name + ", Assembly-CSharp");
    }

    static void SetField(object target, string name, object value)
    {
        if (target == null) return;
        var f = target.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
        if (f == null) return;
        try { f.SetValue(target, value); } catch (Exception) { }
    }
}
