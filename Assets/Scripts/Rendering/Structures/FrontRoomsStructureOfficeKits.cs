// Scene 4's Office and Level 0 pieces that the K3 builder lays (game visual line, structures_assets step 2, group G3-b, 2026-11-10;
// research/structures_assets/30_plan.md P17 and rows S4-02, S4-04, S4-07, S4-08): the storefront run, the raking glass guard, the
// laylight's bays and the drive that keeps each bay's lens with its lamp. Inert until the staged contract K3 calls it in NEW.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Builder pieces for SP4 (and SP3) in NEW, on the kit's mesh and parts, render only: every collider stays the caller's (today's,
/// call for call). Storefront (S4-02): a 1988-91 centre-set interior storefront of the 1-3/4 x 4-1/2 in class (44.45 x 114.3 mm,
/// clear anodised, 1/4 in glass in EPDM wedge gaskets both faces): head and sill channels swept along the opening, P17's
/// Kit_StorefrontJamb at each end and Kit_StorefrontMullion between equal lites, each lite one 6 mm pane built as the game builds
/// its window panes (a unit cube scaled to the pane on the shared Glass_Window, so FrontRooms/Glass takes its frame, edges and
/// grime from the object). Raking glass (the pit's cheeks, the slot hall's flights): the guard's shoe and cap rail swept on the
/// rake and 12 mm panes as sheared cubes, the way the K1 guard builds a raking glass guard. Laylight (S4-08): G1's
/// Kit_Laylight_Module bays (+ _B, _C, _DeadRow, never the same as a neighbour) over the void, each bay's diffuser driven by the
/// laylight lamp nearest it (Laylights.Tick: a dead or flickering lamp leaves its bays dark or flickering with it; FR-9).
/// </summary>
public static class FrontRoomsStructureOfficeKits
{
    // ---------------------------------------------------------------- storefront (P17)

    /// <summary>The members' sightline and depth (1-3/4 x 4-1/2 in), the glazing pocket's half width and depth, 1/4 in glass.</summary>
    public const float SfW = .04445f, SfD = .1143f, SfPocket = .008f, SfPocketDepth = .0127f, SfGlass = .00635f;
    /// <summary>The vertical kits' length: between the sill and head channels of SP4's boardroom opening (1.81 m).</summary>
    public const float SfMember = 1.81f - 2f * SfW;

    static FrontRoomsStructureProfiles.Section sill, head, beadL, beadR;

    // A channel's section (x across the wall, y up, x 0 on the glass's plane): the tube 114.3 deep x 44.45 high, its glazing pocket
    // 16 mm wide and 12.7 mm deep opening up (the sill) or down (the head), the mouth eased 0.6 mm. Counter-clockwise.
    static FrontRoomsStructureProfiles.Section Channel(bool up)
    {
        float hd = SfD * .5f, h = SfW, p = SfPocket, q = SfPocketDepth, e = .0006f;
        Vector2[] pts =
        {
            new Vector2(-hd, 0f), new Vector2(hd, 0f), new Vector2(hd, h), new Vector2(p + e, h), new Vector2(p, h - e), new Vector2(p, h - q),
            new Vector2(-p, h - q), new Vector2(-p, h - e), new Vector2(-p - e, h), new Vector2(-hd, h),
        };
        // the head: the same section turned half round about its middle (a rotation keeps it counter-clockwise)
        if (!up)
            for (var i = 0; i < pts.Length; i++) pts[i] = new Vector2(-pts[i].x, h - pts[i].y);
        var hard = new bool[pts.Length];
        for (var i = 0; i < hard.Length; i++) hard[i] = true;
        return new FrontRoomsStructureProfiles.Section(up ? "storefront sill" : "storefront head", pts, hard, true);
    }

    // A wedge gasket's bead at a pocket's mouth on one face of the glass (side +-1), 2.4 mm round, 1.4 mm out of the pocket.
    static FrontRoomsStructureProfiles.Section Bead(float side)
    {
        float zi = SfGlass * .5f + .0001f, zo = SfPocket - .0002f;
        var pts = new List<Vector2> { new Vector2(side * zi, -.012f), new Vector2(side * zo, -.012f), new Vector2(side * zo, 0f) };
        for (var k = 1; k <= 3; k++)
        {
            var a = Mathf.PI * k / 4f;
            var r = (zo - zi) * .5f;
            pts.Add(new Vector2(side * ((zo + zi) * .5f + (zo - zi) * .5f * Mathf.Cos(a)), r * .62f * Mathf.Sin(a)));
        }
        pts.Add(new Vector2(side * zi, 0f));
        if (side < 0f) pts.Reverse();
        var hard = new bool[pts.Count];
        return new FrontRoomsStructureProfiles.Section("storefront gasket", pts.ToArray(), hard, true);
    }

    static readonly List<Vector3> sv = new List<Vector3>(), sn = new List<Vector3>();
    static readonly List<Vector2> su = new List<Vector2>();
    static readonly List<int> st = new List<int>();

    static void Lay(FrontRoomsStructuresKit kit, Vector3 a, Vector3 b, FrontRoomsStructureProfiles.Section s, Material m)
    {
        if (m == null || (b - a).sqrMagnitude < 1e-6f) return;
        sv.Clear(); sn.Clear(); su.Clear(); st.Clear();
        var o = FrontRoomsStructureProfiles.SweepOptions.Default;
        o.plumb = true;
        o.capStart = o.capEnd = true;
        o.u0 = FrontRoomsStructureProfiles.WorldU(kit.Root.TransformPoint(a), b - a);
        FrontRoomsStructureProfiles.Sweep(sv, sn, su, st, new[] { a, b }, Vector3.up, s, o);
        kit.Mesh.Append(sv, sn, su, st, m, false);
    }

    static Material Surf(string name)
    {
        var m = FrontRoomsStructuresAssets.Real(FrontRoomsSurfaces.TryGet(name));
        return m;
    }

    /// <summary>
    /// A storefront across an opening from plan point a to b (district metres; the glass on the line a..b), its sill channel's foot at
    /// sillY and its head channel's top at headY, in <paramref name="lites"/> equal lites; floorY the room's floor (the panes' smears
    /// and prints sit at hand height over it). Render only; false when the kits are not in this build (the caller then keeps
    /// today's pane).
    /// </summary>
    public static bool Storefront(FrontRoomsStructuresKit kit, Vector3 a, Vector3 b, float sillY, float headY, int lites, float floorY)
    {
        if (kit == null || !FrontRoomsKitLibrary.Has("Kit_StorefrontMullion") || !FrontRoomsKitLibrary.Has("Kit_StorefrontJamb")) return false;
        a.y = b.y = 0f;
        var len = Vector3.Distance(a, b);
        if (len < 4f * SfW || headY - sillY < 3f * SfW) return false;
        lites = Mathf.Max(1, lites);
        var t = (b - a) / len;
        var alu = Surf("Trans_AluminiumSatin");
        var rubber = Surf("Prop_Rubber");
        if (sill == null) { sill = Channel(true); head = Channel(false); beadL = Bead(-1f); beadR = Bead(1f); }
        var top = sillY + SfW;
        var underHead = headY - SfW;
        // the channels run the opening's full length; the jambs stand on the sill inside them
        Lay(kit, a + Vector3.up * sillY, b + Vector3.up * sillY, sill, alu);
        Lay(kit, a + Vector3.up * underHead, b + Vector3.up * underHead, head, alu);
        // the members' yaw: their local +x along the run
        var yaw = Mathf.Atan2(t.z, t.x) * -Mathf.Rad2Deg;
        Member(kit, "Kit_StorefrontJamb", a + t * (SfW * .5f), top, yaw);
        Member(kit, "Kit_StorefrontJamb", b - t * (SfW * .5f), top, yaw + 180f);
        var inner = len - 2f * SfW;
        var lite = (inner - (lites - 1) * SfW) / lites;
        var glassH = underHead - top;
        for (var i = 0; i < lites; i++)
        {
            var s0 = SfW + i * (lite + SfW);
            if (i > 0) Member(kit, "Kit_StorefrontMullion", a + t * (s0 - SfW * .5f), top, yaw);
            // the pane: into each pocket 9.5 mm (3.2 mm clear of its bottom), on setting blocks in the sill
            var p0 = a + t * (s0 - SfPocketDepth + .0032f);
            var p1 = a + t * (s0 + lite + SfPocketDepth - .0032f);
            Pane(kit, p0, p1, top - SfPocketDepth + .0048f, glassH + 2f * SfPocketDepth - .0080f, SfGlass, floorY, 0f, 0f);
            // the gaskets in the head and sill pockets, both faces
            if (rubber != null)
            {
                var g0 = a + t * s0;
                var g1 = a + t * (s0 + lite);
                Lay(kit, g0 + Vector3.up * top, g1 + Vector3.up * top, beadL, rubber);
                Lay(kit, g0 + Vector3.up * top, g1 + Vector3.up * top, beadR, rubber);
                Lay(kit, g1 + Vector3.up * underHead, g0 + Vector3.up * underHead, Flip(beadL), rubber);
                Lay(kit, g1 + Vector3.up * underHead, g0 + Vector3.up * underHead, Flip(beadR), rubber);
            }
        }
        return true;
    }

    static readonly Dictionary<FrontRoomsStructureProfiles.Section, FrontRoomsStructureProfiles.Section> flipped = new Dictionary<FrontRoomsStructureProfiles.Section, FrontRoomsStructureProfiles.Section>();

    // A section turned upside down (a head's gasket hangs from the channel's pocket mouth).
    static FrontRoomsStructureProfiles.Section Flip(FrontRoomsStructureProfiles.Section s)
    {
        if (flipped.TryGetValue(s, out var f)) return f;
        var p = new Vector2[s.P.Length];
        for (var i = 0; i < p.Length; i++) p[i] = new Vector2(-s.P[i].x, -s.P[i].y);
        f = new FrontRoomsStructureProfiles.Section(s.Name + " (head)", p, s.Hard, s.Closed);
        flipped[s] = f;
        return f;
    }

    static void Member(FrontRoomsStructuresKit kit, string asset, Vector3 at, float y, float yaw)
    {
        try { FrontRoomsKitLibrary.Spawn(asset, kit.PartRoot, new Vector3(at.x, y, at.z), yaw, null, false, null, false); }
        catch (System.Exception e) { FrontRoomsPoolMaterials.LogOnce("kit " + asset, "kit " + asset + " failed: " + e.Message); }
    }

    static Mesh paneCube;
    static MaterialPropertyBlock paneBlock;
    static readonly int PaneFloorYId = Shader.PropertyToID("_FloorY");
    static readonly int PaneAlphaId = Shader.PropertyToID("_AlphaFace");
    static readonly int PaneDustId = Shader.PropertyToID("_DustFilm");

    // One pane as the game builds its window panes (FrontRoomsGlassPane; K1's guard panes): a unit cube scaled to the pane on the
    // shared Glass_Window. a, b: its ends on plan; yA: its bottom at a, rise: how much higher its bottom is at b (a raked pane is a
    // parallelogram: a parent's scale and turn and the cube's own turn); height plumb; thick its glass. alpha / dust 0 keep the
    // material's own (a window's 6 mm pane). Render only, no shadow.
    static void Pane(FrontRoomsStructuresKit kit, Vector3 a, Vector3 b, float yA, float height, float thick, float floorY, float alpha, float dust, float rise = 0f)
    {
        var window = FrontRoomsSurfaces.TryGet("Glass_Window");
        if (window == null) return;
        a.y = b.y = 0f;
        var d = b - a;
        var len = d.magnitude;
        if (len < 1e-3f || height < 1e-3f) return;
        var t = d / len;
        if (paneCube == null)
        {
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            paneCube = tmp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tmp);
        }
        var go = new GameObject("Glass pane (storefront)");
        go.transform.SetParent(kit.PartRoot, false);
        go.transform.localPosition = (a + b) * .5f + Vector3.up * (yA + rise * .5f + height * .5f);
        var frame = Quaternion.LookRotation(new Vector3(-t.z, 0f, t.x), Vector3.up);
        var cube = go;
        if (Mathf.Abs(rise) < 1e-4f)
        {
            go.transform.localRotation = frame;
            go.transform.localScale = new Vector3(len, height, thick);
        }
        else
        {
            // the shear [[len, 0], [rise, height]] as R(phi) diag(sx, sy) R(theta) (K1's closed-form 2 x 2 SVD)
            float e = (len + height) * .5f, f = (len - height) * .5f, g = rise * .5f, h = rise * .5f;
            float q = Mathf.Sqrt(e * e + h * h), r = Mathf.Sqrt(f * f + g * g);
            float a1 = Mathf.Atan2(g, f), a2 = Mathf.Atan2(h, e);
            go.transform.localRotation = frame * Quaternion.Euler(0f, 0f, (a2 + a1) * .5f * Mathf.Rad2Deg);
            go.transform.localScale = new Vector3(q + r, Mathf.Max(q - r, 1e-4f), thick);
            cube = new GameObject("Glass pane (sheared)");
            cube.transform.SetParent(go.transform, false);
            cube.transform.localRotation = Quaternion.Euler(0f, 0f, (a2 - a1) * .5f * Mathf.Rad2Deg);
        }
        cube.AddComponent<MeshFilter>().sharedMesh = paneCube;
        var rend = cube.AddComponent<MeshRenderer>();
        rend.sharedMaterial = window;
        rend.shadowCastingMode = ShadowCastingMode.Off;
        if (paneBlock == null) paneBlock = new MaterialPropertyBlock();
        paneBlock.Clear();
        paneBlock.SetFloat(PaneFloorYId, kit.Root.TransformPoint(Vector3.up * floorY).y);
        if (alpha > 0f) paneBlock.SetFloat(PaneAlphaId, alpha);
        if (dust > 0f) paneBlock.SetFloat(PaneDustId, dust);
        rend.SetPropertyBlock(paneBlock);
    }

    // ---------------------------------------------------------------- raking glass (the Office's sloped glass guards)

    static FrontRoomsStructureProfiles.Section shoe, cap;

    /// <summary>
    /// A raking glass guard from a to b (points on the nosing line, b may be higher): the 1988 UBC glass guard as the K1 guard builds
    /// it level (a 64 x 105 aluminium shoe, 12 mm panes of at most 1.52 m on plan with 6 mm joints, a 50 x 38 cap rail over them),
    /// laid on the rake with its sections plumb. Render only: the caller keeps today's collider. False without Glass_Window.
    /// </summary>
    public static bool RakingGlass(FrontRoomsStructuresKit kit, Vector3 a, Vector3 b, float height)
    {
        if (kit == null || FrontRoomsSurfaces.TryGet("Glass_Window") == null) return false;
        if (shoe == null) { shoe = FrontRoomsStructureProfiles.GlassShoe(); cap = FrontRoomsStructureProfiles.CapRail(); }
        var trim = FrontRoomsStructuresAssets.TrimSheet();
        var metal = trim != null ? trim : Surf("Trans_AluminiumSatin");
        var flat = new Vector3(b.x - a.x, 0f, b.z - a.z);
        var run = flat.magnitude;
        if (run < .05f) return true;
        var o = FrontRoomsStructureProfiles.SweepOptions.Default;
        o.plumb = true;
        o.capStart = o.capEnd = true;
        if (trim != null)
        {
            o.vRange = FrontRoomsStructureProfiles.StripV(FrontRoomsStructureProfiles.Strip.ShoeAluminium);
            o.uScale = 1f / FrontRoomsStructureProfiles.TrimMetres;
        }
        o.u0 = FrontRoomsStructureProfiles.WorldU(kit.Root.TransformPoint(a), b - a) * o.uScale;
        sv.Clear(); sn.Clear(); su.Clear(); st.Clear();
        FrontRoomsStructureProfiles.Sweep(sv, sn, su, st, new[] { a, b }, Vector3.up, shoe, o);
        kit.Mesh.Append(sv, sn, su, st, metal, false);
        var capY = height - .038f;
        sv.Clear(); sn.Clear(); su.Clear(); st.Clear();
        FrontRoomsStructureProfiles.Sweep(sv, sn, su, st, new[] { a + Vector3.up * capY, b + Vector3.up * capY }, Vector3.up, cap, o);
        kit.Mesh.Append(sv, sn, su, st, metal, false);
        var panes = Mathf.Max(1, Mathf.CeilToInt(run / 1.52f));
        var pane = (run - (panes - 1) * .006f) / panes;
        var t = flat / run;
        var slope = (b.y - a.y) / run;
        for (var i = 0; i < panes; i++)
        {
            var s0 = i * (pane + .006f);
            var p0 = a + t * s0;
            var p1 = a + t * (s0 + pane);
            var y0 = a.y + slope * s0 + .025f;
            Pane(kit, p0, p1, y0, capY + .018f - .025f, .012f, Mathf.Min(a.y, b.y), .15f, .08f, slope * pane);
        }
        return true;
    }

    // ---------------------------------------------------------------- the laylight (S4-08)

    /// <summary>The laylight's bays and the lamps that light them; Tick keeps each bay's lens with its lamp.</summary>
    public sealed class Laylights
    {
        readonly List<GameObject> bays = new List<GameObject>();
        readonly List<int> lampOf = new List<int>();
        readonly List<float> shown = new List<float>();
        readonly List<Light> lamps = new List<Light>();
        readonly List<float> full = new List<float>();
        public Color Tint = new Color(.95f, .98f, 1f);

        public int Bays => bays.Count;

        /// <summary>A laylight lamp (its intensity now is its full level).</summary>
        public void AddLamp(Light l)
        {
            if (l == null) return;
            lamps.Add(l);
            full.Add(Mathf.Max(1e-3f, l.intensity));
        }

        /// <summary>
        /// Bays of 3 x 3 m over x0..x1, z0..z1 (district metres, whole bays) with their faces at y: Kit_Laylight_Module and its _B
        /// and _C (the atlas's other panels), never the same as the bay to the west or the south, one bay in about eleven _DeadRow.
        /// Each bay is lit by the laylight lamp nearest its centre. False when the kit is not in this build.
        /// </summary>
        public bool Build(FrontRoomsStructuresKit kit, float x0, float z0, float x1, float z1, float y)
        {
            if (kit == null || !FrontRoomsKitLibrary.Has("Kit_Laylight_Module")) return false;
            int nx = Mathf.RoundToInt((x1 - x0) / 3f), nz = Mathf.RoundToInt((z1 - z0) / 3f);
            string[] names = { "Kit_Laylight_Module", "Kit_Laylight_Module_B", "Kit_Laylight_Module_C" };
            var pick = new int[nx, nz];
            for (var i = 0; i < nx; i++)
                for (var j = 0; j < nz; j++)
                {
                    var h = (i * 73856093) ^ (j * 19349663) ^ 0x5bd1e995;
                    h = (h ^ (h >> 13)) * 1274126177;
                    var k = ((h >> 8) & 0xffff) % 3;
                    for (var n = 0; n < 3 && ((i > 0 && pick[i - 1, j] == k) || (j > 0 && pick[i, j - 1] == k)); n++) k = (k + 1) % 3;
                    pick[i, j] = k;
                    var dead = ((h >> 4) & 0xff) < 23 && FrontRoomsKitLibrary.Has("Kit_Laylight_Module_DeadRow");
                    var name = dead ? "Kit_Laylight_Module_DeadRow" : names[k];
                    var c = new Vector3(x0 + 1.5f + 3f * i, y, z0 + 1.5f + 3f * j);
                    GameObject go = null;
                    try { go = FrontRoomsKitLibrary.Spawn(name, kit.PartRoot, c, 0f, null, false, null, false); }
                    catch (System.Exception e) { FrontRoomsPoolMaterials.LogOnce("kit " + name, "kit " + name + " failed: " + e.Message); }
                    if (go == null) continue;
                    foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
                    bays.Add(go);
                    lampOf.Add(Nearest(kit.Root.TransformPoint(c)));
                    shown.Add(-1f);
                }
            Tick();
            return bays.Count > 0;
        }

        int Nearest(Vector3 world)
        {
            var best = -1;
            var bd = float.MaxValue;
            for (var i = 0; i < lamps.Count; i++)
            {
                if (lamps[i] == null) continue;
                var p = lamps[i].transform.position;
                var d = (p.x - world.x) * (p.x - world.x) + (p.z - world.z) * (p.z - world.z);
                if (d < bd) { bd = d; best = i; }
            }
            return best;
        }

        /// <summary>Each bay's lens at its lamp's level now (its intensity over its full level; 0 when off), written only on a step
        /// of .05 or more or at the ends (dark, full): a dead lamp leaves its bays dark, a flicker flickers them (FR-9).</summary>
        public void Tick()
        {
            for (var i = 0; i < bays.Count; i++)
            {
                var b = bays[i];
                if (b == null) continue;
                var k = lampOf[i];
                var level = 1f;
                if (k >= 0 && k < lamps.Count)
                {
                    var l = lamps[k];
                    level = l == null || !l.enabled || !l.gameObject.activeInHierarchy ? 0f : Mathf.Clamp01(l.intensity / full[k]);
                }
                var was = shown[i];
                var ends = (level <= .001f || level >= .999f) && Mathf.Abs(level - was) > 1e-4f;
                if (was >= 0f && Mathf.Abs(level - was) < .05f && !ends) continue;
                shown[i] = level;
                FrontRoomsStructuresLamps.DriveLens(b, FrontRoomsStructuresLamps.LensDrive(Tint, FrontRoomsStructuresLamps.LaylightLensPeak, level));
            }
        }
    }
}
