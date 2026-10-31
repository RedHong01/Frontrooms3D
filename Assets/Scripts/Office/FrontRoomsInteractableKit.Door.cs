using System;
using FrontRooms.Map;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>
/// The door part of the interactables kit facade (D1.6, Documentation/research/interactables/d16/10_design.md §3-§5;
/// 10_spec.md §2, §6). Owned by the visual session.
///
/// Call site (map contract M1, d16/data/contract_map_D16.diff): FrontRoomsMapWorld.BuildEdge, door branch, right after
/// the door is registered: <c>door.rig = FrontRoomsInteractableKit.DressDoor(this, door, chunk.root.transform);</c> in a
/// try/catch that logs. Null keeps the map's cube leaf visible (tools switch, a missing kit, an input missing).
///
/// Mode (A12 / A19, Red 2026-10-22): every map door is DOUBLE-ACTING. The pivot stays on the wall centre plane at the
/// map's hinge (no A2 offset, no handing mirror); S is the door root's +X face (FrontRoomsMapWorld.FaceOf, geometric).
/// No closers, no self-centring: the leaf rests where the map leaves it. Handles kept: a lever on free doors; knob,
/// deadbolt and keyed cylinder on locked doors, the keyhole at Y 1.000 on both faces. The single-acting stop door is a
/// dormant mode (Acting = Single: the stop kits, A2 rig offset, mirror) that D1.6 never selects.
///
/// What it builds (all FrontRoomsKitLibrary.Spawn(colliders: false), render-only: no collider, no light):
///   "Door frame (kit)"  under the chunk root at the hinge's local pose (the door root);
///   "Leaf rig"          (FrontRoomsDoorRig) under "Door hinge {a}-{b}", identity in DA;
///   "Leaf (kit)", "Leaf shadow" (shadows-only cube), the lock parts of both faces, signs and number plates under the rig;
///   "Lock strike"       under the frame.
/// The "Door leaf" cube keeps its collider, name and transform; only its MeshRenderer is disabled.
/// All or nothing: if anything throws, what this call built is destroyed, the cube renderer is restored, and the
/// exception goes back to the map's guard.
/// </summary>
public static partial class FrontRoomsInteractableKit
{
    public const string DoorFrameLabel = "Door frame (kit)", LeafRigLabel = "Leaf rig", LeafLabel = "Leaf (kit)", LeafShadowLabel = "Leaf shadow";

    /// <summary>How map doors are hung. Double (A12/A19) for D1.6; Single is the dormant stop-frame mode.</summary>
    public static DoorActing Acting = DoorActing.Double;

    /// <summary>Tools and tests: DressDoor builds nothing and returns null (the map's cube leaf).</summary>
    public static bool DisableDoorDressForTools { get; set; }

    /// <summary>
    /// Edit mode (the map's own edit-mode suites, the Level Designer preview): doors and keys stay the map's cubes unless
    /// this is set (tools, captures, the kit's own tests). Play Mode always dresses. So the map's edit-mode tests see the
    /// map exactly as before D1.6, and the game, the autopilot and the touch playtests see the kit.
    /// </summary>
    public static bool DressInEditMode { get; set; }

    /// <summary>
    /// WebGL (a separate track, its own budgets: draw-bound at about 3,200 draws): the dressed doors and keys stay OFF in
    /// WebGL players until the WebGL track measures them (T10 in Play: +9 to +117 draws per view on desktop at lodBias 1,
    /// research/interactables/d16/20_build.md). Doors and keys stay the map's cubes there; the lock rule, the unlock shot
    /// and every gameplay read are unchanged (the rig is render-only). The WebGL track flips this to true when it wants
    /// them. Desktop, iOS and the Editor ignore it.
    /// </summary>
    public static bool DressOnWebGL = false;

    /// <summary>
    /// Handheld (FrontRoomsHandheld.Active: iPhone, iPad, the Duo): the dressed doors and keys are ON (10_design 10 keeps them
    /// dressed on mobile). A one-constant fallback for the handheld / settings line if a device measurement asks for it
    /// (verify r2 E4: +200 to +330 draws at a close LOCKED door on desktop, not yet measured on a device); false keeps the
    /// map's cubes there, as DressOnWebGL does on WebGL.
    /// </summary>
    public static bool DressOnHandheld = true;

#if UNITY_WEBGL && !UNITY_EDITOR
    static bool DressAllowed => DressOnWebGL && (Application.isPlaying || DressInEditMode);
#else
    static bool DressAllowed => (Application.isPlaying || DressInEditMode) && (DressOnHandheld || !FrontRoomsHandheld.Active);
#endif

    /// <summary>Tools and the Level Designer: force a member for a door (null = the theme rule).</summary>
    public static Func<FrontRoomsMapWorld.Door, FrontRoomsDoorMember?> DoorMemberOverride { get; set; }

    /// <summary>Contract M8: the map's own word on a door's member (Run, Exit); null = the theme rule. Nothing sets it until the map
    /// line lands M8, so until then no map door is a Run door and the F5 lite shows only through DoorMemberOverride.</summary>
    public static Func<FrontRoomsMapWorld.Door, FrontRoomsDoorMember?> DoorMemberFromMap { get; set; }

    /// <summary>Tools: force the LOCKED (true) or FREE (false) model for a door (null = the map's LockedByRule).</summary>
    public static Func<FrontRoomsMapWorld.Door, bool?> DoorLockedOverride { get; set; }

    /// <summary>
    /// The kit's printed type: the floor token on key tags and door number plates (Prop_KeyTagNo, Courier Prime, hud_build
    /// DECISIONS #23 / #33) and the engraved door signs (Prop_SignEngraved, the info line's two-ply plate rules). HELD
    /// (false) until the graphic-visual line (info design; prints and presentations) OKs the proofs in
    /// research/interactables/d16/images/d16_type_proof_*.jpg (memory rule "type goes through the graphic-visual line"):
    /// while held, tags and plates show blank paper (atlas cell FrontRoomsKeyIdentity.BlankCell) and no door sign is hung.
    /// Flip to true once they sign off; nothing else changes. Tools and captures may set it.
    /// </summary>
    public static bool ShowKitType = ShowKitTypeShipped;

    /// <summary>The shipped values of ShowKitType and ShowTagType (tools and captures may change the statics).</summary>
    public const bool ShowKitTypeShipped = false, ShowTagTypeShipped = false;

    /// <summary>
    /// The key TAG's printed token (the floor token the HUD key glyph prints at the KP-1 hand-off: 00, B1, ...; Courier
    /// Prime Regular as the glyph, DECISIONS #23, Red-confirmed 10-22, which supersedes G12's Bold). HELD (false) until
    /// the graphic-visual line (info design) signs off the 3D key-tag type on the proof sheet
    /// research/interactables/d16/images/d16x_tag_type_proof.jpg (fix 3, verify r2 spec S2; the type rule "type goes
    /// through the graphic-visual line"): while held, the tag shows blank paper and the token appears only on the HUD glyph
    /// at the swap. Every tag shape takes its token as set, B10 on at 0.8x on the same baseline inside the atlas cell
    /// (DECISIONS #33: never scaled to fit the insert, so the round tag's B10+ is not shrunk into its window either).
    /// Flip ShowTagTypeShipped to true once they sign off; nothing else changes. Tools and captures may set the static.
    /// </summary>
    public static bool ShowTagType = ShowTagTypeShipped;

    /// <summary>The door and key hardware's chrome is satin chrome (US26D, the 1990 commercial lockset finish): the kits'
    /// Prop_Chrome (polished, smoothness 0.85) reads black in a map with no reflection source near the hardware, so every
    /// dressed door and key part swaps it for this surface at spawn (D1.6 fix 1, VL323).</summary>
    public const string HardwareChromeFrom = "Prop_Chrome", HardwareChromeTo = "Prop_ChromeSatin";

    /// <summary>Fix 2 (verify r2 look 2): the steel leaves' kick and push plates are ".050 satin stainless" (US32D, the kit
    /// source), but the kits carry the mirror Prop_Aluminium (smoothness 1), which renders black at the foot of every
    /// LOCKED door; every door part but the frame (the leaves' plates, the deadbolt's face) swaps that slot for
    /// Prop_StainlessSatin (brushed: metallic 0.45, smoothness 0.45; a metal-1 plate low on a door reflects only the
    /// carpet, -3 stops under the leaf even at smoothness 0.2). Main's Prop_Aluminium and the Steel_Alu frame keep theirs.</summary>
    public const string LeafPlateFrom = "Prop_Aluminium", LeafPlateTo = "Prop_StainlessSatin";

    /// <summary>Fix 2 (verify r2 look 1): the key is a worn satin brass (smoothness 0.45, Prop_BrassSatin), not the
    /// kits' Prop_Brass (0.6), which read near black held flat to the lens with the room's reflection behind the player.
    /// Only the key kit swaps it; door brass keeps Prop_Brass.</summary>
    public const string KeyBrassFrom = "Prop_Brass", KeyBrassTo = "Prop_BrassSatin";

    /// <summary>Door wood (Red 2026-10-30: option 4, plain-sliced walnut, 15 % lighter; research/door_wood/40_for_red.md).
    /// The wood frame kits' casing and linings carry the furniture walnut (Prop_WoodWalnut, also on chairs, cases and
    /// counters), so the frame swaps that slot at spawn for the door frame stock cut to match the leaf (Door_Frame, grain
    /// along V as the kit's UVs; Tools/lookdev/gen_door_wood.py). The oak saddle keeps Prop_WoodOak. A surface that is
    /// missing leaves the slot as it is.</summary>
    public const string WoodFrameKit = "Kit_DoorFrame_Wood", WoodFrameFrom = "Prop_WoodWalnut", WoodFrameTo = "Door_Frame";

    /// <summary>Door wood: the Office and Exit free leaves (OF-F, EX-F) are the walnut veneer leaf too (they were
    /// Kit_DoorLeaf_Veneer_Oak_DA, the furniture oak with its grain turned onto U, which repeated 2.08 times up the
    /// leaf). That kit's pivot plates and faceplate are brass; on these chrome-hardware rows they take the satin chrome.</summary>
    public const string LeafBrassFrom = "Prop_Brass";

    /// <summary>The member by the door's Standard (non-Low) sides: Office if one of them is an Office zone, else Lobby.
    /// Run and Exit come from a later map hook (contract M8, DoorMemberFor).</summary>
    public static FrontRoomsDoorMember DoorMemberFor(FrontRoomsMapWorld map, FrontRoomsMapWorld.Door door)
    {
        if (map == null || map.Cache == null || door == null) return FrontRoomsDoorMember.Lobby;
        var fromMap = DoorMemberFromMap != null ? DoorMemberFromMap(door) : null;
        if (fromMap.HasValue) return fromMap.Value;
        var za = map.ZoneOf(door.a);
        var zb = map.ZoneOf(door.b);
        var office = (za.height != ZoneHeight.Low && za.theme == ZoneTheme.Office) || (zb.height != ZoneHeight.Low && zb.theme == ZoneTheme.Office);
        if (!office && za.height == ZoneHeight.Low && zb.height == ZoneHeight.Low) office = za.theme == ZoneTheme.Office || zb.theme == ZoneTheme.Office;
        return office ? FrontRoomsDoorMember.Office : FrontRoomsDoorMember.Lobby;
    }

    /// <summary>The parts row of a member and model (10_design §3.2): data, so a variant is a row.</summary>
    public static DoorRow DoorRowFor(FrontRoomsDoorMember member, bool locked) => DoorParts.Row(member, locked);

    /// <summary>
    /// Dress one map door (D1.6). Returns the leaf rig (the map stores it as door.rig), or null when nothing was built.
    /// </summary>
    public static FrontRoomsDoorRig DressDoor(FrontRoomsMapWorld map, FrontRoomsMapWorld.Door door, Transform chunkRoot)
    {
        if (DisableDoorDressForTools || !DressAllowed || map == null || door == null || door.hinge == null || door.leaf == null || chunkRoot == null) return null;
        // The EXIT door (C22/C23) has the map's own leaf rig, crossbar and sign, and the stairwell production dresses it
        // on ExitDoorBuilt (research/stairwell/10_directions.md 7): never a second "Leaf rig" here.
        if (map.IsExitDoor(door)) return null;
        var member = DoorMemberOverride != null ? DoorMemberOverride(door) ?? DoorMemberFor(map, door) : DoorMemberFor(map, door);
        var locked = DoorLockedOverride != null ? DoorLockedOverride(door) ?? map.LockedByRule(door) : map.LockedByRule(door);
        var row = DoorParts.Row(member, locked);
        var acting = Acting;
        if (!DoorParts.Available(row, acting)) return null;
        GameObject frame = null, rigGo = null;
        var cube = door.leaf.GetComponent<MeshRenderer>();
        var hid = false;
        try
        {
            var sSign = acting == DoorActing.Double ? 1f : -door.swing;
            var frameScale = acting == DoorActing.Double ? Vector3.one : new Vector3(sSign, 1f, 1f);
            frame = DoorParts.Spawn(DoorParts.Kit(row.frame, acting), chunkRoot, door.hinge.localPosition, door.closed, frameScale, DoorFrameLabel);
            rigGo = new GameObject(LeafRigLabel);
            rigGo.transform.SetParent(door.hinge, false);
            var rig = rigGo.AddComponent<FrontRoomsDoorRig>();
            rig.Bind(map, door, acting, member, locked, frame.transform);
            DoorParts.Build(map, door, row, acting, rig, frame.transform, cube);
            rig.SetVisualPivot(DoorParts.LeafPivotZ(row, acting));
            // A door a key has opened keeps the LOCKED model with the deadbolt drawn (C15). IsUnlockedForTools is main's
            // public accessor (the contract's M7 alias was dropped in fix 1: nothing needs a second name for it).
            if (locked && map.IsUnlockedForTools(door)) rig.SetDeadboltRest(1f);
            if (door.broken) rig.DetachStrike(door.position + DoorParts.RootRight(door) * door.swing, true);
            if (cube != null && cube.enabled) { cube.enabled = false; hid = true; }
            rig.CollectLods();
            rig.Tick(0f);
            FrontRoomsDoorDressEvents.Bind(map);
            HookFloorToken();
            // Door break (production P1, research/door_break/50_rethink.md 3.9): a door the Relay damaged or broke comes
            // back as it was (its record: the stage, the motif, the throw, the floor pack). Render-only; a failure here
            // never costs the door its dressing.
            try { FrontRoomsDoorBreakVisual.RestoreOnDress(rig); }
            catch (Exception e) { Debug.LogWarning("[FrontRooms] door-break restore failed on " + door.a + "-" + door.b + ": " + e.Message); }
            return rig;
        }
        catch
        {
            if (hid && cube != null) cube.enabled = true;
            DoorParts.Kill(rigGo);
            DoorParts.Kill(frame);
            throw;
        }
    }

    /// <summary>
    /// Dress a door again (the lock rule changed under it, FrontRoomsDoorRigDriver's audit; or a tool): take down its kit
    /// frame and leaf rig, show the cube for a moment, and dress it afresh, so the model matches map.LockedByRule now.
    /// The map's door.rig is set to the new rig (or null with the cube shown). Returns the new rig.
    /// </summary>
    public static FrontRoomsDoorRig RedressDoor(FrontRoomsMapWorld map, FrontRoomsMapWorld.Door door)
    {
        if (map == null || door == null || door.hinge == null) return null;
        var old = door.rig as FrontRoomsDoorRig;
        Transform chunkRoot = null;
        if (old != null)
        {
            chunkRoot = old.Frame != null ? old.Frame.parent : null;
            if (old.Frame != null) DoorParts.Kill(old.Frame.gameObject);
            DoorParts.Kill(old.gameObject);
        }
        if (chunkRoot == null) chunkRoot = door.hinge.parent;
        var cube = door.leaf != null ? door.leaf.GetComponent<MeshRenderer>() : null;
        if (cube != null) cube.enabled = true;
        door.rig = null;
        var rig = DressDoor(map, door, chunkRoot);
        door.rig = rig;
        return rig;
    }

    static bool floorHooked;

    // Live tags and plates follow the floor token (FrontRoomsKeyIdentity.FloorTokenChanged: NextFloor builds chunks
    // before the game raises FloorChanged).
    static void HookFloorToken()
    {
        if (floorHooked) return;
        floorHooked = true;
        FrontRoomsKeyIdentity.FloorTokenChanged += OnFloorToken;
    }

    static void OnFloorToken(int floor) => RefreshPrintedTokens(floor);

    /// <summary>Re-set the printed token of every live number plate and key tag to a floor's (FloorTokenChanged; tools).</summary>
    public static void RefreshPrintedTokens(int floor)
    {
        var rigs = FrontRoomsDoorRig.All;
        for (var i = 0; i < rigs.Count; i++)
        {
            var r = rigs[i];
            if (r == null) continue;
            for (var side = 0; side < 2; side++)
            {
                var plate = r.PlateOf(side);
                if (plate == null) continue;
                var id = r.PlateIdOf(side);
                id.floor = floor;
                r.SetPlateId(side, id);
                DoorParts.SetAtlasCell(plate, DoorParts.KeyTagNoSlot, DoorParts.KeyTagNoFallback, 10, 10, TypeCell(id));
            }
        }
        var keys = FrontRoomsKeyAssembly.All;
        for (var i = 0; i < keys.Count; i++)
        {
            var k = keys[i];
            if (k == null || k.TagPart == null) continue;
            var id = k.Identity;
            id.floor = floor;
            k.SetIdentity(id);
            SetTagToken(k.TagPart.gameObject, id);
        }
    }

    static readonly System.Collections.Generic.List<FrontRoomsDoorRig> auditScratch = new System.Collections.Generic.List<FrontRoomsDoorRig>();

    /// <summary>
    /// The lock-rule audit (verify r1 E3a; FrontRoomsDoorRigDriver calls it a few times a second in Play Mode): a door
    /// whose rule changed under it (DoorsNeedKeysForTools flips mid-run: the autopilot's door phase, "Play with keys on",
    /// the touch playtest driver) is dressed again, so the LOCKED / FREE model always matches map.LockedByRule (or the
    /// tools' override). Never during a door shot (FrontRoomsDoorShot.Weight > 0). Returns how many were re-dressed.
    /// </summary>
    public static int AuditLockModels()
    {
        if (FrontRoomsDoorShot.Weight > 0f) return 0;
        auditScratch.Clear();
        var rigs = FrontRoomsDoorRig.All;
        for (var i = 0; i < rigs.Count; i++)
        {
            var r = rigs[i];
            if (r == null || r.Door == null || r.Map == null) continue;
            var want = DoorLockedOverride != null ? DoorLockedOverride(r.Door) ?? r.Map.LockedByRule(r.Door) : r.Map.LockedByRule(r.Door);
            if (want != r.LockedModel) auditScratch.Add(r);
        }
        var n = 0;
        for (var i = 0; i < auditScratch.Count; i++)
        {
            var r = auditScratch[i];
            if (r == null || r.Door == null || r.Door.rig != (IFrontRoomsDoorRig)r) continue;
            RedressDoor(r.Map, r.Door);
            n++;
        }
        auditScratch.Clear();
        return n;
    }

    /// <summary>The atlas cell a tag or plate shows for an identity: its token, or blank paper while the type is held.</summary>
    public static int TypeCell(FrontRoomsKeyIdentity id) => ShowKitType ? id.Cell : FrontRoomsKeyIdentity.BlankCell;

    /// <summary>The atlas cell a key TAG shows: its token (ShowTagType), or blank paper.</summary>
    public static int TagCell(FrontRoomsKeyIdentity id) => ShowTagType ? id.Cell : FrontRoomsKeyIdentity.BlankCell;

    /// <summary>Print a key tag's token (DressKey, RefreshPrintedTokens): its cell as set, every shape alike (#33).</summary>
    internal static void SetTagToken(GameObject tag, FrontRoomsKeyIdentity id)
    {
        DoorParts.SetAtlasCell(tag, DoorParts.KeyTagNoSlot, DoorParts.KeyTagNoFallback, 10, 10, TagCell(id));
    }

    /// <summary>One parts row (10_design §3.2). Kit names are the DA names; the dormant Single mode drops "_DA".</summary>
    public sealed class DoorRow
    {
        public string code, frame, leaf;
        public bool locked;
        // Free (lever) set: rose + lever; locked (knob) set: escutcheon + cylinder shell + plug + knob, plus a deadbolt.
        public string rose, lever, escutcheon, shell, plug, knob, deadbolt, latch, strike;
        public bool boredStrike;
        // Signs: the engraved sign's atlas cell (-1 = none) and whether the number plates are hung.
        public int signCell = -1;
        public bool plates;
        // Door wood (Red 2026-10-30): the walnut veneer leaf's brass plates in satin chrome (OF-F, EX-F; LeafBrassFrom).
        public bool leafChrome;
    }

    static class DoorParts
    {
        // Leaf face anchors (10_design §2.2) and the escutcheon / rose offsets of today's lock kits (sidecars).
        static readonly Vector3 EscKeyhole = new Vector3(0f, .032f, .0095f), EscKnobPivot = new Vector3(0f, -.0315f, .002f), RoseSpindle = new Vector3(0f, 0f, .01f);
        static readonly Quaternion FaceS = Quaternion.Euler(0f, 90f, 0f), FaceP = Quaternion.Euler(0f, -90f, 0f), StrikeTurn = Quaternion.Euler(0f, 180f, 0f);
        static readonly Vector3 ShadowCentre = new Vector3(0f, 1.055f, .500f), ShadowSize = new Vector3(.10f, 2.08f, .96f);
        internal const string KeyTagNoSlot = "Prop_KeyTagNo", KeyTagNoFallback = "Prop_Paper", SignSlot = "Prop_SignEngraved";
        // The sign atlas's colourways (W/info_type/stair_signs/SPEC.md two-ply plate rules, atlas v2): cells 0, 1, 3 are
        // pale plates (black letters on a white cap), cell 2 a dark plate (white on black). The kit is modelled dark (cap
        // ply Prop_PlasticBlack, bevel core Prop_PlasticWhite); a pale cell swaps the two so the edges match the face.
        static bool PaleSign(int cell) => cell != 2;

        static DoorRow Free(string code, string frame, string leaf, bool brass) => new DoorRow
        {
            code = code, frame = frame, leaf = leaf, locked = false,
            rose = brass ? "Kit_Lock_Rose_Brass" : "Kit_Lock_Rose", lever = brass ? "Kit_Lock_Lever_Brass" : "Kit_Lock_Lever",
            latch = brass ? "Kit_Lock_Latchbolt_Bored_Brass_DA" : "Kit_Lock_Latchbolt_Bored_DA",
            strike = brass ? "Kit_Lock_StrikeBored_Brass_DA" : "Kit_Lock_StrikeBored_DA", boredStrike = true,
        };

        static DoorRow Locked(string code, string frame, string leaf, int signCell) => new DoorRow
        {
            code = code, frame = frame, leaf = leaf, locked = true,
            escutcheon = "Kit_Lock_Escutcheon", shell = "Kit_Lock_CylinderShell", plug = "Kit_Lock_Plug", knob = "Kit_Lock_Knob",
            deadbolt = "Kit_Lock_Deadbolt", latch = "Kit_Lock_Latchbolt_Mortise_DA", strike = "Kit_Lock_StrikeMortise_DA",
            signCell = signCell, plates = true,
        };

        static DoorRow ChromeLeaf(DoorRow row) { row.leafChrome = true; return row; }

        // [member * 2 + (locked ? 1 : 0)]: L0-F, L0-K, OF-F, OF-K, RN-F, RN-K, EX-F, EX-K (10_design §3.2; Q-EX: no crossbar, Q-RN: lever).
        // Door wood (Red 2026-10-30): OF-F and EX-F hang the walnut veneer leaf (was Kit_DoorLeaf_Veneer_Oak_DA) with chrome plates.
        static readonly DoorRow[] Rows =
        {
            Free("L0-F", "Kit_DoorFrame_Wood_DA", "Kit_DoorLeaf_Veneer_DA", true),
            Locked("L0-K", "Kit_DoorFrame_Steel_DA", "Kit_DoorLeaf_Steel_DA", 0),
            ChromeLeaf(Free("OF-F", "Kit_DoorFrame_Steel_DA", "Kit_DoorLeaf_Veneer_DA", false)),
            Locked("OF-K", "Kit_DoorFrame_Steel_DA", "Kit_DoorLeaf_Steel_DA", 0),
            Free("RN-F", "Kit_DoorFrame_Steel_DA", "Kit_DoorLeaf_Ward_DA", false),
            Locked("RN-K", "Kit_DoorFrame_Steel_DA", "Kit_DoorLeaf_SteelRun_DA", 1),   // door vision set A: the Run's F5 lite
            ChromeLeaf(Free("EX-F", "Kit_DoorFrame_Steel_Alu_DA", "Kit_DoorLeaf_Veneer_DA", false)),
            Locked("EX-K", "Kit_DoorFrame_Steel_Alu_DA", "Kit_DoorLeaf_Steel_DA", -1),
        };

        internal static DoorRow Row(FrontRoomsDoorMember member, bool locked) => Rows[(int)member * 2 + (locked ? 1 : 0)];

        // The dormant Single mode spawns today's stop kits (main's GUIDs): the same names without "_DA".
        internal static string Kit(string name, DoorActing acting) =>
            name == null || acting == DoorActing.Double || !name.EndsWith("_DA") ? name : name.Substring(0, name.Length - 3);

        internal static bool Available(DoorRow row, DoorActing acting)
        {
            foreach (var n in new[] { row.frame, row.leaf, row.rose, row.lever, row.escutcheon, row.shell, row.plug, row.knob, row.deadbolt, row.latch, row.strike })
                if (n != null && !FrontRoomsKitLibrary.Has(Kit(n, acting))) return false;
            return true;
        }

        internal static Vector3 RootRight(FrontRoomsMapWorld.Door door) =>
            (door.hinge != null && door.hinge.parent != null ? door.hinge.parent.rotation : Quaternion.identity) * door.closed * Vector3.right;

        internal static GameObject Spawn(string kit, Transform parent, Vector3 pos, Quaternion rot, Vector3 scale, string label)
        {
            var go = FrontRoomsKitLibrary.Spawn(kit, parent, pos, rot, scale, false, label, true);
            if (go == null) throw new InvalidOperationException("door kit missing: " + kit);
            // Render-only, whatever the FBX carries.
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Kill(c);
            Remap(go, label != DoorFrameLabel, false);
            if (kit.StartsWith(WoodFrameKit, StringComparison.Ordinal)) SwapSurface(go, WoodFrameFrom, WoodFrameTo);
            return go;
        }

        /// <summary>Every slot named <paramref name="from"/> on every renderer of a part (all LODs) takes the project surface
        /// <paramref name="to"/> (door wood); a missing surface leaves the slots as they are.</summary>
        internal static void SwapSurface(GameObject go, string from, string to)
        {
            var m = go != null ? FrontRoomsSurfaces.TryGet(to) : null;
            if (m == null) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                var changed = false;
                for (var i = 0; i < mats.Length; i++)
                    if (mats[i] != null && mats[i].name == from) { mats[i] = m; changed = true; }
                if (changed) r.sharedMaterials = mats;
            }
        }

        static Material satin, satinBrass, stainless;
        static bool satinLooked;

        /// <summary>Swap the kits' polished Prop_Chrome for the satin hardware chrome on every renderer of a part (all LODs).</summary>
        internal static void SatinHardware(GameObject go) => Remap(go, false, false);

        /// <summary>
        /// The kit surface swaps at spawn (all LODs): Prop_Chrome to Prop_ChromeSatin on every part; on every door part but
        /// the frame also Prop_Aluminium (the kick / push plates, the deadbolt face) to Prop_StainlessSatin; on the key kit
        /// Prop_Brass to Prop_BrassSatin. A surface that is missing leaves the slot as it is.
        /// </summary>
        internal static void Remap(GameObject go, bool leaf, bool key)
        {
            if (go == null) return;
            if (!satinLooked)
            {
                satinLooked = true;
                satin = FrontRoomsSurfaces.TryGet(HardwareChromeTo);
                satinBrass = FrontRoomsSurfaces.TryGet(KeyBrassTo);
                stainless = FrontRoomsSurfaces.TryGet(LeafPlateTo);
                if (stainless == null) stainless = satin;
            }
            if (satin == null && satinBrass == null && stainless == null) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                var changed = false;
                for (var i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    var n = mats[i].name;
                    var to = n == HardwareChromeFrom ? satin : leaf && n == LeafPlateFrom ? stainless : key && n == KeyBrassFrom ? satinBrass : null;
                    if (to != null) { mats[i] = to; changed = true; }
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        static Vector3 Anchor(FrontRoomsKitLibrary.Info info, string name, Vector3 fallback) =>
            info != null && info.TryAnchor(name, out var p) ? p : fallback;

        /// <summary>The leaf kit's visual pivot Z (its sidecar's pivot_axis anchor; 0 for kits authored about the map's hinge).</summary>
        internal static float LeafPivotZ(DoorRow row, DoorActing acting)
        {
            var info = FrontRoomsKitLibrary.GetInfo(Kit(row.leaf, acting));
            return info != null && info.TryAnchor("pivot_axis", out var p) ? p.z : 0f;
        }

        // Swap two slots' materials on every renderer of a part (the sign's pale colourway).
        static void SwapSlots(GameObject go, string a, string b)
        {
            var ma = FrontRoomsSurfaces.TryGet(a);
            var mb = FrontRoomsSurfaces.TryGet(b);
            if (ma == null || mb == null) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                var changed = false;
                for (var i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    if (mats[i].name == a) { mats[i] = mb; changed = true; }
                    else if (mats[i].name == b) { mats[i] = ma; changed = true; }
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        internal static void Build(FrontRoomsMapWorld map, FrontRoomsMapWorld.Door door, DoorRow row, DoorActing acting, FrontRoomsDoorRig rig, Transform frame, MeshRenderer cube)
        {
            var t = rig.transform;
            var leafKit = Kit(row.leaf, acting);
            var leafGo = Spawn(leafKit, t, Vector3.zero, Quaternion.identity, Vector3.one, LeafLabel);
            if (row.leafChrome) SwapSurface(leafGo, LeafBrassFrom, HardwareChromeTo);
            var leafInfo = FrontRoomsKitLibrary.GetInfo(leafKit);
            ShadowProxy(t, cube, leafInfo);
            // Door vision (research/door_vision/30_design.md 2.3, 4.2): the leaf's lite (LIVE / DARK, the player-side sight
            // rule) and the peek's Available on the EXAMINE target the kit's sidecar block attached at spawn.
            if (leafInfo != null && leafInfo.HasLite)
            {
                var lite = FrontRoomsDoorLite.AttachMap(leafGo, door, t, leafInfo.LiteRect, map);
                var peek = leafGo.GetComponent<FrontRoomsInspectable>();
                if (peek != null && lite != null) peek.Available = lite.PeekAvailableFunc;
            }

            // Face hardware, S (+X, side 0) then P (-X, side 1). Mirror rule: P parts take scale (-1, 1, 1) so a lever still
            // points at the hinge; the plug is never mirrored (G2 §4.1) and its angle is negated on P (§4.7).
            for (var side = 0; side < 2; side++)
            {
                var s = side == 0 ? "_s" : "_p";
                var rot = side == 0 ? FaceS : FaceP;
                var mirror = side == 0 ? Vector3.one : new Vector3(-1f, 1f, 1f);
                var sign = side == 0 ? 1f : -1f;
                var faceLabel = side == 0 ? " s" : " p";
                if (row.locked)
                {
                    var esc = Anchor(leafInfo, "escutcheon" + s, new Vector3(sign * .022f, .968f, .920f));
                    Spawn(row.escutcheon, t, esc, rot, mirror, "Lock escutcheon" + faceLabel);
                    var keyhole = esc + rot * Vector3.Scale(mirror, EscKeyhole);
                    Spawn(row.shell, t, keyhole, rot, mirror, "Lock cylinder" + faceLabel);
                    var plug = Spawn(row.plug, t, keyhole, rot, Vector3.one, "Lock plug" + faceLabel);
                    rig.SetPlugPart(side, plug.transform, sign);
                    var knob = Spawn(row.knob, t, esc + rot * Vector3.Scale(mirror, EscKnobPivot), rot, mirror, "Lock knob" + faceLabel);
                    rig.SetHandle(side, knob.transform, sign);
                }
                else
                {
                    var rose = Anchor(leafInfo, "rose" + s, new Vector3(sign * .022f, 1.000f, .920f));
                    Spawn(row.rose, t, rose, rot, mirror, "Lock rose" + faceLabel);
                    var lever = Spawn(row.lever, t, rose + rot * Vector3.Scale(mirror, RoseSpindle), rot, mirror, "Lock lever" + faceLabel);
                    rig.SetHandle(side, lever.transform, sign);
                }
                // The engraved sign is type: hung only once the graphic-visual line has OKed it (ShowKitType).
                if (ShowKitType && row.signCell >= 0 && FrontRoomsKitLibrary.Has("Kit_DoorSign"))
                {
                    var sign0 = Spawn("Kit_DoorSign", t, Anchor(leafInfo, "sign" + s, new Vector3(sign * .022f, 1.524f, .500f)), rot, Vector3.one, "Door sign" + faceLabel);
                    SetAtlasCell(sign0, SignSlot, null, 2, 2, row.signCell);
                    if (PaleSign(row.signCell)) SwapSlots(sign0, "Prop_PlasticBlack", "Prop_PlasticWhite");
                }
                if (row.plates)
                {
                    // Both faces print the same token (the floor's, DECISIONS #33); the tint is the key that opens the
                    // door from that face (the zone rule's stand-in until M6).
                    var id = FaceIdentity(map, door, side);
                    var plateKit = id.tint == FrontRoomsKeyIdentity.Tint.Blue ? "Kit_DoorNumberPlate_Blue" : id.tint == FrontRoomsKeyIdentity.Tint.White ? "Kit_DoorNumberPlate_White" : "Kit_DoorNumberPlate";
                    if (FrontRoomsKitLibrary.Has(plateKit))
                    {
                        var plate = Spawn(plateKit, t, Anchor(leafInfo, "tagplate" + s, new Vector3(sign * .022f, 1.000f, .790f)), rot, Vector3.one, "Door plate" + faceLabel);
                        SetAtlasCell(plate, KeyTagNoSlot, KeyTagNoFallback, 10, 10, TypeCell(id));
                        rig.SetPlate(side, plate, id);
                    }
                }
            }

            // Bolts on the latch edge (authored along +Z, thrown / extended) and the strike on the frame.
            Transform deadbolt = null;
            if (row.deadbolt != null) deadbolt = Spawn(row.deadbolt, t, Anchor(leafInfo, "deadbolt", new Vector3(0f, 1.000f, .995f)), Quaternion.identity, Vector3.one, "Lock deadbolt").transform;
            var latchAt = Anchor(leafInfo, "latchbolt", new Vector3(0f, row.locked ? .9365f : 1.000f, .995f));
            var latch = Spawn(Kit(row.latch, acting), t, latchAt, Quaternion.identity, Vector3.one, "Lock latch").transform;
            rig.SetBolts(latch, deadbolt);
            var frameInfo = FrontRoomsKitLibrary.GetInfo(Kit(row.frame, acting));
            var strikeAt = Anchor(frameInfo, row.boredStrike ? "strike_bored" : "strike", new Vector3(0f, row.boredStrike ? 1.000f : .968f, .998f));
            var strike = Spawn(Kit(row.strike, acting), frame, strikeAt, StrikeTurn, Vector3.one, "Lock strike").transform;
            rig.SetStrike(strike);
        }

        // A shadows-only cube in the leaf's slab (SPEC §1.7): no collider at all (the map's own cube mesh, no primitive).
        // Door vision (30_design 1.4): a lited leaf's proxy is the same box with a hole over the visible glass (one shared
        // mesh per form, about 32 triangles), so a shadowed lamp behind the door throws a lit rectangle through the lite.
        static void ShadowProxy(Transform rig, MeshRenderer cube, FrontRoomsKitLibrary.Info leafInfo)
        {
            var filter = cube != null ? cube.GetComponent<MeshFilter>() : null;
            if (filter == null || filter.sharedMesh == null) return;
            var go = new GameObject(LeafShadowLabel);
            go.transform.SetParent(rig, false);
            var holed = leafInfo != null && leafInfo.HasLite;
            go.transform.localPosition = holed ? Vector3.zero : ShadowCentre;
            go.transform.localScale = holed ? Vector3.one : ShadowSize;
            go.AddComponent<MeshFilter>().sharedMesh = holed ? FrontRoomsDoorLite.HoledShadowBox(leafInfo.LiteRect) : filter.sharedMesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = cube.sharedMaterial;
            r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            r.receiveShadows = false;
        }

        // The key identity a face's plate shows: the zone on that face's side (the key rule's stand-in until M6).
        static FrontRoomsKeyIdentity FaceIdentity(FrontRoomsMapWorld map, FrontRoomsMapWorld.Door door, int side)
        {
            var probe = door.position + RootRight(door) * (side == 0 ? 1f : -1f) * (MapGrid.CellSize * .5f);
            var cell = map.CellOf(probe);
            // The key that opens a locked door from that face (the map's KeyZoneAt: the zone's key, or on a floor-key floor
            // the floor key, FloorKeyZone, so the plates agree with the one key of A13 / M3).
            var zone = map.Cache != null ? map.KeyZoneAt(cell) : cell;
            return FrontRoomsKeyIdentity.Stub(zone);
        }

        static MaterialPropertyBlock block;

        /// <summary>Show atlas cell n on every renderer submesh of a part whose material is the slot (or its fallback).</summary>
        internal static void SetAtlasCell(GameObject part, string slot, string fallback, int cols, int rows, int n) => SetAtlasCell(part, slot, fallback, cols, rows, n, false);

        /// <summary>
        /// whole = true: every UV of the part samples the middle of cell n (scale 0), for parts whose quads sit on several
        /// cells at once (the key rack's and cabinet's hook labels) when they must all show one cell (the blank).
        /// </summary>
        internal static void SetAtlasCell(GameObject part, string slot, string fallback, int cols, int rows, int n, bool whole)
        {
            if (part == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            n = Mathf.Clamp(n, 0, cols * rows - 1);
            var st = whole
                ? new Vector4(0f, 0f, ((n % cols) + .5f) / cols, 1f - ((n / cols) + .5f) / rows)
                : new Vector4(1f, 1f, (n % cols) / (float)cols, -(n / cols) / (float)rows);
            foreach (var r in part.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mats = r.sharedMaterials;
                for (var i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    var name = mats[i].name;
                    if (name != slot && name != fallback) continue;
                    r.GetPropertyBlock(block, i);
                    block.SetVector("_BaseMap_ST", st);
                    r.SetPropertyBlock(block, i);
                }
            }
        }

        internal static void Kill(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
        }
    }
}
