// FrontRoomsWindowFoil.cs -- V7a (C13, foiled windows): the burglar-alarm look of a window the map foiled
// (FrontRoomsMapWorld.Window.foiled, which the map decides only while the level profile's foil flag is on; off in
// FrontRoomsLevel0.asset, so today nothing here is ever made). Research: Documentation/research/glass/v7_foil/10_research.md.
// Owned by the visual chat (game visual).
//
// The TAPE is drawn on the glass by FrontRooms/Glass (FrontRoomsGlassFoil.hlsl) from the layout this component hands to
// FrontRoomsGlassBreakable.Foil (its property block): so it rides up with a pried sash and tears exactly where the glass tears,
// through both crack stages, the shards, the teeth, the floor glass and the return after a walk away. Lead-tin tape 3/8 in
// (9.5 mm) wide, laid 1 in inside the sight line of each lite, varnished, corners mitre-folded (US 4,026,757, 1977).
// This component builds the HARDWARE round it, render-only (no colliders, no lights, no sound):
//   foil block   a moulded double block on the glass where the loop's two ends run up its ramps under chrome-plated clamps
//                (US 4,084,879, 1978), inline on the lite's run nearest the alarm side; one per lite;
//   lead-in      a twin lead (22 AWG pair) from the block's two clamp screws over the glass, then (with the sashes) up the
//                stile and under a stapled clip, across the sash/frame joint onto the room stop;
//   frame        fixed glazing (sashPry off): a two-screw terminal block on the room stop, where the lead ends;
//                the pocket double-hung (sashPry on, FrontRoomsWindowSash): a surface magnetic reed contact under the meeting
//                rails, the switch on the room stop with exposed screw-post terminals (the leads end under them), the magnet
//                on the room sash's stile beside it, "symmetrically opposite the switch" (NILECJ-STD-0301.00, 1974).
// States, from the map's events (a rebuild takes the state from the map, as FrontRoomsWindowSash does):
//   standing   all fastened; the room sash's 4 mm creep under the bar (V6) carries the magnet (a reed opens only past 2-5 cm);
//   pried      SashPried: the blocks and the leads' sash ends ride up with the glass (FrontRoomsGlassBreakable.SashLift) into
//              the head; the leads tear at the sash/frame joint and their frame ends drop and hang from their last fastening;
//              the magnet, proud of the sash face, is sheared off by the head stop and drops onto the room-side sill;
//   shattered  WindowShattered: each block pops off its glass and hangs by its lead from the clip (or the terminal's edge);
//   rebuilt    IsSashPried -> stowed (blocks gone up, leads hanging, the magnet on the sill); no pane -> blocks hanging.
// Nothing stays in the climb's clear zone (X +-0.600, Y 0.390-1.900) after a break: the hanging blocks keep |X| >= 0.635.
// Every part stays within the stop line's depth so a rising part passes behind the head stop's face (Z 0.022), except the
// magnet, which the head shears off.
// Tiers: desktop = hero meshes (the blocks and the contact hold at 0.3 m: turned screw heads with slots, eased mouldings, the
// copper loop under each clamp screw) within about 3 m, the phone meshes past that (LODGroups, no shadow), culled at about
// 20 m; the leads (each its own tube mesh) are culled at about 8 m. Handhelds (FrontRoomsHandheld.Active) and WebGL: the phone
// meshes and no hardware shadows (FrontRoomsWindowSash.PhoneTier). Part meshes are built once per tier and shared.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

[DisallowMultipleComponent]
public sealed class FrontRoomsWindowFoil : MonoBehaviour
{
    /// <summary>
    /// A hardware part came loose (for the sound line, if a clatter is wanted; the map's DeviceTripped carries the alarm itself):
    /// the part ("magnet": sheared off by the head; "block": popped off its glass), the world point it starts from, and the
    /// seconds until it lands on the sill (the magnet) or hangs still on its lead (a block).
    /// </summary>
    public static event Action<FrontRoomsWindowFoil, string, Vector3, float> PartDropped;

    public const string Label = "Window foil (kit)";

    // ---- the tape (research 10 sec 1): the glass shader draws it from these
    /// <summary>Tape width: 3/8 in (US 4,026,757: "usually about 3/8" wide").</summary>
    public const float TapeWidth = .009525f;
    /// <summary>The tape's outer edge stands this far inside each lite's sight line (1 in).</summary>
    public const float TapeMargin = .0254f;
    /// <summary>The brushed varnish overcoat runs about this far past the tape (it wanders 1.5-4 mm in the shader).</summary>
    public const float VarnishMargin = .0028f;

    // ---- the section (window root frame turned like the kit frame: X along the wall, Y up, +Z = the room's face)
    const float StopX = FrontRoomsInteractableKit.WindowSection.StopX;            // 0.6835, the stop line
    const float StopSill = FrontRoomsInteractableKit.WindowSection.StopSill;
    const float StopHead = FrontRoomsInteractableKit.WindowSection.StopHead;
    const float StopFaceZ = FrontRoomsInteractableKit.WindowSection.StopZOut;     // 0.022, the room stop's face
    const float StopMidX = StopX + .5f * FrontRoomsInteractableKit.WindowSection.StopBand;   // 0.6915, the stop's centre line
    const float GlassZ = FrontRoomsInteractableKit.WindowSection.GlassHalfZ;     // 0.003, the glass's room face
    const float RailX = FrontRoomsWindowSash.RailX;                                // 0.6385, the sash stiles' inner edge
    const float RoomFaceZ = FrontRoomsWindowSash.ZFace;                            // 0.0205, the room sash's face
    const float HallFaceZ = FrontRoomsWindowSash.ZStep;                            // 0.0105, the hall sash's room-side face
    static readonly float SlabCentreY = (float)FrontRoomsGlassFracturePattern.SlabCentreY;

    // ---- the parts and their places (research 10 sec 3)
    /// <summary>Double foil block: a 1-1/4 x 3/4 in base, its long side along the run.</summary>
    public const float BlockLength = .032f, BlockDepth = .019f, BlockHeight = .0055f;
    const float ClampOffset = .0085f, ClampTop = BlockHeight + .00095f;   // the clamp screws (along the run) and the plates' top
    /// <summary>The cluster's height: the reed contact sits just under the meeting rails (the lower sash's top), as the period
    /// installs; the fixed pane's block and terminal keep the same height.</summary>
    public const float ContactY = 1.125f;
    const float BlockLowerY = 1.088f, BlockUpperY = 1.262f, LeadLowerY = 1.085f, LeadUpperY = 1.262f, UpperMidStopY = 1.212f;
    /// <summary>Switch: body 44 x 12.7 x 11 mm on the stop's centre line, an end tab with a terminal post at each end.</summary>
    public const float SwitchBody = .044f, SwitchWidth = .0127f, SwitchHeight = .011f, SwitchTab = .009f, SwitchTabHeight = .004f;
    /// <summary>Magnet: body 36 x 12.7 x 10 mm with screw ears, on the room stile 4.7 mm from the switch.</summary>
    public const float MagnetBody = .036f, MagnetWidth = .0127f, MagnetHeight = .010f, MagnetEar = .006f;
    const float MagnetX = StopX - .0047f - .5f * MagnetWidth;           // 0.67245
    const float TerminalBody = .026f, TerminalHeight = .008f, TerminalScrew = .0065f;
    const float LeadR = .00055f;                                         // each conductor of the twin lead: 22 AWG, 1.1 mm over the PVC
    const float Gravity = 9.81f;

    /// <summary>The phone meshes (handhelds, WebGL, or the tools' switch): as FrontRoomsWindowSash.</summary>
    public static bool PhoneTier => FrontRoomsWindowSash.PhoneTier;

    public enum Phase { Standing, Rising, Stowed, Broken }
    /// <summary>What the hardware shows now.</summary>
    public Phase Now { get; private set; }
    /// <summary>True when the window carries the sashes (sashPry on): then the reed contact is there.</summary>
    public bool HasContact => sashes;
    public Transform Magnet => magnet;
    public Transform FrameUnit => frameUnit;
    /// <summary>The layout handed to the glass (pane metres): what the shader draws.</summary>
    public FrontRoomsGlassBreakable.FoilLook Look => look;

    FrontRoomsMapWorld map;
    FrontRoomsMapWorld.Window window;
    FrontRoomsGlassBreakable glass;
    FrontRoomsWindowSash sash;
    Transform pivot, rider, magnet, frameUnit;
    bool bound, sashes, magnetOn;
    float side = 1f, xSign = 1f, prySince = 10f, shearLift;
    FrontRoomsGlassBreakable.FoilLook look;

    sealed class Block
    {
        public Transform t;
        public Vector3 home;          // on the glass (rider frame = pivot frame at no lift)
        public Quaternion homeRot;
        public Lead lead;
        public float surfaceZ;        // the face it hangs against once its glass is gone
        public bool hanging;
    }

    sealed class Lead
    {
        public Transform node;        // the tube (pivot frame)
        public Mesh mesh;
        public Vector3[] attached;    // the run as fastened (pivot frame, no lift)
        public Vector3[] now;         // what the tube shows (pivot frame)
        public Block block;           // the block its start is screwed to (null once it tore away from it)
        public Vector3 endA, endB;    // where its two conductors end, under the post or the terminal's screws (pivot frame)
        public int clip;              // the point it is clipped at, where the block hangs from at a shatter
        public int joint = -1;        // the sash/frame joint (with the sashes), where a pry tears it
        public int frameFix;          // the first point fastened to the frame after the joint
        public float hangFactor = .9f;
        public bool torn;
        public float clock = -1f, seconds;
        public Vector3[] from, to;    // a swing in progress (same count)
    }

    readonly List<Block> blocks = new List<Block>(2);
    readonly List<Lead> leads = new List<Lead>(2);
    readonly List<Mesh> ownMeshes = new List<Mesh>(4);

    sealed class Drop
    {
        public Transform t;
        public Vector3 p0, p1;
        public Quaternion q0, q1;
        public float vy, clock, seconds;
    }
    readonly List<Drop> drops = new List<Drop>(1);

    sealed class Hang
    {
        public Block b;
        public Vector3 p0, p1;
        public Quaternion q0, q1;
        public float clock, seconds;
    }
    readonly List<Hang> hangs = new List<Hang>(2);

    // =========================================================================================== attach (the kit's DressWindow)
    /// <summary>
    /// Called by FrontRoomsInteractableKit.DressWindow for every window the map foiled (Window.foiled), after the frame and the
    /// sashes, with the frame's turn (face A to the room). Hands the glass its tape and builds the hardware for the window's
    /// state now (standing, pried or broken).
    /// </summary>
    public static FrontRoomsWindowFoil Attach(FrontRoomsMapWorld map, FrontRoomsMapWorld.Window window, FrontRoomsMapWorld.GlassBreakRecord record, bool woodFrame, Quaternion turn)
    {
        if (map == null || window == null || window.root == null || !window.foiled) return null;
        var foil = window.root.GetComponent<FrontRoomsWindowFoil>();
        if (foil == null) foil = window.root.gameObject.AddComponent<FrontRoomsWindowFoil>();
        foil.Build(map, window, turn);
        return foil;
    }

    void Build(FrontRoomsMapWorld world, FrontRoomsMapWorld.Window w, Quaternion turn)
    {
        Unbind();
        if (pivot != null) Kill(pivot.gameObject);
        FreeMeshes();
        blocks.Clear(); leads.Clear(); drops.Clear(); hangs.Clear();
        map = world;
        window = w;
        glass = GetComponent<FrontRoomsGlassBreakable>();
        sash = GetComponent<FrontRoomsWindowSash>();
        sashes = world.SashPry && sash != null;
        side = Hash01(w.edge, 31) < .5f ? -1f : 1f;
        xSign = (turn * Vector3.right).x >= 0f ? 1f : -1f;
        var faceSign = (turn * Vector3.forward).z >= 0f ? 1f : -1f;
        magnetOn = sashes;
        prySince = 10f;
        var parts = Parts.For(PhoneTier);
        var lite = Parts.For(true);
        var phone = parts.phone;
        var hw = phone ? ShadowCastingMode.Off : ShadowCastingMode.On;
        var blockMats = new[] { Plastic, Clamp, SlotInk, TapeOnBlock, Copper };   // the block's five submeshes
        var unitMats = new[] { Plastic, Clamp, SlotInk };                         // the switch's and the magnet's three
        var terminalMats = new[] { Plastic, Clamp, SlotInk, Copper };             // the terminal's four

        pivot = new GameObject(Label).transform;
        pivot.SetParent(transform, false);
        pivot.localRotation = turn;
        rider = Node("riding with the glass", pivot, Vector3.zero);

        // the tape's layout on the glass: pane metres (u = root X, v = root Y from the slab centre); the varnish ages per window
        look = new FrontRoomsGlassBreakable.FoilLook { shape = new Vector4(TapeWidth, VarnishMargin, faceSign, .45f + .4f * Hash01(w.edge, 23)) };
        if (!sashes)
        {
            // Fixed glazing: one loop inside the stops, the block on the alarm side's run, a terminal block on the room stop.
            var outer = StopX - TapeMargin;
            look.rectA = PaneRect(outer, StopSill + TapeMargin, StopHead - TapeMargin);
            var bx = side * (outer - .5f * TapeWidth);
            look.gapA = PaneGap(bx, ContactY);
            var block = AddBlock(parts, lite, bx, ContactY, StopFaceZ, blockMats, hw);
            frameUnit = Draw("foil terminal block", pivot, new Vector3(side * StopMidX, ContactY, StopFaceZ), parts.terminal, terminalMats, hw);
            if (!phone) PartLods(frameUnit, lite.terminal, terminalMats);
            var y = ContactY;
            var lead = NewLead(block, new[]
            {
                new Vector3(bx + side * (.5f * BlockDepth + .0008f), y, GlassZ + BlockHeight + .0009f),
                new Vector3(bx + side * (.5f * BlockDepth + .0055f), y, GlassZ + LeadR),
                new Vector3(side * (StopX - .003f), y, GlassZ + LeadR),
                new Vector3(side * (StopX - .0006f), y, GlassZ + .0012f),
                new Vector3(side * (StopX - .0006f), y, StopFaceZ - .0015f),
                new Vector3(side * (StopX + .0012f), y, StopFaceZ + LeadR),
                new Vector3(side * (StopMidX - .5f * SwitchWidth - .0012f), y, StopFaceZ + LeadR),
                new Vector3(side * (StopMidX - .5f * SwitchWidth + .0008f), y, StopFaceZ + TerminalHeight + .0009f),
            }, phone);
            lead.clip = 5;
            lead.endA = new Vector3(side * (StopMidX - .0016f), y - TerminalScrew, StopFaceZ + TerminalHeight + .0007f);
            lead.endB = new Vector3(side * (StopMidX - .0016f), y + TerminalScrew, StopFaceZ + TerminalHeight + .0007f);
            Show(lead, lead.attached);
        }
        else
        {
            // The pocket double-hung (V6): a loop on each lite inside the sash members, a block on each, the reed contact under the
            // meeting rails on the alarm side, each lead to a post of the switch.
            var outer = RailX - TapeMargin;
            look.rectA = PaneRect(outer, FrontRoomsWindowSash.BottomRailTop + TapeMargin, FrontRoomsWindowSash.MeetBottom - TapeMargin);
            look.rectB = PaneRect(outer, FrontRoomsWindowSash.MeetTop + TapeMargin, FrontRoomsWindowSash.TopRailBottom - TapeMargin);
            var bx = side * (outer - .5f * TapeWidth);
            look.gapA = PaneGap(bx, BlockLowerY);
            look.gapB = PaneGap(bx, BlockUpperY);
            var lower = AddBlock(parts, lite, bx, BlockLowerY, RoomFaceZ, blockMats, hw);
            var upper = AddBlock(parts, lite, bx, BlockUpperY, HallFaceZ, blockMats, hw);
            frameUnit = Draw("reed switch", pivot, new Vector3(side * StopMidX, ContactY, StopFaceZ), parts.contactSwitch, unitMats, hw);
            if (!phone) PartLods(frameUnit, lite.contactSwitch, unitMats);
            magnet = Draw("reed magnet", pivot, new Vector3(side * MagnetX, ContactY, RoomFaceZ), parts.magnet, unitMats, hw);
            if (!phone) PartLods(magnet, lite.magnet, unitMats);
            // the magnet's top meets the head stop's underside after this much lift: the head shears it off there
            shearLift = StopHead - (ContactY + .5f * MagnetBody + MagnetEar) - .001f;
            var leadX = side * (StopMidX + .006f);   // the stop face's outer strip, clear of the steel stop's oval heads
            var postLow = ContactY - .5f * SwitchBody - .5f * SwitchTab;
            var postHigh = ContactY + .5f * SwitchBody + .5f * SwitchTab;
            var tabTop = StopFaceZ + SwitchTabHeight;
            // lower lite: over the glass, up the room stile's edge, under the clip, across the joint, up the stop to the lower post
            var lowerLead = NewLead(lower, new[]
            {
                new Vector3(bx + side * (.5f * BlockDepth + .0008f), BlockLowerY, GlassZ + BlockHeight + .0009f),
                new Vector3(bx + side * (.5f * BlockDepth + .0055f), LeadLowerY + .001f, GlassZ + LeadR),
                new Vector3(side * (RailX - .003f), LeadLowerY, GlassZ + LeadR),
                new Vector3(side * (RailX - .0006f), LeadLowerY, GlassZ + .0012f),
                new Vector3(side * (RailX - .0006f), LeadLowerY, RoomFaceZ - .0015f),
                new Vector3(side * (RailX + .0015f), LeadLowerY, RoomFaceZ + LeadR),
                new Vector3(side * (RailX + .0075f), LeadLowerY, RoomFaceZ + LeadR),
                new Vector3(side * (StopX - .0008f), LeadLowerY, RoomFaceZ + LeadR),
                new Vector3(side * (StopX + .0012f), LeadLowerY, StopFaceZ + LeadR),
                new Vector3(leadX, LeadLowerY + .002f, StopFaceZ + LeadR),
                new Vector3(leadX, postLow - .0075f, StopFaceZ + LeadR),
                new Vector3(side * (StopMidX + .0028f), postLow - .0058f, StopFaceZ + .0012f),
                new Vector3(side * (StopMidX + .0018f), postLow - .0049f, tabTop + .0007f),
            }, phone);
            lowerLead.clip = 6;
            lowerLead.joint = 7;
            lowerLead.frameFix = 9;
            lowerLead.endA = new Vector3(side * StopMidX + .0016f, postLow - .0012f, tabTop + .0009f);
            lowerLead.endB = new Vector3(side * StopMidX - .0016f, postLow - .0012f, tabTop + .0009f);
            // upper lite: over the glass, up the hall stile's edge, under the clip, across the joint, down the stop to the upper post
            var upperLead = NewLead(upper, new[]
            {
                new Vector3(bx + side * (.5f * BlockDepth + .0008f), BlockUpperY, GlassZ + BlockHeight + .0009f),
                new Vector3(bx + side * (.5f * BlockDepth + .0055f), LeadUpperY, GlassZ + LeadR),
                new Vector3(side * (RailX - .003f), LeadUpperY, GlassZ + LeadR),
                new Vector3(side * (RailX - .0006f), LeadUpperY, GlassZ + .0012f),
                new Vector3(side * (RailX - .0006f), LeadUpperY, HallFaceZ - .0015f),
                new Vector3(side * (RailX + .0015f), LeadUpperY, HallFaceZ + LeadR),
                new Vector3(side * (RailX + .0075f), LeadUpperY, HallFaceZ + LeadR),
                new Vector3(side * (StopX - .0008f), LeadUpperY, HallFaceZ + LeadR),
                new Vector3(side * (StopX - .0005f), LeadUpperY, HallFaceZ + .002f),
                new Vector3(side * (StopX - .0005f), LeadUpperY, StopFaceZ - .0015f),
                new Vector3(side * (StopX + .0012f), LeadUpperY, StopFaceZ + LeadR),
                new Vector3(leadX, LeadUpperY - .003f, StopFaceZ + LeadR),
                new Vector3(leadX, UpperMidStopY, StopFaceZ + LeadR),
                new Vector3(leadX, postHigh + .0075f, StopFaceZ + LeadR),
                new Vector3(side * (StopMidX + .0028f), postHigh + .0058f, StopFaceZ + .0012f),
                new Vector3(side * (StopMidX + .0018f), postHigh + .0049f, tabTop + .0007f),
            }, phone);
            upperLead.clip = 6;
            upperLead.joint = 7;
            upperLead.frameFix = 11;   // stapled where it turns down the stop: a pry leaves a short end there
            upperLead.hangFactor = .75f;   // shorter: it must hang clear of the room sash's top (Y 1.1955)
            upperLead.endA = new Vector3(side * StopMidX + .0016f, postHigh + .0012f, tabTop + .0009f);
            upperLead.endB = new Vector3(side * StopMidX - .0016f, postHigh + .0012f, tabTop + .0009f);
            AddClip(parts, lowerLead.attached[6] - new Vector3(0f, 0f, LeadR), rider, false);
            AddClip(parts, upperLead.attached[6] - new Vector3(0f, 0f, LeadR), rider, false);
            AddClip(parts, upperLead.attached[11] - new Vector3(0f, 0f, LeadR), pivot, true);
            Show(lowerLead, lowerLead.attached);
            Show(upperLead, upperLead.attached);
        }
        foreach (var l in leads) CullLod(l.node, LeadCullMetres);
        if (glass != null) glass.Foil = look;

        // ---- the state now
        if (sashes && world.IsSashPried(w.edge))
        {
            // Pried before this build: the glass, its tape and blocks are up in the head pocket; the leads' frame ends hang; the
            // sheared magnet lies on the room-side sill.
            rider.gameObject.SetActive(false);
            foreach (var l in leads) TearLead(l, false);
            ShearMagnet(false);
            Now = Phase.Stowed;
        }
        else if (w.pane == null)
        {
            // Broken before this build: the blocks hang by their leads (the glass under them is on the floor).
            foreach (var b in blocks) HangBlock(b, false);
            Now = Phase.Broken;
        }
        else
        {
            Now = Phase.Standing;
            if (sashes) PoseMagnet(0f, CreepOf(Mathf.Clamp01(w.sashHold / FrontRoomsMapWorld.SashPryHoldSeconds)));
            Bind();
        }
        enabled = false;
    }

    // Pane metres (the shader's frame): u = root X (the kit's X turned), v = root Y from the slab centre.
    static Vector4 PaneRect(float outerX, float y0, float y1) => new Vector4(-outerX, y0 - SlabCentreY, outerX, y1 - SlabCentreY);

    Vector4 PaneGap(float kitX, float y) => new Vector4(xSign * kitX, y - SlabCentreY, .5f * BlockDepth + .001f, .5f * BlockLength);

    Block AddBlock(Parts parts, Parts lite, float x, float y, float surfaceZ, Material[] materials, ShadowCastingMode hw)
    {
        var b = new Block
        {
            // the block's long side along the (vertical) run, its +X toward the frame, so the lead leaves toward the alarm side
            homeRot = side < 0f ? Quaternion.AngleAxis(180f, Vector3.forward) : Quaternion.identity,
            home = new Vector3(x, y, GlassZ),
            surfaceZ = surfaceZ,
        };
        b.t = Draw("foil block", rider, b.home, parts.block, materials, hw);
        b.t.localRotation = b.homeRot;
        if (!parts.phone) PartLods(b.t, lite.block, materials);
        blocks.Add(b);
        return b;
    }

    void AddClip(Parts parts, Vector3 at, Transform parent, bool alongY)
    {
        if (parts.phone) return;   // a 6 mm stapled saddle: hero only
        var c = Draw("lead clip", parent, at, parts.clip, new[] { Plastic, Clamp }, ShadowCastingMode.Off);
        if (alongY) c.localRotation = Quaternion.AngleAxis(90f, Vector3.forward);
        CullLod(c, ClipCullMetres);
    }

    Lead NewLead(Block block, Vector3[] path, bool phone)
    {
        var l = new Lead { block = block, attached = path };
        block.lead = l;
        l.node = new GameObject("foil lead").transform;
        l.node.SetParent(pivot, false);
        l.node.gameObject.AddComponent<MeshFilter>();
        var r = l.node.gameObject.AddComponent<MeshRenderer>();
        r.sharedMaterials = new[] { LeadPvc, Copper };
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = true;
        r.lightProbeUsage = LightProbeUsage.BlendProbes;
        l.mesh = NewMesh("Window foil / lead");
        l.node.GetComponent<MeshFilter>().sharedMesh = l.mesh;
        leads.Add(l);
        return l;
    }

    Mesh NewMesh(string name)
    {
        var m = new Mesh { name = name, hideFlags = HideFlags.DontSave };
        m.MarkDynamic();
        ownMeshes.Add(m);
        return m;
    }

    void FreeMeshes()
    {
        foreach (var m in ownMeshes) Kill(m);
        ownMeshes.Clear();
    }

    // =========================================================================================== map events
    void Bind()
    {
        if (bound || map == null) return;
        bound = true;
        map.SashPryProgress += OnPryProgress;
        map.SashPryReleased += OnPryReleased;
        map.SashPried += OnPried;
        map.WindowShattered += OnShattered;
    }

    void Unbind()
    {
        if (!bound || map == null) return;
        bound = false;
        map.SashPryProgress -= OnPryProgress;
        map.SashPryReleased -= OnPryReleased;
        map.SashPried -= OnPried;
        map.WindowShattered -= OnShattered;
    }

    void OnDestroy()
    {
        Unbind();
        FreeMeshes();
    }

    /// <summary>The event is this foil's. Edit mode never calls OnDestroy (DestroyImmediate on a component without
    /// ExecuteAlways), so a foil destroyed there is still on the map's events: the first one after that drops its
    /// handlers instead of reading the dead transform. In play OnDestroy has already unbound it.</summary>
    bool Mine(FrontRoomsMapWorld.Window w)
    {
        if (this == null) { Unbind(); return false; }
        return w != null && w.root == transform && Now == Phase.Standing;
    }

    void OnPryProgress(FrontRoomsMapWorld.Window w, float progress)
    {
        if (!Mine(w) || !sashes) return;
        prySince = 0f;
        enabled = true;
    }

    void OnPryReleased(FrontRoomsMapWorld.Window w)
    {
        if (!Mine(w) || !sashes) return;
        prySince = 0f;
        enabled = true;
    }

    void OnPried(FrontRoomsMapWorld.Window w)
    {
        if (!Mine(w) || !sashes) return;
        Unbind();
        Now = Phase.Rising;
        enabled = true;
    }

    void OnShattered(FrontRoomsMapWorld.Window w, Vector3 impactWorld, Vector3 impulse)
    {
        if (!Mine(w)) return;
        Unbind();
        foreach (var b in blocks) HangBlock(b, true);
        Now = Phase.Broken;
        enabled = true;
    }

    // =========================================================================================== per frame (only while something moves)
    // LateUpdate: after FrontRoomsWindowSash's Update has moved the sashes and the glass this frame.
    void LateUpdate()
    {
        var dt = Mathf.Min(Time.deltaTime, .05f);
        var busy = false;
        if (Now == Phase.Standing && sashes)
        {
            // V6's room sash creeps 4 mm under the bar and settles to the banked strain: the magnet goes with it.
            prySince += dt;
            PoseMagnet(0f, CreepOf(sash != null ? sash.Strain : 0f));
            busy = prySince < .6f;
        }
        else if (Now == Phase.Rising)
        {
            var lift = glass != null ? glass.SashLift : (sash != null ? sash.Lift : 0f);
            rider.localPosition = new Vector3(0f, lift, 0f);
            // the sashes leave at once: each lead tears at the joint as soon as its slack is taken up
            if (lift > .012f) foreach (var l in leads) if (!l.torn) TearLead(l, true);
            if (magnetOn)
            {
                if (lift >= shearLift) ShearMagnet(true);
                else PoseMagnet(lift, CreepOf(1f));
            }
            var stowed = (glass != null && glass.SashStowed) || (sash != null && sash.Now == FrontRoomsWindowSash.Phase.Stowed);
            if (stowed)
            {
                if (magnetOn) ShearMagnet(true);
                rider.gameObject.SetActive(false);
                Now = Phase.Stowed;
            }
            else busy = true;
        }
        for (var i = drops.Count - 1; i >= 0; i--)
        {
            if (TickDrop(drops[i], dt)) busy = true;
            else drops.RemoveAt(i);
        }
        for (var i = hangs.Count - 1; i >= 0; i--)
        {
            if (TickHang(hangs[i], dt)) busy = true;
            else hangs.RemoveAt(i);
        }
        foreach (var l in leads)
            if (l.clock >= 0f && TickSwing(l, dt)) busy = true;
        if (!busy) enabled = false;
    }

    static float CreepOf(float strain) => FrontRoomsWindowSash.CreepMax * Smooth(0f, 1f, strain);

    void PoseMagnet(float lift, float creep)
    {
        if (magnet == null || !magnetOn) return;
        magnet.localPosition = new Vector3(side * MagnetX, ContactY + lift + creep, RoomFaceZ);
    }

    // =========================================================================================== tear, shear, hang
    // The pry: the lead tears at the sash/frame joint. The sash piece (block .. joint) goes up on the rider with its block; the
    // frame piece (joint .. post) swings down from its first fastening on the frame and hangs, its torn end bare copper.
    void TearLead(Lead l, bool animate)
    {
        if (l.joint < 0 || l.torn) return;
        l.torn = true;
        var sashPart = new Vector3[l.joint + 1];
        Array.Copy(l.attached, sashPart, l.joint + 1);
        var sashNode = Node("foil lead (sash end)", rider, Vector3.zero);
        sashNode.gameObject.AddComponent<MeshFilter>().sharedMesh = NewMesh("Window foil / lead (sash end)");
        var sr = sashNode.gameObject.AddComponent<MeshRenderer>();
        sr.sharedMaterials = new[] { LeadPvc, Copper };
        sr.shadowCastingMode = ShadowCastingMode.Off;
        var pts = Resample(sashPart, Samples(sashPart));
        Vector3 a, b;
        BlockScrews(l.block, rider, out a, out b);
        Tube(sashNode.GetComponent<MeshFilter>().sharedMesh, pts, PhoneTier, true, a, b, false, Vector3.zero, Vector3.zero);
        CullLod(sashNode, LeadCullMetres);
        // the frame piece: from its fastening on the frame the free end drops and hangs, a little out from the stop
        var frame = new Vector3[l.attached.Length - l.joint];
        Array.Copy(l.attached, l.joint, frame, 0, frame.Length);
        var k = Mathf.Clamp(l.frameFix - l.joint, 1, frame.Length - 1);
        var hangLen = PathLength(frame, 0, k);
        var anchor = frame[k];
        var free = (Vector3[])frame.Clone();
        for (var i = 0; i <= k; i++)
        {
            var u = (float)(k - i) / k;   // 1 at the torn end, 0 at the fastening
            free[i] = anchor + new Vector3(side * .0008f * u, -hangLen * u, .003f * Mathf.Sin(u * Mathf.PI) + .0012f * u);
        }
        l.block = null;
        Swing(l, frame, free, animate, .45f);
    }

    // The shatter: the block's glass is gone; it pops off and hangs on its lead from the clip (or the terminal's edge).
    void HangBlock(Block b, bool animate)
    {
        if (b.hanging || b.t == null || b.lead == null) return;
        b.hanging = true;
        var l = b.lead;
        var clipP = l.attached[l.clip];
        var len = PathLength(l.attached, 0, l.clip) * l.hangFactor;
        // it hangs from its upper clamp screw, long side down the face behind it, turned a little
        var twist = Mathf.Lerp(-14f, 14f, Hash01(window.edge, 41 + blocks.IndexOf(b)));
        var q1 = Quaternion.AngleAxis(twist, Vector3.forward) * b.homeRot;
        var topLocal = new Vector3(0f, (q1 * Vector3.up).y >= 0f ? ClampOffset : -ClampOffset, 0f);
        var p1 = new Vector3(clipP.x - side * .0015f, clipP.y - len, b.surfaceZ + .0005f) - q1 * topLocal;
        var top = p1 + q1 * (topLocal + new Vector3(0f, 0f, ClampTop));
        // the lead from the block's upper screw up to the clip: nearly straight, a slight belly out from the face
        var to = (Vector3[])l.attached.Clone();
        for (var i = 0; i <= l.clip; i++)
        {
            var u = (float)i / Mathf.Max(1, l.clip);   // 0 at the block, 1 at the clip
            to[i] = Vector3.Lerp(top, clipP, u) + new Vector3(0f, 0f, .0025f * Mathf.Sin(u * Mathf.PI));
        }
        if (!animate)
        {
            b.t.localPosition = p1;
            b.t.localRotation = q1;
            Show(l, to);
            return;
        }
        // pops off the glass toward the room, drops and is caught by its lead: about 0.55 s to hang still
        hangs.Add(new Hang { b = b, p0 = b.t.localPosition, p1 = p1, q0 = b.t.localRotation, q1 = q1, clock = 0f, seconds = .55f });
        Swing(l, l.now ?? l.attached, to, true, .55f);
        PartDropped?.Invoke(this, "block", b.t.position, .55f);
        enabled = true;
    }

    void ShearMagnet(bool animate)
    {
        if (magnet == null || !magnetOn) return;
        magnetOn = false;
        // knocked off its screws by the head stop, it lands base down or face down on the room-side sill, turned
        var yaw = Mathf.Lerp(-60f, 60f, Hash01(window.edge, 47));
        var faceDown = Hash01(window.edge, 53) < .5f;
        var q1 = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(faceDown ? 90f : -90f, Vector3.right);
        var p1 = new Vector3(side * Mathf.Lerp(.60f, .66f, Hash01(window.edge, 59)), 0f, FrontRoomsWindowSash.RestZ + .01f);
        p1.y = FrontRoomsWindowSash.SillTop - LowestUnder(magnet, q1);
        if (!animate) { magnet.localPosition = p1; magnet.localRotation = q1; return; }
        // struck by the head stop at the sash's speed it rebounds down and out toward the room, then falls to the sill
        var d = new Drop { t = magnet, p0 = magnet.localPosition, p1 = p1, q0 = magnet.localRotation, q1 = q1, vy = -.6f, clock = 0f };
        var h = Mathf.Max(0f, d.p0.y - p1.y);
        d.seconds = Mathf.Max(.05f, (d.vy + Mathf.Sqrt(d.vy * d.vy + 2f * Gravity * h)) / Gravity);
        drops.Add(d);
        PartDropped?.Invoke(this, "magnet", magnet.position, d.seconds);
        enabled = true;
    }

    // A ballistic fall from where it was to its rest on the sill, turning on the way, with one small bounce (as the V6 keeper:
    // vy is the start's vertical speed, + up).
    static bool TickDrop(Drop d, float dt)
    {
        if (d.t == null) return false;
        d.clock += dt;
        const float BounceSeconds = .1f;
        if (d.clock < d.seconds)
        {
            var t = d.clock;
            var u = t / d.seconds;
            var p = Vector3.Lerp(d.p0, d.p1, u);
            p.y = Mathf.Max(d.p1.y, d.p0.y + d.vy * t - .5f * Gravity * t * t);
            d.t.localPosition = p;
            d.t.localRotation = Quaternion.Slerp(d.q0, d.q1, u * u * (3f - 2f * u));
            return true;
        }
        var b = (d.clock - d.seconds) / BounceSeconds;
        var bounce = b < 1f ? Mathf.Sin(b * Mathf.PI) * Mathf.Min(.006f, .06f * (d.p0.y - d.p1.y)) : 0f;
        d.t.localPosition = d.p1 + Vector3.up * bounce;
        d.t.localRotation = d.q1;
        return b < 1f;
    }

    // A block popped off: out from the glass, then down until the lead catches it, and a damped swing to rest.
    bool TickHang(Hang h, float dt)
    {
        if (h.b.t == null) return false;
        h.clock += dt;
        var u = Mathf.Clamp01(h.clock / h.seconds);
        var fall = 1f - (1f - u) * (1f - u);
        var swing = Mathf.Exp(-6f * u) * Mathf.Sin(u * 19f) * (1f - u);
        var p = Vector3.Lerp(h.p0, h.p1, fall);
        p.z += .012f * Mathf.Sin(Mathf.Min(1f, u * 2f) * Mathf.PI);
        p.x += side * .006f * swing;
        h.b.t.localPosition = p;
        h.b.t.localRotation = Quaternion.Slerp(h.q0, h.q1, Smooth(0f, .7f, u)) * Quaternion.AngleAxis(10f * swing, Vector3.up);
        if (u < 1f) return true;
        h.b.t.localPosition = h.p1;
        h.b.t.localRotation = h.q1;
        return false;
    }

    // ---- the leads (tubes rebuilt only while they swing)
    // from and to share their points one for one (the same run, partly moved), so the fastened part never slides.
    void Swing(Lead l, Vector3[] from, Vector3[] to, bool animate, float seconds)
    {
        l.to = (Vector3[])to.Clone();
        l.from = from != null && from.Length == to.Length ? (Vector3[])from.Clone() : (Vector3[])to.Clone();
        if (!animate) { l.clock = -1f; Show(l, l.to); return; }
        l.clock = 0f;
        l.seconds = seconds;
        enabled = true;
    }

    bool TickSwing(Lead l, float dt)
    {
        l.clock += dt;
        var u = Mathf.Clamp01(l.clock / l.seconds);
        var e = u >= 1f ? 1f : 1f - Mathf.Exp(-5f * u) * Mathf.Cos(u * 11f);
        var pts = new Vector3[l.from.Length];
        for (var i = 0; i < pts.Length; i++) pts[i] = Vector3.LerpUnclamped(l.from[i], l.to[i], e);
        Show(l, pts);
        if (u < 1f) return true;
        l.clock = -1f;
        return false;
    }

    // Draw a lead along pts (pivot frame): its start split to the block's two clamp screws while it is screwed to a block, its
    // end split to the post or the terminal's two screws.
    void Show(Lead l, Vector3[] pts)
    {
        l.now = pts;
        if (l.mesh == null) return;
        var start = l.block != null;
        var a = Vector3.zero;
        var b = Vector3.zero;
        if (start) BlockScrews(l.block, pivot, out a, out b);
        Tube(l.mesh, Resample(pts, Samples(pts)), PhoneTier, start, a, b, true, l.endA, l.endB);
    }

    // The block's two conductor ends, under its clamp screws on the frame side, in `space`'s frame.
    void BlockScrews(Block b, Transform space, out Vector3 a, out Vector3 c)
    {
        a = c = Vector3.zero;
        if (b == null || b.t == null || space == null) return;
        a = space.InverseTransformPoint(b.t.TransformPoint(new Vector3(.0035f, ClampOffset, ClampTop + .0003f)));
        c = space.InverseTransformPoint(b.t.TransformPoint(new Vector3(.0035f, -ClampOffset, ClampTop + .0003f)));
    }

    // About one ring every 2.5 mm (so the conductors' 7 mm parting at each end is smooth); half on the phone tier.
    static int Samples(Vector3[] p)
    {
        var n = Mathf.Clamp(Mathf.CeilToInt(PathLength(p, 0, p.Length - 1) / .0025f) + 2, 12, 96);
        return PhoneTier ? Mathf.Max(8, n / 2) : n;
    }

    static float PathLength(Vector3[] p, int from, int to)
    {
        var s = 0f;
        for (var i = Mathf.Max(0, from); i < Mathf.Min(to, p.Length - 1); i++) s += Vector3.Distance(p[i], p[i + 1]);
        return s;
    }

    // n points evenly along the polyline (arc length), ends kept
    static Vector3[] Resample(Vector3[] p, int n)
    {
        if (p == null || p.Length == 0) return new Vector3[0];
        if (p.Length == 1 || n < 2) return new[] { p[0] };
        var total = PathLength(p, 0, p.Length - 1);
        var outPts = new Vector3[n];
        var seg = 0;
        var acc = 0f;
        for (var i = 0; i < n; i++)
        {
            var target = total * i / (n - 1);
            while (seg < p.Length - 2 && acc + Vector3.Distance(p[seg], p[seg + 1]) < target) { acc += Vector3.Distance(p[seg], p[seg + 1]); seg++; }
            var len = Vector3.Distance(p[seg], p[seg + 1]);
            var t = len > 1e-7f ? Mathf.Clamp01((target - acc) / len) : 0f;
            outPts[i] = Vector3.Lerp(p[seg], p[seg + 1], t);
        }
        return outPts;
    }

    /// <summary>
    /// The twin lead along a polyline (local metres): two PVC conductors side by side (desktop: 8 sides each; phone: one 4-sided
    /// cord), a parallel-transported frame starting flat on the surface (+Z up), so the pair lies flat where it is laid. Over
    /// the first and last 7 mm the two conductors part toward their own screws (s0a/s0b, s1a/s1b; each goes to the screw on its
    /// side); the first and last segments are bare copper (submesh 1): under the screws, or the torn end.
    /// </summary>
    static void Tube(Mesh mesh, Vector3[] pts, bool phone, bool split0, Vector3 s0a, Vector3 s0b, bool split1, Vector3 s1a, Vector3 s1b)
    {
        var n = pts.Length;
        if (n < 2) { mesh.Clear(); return; }
        var sides = phone ? 4 : 8;
        var cords = phone ? 1 : 2;
        var radius = phone ? LeadR * 1.4f : LeadR;
        var tang = new Vector3[n];
        for (var i = 0; i < n; i++)
        {
            var d = pts[Mathf.Min(n - 1, i + 1)] - pts[Mathf.Max(0, i - 1)];
            tang[i] = d.sqrMagnitude > 1e-12f ? d.normalized : Vector3.up;
        }
        var up = Mathf.Abs(Vector3.Dot(tang[0], Vector3.forward)) < .9f ? Vector3.forward : Vector3.right;
        var nrmF = new Vector3[n];
        var binF = new Vector3[n];
        nrmF[0] = (up - tang[0] * Vector3.Dot(up, tang[0])).normalized;
        binF[0] = Vector3.Cross(tang[0], nrmF[0]);
        for (var i = 1; i < n; i++)
        {
            nrmF[i] = (Quaternion.FromToRotation(tang[i - 1], tang[i]) * nrmF[i - 1]).normalized;
            binF[i] = Vector3.Cross(tang[i], nrmF[i]);
        }
        var arc = new float[n];
        for (var i = 1; i < n; i++) arc[i] = arc[i - 1] + Vector3.Distance(pts[i - 1], pts[i]);
        var total = arc[n - 1];
        const float splitLen = .007f;
        var v = new List<Vector3>(n * (sides + 1) * cords);
        var nm = new List<Vector3>(n * (sides + 1) * cords);
        var uv = new List<Vector2>(n * (sides + 1) * cords);
        var pvc = new List<int>(n * sides * 6 * cords);
        var cu = new List<int>(sides * 12 * cords);
        for (var c = 0; c < cords; c++)
        {
            var off = cords == 1 ? 0f : (c == 0 ? -radius : radius);
            // each conductor takes the screw on its own side of the pair
            var t0 = Vector3.zero;
            var t1 = Vector3.zero;
            if (split0)
            {
                var aSide = Vector3.Dot(s0a - pts[0], binF[0]);
                var bSide = Vector3.Dot(s0b - pts[0], binF[0]);
                var first = cords == 1 ? (s0a + s0b) * .5f : ((off < 0f) == (aSide < bSide) ? s0a : s0b);
                t0 = first - (pts[0] + binF[0] * off);
            }
            if (split1)
            {
                var aSide = Vector3.Dot(s1a - pts[n - 1], binF[n - 1]);
                var bSide = Vector3.Dot(s1b - pts[n - 1], binF[n - 1]);
                var last = cords == 1 ? (s1a + s1b) * .5f : ((off < 0f) == (aSide < bSide) ? s1a : s1b);
                t1 = last - (pts[n - 1] + binF[n - 1] * off);
            }
            var start = v.Count;
            for (var i = 0; i < n; i++)
            {
                var w0 = split0 ? 1f - Smooth(0f, splitLen, arc[i]) : 0f;
                var w1 = split1 ? Smooth(total - splitLen, total, arc[i]) : 0f;
                var centre = pts[i] + binF[i] * off + t0 * w0 + t1 * w1;
                for (var s = 0; s <= sides; s++)
                {
                    var ang = 2f * Mathf.PI * s / sides;
                    var dir = nrmF[i] * Mathf.Cos(ang) + binF[i] * Mathf.Sin(ang);
                    v.Add(centre + dir * radius);
                    nm.Add(dir);
                    uv.Add(new Vector2(arc[i], (float)s / sides * 2f * Mathf.PI * radius));
                }
            }
            for (var i = 0; i < n - 1; i++)
                for (var s = 0; s < sides; s++)
                {
                    var a = start + i * (sides + 1) + s;
                    var b = a + sides + 1;
                    var list = i == 0 || i == n - 2 ? cu : pvc;
                    list.Add(a); list.Add(a + 1); list.Add(b);
                    list.Add(b); list.Add(a + 1); list.Add(b + 1);
                }
        }
        mesh.Clear();
        mesh.SetVertices(v);
        mesh.SetNormals(nm);
        mesh.SetUVs(0, uv);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(pvc, 0);
        mesh.SetTriangles(cu, 1);
        mesh.RecalculateBounds();
    }

    // The lowest point of a part's meshes (local, unrotated) under rotation q about the part's own origin.
    static float LowestUnder(Transform part, Quaternion q)
    {
        var low = float.MaxValue;
        foreach (var f in part.GetComponentsInChildren<MeshFilter>(true))
        {
            if (f.sharedMesh == null) continue;
            var b = f.sharedMesh.bounds;
            var offset = f.transform == part ? Vector3.zero : part.InverseTransformPoint(f.transform.position);
            for (var i = 0; i < 8; i++)
            {
                var c = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                low = Mathf.Min(low, (q * (c + offset)).y);
            }
        }
        return low == float.MaxValue ? 0f : low;
    }

    // =========================================================================================== objects
    static Transform Node(string name, Transform parent, Vector3 at)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = at;
        return t;
    }

    static Transform Draw(string name, Transform parent, Vector3 at, Mesh mesh, Material[] materials, ShadowCastingMode shadows)
    {
        var t = Node(name, parent, at);
        t.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = t.gameObject.AddComponent<MeshRenderer>();
        r.sharedMaterials = materials;
        r.shadowCastingMode = shadows;
        r.receiveShadows = true;
        r.lightProbeUsage = LightProbeUsage.BlendProbes;
        return t;
    }

    /// <summary>The parts' switch heights (screen share of a 32-62 mm part: hero within about 3 m, the phone mesh to about 20 m).</summary>
    public const float HeroLodHeight = .010f, LiteLodHeight = .0016f;

    // A LODGroup on a part: LOD0 its own hero renderer, LOD1 a child with the phone mesh (no shadow), culled beyond.
    static void PartLods(Transform node, Mesh lite, Material[] materials)
    {
        if (node == null || lite == null) return;
        var hero = node.GetComponent<MeshRenderer>();
        var far = Draw("LOD1", node, Vector3.zero, lite, materials, ShadowCastingMode.Off).GetComponent<MeshRenderer>();
        var group = node.gameObject.AddComponent<LODGroup>();
        group.SetLODs(new[] { new LOD(HeroLodHeight, new Renderer[] { hero }), new LOD(LiteLodHeight, new Renderer[] { far }) });
        group.RecalculateBounds();
        FrontRoomsLodFade.Apply(group);
    }

    // A single-LOD group that culls a small part (a clip, a lead) beyond about `metres` (a 60 deg view; the screen share of the
    // part's own bounds there).
    static void CullLod(Transform node, float metres)
    {
        var r = node != null ? node.GetComponent<MeshRenderer>() : null;
        if (r == null) return;
        var group = node.gameObject.AddComponent<LODGroup>();
        group.SetLODs(new[] { new LOD(.01f, new Renderer[] { r }) });
        group.RecalculateBounds();
        var height = Mathf.Clamp(group.size / (2f * metres * .57735f), .0005f, .5f);
        group.SetLODs(new[] { new LOD(height, new Renderer[] { r }) });
        FrontRoomsLodFade.Apply(group);
    }

    /// <summary>Beyond these distances a lead (1 mm conductors) and a clip are not drawn.</summary>
    public const float LeadCullMetres = 8f, ClipCullMetres = 4f;

    static void Kill(Object o)
    {
        if (o == null) return;
        if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
    }

    static float Smooth(float a, float b, float x)
    {
        var t = Mathf.Clamp01((x - a) / (b - a));
        return t * t * (3f - 2f * t);
    }

    static float Hash01(long edge, int salt)
    {
        unchecked
        {
            var h = (ulong)edge * 0x9E3779B97F4A7C15UL + (ulong)salt * 0xBF58476D1CE4E5B9UL;
            h ^= h >> 31; h *= 0x94D049BB133111EBUL; h ^= h >> 29;
            return (h >> 40) / (float)(1UL << 24);
        }
    }

    // =========================================================================================== materials
    static readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();

    static Material Surface(string name, Color fallback, float smoothness, float metallic)
    {
        if (materialCache.TryGetValue(name, out var m) && m != null) return m;
        m = FrontRoomsSurfaces.TryGet(name);
        if (m == null) m = FrontRoomsSurfaces.Lit("Window foil / " + name, fallback, smoothness, metallic);
        materialCache[name] = m;
        return m;
    }

    /// <summary>The blocks, the reed switch and magnet, the terminal and the clips: aged ivory moulding.</summary>
    static Material Plastic => Surface("Prop_PlasticPutty", new Color(.80f, .77f, .68f), .45f, 0f);
    /// <summary>The clamp plates, washers and screws: chrome-plated bronze (US 4,084,879), worn satin.</summary>
    static Material Clamp => Surface("Prop_ChromeSatin", new Color(.76f, .76f, .74f), .55f, 1f);
    static Material SlotInk => FrontRoomsSurfaces.Lit("Window foil / slot", new Color(.06f, .05f, .04f), .15f, 0f);
    /// <summary>The tape's two ends where they run up a block (the glass shader draws the rest): lead-tin under amber varnish.</summary>
    static Material TapeOnBlock => FrontRoomsSurfaces.Lit("Window foil / tape end", new Color(.56f, .53f, .47f), .5f, .9f);
    /// <summary>The twin lead's PVC (off-white, yellowed) and the bare copper under the screws.</summary>
    static Material LeadPvc => FrontRoomsSurfaces.Lit("Window foil / lead PVC", new Color(.80f, .76f, .66f), .55f, 0f);
    static Material Copper => FrontRoomsSurfaces.Lit("Window foil / copper", new Color(.78f, .48f, .30f), .55f, 1f);

    // =========================================================================================== meshes (once per tier)
    sealed class Parts
    {
        public bool phone;
        public Mesh block, contactSwitch, magnet, terminal, clip;
        static readonly Parts[] cache = new Parts[2];

        public static Parts For(bool phone)
        {
            var i = phone ? 1 : 0;
            if (cache[i] != null && cache[i].block != null) return cache[i];
            var p = new Parts { phone = phone };
            p.block = BuildBlock(phone);
            p.contactSwitch = BuildSwitch(phone);
            p.magnet = BuildMagnet(phone);
            p.terminal = BuildTerminal(phone);
            p.clip = BuildClip();
            cache[i] = p;
            return p;
        }
    }

    // ---- the double foil block (US 4,084,879): origin on the glass at its centre, Y along the run, +X toward the frame, +Z off
    // the glass. A moulded base with a ramp at each end (the tape comes up it), a chrome clamp plate and a binding screw over each
    // end, the lead's copper loop under each head. Submeshes: 0 moulding, 1 chrome, 2 slot ink, 3 the tape's ends, 4 copper.
    static Mesh BuildBlock(bool phone)
    {
        var m = new MeshKit(5);
        const float hl = .5f * BlockLength, hd = .5f * BlockDepth, ramp = .0042f;
        var ease = phone ? 0f : .0006f;
        m.ChamferBox(0, new Vector3(-hd, -hl + ramp, 0f), new Vector3(hd, hl - ramp, BlockHeight), ease, 1);
        foreach (var s in new[] { -1f, 1f })
        {
            // the ramp: a wedge from the glass up to the top
            var yTop = s * (hl - ramp);
            var yFoot = s * hl;
            var a = new Vector3(-hd, yTop, BlockHeight);
            var b = new Vector3(hd, yTop, BlockHeight);
            var c = new Vector3(hd, yFoot, 0f);
            var d = new Vector3(-hd, yFoot, 0f);
            var rampN = new Vector3(0f, s * BlockHeight, ramp).normalized;
            m.Quad(0, a, b, c, d, rampN, 1);
            m.Tri(0, new Vector3(-hd, yTop, 0f), a, d, Vector3.left, 1);
            m.Tri(0, new Vector3(hd, yTop, 0f), b, c, Vector3.right, 1);
            // the tape's end: up the ramp and onto the top under the clamp (a hair proud of the moulding)
            var tw = .5f * TapeWidth;
            var lift = rampN * .00012f;
            m.Quad(3, new Vector3(-tw, yFoot, 0f) + lift, new Vector3(tw, yFoot, 0f) + lift, new Vector3(tw, yTop, BlockHeight) + lift,
                new Vector3(-tw, yTop, BlockHeight) + lift, rampN, 1);
            var yIn = s * (hl - ramp - .0105f);
            var up = new Vector3(0f, 0f, .00012f);
            m.Quad(3, new Vector3(-tw, yTop, BlockHeight) + up, new Vector3(tw, yTop, BlockHeight) + up, new Vector3(tw, yIn, BlockHeight) + up,
                new Vector3(-tw, yIn, BlockHeight) + up, Vector3.forward, 1);
            // the clamp plate over the tape's end, its binding screw, the lead's copper loop under the head
            var cy = s * ClampOffset;
            const float top = BlockHeight + .00015f;
            m.ChamferBox(1, new Vector3(-.0052f, cy - .0050f, top), new Vector3(.0052f, cy + .0050f, top + .0008f), phone ? 0f : .00025f, 1);
            Screw(m, new Vector3(0f, cy, ClampTop), .0034f, s > 0f ? 23f : 131f, phone, true);
            if (!phone) Ring(m, 4, new Vector3(0f, cy, ClampTop), .0030f, .0039f, .00045f, false);
        }
        return m.ToMesh("Window foil / foil block");
    }

    // ---- the reed switch (frame half): origin on the stop face at the body's centre, Y up the stop, +Z off it. A moulded body
    // with eased arrises and a shallow crown, an end tab at each end with a washer and a binding-head terminal post (the leads
    // end under them: the exposed screw-post terminals of US 4,409,577's prior art), two countersunk mounting screws.
    static Mesh BuildSwitch(bool phone)
    {
        var m = new MeshKit(3);
        const float hb = .5f * SwitchBody, hw = .5f * SwitchWidth;
        m.ChamferBox(0, new Vector3(-hw, -hb, 0f), new Vector3(hw, hb, SwitchHeight), phone ? 0f : .0009f, 1);
        if (!phone)
        {
            m.ChamferBox(0, new Vector3(-hw + .0016f, -hb + .0016f, SwitchHeight - .0001f), new Vector3(hw - .0016f, hb - .0016f, SwitchHeight + .0004f), .0003f, 1);
            // the moulding's parting line along both sides
            foreach (var s in new[] { -1f, 1f })
                m.ChamferBox(2, new Vector3(s * hw - .00008f, -hb + .002f, .0054f), new Vector3(s * hw + .00008f, hb - .002f, .0058f), 0f, 1);
        }
        foreach (var s in new[] { -1f, 1f })
        {
            var inner = s * (hb - .0005f);
            var outer = s * (hb + SwitchTab);
            m.ChamferBox(0, new Vector3(-.0047f, Mathf.Min(inner, outer), 0f), new Vector3(.0047f, Mathf.Max(inner, outer), SwitchTabHeight), phone ? 0f : .0005f, 1);
            var post = new Vector3(0f, s * (hb + .5f * SwitchTab), SwitchTabHeight);
            Ring(m, 1, post, 0f, .0039f, .0005f, phone);
            Screw(m, post + new Vector3(0f, 0f, .0005f), .0031f, s > 0f ? 64f : 152f, phone, true);
            Screw(m, new Vector3(0f, s * .0105f, SwitchHeight + (phone ? 0f : .0004f)), .0021f, s > 0f ? 88f : 12f, phone, false);
        }
        return m.ToMesh("Window foil / reed switch");
    }

    // ---- the magnet (sash half): origin on the sash face at the body's centre; a body and two screw ears.
    static Mesh BuildMagnet(bool phone)
    {
        var m = new MeshKit(3);
        const float hb = .5f * MagnetBody, hw = .5f * MagnetWidth;
        m.ChamferBox(0, new Vector3(-hw, -hb, 0f), new Vector3(hw, hb, MagnetHeight), phone ? 0f : .0009f, 1);
        if (!phone) m.ChamferBox(0, new Vector3(-hw + .0016f, -hb + .0016f, MagnetHeight - .0001f), new Vector3(hw - .0016f, hb - .0016f, MagnetHeight + .0004f), .0003f, 1);
        foreach (var s in new[] { -1f, 1f })
        {
            var inner = s * (hb - .0005f);
            var outer = s * (hb + MagnetEar);
            m.ChamferBox(0, new Vector3(-.0040f, Mathf.Min(inner, outer), 0f), new Vector3(.0040f, Mathf.Max(inner, outer), .0028f), phone ? 0f : .0005f, 1);
            Screw(m, new Vector3(0f, s * (hb + .5f * MagnetEar), .0028f), .0024f, s > 0f ? 33f : 101f, phone, false);
        }
        return m.ToMesh("Window foil / reed magnet");
    }

    // ---- the terminal block on the stop (fixed glazing): a moulding with two binding screws along it, washers, copper loops.
    static Mesh BuildTerminal(bool phone)
    {
        var m = new MeshKit(4);
        const float hb = .5f * TerminalBody, hw = .5f * SwitchWidth;
        m.ChamferBox(0, new Vector3(-hw, -hb, 0f), new Vector3(hw, hb, TerminalHeight), phone ? 0f : .0008f, 1);
        foreach (var s in new[] { -1f, 1f })
        {
            var at = new Vector3(0f, s * TerminalScrew, TerminalHeight);
            Ring(m, 1, at, 0f, .0038f, .0005f, phone);
            Screw(m, at + new Vector3(0f, 0f, .0005f), .0030f, s > 0f ? 47f : 118f, phone, true);
            if (!phone) Ring(m, 3, at + new Vector3(0f, 0f, .0005f), .0028f, .0036f, .00042f, false);
        }
        return m.ToMesh("Window foil / terminal block");
    }

    // ---- a stapled lead clip: a moulded saddle over the lead with a nail head (hero only); origin at the lead's foot on the
    // surface, the saddle across X (the lead runs along X under it).
    static Mesh BuildClip()
    {
        var m = new MeshKit(2);
        m.ChamferBox(0, new Vector3(-.0016f, -.003f, .0011f), new Vector3(.0016f, .003f, .0019f), .0003f, 1);
        m.ChamferBox(0, new Vector3(-.0016f, .0021f, 0f), new Vector3(.0016f, .003f, .0019f), .0002f, 1);
        m.ChamferBox(0, new Vector3(-.0016f, -.003f, 0f), new Vector3(.0016f, -.0021f, .0019f), .0002f, 1);
        Ring(m, 1, new Vector3(0f, .0042f, 0f), 0f, .0011f, .00045f, false);
        return m.ToMesh("Window foil / lead clip");
    }

    // A flat ring (washer, copper loop, nail head) on a surface: inner and outer radius, thickness, axis +Z of the part.
    static void Ring(MeshKit m, int sub, Vector3 at, float r0, float r1, float h, bool phone)
    {
        var keep = m.M;
        m.M = keep * Matrix4x4.TRS(at, Quaternion.AngleAxis(90f, Vector3.right), Vector3.one);
        var seg = phone ? 8 : 20;
        if (r0 > 0f) m.Lathe(sub, Vector3.zero, new[] { new Vector2(r1, 0f), new Vector2(r1, h), new Vector2(r0, h), new Vector2(r0, 0f) }, seg);
        else m.Lathe(sub, Vector3.zero, new[] { new Vector2(r1, 0f), new Vector2(r1, h), new Vector2(0f, h) }, seg);
        m.M = keep;
    }

    // A screw head on a surface at `at` (axis +Z of the part): binding (a pan head, flat-topped) or countersunk oval; hero: the
    // slot as a dark strip across the head. The builder's lathe turns round its own Y, so the head is built through a turn.
    static void Screw(MeshKit m, Vector3 at, float r, float slotDeg, bool phone, bool binding)
    {
        var keep = m.M;
        m.M = keep * Matrix4x4.TRS(at, Quaternion.AngleAxis(90f, Vector3.right), Vector3.one);
        var steps = phone ? 3 : 6;
        float H(float x)
        {
            if (binding) return x > .82f * r ? .0013f * (1f - (x - .82f * r) / (.18f * r)) + .0006f : .0019f - .0003f * (x / r) * (x / r);
            const float dome = .0007f;
            var rho = (r * r + dome * dome) / (2f * dome);
            return .0001f + Mathf.Sqrt(Mathf.Max(0f, rho * rho - x * x)) - (rho - dome);
        }
        var prof = new List<Vector2> { new Vector2(r, 0f) };
        for (var i = 0; i <= steps; i++) { var x = r * (1f - (float)i / steps); prof.Add(new Vector2(x, H(x))); }
        m.Lathe(1, Vector3.zero, prof.ToArray(), phone ? 8 : 22);
        if (!phone)
        {
            var dir = Quaternion.AngleAxis(slotDeg, Vector3.up) * Vector3.right;
            var sideV = Vector3.Cross(Vector3.up, dir) * .00032f;
            const int k = 8;
            for (var i = 0; i < k; i++)
            {
                var s0 = Mathf.Lerp(-.84f * r, .84f * r, (float)i / k);
                var s1 = Mathf.Lerp(-.84f * r, .84f * r, (float)(i + 1) / k);
                var a = dir * s0 + Vector3.up * (H(Mathf.Abs(s0)) + .00004f);
                var b = dir * s1 + Vector3.up * (H(Mathf.Abs(s1)) + .00004f);
                m.Quad(2, a - sideV, b - sideV, b + sideV, a + sideV, Vector3.up, 0);
            }
        }
        m.M = keep;
    }

    // =========================================================================================== mesh builder (as FrontRoomsWindowSash's)
    /// <summary>A small mesh builder: flat or smooth faces, every point through M (a rotation and translation), UVs in metres
    /// (for FrontRooms/Surface's mesh UVs), winding from the intended outward side (given before M; Unity: clockwise front).</summary>
    sealed class MeshKit
    {
        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Vector3> n = new List<Vector3>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<int>[] tris;
        public Matrix4x4 M = Matrix4x4.identity;

        public MeshKit(int submeshes)
        {
            tris = new List<int>[submeshes];
            for (var i = 0; i < submeshes; i++) tris[i] = new List<int>();
        }

        int Add(Vector3 p, Vector3 normal, Vector2 t)
        {
            v.Add(M.MultiplyPoint3x4(p));
            n.Add(M.MultiplyVector(normal).normalized);
            uv.Add(t);
            return v.Count - 1;
        }

        static Vector2 Planar(Vector3 p, Vector3 normal, int uAxis)
        {
            var ax = Mathf.Abs(normal.x); var ay = Mathf.Abs(normal.y); var az = Mathf.Abs(normal.z);
            var drop = ax >= ay && ax >= az ? 0 : ay >= az ? 1 : 2;
            var a1 = drop == 0 ? 1 : 0;
            var a2 = drop == 2 ? 1 : 2;
            if (uAxis == a2) return new Vector2(p[a2], p[a1]);
            return new Vector2(p[a1], p[a2]);
        }

        /// <summary>One flat triangle, wound to face `outward`.</summary>
        public void Tri(int sub, Vector3 a, Vector3 b, Vector3 c, Vector3 outward, int uAxis)
        {
            var normal = Vector3.Cross(b - a, c - a);
            if (normal.sqrMagnitude < 1e-22f) return;
            var flip = Vector3.Dot(normal, outward) < 0f;
            normal = normal.normalized * (flip ? -1f : 1f);
            var i0 = Add(a, normal, Planar(a, normal, uAxis));
            var i1 = Add(b, normal, Planar(b, normal, uAxis));
            var i2 = Add(c, normal, Planar(c, normal, uAxis));
            var t = tris[sub];
            if (!flip) { t.Add(i0); t.Add(i1); t.Add(i2); } else { t.Add(i0); t.Add(i2); t.Add(i1); }
        }

        /// <summary>A flat planar quad a-b-c-d (in order round its edge), wound to face `outward`.</summary>
        public void Quad(int sub, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward, int uAxis)
        {
            Tri(sub, a, b, c, outward, uAxis);
            Tri(sub, a, c, d, outward, uAxis);
        }

        /// <summary>A smooth triangle (per-vertex normals); the winding follows the normals' sum.</summary>
        public void TriN(int sub, Vector3 a, Vector3 b, Vector3 c, Vector3 na, Vector3 nb, Vector3 nc, Vector2 ta, Vector2 tb, Vector2 tc)
        {
            var normal = Vector3.Cross(b - a, c - a);
            if (normal.sqrMagnitude < 1e-22f) return;
            var flip = Vector3.Dot(normal, na + nb + nc) < 0f;
            var i0 = Add(a, na, ta);
            var i1 = Add(b, nb, tb);
            var i2 = Add(c, nc, tc);
            var t = tris[sub];
            if (!flip) { t.Add(i0); t.Add(i1); t.Add(i2); } else { t.Add(i0); t.Add(i2); t.Add(i1); }
        }

        /// <summary>A box with every arris chamfered by c (flat chamfers and corner facets: crisp highlights); c = 0 is a plain box.</summary>
        public void ChamferBox(int sub, Vector3 lo, Vector3 hi, float c, int uAxis)
        {
            var size = hi - lo;
            c = Mathf.Min(c, .45f * Mathf.Min(size.x, Mathf.Min(size.y, size.z)));
            Vector3 F(int sx, int sy, int sz, int f)
            {
                var p = new Vector3(sx == 1 ? hi.x : lo.x, sy == 1 ? hi.y : lo.y, sz == 1 ? hi.z : lo.z);
                if (f != 0) p.x += sx == 1 ? -c : c;
                if (f != 1) p.y += sy == 1 ? -c : c;
                if (f != 2) p.z += sz == 1 ? -c : c;
                return p;
            }
            for (var f = 0; f < 3; f++)
                for (var sideIx = 0; sideIx < 2; sideIx++)
                {
                    var a1 = f == 0 ? 1 : 0;
                    var a2 = f == 2 ? 1 : 2;
                    var pts = new Vector3[4];
                    var k = 0;
                    foreach (var pair in new[] { new[] { 0, 0 }, new[] { 1, 0 }, new[] { 1, 1 }, new[] { 0, 1 } })
                    {
                        var s = new int[3];
                        s[f] = sideIx; s[a1] = pair[0]; s[a2] = pair[1];
                        pts[k++] = F(s[0], s[1], s[2], f);
                    }
                    var outward = Vector3.zero;
                    outward[f] = sideIx == 1 ? 1f : -1f;
                    Quad(sub, pts[0], pts[1], pts[2], pts[3], outward, uAxis);
                }
            if (c <= 1e-6f) return;
            for (var e = 0; e < 3; e++)
            {
                var a = e == 0 ? 1 : 0;
                var b = e == 2 ? 1 : 2;
                for (var sa = 0; sa < 2; sa++)
                    for (var sb = 0; sb < 2; sb++)
                    {
                        var lo3 = new int[3]; lo3[e] = 0; lo3[a] = sa; lo3[b] = sb;
                        var hi3 = new int[3]; hi3[e] = 1; hi3[a] = sa; hi3[b] = sb;
                        var outward = Vector3.zero;
                        outward[a] = sa == 1 ? 1f : -1f;
                        outward[b] = sb == 1 ? 1f : -1f;
                        Quad(sub, F(lo3[0], lo3[1], lo3[2], a), F(hi3[0], hi3[1], hi3[2], a), F(hi3[0], hi3[1], hi3[2], b), F(lo3[0], lo3[1], lo3[2], b), outward, uAxis);
                    }
            }
            for (var i = 0; i < 8; i++)
            {
                int sx = i & 1, sy = (i >> 1) & 1, sz = (i >> 2) & 1;
                var outward = new Vector3(sx == 1 ? 1f : -1f, sy == 1 ? 1f : -1f, sz == 1 ? 1f : -1f);
                Tri(sub, F(sx, sy, sz, 0), F(sx, sy, sz, 1), F(sx, sy, sz, 2), outward, uAxis);
            }
        }

        /// <summary>
        /// A surface of revolution round the local Y axis through `c`: the profile (radius, height) runs up and in (its outward
        /// side is to the right of the direction of travel). Smooth round the axis; along the profile, joints under 40 deg are
        /// smoothed and sharper ones kept crisp.
        /// </summary>
        public void Lathe(int sub, Vector3 c, Vector2[] prof, int seg)
        {
            var count = prof.Length - 1;
            if (count < 1 || seg < 3) return;
            var ns = new Vector2[count];
            for (var i = 0; i < count; i++)
            {
                var d = prof[i + 1] - prof[i];
                ns[i] = d.sqrMagnitude > 1e-14f ? new Vector2(d.y, -d.x).normalized : Vector2.up;
            }
            bool SmoothJoin(int i, int j) => Vector2.Dot(ns[i], ns[j]) > .766f;
            var len = 0f;
            for (var i = 0; i < count; i++)
            {
                var p0 = prof[i];
                var p1 = prof[i + 1];
                var segLen = Vector2.Distance(p0, p1);
                if (segLen < 1e-7f) continue;
                var n0 = i > 0 && SmoothJoin(i - 1, i) ? (ns[i - 1] + ns[i]).normalized : ns[i];
                var n1 = i + 1 < count && SmoothJoin(i, i + 1) ? (ns[i] + ns[i + 1]).normalized : ns[i];
                for (var j = 0; j < seg; j++)
                {
                    var a0 = 2f * Mathf.PI * j / seg;
                    var a1 = 2f * Mathf.PI * (j + 1) / seg;
                    var r0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                    var r1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                    var A = c + r0 * p0.x + Vector3.up * p0.y;
                    var B = c + r1 * p0.x + Vector3.up * p0.y;
                    var C = c + r1 * p1.x + Vector3.up * p1.y;
                    var D = c + r0 * p1.x + Vector3.up * p1.y;
                    var nA = r0 * n0.x + Vector3.up * n0.y;
                    var nB = r1 * n0.x + Vector3.up * n0.y;
                    var nC = r1 * n1.x + Vector3.up * n1.y;
                    var nD = r0 * n1.x + Vector3.up * n1.y;
                    var u0 = (float)j / seg * 2f * Mathf.PI * Mathf.Max(p0.x, p1.x);
                    var u1 = (float)(j + 1) / seg * 2f * Mathf.PI * Mathf.Max(p0.x, p1.x);
                    var tA = new Vector2(u0, len); var tB = new Vector2(u1, len);
                    var tC = new Vector2(u1, len + segLen); var tD = new Vector2(u0, len + segLen);
                    TriN(sub, A, B, C, nA, nB, nC, tA, tB, tC);
                    TriN(sub, A, C, D, nA, nC, nD, tA, tC, tD);
                }
                len += segLen;
            }
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.subMeshCount = tris.Length;
            for (var i = 0; i < tris.Length; i++) mesh.SetTriangles(tris[i], i);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
