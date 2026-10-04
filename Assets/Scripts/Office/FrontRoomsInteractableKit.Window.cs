using System;
using System.Collections.Generic;
using FrontRooms.Map;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>
/// The window part of the interactables kit facade (Documentation/research/interactables/10_spec.md §5 and §6.1;
/// 06_period_windows.md §3-§4; window_landing/03_contract_map.md). Owned by the visual session. The map calls
/// DressWindow through reflection, the way it calls FrontRoomsOfficeKit, so the map builds as today when this file is
/// absent. Rebased 2026-10-03 on the map chat's window model (commit df4cb03): the map owns the root
/// "Window {a}-{b}" (unscaled, opening centre on the wall line at floor level, +Z into cell b), the gameplay pane under
/// it, GlassBreakRecord, WindowBuilt / WindowReleased and the FrontRoomsGlassBreakable hook.
///
/// DressWindow(map, window, record): once per window edge, broken windows included, right after the map's breakable
/// hook. It hangs the frame member for the room side (the non-tall cell) from the root, turned so face A (the kit's
/// +Z) looks into the room:
///   Level 0 room -> W-L0 Kit_WindowFrame_Wood  (walnut back-office light)
///   Office room  -> W-OF Kit_WindowFrame_Steel (dark-bronze pressed-steel borrowed light)
/// Until GD3's FrontRoomsGlassBreakable exists (the map then still draws its 1.4 x 1.65 x 0.03 pane cube itself), an
/// intact window also gets the INTERIM glass: a 6 mm slab (1.391 x 1.642, edges 12 mm behind the stops, Glass_Window,
/// no shadow, FrontRoomsMetalGlassTarget) hung from the pane, so the map's own Kill(pane) at the shatter removes it.
/// The pane cube's renderer is hidden only once the slab exists. When the breakable exists the map has already hidden
/// the pane and the breakable owns the visible glass on the root: no interim slab is made.
///
/// All or nothing: if anything throws, what this call built is destroyed and the pane is left as the map made it;
/// the exception goes back to the map's guard, which keeps today's trims for that window and logs once.
/// Everything here is render-only: no colliders, no lights (00_map_constraints.md). Nothing visible enters the clear
/// opening X ±0.6835 x Y 0.3665-1.9835 except the glass; the stop band is the 16 mm inside the wall cut (S1, approved
/// by the map chat 2026-10-03).
/// </summary>
public static partial class FrontRoomsInteractableKit
{
    public enum WindowMember { Lobby, Office, Run, Exit }

    public const string WindowFrameLabel = "Window frame (kit)";
    public const string WindowGlassLabel = "Window glass";

    /// <summary>Tools and tests: make DressWindow throw (after the frame is spawned) for the windows it returns true for.</summary>
    public static Func<FrontRoomsMapWorld.Window, bool> FailWindowDressForTools { get; set; }

    /// <summary>The member for a room theme (the non-tall side). Run and Exit have no map windows yet.</summary>
    public static WindowMember WindowMemberFor(ZoneTheme roomTheme) => roomTheme == ZoneTheme.Office ? WindowMember.Office : WindowMember.Lobby;

    public static string WindowFrameAssetFor(WindowMember member) => WindowParts.FrameAsset[(int)member];

    /// <summary>
    /// Dress one map window: the frame on the root (and the interim glass while the map draws the pane itself).
    /// Returns true when a frame was hung, so the map leaves its own trims out for this window.
    /// </summary>
    public static bool DressWindow(FrontRoomsMapWorld map, FrontRoomsMapWorld.Window window, FrontRoomsMapWorld.GlassBreakRecord record)
    {
        if (map == null || map.Cache == null || window == null || window.root == null) return false;
        GameObject frame = null, slab = null;
        try
        {
            // A window always joins a Tall hall to a Low/Standard room (FrontRoomsMap.Resolve): the room is the non-tall side.
            var roomIsB = map.Cache.ZoneOf(window.a).height == ZoneHeight.Tall;
            var member = WindowMemberFor(map.Cache.ZoneOf(roomIsB ? window.b : window.a).theme);
            var turn = roomIsB ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
            frame = WindowParts.Frame(member, window.root, turn);
            if (FailWindowDressForTools != null && FailWindowDressForTools(window)) throw new InvalidOperationException("tools: forced window dress failure");
            // Interim glass, only while the map still draws its pane cube (no FrontRoomsGlassBreakable yet) and the pane stands.
            var paneRenderer = window.pane != null ? window.pane.GetComponent<MeshRenderer>() : null;
            if (paneRenderer != null && paneRenderer.enabled && record.stage < 3)
            {
                slab = WindowParts.InterimSlab(window.pane, paneRenderer.sharedMaterial);
                if (slab != null) paneRenderer.enabled = false;   // last: the cube's collider, name and mapping stay
            }
            return frame != null;
        }
        catch
        {
            WindowParts.Kill(slab);
            WindowParts.Kill(frame);
            throw;
        }
    }

    /// <summary>The window section (10_spec §5.2, sleeve mode, S1 stop band), in the window root frame, metres.</summary>
    public static class WindowSection
    {
        public const float OpeningHalf = .700f, OpeningSill = .350f, OpeningHead = 2.000f;     // the map's wall cut
        public const float LiningX = .6995f, LiningSill = .3505f, LiningHead = 1.9995f;        // lining / soffit visible face
        public const float StopX = .6835f, StopSill = .3665f, StopHead = 1.9835f;              // stop line = sight line
        public const float StopZIn = .006f, StopZOut = .022f;                                 // stops at Z ±(0.006 -> 0.022)
        public const float StopBand = LiningX - StopX;                                        // 0.016 inside the opening edge
        public const float GlassEdgeX = .6955f, GlassSill = .354f, GlassHead = 1.996f, GlassHalfZ = .003f;
        public const float FaceOuterX = .775f, FaceHead = 2.075f, WallFaceZ = .080f, CasingZ = .105f, CasingInnerZ = .1005f;
        public static readonly Vector3 GlassSlabSize = new Vector3(2f * GlassEdgeX, GlassHead - GlassSill, 2f * GlassHalfZ); // 1.391 x 1.642 x 0.006
        public static readonly Vector3 GlassSlabCentre = new Vector3(0f, (GlassSill + GlassHead) * .5f, 0f);                    // (0, 1.175, 0)
    }

    /// <summary>Window-only helpers, nested so the door and key partials (10_spec §6.1) can use the same short names.</summary>
    static class WindowParts
    {
        // ===== One-line swap: the kit asset per member (10_spec §9.4). When the FBX + JSON exist in Resources/Props/Models,
        // ===== they are spawned; until then a placeholder with the exact §5.2 section is built here.
        internal static readonly string[] FrameAsset = { "Kit_WindowFrame_Wood", "Kit_WindowFrame_Steel", "Kit_WindowFrame_Steel_Enamel", "Kit_WindowFrame_Alu" };

        // W-L0 glazing: period wood-stop glazing used bedding compound or putty, not a black gasket. The slot swaps in
        // when the visual chat registers it (Resources/Surfaces/Prop_Putty); until then the kit's Prop_Rubber stays.
        const string RubberSlot = "Prop_Rubber", PuttySlot = "Prop_Putty";

        internal static GameObject Frame(WindowMember member, Transform root, Quaternion turn)
        {
            var asset = FrameAsset[(int)member];
            GameObject frame = null;
            if (FrontRoomsKitLibrary.Has(asset))
            {
                frame = FrontRoomsKitLibrary.Spawn(asset, root, Vector3.zero, turn, null, false, WindowFrameLabel);
                if (frame != null)
                {
                    // Render-only, whatever the FBX carries.
                    foreach (var c in frame.GetComponentsInChildren<Collider>(true)) Kill(c);
                    if (member == WindowMember.Lobby && Putty != null) SwapSlot(frame, RubberSlot, Putty);
                }
            }
            if (frame == null) frame = Placeholder(member, root, turn);
            WebGLTier(frame);
            return frame;
        }

        static Material putty;
        static bool puttyLooked;

        /// <summary>Resources/Surfaces/Prop_Putty once the visual chat registers it, else null (looked up once).</summary>
        static Material Putty
        {
            get
            {
                if (putty == null && !puttyLooked) { puttyLooked = true; putty = FrontRoomsSurfaces.TryGet(PuttySlot); }
                return putty;
            }
        }

        static void SwapSlot(GameObject frame, string from, Material replacement)
        {
            foreach (var r in frame.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mats = r.sharedMaterials;
                var changed = false;
                for (var i = 0; i < mats.Length; i++)
                    if (mats[i] != null && mats[i].name == from) { mats[i] = replacement; changed = true; }
                if (changed) r.sharedMaterials = mats;
            }
        }

        // WebGL is a separate, reduced tier (never applied to the Editor, Mac or Windows): the frame draws its LOD1 mesh at
        // every distance and casts no shadow. Same materials, same section, same render-only rules.
        static void WebGLTier(GameObject frame)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (frame == null) return;
            var group = frame.GetComponent<LODGroup>();
            if (group != null && group.lodCount > 1) group.ForceLOD(1);
            foreach (var r in frame.GetComponentsInChildren<MeshRenderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;
#endif
        }

        /// <summary>
        /// The interim 6 mm slab under the pane cube (1.4 x 1.65 x 0.03, unrotated under the window root): same centre
        /// (0, 1.175, 0) in root space, 1.391 x 1.642 x 0.006, no collider, no shadow, the RT target.
        /// </summary>
        internal static GameObject InterimSlab(GameObject pane, Material fallback)
        {
            var filter = pane.GetComponent<MeshFilter>();
            var s = pane.transform.localScale;
            s = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
            if (filter == null || filter.sharedMesh == null || s.x < 1e-4f || s.y < 1e-4f || s.z < 1e-4f) return null;
            // Under the root the pane's thin axis is local Z; keep the old either-axis rule for a pane placed otherwise.
            var thinX = s.x < s.z;
            var size = WindowSection.GlassSlabSize;
            var slab = new GameObject(WindowGlassLabel);
            slab.transform.SetParent(pane.transform, false);
            slab.transform.localPosition = Vector3.zero;
            slab.transform.localRotation = Quaternion.identity;
            slab.transform.localScale = new Vector3((thinX ? size.z : size.x) / s.x, size.y / s.y, (thinX ? size.x : size.z) / s.z);
            slab.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;   // Unity's cube: what FrontRooms/Glass expects
            var r = slab.AddComponent<MeshRenderer>();
            var glass = WindowGlass;
            r.sharedMaterial = glass != null ? glass : fallback;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            slab.AddComponent<FrontRoomsMetalGlassTarget>();
            return slab;
        }

        static Material windowGlass;
        static bool windowGlassLooked;

        /// <summary>Resources/Surfaces/Glass_Window (the glass track's G1/G3), or null before it lands.</summary>
        static Material WindowGlass
        {
            get
            {
                if (windowGlass == null && !windowGlassLooked)
                {
                    windowGlassLooked = true;
                    windowGlass = Resources.Load<Material>("Surfaces/Glass_Window");
                }
                return windowGlass;
            }
        }

        internal static void Kill(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
        }

        // ===================================== Placeholders =====================================
        // One shared mesh per member (an asset, never freed with a chunk). Section numbers are exactly 10_spec §5.2;
        // the profiles are simplified (eased arrises and the ovolo as 3 mm chamfers, nosings as 12-sided half-rounds).

        static readonly Dictionary<WindowMember, Mesh> PlaceholderMeshes = new Dictionary<WindowMember, Mesh>();
        static readonly Dictionary<WindowMember, Material[]> PlaceholderMaterials = new Dictionary<WindowMember, Material[]>();

        static GameObject Placeholder(WindowMember member, Transform root, Quaternion turn)
        {
            // Run and Exit have no map windows yet; until their meshes exist they borrow the steel member.
            var buildAs = member == WindowMember.Lobby ? WindowMember.Lobby : WindowMember.Office;
            if (!PlaceholderMeshes.TryGetValue(buildAs, out var mesh) || mesh == null)
                PlaceholderMeshes[buildAs] = mesh = buildAs == WindowMember.Lobby ? BuildWood() : BuildSteel();
            if (!PlaceholderMaterials.TryGetValue(member, out var mats) || mats == null || mats[0] == null)
            {
                var main = member == WindowMember.Lobby ? Slot("Prop_WoodWalnut", new Color(.20f, .12f, .07f), .45f)
                    : member == WindowMember.Run ? Slot("Painted_Metal", new Color(.80f, .80f, .76f), .5f)
                    : Slot("Prop_SteelBrown", new Color(.16f, .12f, .09f), .45f);
                var glazing = member == WindowMember.Lobby && Putty != null ? Putty : Slot(RubberSlot, new Color(.02f, .02f, .02f), .15f);
                PlaceholderMaterials[member] = mats = new[] { main, glazing };
            }
            var go = new GameObject(WindowFrameLabel);
            go.transform.SetParent(root, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = turn;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterials = mats;
            r.shadowCastingMode = ShadowCastingMode.On;
            r.receiveShadows = true;
            return go;
        }

        static Material Slot(string name, Color fallback, float smoothness)
        {
            var m = FrontRoomsSurfaces.TryGet(name);
            return m != null ? m : FrontRoomsSurfaces.Lit("Window kit / " + name, fallback, smoothness);
        }

        const int Main = 0, Rubber = 1;

        // Section shorthands.
        const float LiningX = WindowSection.LiningX, LiningSill = WindowSection.LiningSill, LiningHead = WindowSection.LiningHead;
        const float StopX = WindowSection.StopX, StopSill = WindowSection.StopSill, StopHead = WindowSection.StopHead;
        const float StopZIn = WindowSection.StopZIn, StopZOut = WindowSection.StopZOut, StopBand = WindowSection.StopBand;
        const float GlassHalfZ = WindowSection.GlassHalfZ, FaceOuterX = WindowSection.FaceOuterX, WallFaceZ = WindowSection.WallFaceZ;
        const float CasingZ = WindowSection.CasingZ, CasingInnerZ = WindowSection.CasingInnerZ;

        /// <summary>W-L0 (10_spec §5.1, 06 §3.1): walnut liner, ranch casings on both faces landing on a through-stool with
        /// horns and half-round nosings, aprons, 16 x 16 wood stops on both faces, the glazing line in the pocket.</summary>
        static Mesh BuildWood()
        {
            var m = new KitMesh(2);
            // Jamb and head liner: 19 mm stock, visible face at |X| 0.6995 / Y 1.9995; jambs stand on the stool. The spec's
            // Z ±0.1005 is reached by the casings' inner edge faces (the same plane, Z 0.080 -> 0.1005), so the liner itself
            // stops at the wall faces (±0.080) and no two faces share a plane (no z-fight).
            m.Ring(Rect(0f, -WallFaceZ, .019f, WallFaceZ), LiningX, LiningSill, LiningHead, false, Main);
            // Casing, both faces: 0.0755 wide (X 0.6995 -> 0.775), back at the wall face (0.080), front 0.105 over the
            // outer 25 mm, bevelling down to 0.1005 at the inner edge; mitred at the head, jambs land on the stool.
            foreach (var side in new[] { 1f, -1f })
                m.Ring(Z(side, new Vector2(0f, WallFaceZ), new Vector2(0f, CasingInnerZ), new Vector2(.0505f, CasingZ),
                    new Vector2(FaceOuterX - LiningX, CasingZ), new Vector2(FaceOuterX - LiningX, WallFaceZ)), LiningX, LiningSill, LiningHead, false, Main);
            // Through-stool: X ±0.800 (25 mm horns past the casing), Z ±0.121, Y 0.3255 -> 0.3505, half-round nosing on both long edges.
            m.Extrude(Stadium(.121f, .3255f, LiningSill, 12), -.800f, .800f, Main);
            // Aprons, both faces, under the stool nose: X ±0.775, Y 0.250 -> 0.3255, Z 0.080 -> 0.105.
            foreach (var side in new[] { 1f, -1f })
                m.Box(new Vector3(-FaceOuterX, .250f, side > 0 ? WallFaceZ : -CasingZ), new Vector3(FaceOuterX, .3255f, side > 0 ? CasingZ : -WallFaceZ), Main);
            // Glazing stops, both faces, all four sides: 16 x 16 square bead with the exposed arris eased 3 mm, mitred.
            foreach (var side in new[] { 1f, -1f })
                m.Ring(Z(side, new Vector2(0f, StopZIn), new Vector2(0f, StopZOut - .003f), new Vector2(.003f, StopZOut),
                    new Vector2(StopBand, StopZOut), new Vector2(StopBand, StopZIn)), StopX, StopSill, StopHead, true, Main);
            GlazingLine(m);
            return m.ToMesh("Kit_WindowFrame_Wood (placeholder)");
        }

        /// <summary>W-OF (10_spec §5.1, 06 §3.2): one pressed-steel C section round all four sides (face bands on both wall
        /// faces, soffit, returns to the wall), integral stop on face B, screwed channel stop on face A with 30 oval heads.</summary>
        static Mesh BuildSteel()
        {
            var m = new KitMesh(2);
            const float t = .0015f, band = FaceOuterX - LiningX;   // 16 ga sheet; 0.0755 face band
            // The sill band is 0.2745 -> 0.3505 (76 mm) where the jambs and head are 75.5 mm: the sill side's u is scaled.
            var kSill = (LiningSill - .2745f) / band;
            m.Ring(new[]
            {
                new Vector2(0f, -CasingZ), new Vector2(0f, CasingZ), new Vector2(band, CasingZ), new Vector2(band, WallFaceZ),
                new Vector2(band - t, WallFaceZ), new Vector2(band - t, CasingZ - t), new Vector2(t, CasingZ - t), new Vector2(t, -CasingZ + t),
                new Vector2(band - t, -CasingZ + t), new Vector2(band - t, -WallFaceZ), new Vector2(band, -WallFaceZ), new Vector2(band, -CasingZ),
            }, LiningX, LiningSill, LiningHead, true, Main, kSill);
            // Integral stop (face B, hall side) and removable channel stop (face A, office side): 16 x 16, 1.5 mm eased arris.
            foreach (var side in new[] { 1f, -1f })
                m.Ring(Z(side, new Vector2(0f, StopZIn), new Vector2(0f, StopZOut - t), new Vector2(t, StopZOut),
                    new Vector2(StopBand, StopZOut), new Vector2(StopBand, StopZIn)), StopX, StopSill, StopHead, true, Main);
            // 30 oval-head screws on face A's stop face (Z 0.022): 8 per jamb from Y 0.4175, 0.2164 apart; 7 on head and sill
            // from X -0.6325, 0.2108 apart (UH spec: <= 9" o.c., <= 2" from the corners).
            var mid = StopX + StopBand * .5f;
            for (var i = 0; i < 8; i++)
            {
                var y = .4175f + .2164f * i;
                m.Dome(new Vector3(-mid, y, StopZOut), .0035f, .0015f, Main);
                m.Dome(new Vector3(mid, y, StopZOut), .0035f, .0015f, Main);
            }
            for (var i = 0; i < 7; i++)
            {
                var x = -.6325f + .2108f * i;
                m.Dome(new Vector3(x, StopHead + StopBand * .5f, StopZOut), .0035f, .0015f, Main);
                m.Dome(new Vector3(x, StopSill - StopBand * .5f, StopZOut), .0035f, .0015f, Main);
            }
            GlazingLine(m);
            return m.ToMesh("Kit_WindowFrame_Steel (placeholder)");
        }

        /// <summary>The 3 mm glazing tape / compound between the glass face (±0.003) and each stop's inner face (±0.006),
        /// from the stop line 10 mm into the pocket, both faces.</summary>
        static void GlazingLine(KitMesh m)
        {
            foreach (var side in new[] { 1f, -1f })
                m.Ring(Z(side, new Vector2(0f, GlassHalfZ), new Vector2(0f, StopZIn), new Vector2(.010f, StopZIn), new Vector2(.010f, GlassHalfZ)),
                    StopX, StopSill, StopHead, true, Rubber);
        }

        static Vector2[] Rect(float u0, float z0, float u1, float z1) =>
            new[] { new Vector2(u0, z0), new Vector2(u0, z1), new Vector2(u1, z1), new Vector2(u1, z0) };

        /// <summary>A face-A profile, or its mirror on face B (z negated).</summary>
        static Vector2[] Z(float side, params Vector2[] p)
        {
            var o = new Vector2[p.Length];
            for (var i = 0; i < p.Length; i++) o[i] = new Vector2(p[i].x, p[i].y * side);
            return o;
        }

        /// <summary>A stadium in (z, y): flat top and bottom, half-round ends reaching ±halfZ.</summary>
        static Vector2[] Stadium(float halfZ, float y0, float y1, int segments)
        {
            var r = (y1 - y0) * .5f;
            var cy = (y0 + y1) * .5f;
            var c = halfZ - r;
            var pts = new List<Vector2>();
            for (var i = 0; i <= segments; i++)
            {
                var a = -Mathf.PI * .5f + Mathf.PI * i / segments;
                pts.Add(new Vector2(c + r * Mathf.Cos(a), cy + r * Mathf.Sin(a)));
            }
            for (var i = 0; i <= segments; i++)
            {
                var a = Mathf.PI * .5f + Mathf.PI * i / segments;
                pts.Add(new Vector2(-c + r * Mathf.Cos(a), cy + r * Mathf.Sin(a)));
            }
            return pts.ToArray();
        }

        /// <summary>A tiny flat-shaded mesh builder in the window root frame (X along the wall, Y up, Z = face A).
        /// UVs are metres (u along the run, v round the profile), for FrontRooms/Surface with _FR_MESH_UV.</summary>
        sealed class KitMesh
        {
            readonly List<Vector3> v = new List<Vector3>();
            readonly List<Vector3> n = new List<Vector3>();
            readonly List<Vector2> uv = new List<Vector2>();
            readonly List<int>[] tris;

            public KitMesh(int submeshes)
            {
                tris = new List<int>[submeshes];
                for (var i = 0; i < submeshes; i++) tris[i] = new List<int>();
            }

            /// <summary>A planar quad a-b-c-d (in order round its edge), turned to face <paramref name="outward"/>.</summary>
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud, Vector3 outward, int sub)
            {
                var normal = Vector3.Cross(b - a, c - a);
                if (normal.sqrMagnitude < 1e-16f) normal = Vector3.Cross(c - a, d - a);
                if (normal.sqrMagnitude < 1e-16f) return;
                var flip = Vector3.Dot(normal, outward) < 0f;
                normal = normal.normalized * (flip ? -1f : 1f);
                var i = v.Count;
                v.Add(a); v.Add(b); v.Add(c); v.Add(d);
                for (var k = 0; k < 4; k++) n.Add(normal);
                uv.Add(ua); uv.Add(ub); uv.Add(uc); uv.Add(ud);
                var t = tris[sub];
                if (!flip) { t.Add(i); t.Add(i + 1); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 3); }
                else { t.Add(i); t.Add(i + 2); t.Add(i + 1); t.Add(i); t.Add(i + 3); t.Add(i + 2); }
            }

            void Tri(Vector3 a, Vector3 b, Vector3 c, Vector2 ua, Vector2 ub, Vector2 uc, Vector3 outward, int sub)
            {
                var normal = Vector3.Cross(b - a, c - a);
                if (normal.sqrMagnitude < 1e-16f) return;
                var flip = Vector3.Dot(normal, outward) < 0f;
                normal = normal.normalized * (flip ? -1f : 1f);
                var i = v.Count;
                v.Add(a); v.Add(b); v.Add(c);
                for (var k = 0; k < 3; k++) n.Add(normal);
                uv.Add(ua); uv.Add(ub); uv.Add(uc);
                var t = tris[sub];
                if (!flip) { t.Add(i); t.Add(i + 1); t.Add(i + 2); }
                else { t.Add(i); t.Add(i + 2); t.Add(i + 1); }
            }

            static float SignedArea(Vector2[] p)
            {
                var a = 0f;
                for (var i = 0; i < p.Length; i++)
                {
                    var q = p[(i + 1) % p.Length];
                    a += p[i].x * q.y - q.x * p[i].y;
                }
                return a * .5f;
            }

            static Vector2 Outward(Vector2 a, Vector2 b, float area)
            {
                var d = b - a;
                var o = area > 0f ? new Vector2(d.y, -d.x) : new Vector2(-d.y, d.x);
                return o.normalized;
            }

            /// <summary>
            /// A closed profile (u, z) swept round the inner rectangle X ±xi, Y y0..y1 with mitred corners. u runs outward
            /// from the inner edge, z across the wall. Without the sill side the jambs end flat at y0, capped.
            /// <paramref name="kSill"/> scales u on the sill side (a wider sill band).
            /// </summary>
            public void Ring(Vector2[] profile, float xi, float y0, float y1, bool sill, int sub, float kSill = 1f)
            {
                var area = SignedArea(profile);
                Vector3 Corner(int c, float u, float z)
                {
                    switch (c)
                    {
                        case 0: return new Vector3(-xi - u, sill ? y0 - u * kSill : y0, z);   // bottom left
                        case 1: return new Vector3(-xi - u, y1 + u, z);                       // top left
                        case 2: return new Vector3(xi + u, y1 + u, z);                        // top right
                        default: return new Vector3(xi + u, sill ? y0 - u * kSill : y0, z);   // bottom right
                    }
                }
                var sides = new List<(int s, int e, Vector3 dir, bool alongX)>
                {
                    (0, 1, Vector3.left, false), (1, 2, Vector3.up, true), (2, 3, Vector3.right, false),
                };
                if (sill) sides.Add((3, 0, Vector3.down, true));
                foreach (var (s, e, dir, alongX) in sides)
                {
                    var vAcc = 0f;
                    for (var i = 0; i < profile.Length; i++)
                    {
                        var p = profile[i];
                        var q = profile[(i + 1) % profile.Length];
                        var len = Vector2.Distance(p, q);
                        var o = Outward(p, q, area);
                        var a = Corner(s, p.x, p.y);
                        var b = Corner(e, p.x, p.y);
                        var c = Corner(e, q.x, q.y);
                        var d = Corner(s, q.x, q.y);
                        float R(Vector3 w) => alongX ? w.x : w.y;
                        Quad(a, b, c, d, new Vector2(R(a), vAcc), new Vector2(R(b), vAcc), new Vector2(R(c), vAcc + len), new Vector2(R(d), vAcc + len),
                            dir * o.x + Vector3.forward * o.y, sub);
                        vAcc += len;
                    }
                }
                if (!sill)
                    foreach (var c in new[] { 0, 3 })
                    {
                        var pts = new Vector3[profile.Length];
                        for (var i = 0; i < profile.Length; i++) pts[i] = Corner(c, profile[i].x, profile[i].y);
                        for (var i = 1; i + 1 < pts.Length; i++)
                            Tri(pts[0], pts[i], pts[i + 1], new Vector2(pts[0].x, pts[0].z), new Vector2(pts[i].x, pts[i].z), new Vector2(pts[i + 1].x, pts[i + 1].z), Vector3.down, sub);
                    }
            }

            /// <summary>A closed convex profile (z, y) extruded along X from x0 to x1, capped.</summary>
            public void Extrude(Vector2[] profile, float x0, float x1, int sub)
            {
                var area = SignedArea(profile);
                var vAcc = 0f;
                for (var i = 0; i < profile.Length; i++)
                {
                    var p = profile[i];
                    var q = profile[(i + 1) % profile.Length];
                    var len = Vector2.Distance(p, q);
                    var o = Outward(p, q, area);
                    Quad(new Vector3(x0, p.y, p.x), new Vector3(x1, p.y, p.x), new Vector3(x1, q.y, q.x), new Vector3(x0, q.y, q.x),
                        new Vector2(x0, vAcc), new Vector2(x1, vAcc), new Vector2(x1, vAcc + len), new Vector2(x0, vAcc + len),
                        new Vector3(0f, o.y, o.x), sub);
                    vAcc += len;
                }
                foreach (var x in new[] { x0, x1 })
                    for (var i = 1; i + 1 < profile.Length; i++)
                    {
                        Vector3 P(Vector2 w) => new Vector3(x, w.y, w.x);
                        Tri(P(profile[0]), P(profile[i]), P(profile[i + 1]), new Vector2(profile[0].x, profile[0].y), new Vector2(profile[i].x, profile[i].y),
                            new Vector2(profile[i + 1].x, profile[i + 1].y), x < 0f ? Vector3.left : Vector3.right, sub);
                    }
            }

            public void Box(Vector3 min, Vector3 max, int sub)
            {
                Vector3 P(float x, float y, float z) => new Vector3(x, y, z);
                float x0 = min.x, y0 = min.y, z0 = min.z, x1 = max.x, y1 = max.y, z1 = max.z;
                Quad(P(x0, y0, z1), P(x1, y0, z1), P(x1, y1, z1), P(x0, y1, z1), new Vector2(x0, y0), new Vector2(x1, y0), new Vector2(x1, y1), new Vector2(x0, y1), Vector3.forward, sub);
                Quad(P(x0, y0, z0), P(x1, y0, z0), P(x1, y1, z0), P(x0, y1, z0), new Vector2(x0, y0), new Vector2(x1, y0), new Vector2(x1, y1), new Vector2(x0, y1), Vector3.back, sub);
                Quad(P(x0, y1, z0), P(x1, y1, z0), P(x1, y1, z1), P(x0, y1, z1), new Vector2(x0, z0), new Vector2(x1, z0), new Vector2(x1, z1), new Vector2(x0, z1), Vector3.up, sub);
                Quad(P(x0, y0, z0), P(x1, y0, z0), P(x1, y0, z1), P(x0, y0, z1), new Vector2(x0, z0), new Vector2(x1, z0), new Vector2(x1, z1), new Vector2(x0, z1), Vector3.down, sub);
                Quad(P(x0, y0, z0), P(x0, y1, z0), P(x0, y1, z1), P(x0, y0, z1), new Vector2(z0, y0), new Vector2(z0, y1), new Vector2(z1, y1), new Vector2(z1, y0), Vector3.left, sub);
                Quad(P(x1, y0, z0), P(x1, y1, z0), P(x1, y1, z1), P(x1, y0, z1), new Vector2(z0, y0), new Vector2(z0, y1), new Vector2(z1, y1), new Vector2(z1, y0), Vector3.right, sub);
            }

            /// <summary>An 8-sided oval-head dome on a face looking +Z (radius r at z, height h).</summary>
            public void Dome(Vector3 c, float r, float h, int sub)
            {
                const int seg = 8;
                var apex = c + Vector3.forward * h;
                for (var i = 0; i < seg; i++)
                {
                    var a0 = Mathf.PI * 2f * i / seg;
                    var a1 = Mathf.PI * 2f * (i + 1) / seg;
                    var d0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f);
                    var d1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f);
                    var o0 = c + d0 * r;
                    var o1 = c + d1 * r;
                    var i0 = c + d0 * (r * .62f) + Vector3.forward * (h * .7f);
                    var i1 = c + d1 * (r * .62f) + Vector3.forward * (h * .7f);
                    var mid = (d0 + d1).normalized;
                    Quad(o0, o1, i1, i0, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, mid * .6f + Vector3.forward * .8f, sub);
                    Tri(i0, i1, apex, Vector2.zero, Vector2.zero, Vector2.zero, mid * .2f + Vector3.forward, sub);
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
}
