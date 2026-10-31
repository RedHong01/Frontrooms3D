using System.Collections.Generic;
using FrontRooms.Map;
using UnityEngine;

/// <summary>
/// Level transitions (production, 2026-10-22): the FRAME kit. Every Office | Level 0 change sits on a
/// piece the Office side (the 1990 fit-out) owns:
///  - an open edge gets a portal (pilasters 0.12 x 0.20, header 0.35 deep, 6 mm beads, white wall
///    angles, a satin aluminium binder bar); consecutive border open edges form an arcade with a
///    0.24 shared pilaster; where border open edges meet with no wall, a free post (with a collider);
///  - an arch is cased (Office lining, 6 mm beads, bar); the map insets the wall pieces by BeadInset
///    (render only);
///  - a door keeps its real D1.6 _DA frame and saddle (DressDoor); the kit's DOUBLE-ACTING sleeve to the
///    Kit_DoorFrame_Steel_DA envelope (A12: no stop, the same casing on both faces) + dark-bronze saddle
///    is a dormant proxy (DoorFramesLive), and the base stop and the halo still use that envelope;
///  - a window keeps main's DressWindow frame (Kit_WindowFrame_Steel, the Office member): no casing here;
///  - a free border wall end gets an Office end cap, an exposed Office outside corner a vinyl guard;
///  - every Office wall face gets a 0.10 rubber cove base.
/// On a renovated crossing the open edge keeps its portal in an UNFINISHED state (primer, galvanised
/// corner beads, compound spots, no bar), the arch is left to the renovation kit's bare reveal, the door
/// keeps its frame. Render-only, except one collider box per free post.
/// From the 10-08 frame kit (code/pick/FrontRoomsTransitionFrameKit.cs.txt), re-based on main + A12.
/// Coordinates: chunk-local metres. An edge runs from <c>start</c> along <c>along</c> for 3 m;
/// <c>across</c> points from cell a into cell b. A corner is the lattice point at the south-west
/// corner of cell <c>point</c>.
/// </summary>
public static partial class FrontRoomsTransitionKit
{
    /// <summary>
    /// D1.6 seam: FrontRoomsInteractableKit.DressDoor dresses every map door with its real _DA frame (live
    /// since 2026-10-29), so the frame kit builds no proxy sleeve or saddle; the base stop and the renovation
    /// halo stay. False brings the proxy back for a tool run on undressed doors.
    /// </summary>
    public static bool DoorFramesLive = true;

    public static class FrameKit
    {
        // ---------- units (ModuleUnits constants in the map contract, T7) ----------

        public const float WallHalf = ModuleUnits.WallHalf;
        /// <summary>Wall pieces beside an undressed border arch stop this short of the opening; the Office lining fills the gap.</summary>
        public const float BeadInset = .012f;
        /// <summary>Quarter-round corner bead radius, 8 segments.</summary>
        public const float Bead = .006f;
        public const int BeadSegments = 8;
        public const float PortalPilaster = .12f, PortalDepth = .20f, PortalHeaderDrop = ModuleUnits.BulkheadDepth;
        /// <summary>A free post in an arcade (a border opening runs on along that axis) is 0.24, else 0.20.</summary>
        public const float SharedPilaster = .24f, FreePostSize = .20f;
        public const float BarWidth = .035f, BarHeight = .005f;
        public const float WallAngle = .022f, WallAngleGauge = .0015f;
        public const float CoveHeight = ModuleUnits.CoveHeight, CoveProud = ModuleUnits.CoveProud, BaseStopGap = .005f;
        public const float GuardWing = .05f, GuardHeight = 1.22f, GuardGauge = .002f, GuardNose = .003f;
        // D1.6 Kit_DoorFrame_Steel_DA envelope at the F4 period size (research/door_frame/20_for_red.md, 50.8 x 11.1 mm), door root, S = +X:
        // casing S 0.0795 -> 0.0911 and P -0.0911 -> -0.0795 off the wall centre, 0.0488 beyond each jamb
        // (hinge -0.0488 -> 0.002, latch 0.998 -> 1.0488), jambs 0 -> 2.1488, head 2.098 -> 2.1488; no stop.
        public const float CasingFace = .0911f, CasingInner = .0795f, CasingReturn = .0488f, Soffit = .002f;
        public const float SaddleHalf = .076f, SaddleTop = .052f, SaddleHeight = .012f;
        const float CasingConvex = .003f;
        // Unfinished portal (renovated open edge): galvanised corner bead flange and nose, compound spot size.
        const float GalvFlange = .030f, GalvProud = .0035f, SpotOff = .0016f;

        // ---------- counters (reset by tools) ----------

        public sealed class Stats
        {
            public int casedArches, portals, unfinishedPortals, portalPilasters, freePosts, sharedPilasters, doorFrames, endCaps, guards, bars, saddles, baseRuns;
            public float baseMetres, barMetres;
            public int officeWallTris, kitTris;

            public void Reset()
            {
                casedArches = portals = unfinishedPortals = portalPilasters = freePosts = sharedPilasters = doorFrames = endCaps = guards = bars = saddles = baseRuns = 0;
                baseMetres = barMetres = 0f;
                officeWallTris = kitTris = 0;
            }

            public override string ToString() =>
                "cased arches " + casedArches + ", portals " + portals + " (unfinished " + unfinishedPortals + ", pilasters " + portalPilasters + "), free posts " + freePosts
                + " (shared " + sharedPilasters + "), door frames " + doorFrames + ", end caps " + endCaps + ", corner guards " + guards
                + ", bars " + bars + " (" + barMetres.ToString("0.0") + " m), saddles " + saddles + ", base runs " + baseRuns + " (" + baseMetres.ToString("0.0") + " m)"
                + ", tris: Office_Wall merged " + officeWallTris + ", kit renderers " + kitTris;
        }

        public static readonly Stats Count = new Stats();

        /// <summary>Tools: every border piece built since the last Clear (kind, cells, centre and width of the opening).</summary>
        public static readonly List<string> Built = new List<string>();
        public static bool LogBuilt;

        static void Note(TransitionEdge e, string what)
        {
            if (!LogBuilt) return;
            Built.Add(what + " " + e.kind + " " + e.a + "|" + e.b + " " + e.themeA + "|" + e.themeB + " start " + e.start.ToString("F2") + " along " + e.along.ToString("F0")
                + " centre " + e.centre.ToString("0.000") + " width " + e.width.ToString("0.000") + " top " + e.top.ToString("0.00") + " h " + e.height.ToString("0.0")
                + (e.renovated ? " renovated" : ""));
        }

        // ---------- descriptors (filled by ChunkBuild from the map's view) ----------

        /// <summary>What meets at one lattice point. Arms: 0 W (-x), 1 E (+x), 2 S (-z), 3 N (+z). Quadrants: 0 SW, 1 SE, 2 NW, 3 NE.</summary>
        public sealed class TransitionCorner
        {
            public GridCoord point;
            public Vector3 at;
            public readonly EdgeKind[] arm = new EdgeKind[4];
            public readonly ZoneTheme[] theme = new ZoneTheme[4];
            public readonly float[] ceiling = new float[4];
            public readonly Material[] wall = new Material[4];
            public readonly float[] armHeight = new float[4];
            /// <summary>Touches an excluded cell (the start area): left exactly as today.</summary>
            public bool special;

            public bool Has(int dir) => arm[dir] != EdgeKind.Open;

            public int Arms
            {
                get
                {
                    var n = 0;
                    for (var d = 0; d < 4; d++) if (Has(d)) n++;
                    return n;
                }
            }

            /// <summary>The four cells do not share one wall finish.</summary>
            public bool Mixed => wall[0] != wall[1] || wall[0] != wall[2] || wall[0] != wall[3];

            /// <summary>The two quadrants an arm separates.</summary>
            public static void Sides(int dir, out int q0, out int q1)
            {
                switch (dir)
                {
                    case 0: q0 = 0; q1 = 2; break;   // W: SW | NW
                    case 1: q0 = 1; q1 = 3; break;   // E: SE | NE
                    case 2: q0 = 0; q1 = 1; break;   // S: SW | SE
                    default: q0 = 2; q1 = 3; break;  // N: NW | NE
                }
            }

            public bool Border(int dir)
            {
                Sides(dir, out var q0, out var q1);
                return theme[q0] != theme[q1];
            }

            public bool BorderOpen(int dir) => !Has(dir) && Border(dir);

            public bool AnyBorderOpen()
            {
                for (var d = 0; d < 4; d++) if (BorderOpen(d)) return true;
                return false;
            }
        }

        public sealed class TransitionEdge
        {
            public EdgeKind kind;
            public GridCoord a, b;
            public ZoneTheme themeA, themeB;
            /// <summary>Each side's own ceiling, and the wall's height (the taller).</summary>
            public float heightA, heightB, height;
            /// <summary>The opening exactly as the map built it (width, centre along the edge, top, sill).</summary>
            public float width, centre, top, sill;
            public Vector3 start, along, across;
            public Material officeWall;
            public TransitionCorner c0, c1;
            /// <summary>The map's BorderRenovated for this crossing.</summary>
            public bool renovated;

            public bool Border => themeA != themeB;
            public Vector3 P(float u, float y, float w) => start + along * u + Vector3.up * y + across * w;
        }

        /// <summary>Where the kit's meshes go: Office-wall pieces into their block's wall mesh, everything else one renderer per chunk and material (no property block).</summary>
        public interface ISink
        {
            TransitionMesh Mesh(Material material, float ceiling, bool castShadows);
            /// <summary>The base's mesh: its own renderer on desktop; the block's Cove_Base trim mesh on the reduced tier.</summary>
            TransitionMesh BaseMesh(float ceiling);
            /// <summary>A collider box (chunk-local centre and size). Free posts only.</summary>
            void Collider(Vector3 centre, Vector3 size);
        }

        // ---------- sections ----------

        /// <summary>Office lining round a reveal: face strips (depth deep) and 6 mm quarter-round beads on both arrises.</summary>
        static Profile Lining(float half, float depth)
        {
            var p = new Profile();
            var r = Bead;
            if (depth > r) p.Line(new Vector2(depth, -half), new Vector2(r, -half), new Vector2(0f, -1f));
            p.Arc(new Vector2(r, -half + r), r, new Vector2(0f, -1f), new Vector2(-1f, 0f), BeadSegments);
            p.Line(new Vector2(0f, -half + r), new Vector2(0f, half - r), new Vector2(-1f, 0f));
            p.Arc(new Vector2(r, half - r), r, new Vector2(-1f, 0f), new Vector2(0f, 1f), BeadSegments);
            if (depth > r) p.Line(new Vector2(r, half), new Vector2(depth, half), new Vector2(0f, 1f));
            return p;
        }

        static Profile barProfile, saddleProfile, baseProfile, wallAngleProfile, guardProfile, sleeveProfile, galvProfile;
        static Profile saddleFlat, baseFlat, sleeveCoarse;

        /// <summary>The reduced tier for the chunk being built (set by ChunkBuild): a flat saddle, a square base, single-segment fillets. Desktop never.</summary>
        internal static bool R;

        /// <summary>Aluminium binder bar: 35 mm wide, 5 mm high, rounded top (s = up, w = across the line).</summary>
        static Profile BarProfile()
        {
            if (barProfile != null) return barProfile;
            var h = BarWidth * .5f;
            var poly = new List<Vector2> { new Vector2(0f, h), new Vector2(.0022f, h - .0012f), new Vector2(BarHeight, h - .0065f), new Vector2(BarHeight, -h + .0065f), new Vector2(.0022f, -h + .0012f), new Vector2(0f, -h) };
            var radii = new List<float> { 0f, .0015f, .003f, .003f, .0015f, 0f };
            barProfile = Profile.Rounded(poly, radii, false, 4);
            return barProfile;
        }

        /// <summary>Fluted saddle: 152 x 12 mm, 1:2 bevels, 104 mm flat top with 13 flutes (D1.6: 0 -> 0.012, flat top +-0.052).</summary>
        static Profile SaddleProfile()
        {
            if (R)
            {
                // Reduced tier: the same envelope (+-0.076, 0.012, flat top +-0.052), no flutes.
                if (saddleFlat == null) saddleFlat = Profile.Rounded(new List<Vector2> { new Vector2(0f, SaddleHalf), new Vector2(SaddleHeight, SaddleTop), new Vector2(SaddleHeight, -SaddleTop), new Vector2(0f, -SaddleHalf) }, new List<float> { 0f, 0f, 0f, 0f }, false, 1);
                return saddleFlat;
            }
            if (saddleProfile != null) return saddleProfile;
            var poly = new List<Vector2> { new Vector2(0f, SaddleHalf), new Vector2(SaddleHeight, SaddleTop) };
            var radii = new List<float> { 0f, .0015f };
            const int flutes = 13;
            var pitch = SaddleTop * 2f / flutes;
            for (var f = 0; f < flutes; f++)
            {
                var w0 = SaddleTop - f * pitch;
                poly.Add(new Vector2(SaddleHeight - .0006f, w0 - pitch * .5f));
                radii.Add(.0012f);
                if (f < flutes - 1) { poly.Add(new Vector2(SaddleHeight, w0 - pitch)); radii.Add(.0008f); }
            }
            poly.Add(new Vector2(SaddleHeight, -SaddleTop));
            radii.Add(.0015f);
            poly.Add(new Vector2(0f, -SaddleHalf));
            radii.Add(0f);
            saddleProfile = Profile.Rounded(poly, radii, false, 2);
            return saddleProfile;
        }

        /// <summary>Rubber cove base, 100 mm, 3.2 mm thick, toe out to 6 mm (s = off the wall, w = up).</summary>
        static Profile BaseProfile()
        {
            if (R)
            {
                // Reduced tier: the 0.10 x 6 mm envelope without the cove fillets.
                if (baseFlat == null)
                {
                    baseFlat = Profile.Rounded(new List<Vector2> { new Vector2(0f, CoveHeight), new Vector2(.0032f, CoveHeight), new Vector2(.0032f, .0015f), new Vector2(CoveProud, 0f) }, new List<float> { 0f, 0f, 0f, 0f }, false, 1);
                    baseFlat.extraCap.Add(new Vector2(0f, 0f));
                }
                return baseFlat;
            }
            if (baseProfile != null) return baseProfile;
            var poly = new List<Vector2> { new Vector2(0f, CoveHeight), new Vector2(.0032f, CoveHeight - .0035f), new Vector2(.0032f, .0015f), new Vector2(CoveProud, .0015f), new Vector2(CoveProud, 0f) };
            var radii = new List<float> { 0f, .001f, .0026f, 0f, 0f };
            baseProfile = Profile.Rounded(poly, radii, false, 2);
            baseProfile.extraCap.Add(new Vector2(0f, 0f));
            return baseProfile;
        }

        /// <summary>White steel wall angle, 22 x 22 x 1.5 mm (s = off the header face, w = up from the ceiling plane).</summary>
        static Profile WallAngleProfile()
        {
            if (wallAngleProfile != null) return wallAngleProfile;
            var poly = new List<Vector2> { new Vector2(0f, -WallAngle), new Vector2(WallAngleGauge, -WallAngle), new Vector2(WallAngleGauge, -WallAngleGauge), new Vector2(WallAngle, -WallAngleGauge), new Vector2(WallAngle, 0f) };
            poly.Reverse();
            var radii = new List<float> { 0f, .0004f, 0f, .0004f, 0f };
            wallAngleProfile = Profile.Rounded(poly, radii, false, 2);
            wallAngleProfile.extraCap.Add(new Vector2(0f, 0f));
            return wallAngleProfile;
        }

        /// <summary>Vinyl corner guard: two 50 mm wings, 2 mm, 3 mm nose (s along face 1's normal, w along face 2's normal; corner at 0).</summary>
        static Profile GuardProfile()
        {
            if (guardProfile != null) return guardProfile;
            var t = GuardGauge;
            var poly = new List<Vector2> { new Vector2(-GuardWing, 0f), new Vector2(-GuardWing, t), new Vector2(t, t), new Vector2(t, -GuardWing), new Vector2(0f, -GuardWing) };
            var radii = new List<float> { 0f, .0005f, GuardNose, .0005f, 0f };
            guardProfile = Profile.Rounded(poly, radii, false, 6);
            guardProfile.extraCap.Add(new Vector2(0f, 0f));
            return guardProfile;
        }

        /// <summary>
        /// The double-acting pressed-steel sleeve (A12, D1.6 DA envelope): the casing returns 0.0488 onto both
        /// faces, the soffit is flat (no stop, no push side), so the section is symmetric across the wall.
        /// </summary>
        static Profile SleeveDA()
        {
            if (R && sleeveCoarse != null) return sleeveCoarse;
            if (!R && sleeveProfile != null) return sleeveProfile;
            var poly = new List<Vector2>();
            var radii = new List<float>();
            void Pt(float s, float x, float r) { poly.Add(new Vector2(s, x)); radii.Add(r); }
            Pt(CasingReturn, -CasingInner, 0f);
            Pt(CasingReturn, -CasingFace, CasingConvex);
            Pt(-Soffit, -CasingFace, CasingConvex);
            Pt(-Soffit, CasingFace, CasingConvex);
            Pt(CasingReturn, CasingFace, CasingConvex);
            Pt(CasingReturn, CasingInner, 0f);
            if (R) return sleeveCoarse = Profile.Rounded(poly, radii, false, 1);
            sleeveProfile = Profile.Rounded(poly, radii, false, 3);
            return sleeveProfile;
        }

        /// <summary>
        /// Galvanised corner bead round a sharp arris at the profile origin. Sweep frame: s runs out of face 2
        /// (the reveal, the sweep's outward vector), w out of face 1 (the sweep's fixed axis). 30 mm flanges
        /// 3.5 mm proud on both faces and a round nose (the renovation kit's iteration-6 bead, as a profile).
        /// </summary>
        static Profile GalvBead()
        {
            if (galvProfile != null) return galvProfile;
            const float fp = GalvProud;
            var pts = new List<Vector2> { new Vector2(-GalvFlange, 0f), new Vector2(-GalvFlange, fp), new Vector2(-.008f, fp) };
            for (var i = 0; i <= 6; i++)
            {
                var t = Mathf.PI * .5f * i / 6f;
                pts.Add(new Vector2(fp * Mathf.Sin(t), fp * Mathf.Cos(t)));
            }
            pts.Add(new Vector2(fp, -.008f));
            pts.Add(new Vector2(fp, -GalvFlange));
            pts.Add(new Vector2(0f, -GalvFlange));
            var radii = new List<float>();
            for (var i = 0; i < pts.Count; i++) radii.Add(0f);
            galvProfile = Profile.Rounded(pts, radii, false, 2);
            return galvProfile;
        }

        // ---------- materials (all in main except Trans_AluminiumSatin) ----------

        static Material Mat(string name) => FrontRoomsSurfaces.Get(name);
        static Material Steel => Mat("Prop_SteelBrown");
        // Satin variant of Prop_Aluminium (same maps, FrontRoomsRenderSetup row); falls back to Prop_Aluminium.
        static Material Alu => FrontRoomsSurfaces.TryGet("Trans_AluminiumSatin") ?? Mat("Prop_Aluminium");
        static Material Galv => Mat("Prop_Aluminium");
        // White enamel wall angle: Prop_PlasticWhite is already loaded by the office kit.
        static Material Angle => Mat("Prop_PlasticWhite");
        static Material Vinyl => Mat("Prop_PlasticBeige");
        static Material Primer => FrontRoomsSurfaces.TryGet("Wall_Primer") ?? Mat("Prop_PlasticWhite");
        static Material Compound => FrontRoomsSurfaces.TryGet("Wall_Compound") ?? Mat("Prop_PlasticWhite");

        static void Tally(Material material, int tris, Material officeWall)
        {
            if (material == officeWall) Count.officeWallTris += tris;
            else Count.kitTris += tris;
        }

        // ---------- edges ----------

        /// <summary>The BuildEdge hook: a theme-border edge gets its piece.</summary>
        public static void Edge(TransitionEdge e, ISink sink)
        {
            if (sink == null || !e.Border) return;
            Note(e, "border");
            switch (e.kind)
            {
                case EdgeKind.Arch:
                    if (!e.renovated) CasedArch(e, sink);
                    break;
                case EdgeKind.Open:
                    if (!e.renovated) Portal(e, sink, false);
                    else if (!BareRenovatedOpen) Portal(e, sink, true);
                    break;
                case EdgeKind.Door:
                    if (!DoorFramesLive) DoorFrame(e, sink);
                    break;
                // Window: main's DressWindow frames every map window (the Office member, Kit_WindowFrame_Steel).
            }
        }

        static float OfficeCeiling(TransitionEdge e) => e.themeA == ZoneTheme.Office ? e.heightA : e.heightB;

        static void CasedArch(TransitionEdge e, ISink sink)
        {
            var cL = e.centre - e.width * .5f;
            var cR = e.centre + e.width * .5f;
            var T = e.top;
            var wall = sink.Mesh(e.officeWall, OfficeCeiling(e), true);
            var path = new[] { e.P(cL, 0f, 0f), e.P(cL, T, 0f), e.P(cR, T, 0f), e.P(cR, 0f, 0f) };
            Tally(e.officeWall, Sweep(wall, path, false, e.across, -e.along, Lining(WallHalf, BeadInset)), e.officeWall);
            Bar(e, sink, cL, cR);
            Count.casedArches++;
        }

        static void Bar(TransitionEdge e, ISink sink, float u0, float u1)
        {
            var m = sink.Mesh(Alu, e.height, false);
            var tris = Sweep(m, new[] { e.P(u0, 0f, 0f), e.P(u1, 0f, 0f) }, false, e.across, Vector3.up, BarProfile(), 0f, 0f, true, true);
            Tally(Alu, tris, e.officeWall);
            Count.bars++;
            Count.barMetres += u1 - u0;
        }

        /// <summary>How far from a corner a portal's clear span starts, and whether a pilaster stands there (a wall meets the corner) or a free post.</summary>
        static void PortalEnd(TransitionCorner c, Vector3 lineAxis, out float back, out float face, out bool pilaster)
        {
            if (c.special || c.Arms > 0)
            {
                pilaster = true;
                back = WallHalf;
                face = WallHalf + PortalPilaster;
                return;
            }
            pilaster = false;
            back = face = FreePostHalf(c, Mathf.Abs(lineAxis.x) > .5f);
        }

        /// <summary>Half-size of the free post along x (alongX) or z: 0.12 if a border opening runs along that axis, else 0.10.</summary>
        static float FreePostHalf(TransitionCorner c, bool alongX)
        {
            var along = alongX ? c.BorderOpen(0) || c.BorderOpen(1) : c.BorderOpen(2) || c.BorderOpen(3);
            return along ? SharedPilaster * .5f : FreePostSize * .5f;
        }

        /// <summary>
        /// A portal on a border open edge. <paramref name="unfinished"/>: the renovated state, the same
        /// pilasters and header faced in Wall_Primer with galvanised corner beads and compound spots, no bar
        /// (the Level 0 carpet is cut back and the Office tiles run to the line) and no base on the pilasters.
        /// </summary>
        static void Portal(TransitionEdge e, ISink sink, bool unfinished)
        {
            var L = MapGrid.CellSize;
            var H = e.height;
            var D = PortalHeaderDrop;
            var hd = PortalDepth * .5f;
            var r = Bead;
            PortalEnd(e.c0, e.along, out var backL, out var faceL, out var jambL);
            PortalEnd(e.c1, e.along, out var backR, out var faceR, out var jambR);
            float uStart = backL, uEnd = L - backR, pL = faceL, pR = L - faceR;
            var skin = unfinished ? Primer : e.officeWall;
            var wall = sink.Mesh(skin, H, true);
            var tris = 0;

            // Reveal: pilaster inner faces and the header soffit, beaded.
            var path = new List<Vector3>();
            if (jambL) path.Add(e.P(pL, 0f, 0f));
            path.Add(e.P(pL, H - D, 0f));
            path.Add(e.P(pR, H - D, 0f));
            if (jambR) path.Add(e.P(pR, 0f, 0f));
            tris += Sweep(wall, path, false, e.across, jambL ? -e.along : Vector3.up, Lining(hd, 0f));

            // Flat faces on both sides (w = +-0.10): pilaster faces full height, header face above the bead.
            foreach (var sg in new[] { -1f, 1f })
            {
                var n = e.across * sg;
                var w = sg * hd;
                var hx0 = jambL ? pL - r : uStart;
                var hx1 = jambR ? pR + r : uEnd;
                if (jambL) wall.Quad(e.P(uStart, 0f, w), e.P(pL - r, 0f, w), e.P(pL - r, H, w), e.P(uStart, H, w), n);
                wall.Quad(e.P(hx0, H - D + r, w), e.P(hx1, H - D + r, w), e.P(hx1, H, w), e.P(hx0, H, w), n);
                if (!jambL) wall.Quad(e.P(uStart, H - D, w), e.P(uStart + .0001f, H - D, w), e.P(uStart + .0001f, H - D + r, w), e.P(uStart, H - D + r, w), n);
                if (jambR) wall.Quad(e.P(pR + r, 0f, w), e.P(uEnd, 0f, w), e.P(uEnd, H, w), e.P(pR + r, H, w), n);
                tris += (jambL ? 2 : 0) + 2 + (jambR ? 2 : 0);
            }
            // Back faces where a pilaster stands proud of a narrower wall end.
            if (jambL) { wall.Quad(e.P(uStart, 0f, -hd), e.P(uStart, 0f, hd), e.P(uStart, H, hd), e.P(uStart, H, -hd), -e.along); tris += 2; }
            if (jambR) { wall.Quad(e.P(uEnd, 0f, -hd), e.P(uEnd, 0f, hd), e.P(uEnd, H, hd), e.P(uEnd, H, -hd), e.along); tris += 2; }
            Tally(skin, tris, e.officeWall);

            // White wall angles where both ceiling grids die into the header.
            var angle = sink.Mesh(Angle, H, false);
            foreach (var sg in new[] { -1f, 1f })
                Tally(Angle, Sweep(angle, new[] { e.P(uStart, H, sg * hd), e.P(uEnd, H, sg * hd) }, false, Vector3.up, e.across * sg, WallAngleProfile(), 0f, 0f, true, true), e.officeWall);

            if (unfinished)
            {
                UnfinishedDetail(e, sink, jambL, jambR, pL, pR, uStart, uEnd, H - D, hd);
                Count.unfinishedPortals++;
            }
            else
            {
                Bar(e, sink, pL, pR);
                if (jambL) PilasterBase(e, sink, uStart, pL, 1f);
                if (jambR) PilasterBase(e, sink, uEnd, pR, -1f);
            }
            if (jambL) Count.portalPilasters++;
            if (jambR) Count.portalPilasters++;
            Count.portals++;
        }

        /// <summary>The unfinished portal's galvanised beads on every reveal arris and its compound screw spots.</summary>
        static void UnfinishedDetail(TransitionEdge e, ISink sink, bool jambL, bool jambR, float pL, float pR, float uStart, float uEnd, float soffit, float hd)
        {
            var galv = sink.Mesh(Galv, e.height, false);
            var tris = 0;
            foreach (var sg in new[] { -1f, 1f })
            {
                var nf = e.across * sg;
                // Vertical arrises: face 1 = the pilaster face (normal nf), face 2 = the reveal (normal into the opening).
                if (jambL) tris += Sweep(galv, new[] { e.P(pL, 0f, sg * hd), e.P(pL, soffit, sg * hd) }, false, nf, e.along, GalvBead(), 0f, 0f, true, true);
                if (jambR) tris += Sweep(galv, new[] { e.P(pR, 0f, sg * hd), e.P(pR, soffit, sg * hd) }, false, nf, -e.along, GalvBead(), 0f, 0f, true, true);
                // The header's soffit arris on this face (face 2 = the soffit, normal down).
                tris += Sweep(galv, new[] { e.P(jambL ? pL : uStart, soffit, sg * hd), e.P(jambR ? pR : uEnd, soffit, sg * hd) }, false, nf, Vector3.down, GalvBead(), 0f, 0f, true, true);
            }
            Tally(Galv, tris, e.officeWall);
            // Compound over the screw heads: 40 mm spots every 0.40 m on each pilaster face and the header faces.
            var mud = sink.Mesh(Compound, e.height, false);
            var spots = 0;
            foreach (var sg in new[] { -1f, 1f })
            {
                var n = e.across * sg;
                var w = sg * (hd + SpotOff);
                void Spot(float u, float y)
                {
                    const float s = .02f;
                    mud.Quad(e.P(u - s, y - s, w), e.P(u + s, y - s, w), e.P(u + s, y + s, w), e.P(u - s, y + s, w), n);
                    spots++;
                }
                for (var y = .30f; y < e.height - .05f; y += .40f)
                {
                    if (jambL) Spot((uStart + pL) * .5f, y);
                    if (jambR) Spot((pR + uEnd) * .5f, y);
                }
                for (var u = (jambL ? pL : uStart) + .20f; u < (jambR ? pR : uEnd) - .10f; u += .40f) Spot(u, (soffit + e.height) * .5f);
            }
            Count.kitTris += spots * 2;
        }

        /// <summary>Cove base round a portal pilaster: both sides and the inner face (it is an Office object on both sides).</summary>
        static void PilasterBase(TransitionEdge e, ISink sink, float uBack, float uFace, float dirSign)
        {
            var hd = PortalDepth * .5f;
            BaseRun(sink, e.height, e.P(uBack, 0f, hd), e.P(uFace, 0f, hd), e.across, BaseEnd.Flat, BaseEnd.Convex);
            BaseRun(sink, e.height, e.P(uFace, 0f, hd), e.P(uFace, 0f, -hd), e.along * dirSign, BaseEnd.Convex, BaseEnd.Convex);
            BaseRun(sink, e.height, e.P(uFace, 0f, -hd), e.P(uBack, 0f, -hd), -e.across, BaseEnd.Convex, BaseEnd.Flat);
        }

        /// <summary>The steel DA sleeve and its saddle (no stop: the leaf swings through both faces).</summary>
        static void DoorFrame(TransitionEdge e, ISink sink)
        {
            var cL = e.centre - e.width * .5f;
            var cR = e.centre + e.width * .5f;
            var T = e.top;
            var steel = sink.Mesh(Steel, OfficeCeiling(e), true);
            var path = new[] { e.P(cL, 0f, 0f), e.P(cL, T, 0f), e.P(cR, T, 0f), e.P(cR, 0f, 0f) };
            Tally(Steel, Sweep(steel, path, false, e.across, -e.along, SleeveDA()), e.officeWall);
            // Saddle between the soffits (D1.6: 0.002 -> 0.998), in the frame's own dark-bronze slot (D1.6 s3, merge M1).
            Tally(Steel, Sweep(steel, new[] { e.P(cL + Soffit, 0f, 0f), e.P(cR - Soffit, 0f, 0f) }, false, e.across, Vector3.up, SaddleProfile(), 0f, 0f, true, true), e.officeWall);
            Count.saddles++;
            Count.doorFrames++;
        }

        // ---------- Office base ----------

        enum BaseEnd { Flat, Convex, Cap }

        static void BaseRun(ISink sink, float ceiling, Vector3 p0, Vector3 p1, Vector3 outward, BaseEnd e0, BaseEnd e1)
        {
            var length = (p1 - p0).magnitude;
            if (length < .002f) return;
            var m = sink.BaseMesh(ceiling);
            var tris = Sweep(m, new[] { p0, p1 }, false, Vector3.up, outward, BaseProfile(),
                e0 == BaseEnd.Convex ? 1f : 0f, e1 == BaseEnd.Convex ? 1f : 0f, e0 == BaseEnd.Cap, e1 == BaseEnd.Cap);
            Count.kitTris += tris;
            Count.baseRuns++;
            Count.baseMetres += length;
        }

        static int DirIndex(Vector3 d) => Mathf.Abs(d.x) > .5f ? (d.x < 0f ? 0 : 1) : (d.z < 0f ? 2 : 3);

        /// <summary>Where the side-q face run of an edge ends at corner c (u measured from that corner into the edge).</summary>
        static void FaceEnd(TransitionEdge e, TransitionCorner c, bool atStart, float q, out float u, out BaseEnd end)
        {
            var dd = atStart ? e.along : -e.along;
            var perp = DirIndex(e.across * q);
            var col = DirIndex(-dd);
            if (c.special) { u = 0f; end = BaseEnd.Flat; return; }
            if (c.Has(perp)) { u = WallHalf; end = BaseEnd.Flat; return; }                          // inside corner
            if (c.BorderOpen(perp)) { u = PortalDepth * .5f; end = BaseEnd.Flat; return; }          // a portal pilaster stands on this face
            if (c.Has(col)) { u = 0f; end = BaseEnd.Flat; return; }                                 // the run goes on along the next edge
            if (c.BorderOpen(col)) { u = -WallHalf; end = BaseEnd.Flat; return; }                   // a portal pilaster stands on the wall end
            u = -WallHalf;                                                                          // outside corner or free end: wrap
            end = BaseEnd.Convex;
        }

        /// <summary>The base on every Office face of a built edge (border or not).</summary>
        public static void Base(TransitionEdge e, ISink sink)
        {
            if (sink == null || e.kind == EdgeKind.Open) return;
            var L = MapGrid.CellSize;
            foreach (var q in new[] { -1f, 1f })
            {
                var theme = q < 0f ? e.themeA : e.themeB;
                if (theme != ZoneTheme.Office) continue;
                var otherOffice = (q < 0f ? e.themeB : e.themeA) == ZoneTheme.Office;
                var ceiling = q < 0f ? e.heightA : e.heightB;
                FaceEnd(e, e.c0, true, q, out var u0, out var end0);
                FaceEnd(e, e.c1, false, q, out var du1, out var end1);
                var u1 = L - du1;
                var w = q * WallHalf;
                var outward = e.across * q;
                Vector3 F(float u) => e.P(u, 0f, w);
                var cL = e.centre - e.width * .5f;
                var cR = e.centre + e.width * .5f;
                switch (e.kind)
                {
                    case EdgeKind.Door:
                    {
                        // The base stops 5 mm short of the DA casing (0.0488 beyond each jamb), border door or not. Every door with an
                        // Office face wears the steel frame (DoorMemberFor); the EXIT's own 50.8 mm frame leaves a 3 mm gap.
                        var gap = CasingReturn + BaseStopGap;
                        BaseRun(sink, ceiling, F(u0), F(cL - gap), outward, end0, BaseEnd.Cap);
                        BaseRun(sink, ceiling, F(cR + gap), F(u1), outward, BaseEnd.Cap, end1);
                        break;
                    }
                    case EdgeKind.Arch:
                    {
                        BaseRun(sink, ceiling, F(u0), F(cL), outward, end0, BaseEnd.Convex);
                        BaseRun(sink, ceiling, F(cR), F(u1), outward, BaseEnd.Convex, end1);
                        // Into the reveals: wrap from this face; stop at the other face's plane unless it is Office too (then build once).
                        // A renovated border arch has no lining yet: the base stops at the arris.
                        if ((!otherOffice || q > 0f) && !e.renovated)
                        {
                            var far = otherOffice ? BaseEnd.Convex : BaseEnd.Cap;
                            BaseRun(sink, ceiling, e.P(cL, 0f, w), e.P(cL, 0f, -w), e.along, BaseEnd.Convex, far);
                            BaseRun(sink, ceiling, e.P(cR, 0f, w), e.P(cR, 0f, -w), -e.along, BaseEnd.Convex, far);
                        }
                        break;
                    }
                    default:
                        BaseRun(sink, ceiling, F(u0), F(u1), outward, end0, end1);
                        break;
                }
            }
        }

        // ---------- corners ----------

        /// <summary>A free wall end on a border (one split arm, no portal against its end) gets an Office end cap; the map insets that end by BeadInset (render only).</summary>
        public static bool EndCapAt(TransitionCorner c)
        {
            if (c == null || c.special || c.Arms != 1) return false;
            var dir = 0;
            for (var d = 0; d < 4; d++) if (c.Has(d)) dir = d;
            if (!c.Border(dir)) return false;
            var opposite = dir ^ 1;
            return !c.BorderOpen(opposite);
        }

        static Vector3 DirVec(int dir) => dir == 0 ? Vector3.left : dir == 1 ? Vector3.right : dir == 2 ? Vector3.back : Vector3.forward;

        static int Quad(float sx, float sz) => (sx > 0f ? 1 : 0) + (sz > 0f ? 2 : 0);

        /// <summary>The corner hook: free posts, end caps, corner guards and their base.</summary>
        public static void Corner(TransitionCorner c, ISink sink, Material officeWall)
        {
            if (sink == null || c.special) return;
            var arms = c.Arms;
            if (arms == 0)
            {
                if (c.AnyBorderOpen()) { FreePost(c, sink, officeWall); if (LogBuilt) Built.Add("post " + c.point.x + " " + c.point.y); }
                return;
            }
            if (arms == 1 && EndCapAt(c)) { EndCap(c, sink, officeWall); if (LogBuilt) Built.Add("endcap " + c.point.x + " " + c.point.y); }
            if (arms == 2 && LGuard(c, sink) && LogBuilt) Built.Add("guard " + c.point.x + " " + c.point.y);
        }

        static void FreePost(TransitionCorner c, ISink sink, Material officeWall)
        {
            var hx = FreePostHalf(c, true);
            var hz = FreePostHalf(c, false);
            var H = 0f;
            for (var q = 0; q < 4; q++) H = Mathf.Max(H, c.ceiling[q]);
            var wall = sink.Mesh(officeWall, H, true);
            var poly = new List<Vector2> { new Vector2(hx, hz), new Vector2(hx, -hz), new Vector2(-hx, -hz), new Vector2(-hx, hz) };
            var prof = Profile.Rounded(poly, new List<float> { Bead, Bead, Bead, Bead }, true, BeadSegments);
            Tally(officeWall, Sweep(wall, new[] { c.at, c.at + Vector3.up * H }, false, Vector3.forward, Vector3.right, prof), officeWall);
            // Base round all four faces.
            Vector3 Pt(float x, float z) => c.at + new Vector3(x, 0f, z);
            BaseRun(sink, H, Pt(hx, -hz), Pt(hx, hz), Vector3.right, BaseEnd.Convex, BaseEnd.Convex);
            BaseRun(sink, H, Pt(hx, hz), Pt(-hx, hz), Vector3.forward, BaseEnd.Convex, BaseEnd.Convex);
            BaseRun(sink, H, Pt(-hx, hz), Pt(-hx, -hz), Vector3.left, BaseEnd.Convex, BaseEnd.Convex);
            BaseRun(sink, H, Pt(-hx, -hz), Pt(hx, -hz), Vector3.back, BaseEnd.Convex, BaseEnd.Convex);
            // LT16 default: a free post gets a collider (the only collision the kits add).
            sink.Collider(c.at + Vector3.up * (H * .5f), new Vector3(hx * 2f, H, hz * 2f));
            Count.freePosts++;
            var straight = (c.BorderOpen(0) && c.BorderOpen(1)) || (c.BorderOpen(2) && c.BorderOpen(3));
            if (straight) Count.sharedPilasters++;
        }

        static void EndCap(TransitionCorner c, ISink sink, Material officeWall)
        {
            var dir = 0;
            for (var d = 0; d < 4; d++) if (c.Has(d)) dir = d;
            var ad = DirVec(dir);
            var aa = dir < 2 ? Vector3.forward : Vector3.right;
            var H = c.armHeight[dir];
            // Which side of the arm is Office: the quadrant toward +aa or -aa.
            TransitionCorner.Sides(dir, out var q0, out var q1);
            var qPlus = Quad(ad.x + aa.x, ad.z + aa.z);
            var officeSide = c.theme[qPlus] == ZoneTheme.Office ? 1f : -1f;
            var officeCeiling = c.ceiling[officeSide > 0f ? qPlus : (qPlus == q0 ? q1 : q0)];
            var plane = c.at - ad * WallHalf;
            var wall = sink.Mesh(officeWall, officeCeiling, true);
            Tally(officeWall, Sweep(wall, new[] { plane, plane + Vector3.up * H }, false, aa, ad, Lining(WallHalf, BeadInset)), officeWall);
            Count.endCaps++;
            // Base across the cap face, wrapping from the Office face, stopping at the Level 0 face's plane.
            var perpOffice = DirIndex(aa * officeSide);
            BaseRun(sink, officeCeiling, plane + aa * (officeSide * WallHalf), plane - aa * (officeSide * WallHalf), -ad, BaseEnd.Convex, BaseEnd.Cap);
            // Corner guard on the Office arris, unless a portal pilaster stands on that face.
            if (!c.BorderOpen(perpOffice))
                Guard(sink, officeCeiling, plane + aa * (officeSide * WallHalf), aa * officeSide, -ad);
        }

        static bool LGuard(TransitionCorner c, ISink sink)
        {
            int ax = c.Has(0) ? 0 : c.Has(1) ? 1 : -1;
            int az = c.Has(2) ? 2 : c.Has(3) ? 3 : -1;
            if (ax < 0 || az < 0 || !c.Mixed || c.AnyBorderOpen()) return false;
            var sx = ax == 0 ? 1f : -1f;   // the outside quadrant lies away from both arms
            var sz = az == 2 ? 1f : -1f;
            var q = Quad(sx, sz);
            if (c.theme[q] != ZoneTheme.Office) return false;
            var corner = c.at + new Vector3(sx * WallHalf, 0f, sz * WallHalf);
            Guard(sink, c.ceiling[q], corner, new Vector3(sx, 0f, 0f), new Vector3(0f, 0f, sz));
            return true;
        }

        /// <summary>A beige vinyl corner guard on an outside corner whose faces point along n1 and n2, standing on the base.</summary>
        static void Guard(ISink sink, float ceiling, Vector3 corner, Vector3 n1, Vector3 n2)
        {
            var m = sink.Mesh(Vinyl, ceiling, false);
            var y0 = CoveHeight;
            var tris = Sweep(m, new[] { corner + Vector3.up * y0, corner + Vector3.up * (y0 + GuardHeight) }, false, n2, n1, GuardProfile(), 0f, 0f, true, true);
            Count.kitTris += tris;
            Count.guards++;
        }
    }
}
