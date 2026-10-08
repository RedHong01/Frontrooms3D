using System.Collections.Generic;
using FrontRooms.Map;
using UnityEngine;

/// <summary>
/// The evacuation placard's phosphor legend (Q16; research/placard/10_spec.md §4,
/// placard/20_build.md). Art v2 (平面视觉, 2026-10-07): the legend shows WP03
/// wallpaper swatches at 1/25, and the placard's own ZnS:Cu overprint (A.12,
/// "the legend glows") covers the IN POWER FAILURE head, the labels and the one
/// turned or flattened band in each swatch. The sheet's glow mask
/// (Prop_EvacPlan_E) is its emission map, and this component drives
/// _EmissionColor = TintLinear × FrontRoomsPhosphor.PeakEmission × G on ONE
/// material instance per placard, shared by every LOD renderer (no pop between
/// LODs). k is the ink's one absolute value, shared with the walls' driver
/// (placard 35_fix.md §3). New artwork of the same size needs no code change
/// (Tools/lookdev/pack_evac_plan.py).
///
/// G follows the walls' ink rules (FrontRoomsPhosphor, FrontRoomsPhosphorCell):
/// the placard cell's LOGICAL lamp level L (map.LampLevel, overrides included)
/// and its four neighbours charge the ink; the gate opens only as the cell's own
/// lamp goes dark. Never read: light.enabled / intensity, the lens, or the
/// start-area hold (StartLampsNorth): a lamp held dark at the start still reads
/// lit, so nothing glows unseen behind the shut door and there is no false glow
/// at the reveal.
///
/// Photosafety (FrontRoomsSettings.ReduceFlashing): a stutter lamp never glows;
/// Ls τ 0.35 s (calm 1.0 s); when calm, a failing cell shows the 3 s mean of the
/// normal path's target, low-passed 6 s (a steady read, no pulse); the target G
/// is held at its peak 0.6 s (calm 1.5 s) and slewed at 2/s (calm 0.5/s), so a
/// burst train becomes one swell.
///
/// Material rules: new Material(shared) put into the Prop_EvacPlan slot through
/// sharedMaterials (never renderer.material, never a MaterialPropertyBlock:
/// _EmissionColor is in UnityPerMaterial, so the SRP Batcher keeps it); written
/// with SetVector in linear values; only when a channel moves by more than 1/1024;
/// destroyed in OnDestroy. The shared Prop_EvacPlan.mat is never touched.
/// </summary>
[AddComponentMenu("")]
public sealed class FrontRoomsPlacardGlow : MonoBehaviour
{
    /// <summary>
    /// Kill switch (D17): false keeps the emission black, so the legend reads only in light. Art v2: the placard's
    /// overprint does not depend on the walls' §8 option A/B (A.12), so nothing sets this today; it stays for Red.
    /// </summary>
    public static bool Enabled = true;

    /// <summary>The printed sheet's material slot (kit Kit_EvacPlacard).</summary>
    public const string PrintSlot = "Prop_EvacPlan";

    const float ModeRefresh = 1f;                  // seconds between LampModeOf reads
    const float WriteEpsilon = 1f / 1024f;

    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    FrontRoomsMapWorld map;
    FrontRoomsMapHunter relay;
    GridCoord cell;
    Material instance;
    readonly List<Renderer> bound = new List<Renderer>();
    readonly FrontRoomsPhosphorCell ink = new FrontRoomsPhosphorCell();
    ModuleLamp mode;
    float modeAge;
    bool frozen, calm, dark;
    Vector3 last = new Vector3(-1f, -1f, -1f);

#if UNITY_EDITOR
    /// <summary>Editor only (lookdev): when ≥ 0, replaces the cell's own lamp level L (the neighbours still read the map).
    /// Not serialized, so the Editor-only field never changes the component's serialized layout between Editor and player.</summary>
    [System.NonSerialized] public float ForceLevel = -1f;
#endif

    public GridCoord Cell => cell;
    public ModuleLamp Mode => mode;
    public float Charge => ink.Charge;
    public float Shown => ink.Shown;
    public float SmoothedLevel => ink.SmoothedLevel;
    public bool Frozen => frozen;
    public Material Instance => instance;
    public IReadOnlyList<Renderer> Renderers => bound;

    /// <summary>
    /// Start driving the placard: one material instance replaces the shared
    /// Prop_EvacPlan in every renderer given (all LODs of the frame kit).
    /// </summary>
    public void Bind(FrontRoomsMapWorld world, FrontRoomsMapHunter hunter, GridCoord glowCell, IEnumerable<Renderer> renderers)
    {
        Unhook();
        map = world;
        relay = hunter;
        cell = glowCell;
        frozen = false;
        ink.Reset();
        modeAge = 0f;
        foreach (var r in renderers)
        {
            if (r == null) continue;
            var mats = r.sharedMaterials;
            var changed = false;
            for (var i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m == null || m == instance || !m.name.StartsWith(PrintSlot, System.StringComparison.Ordinal)) continue;
                if (instance == null) instance = new Material(m) { name = PrintSlot + " (Placard)" };
                mats[i] = instance;
                changed = true;
            }
            if (!changed) continue;
            r.sharedMaterials = mats;
            bound.Add(r);
        }
        if (instance == null) Debug.LogWarning("[Placard] no " + PrintSlot + " slot on the placard's renderers: the glow has nothing to drive");
        else Write(Vector3.zero);
        if (relay != null) relay.Caught += Freeze;
        calm = FrontRoomsSettings.ReduceFlashing;
        FrontRoomsSettings.Changed += OnSettings;
    }

    /// <summary>Lookdev: the emission for a given G with no map (the kit captures call it).</summary>
    public static void Preview(Material material, float g)
    {
        if (material == null) return;
        var e = FrontRoomsPhosphor.TintLinear * (FrontRoomsPhosphor.PeakEmission * Mathf.Clamp01(g));
        material.SetVector(EmissionColorId, new Vector4(e.x, e.y, e.z, 1f));
    }

    void OnSettings() => calm = FrontRoomsSettings.ReduceFlashing;

    // The Relay caught the player: the ink freezes where it is (LD §4).
    void Freeze() => frozen = true;

    void Update()
    {
        if (instance == null || map == null || frozen) return;
        if (!Enabled)
        {
            if (!dark) { Write(Vector3.zero); dark = true; }
            return;
        }
        dark = false;
        var dt = Time.deltaTime;
        if ((modeAge -= dt) <= 0f)
        {
            mode = map.LampModeOf(cell);
            modeAge = ModeRefresh;
        }
        // Until the cell's chunk is built there is no lamp to read: hold (the start door is shut then anyway).
        if (!map.IsBuilt(cell)) return;
        var l = Level(cell);
#if UNITY_EDITOR
        if (ForceLevel >= 0f) l = Mathf.Clamp01(ForceLevel);
#endif
        var w = FrontRoomsPhosphor.WallLight(l,
            Level(new GridCoord(cell.x + 1, cell.y)), Level(new GridCoord(cell.x - 1, cell.y)),
            Level(new GridCoord(cell.x, cell.y + 1)), Level(new GridCoord(cell.x, cell.y - 1)));
        var g = ink.Tick(l, w, mode, calm, dt);
        var e = FrontRoomsPhosphor.TintLinear * (FrontRoomsPhosphor.PeakEmission * g);
        if (Mathf.Abs(e.x - last.x) < WriteEpsilon && Mathf.Abs(e.y - last.y) < WriteEpsilon && Mathf.Abs(e.z - last.z) < WriteEpsilon) return;
        Write(e);
    }

    float Level(GridCoord c)
    {
        var v = map.LampLevel(c);
        return v < 0f ? 0f : v;
    }

    void Write(Vector3 e)
    {
        instance.SetVector(EmissionColorId, new Vector4(e.x, e.y, e.z, 1f));
        last = e;
    }

    void Unhook()
    {
        if (relay != null) relay.Caught -= Freeze;
        FrontRoomsSettings.Changed -= OnSettings;
    }

    void OnDestroy()
    {
        Unhook();
        if (instance == null) return;
        if (Application.isPlaying) Destroy(instance);
        else DestroyImmediate(instance);
        instance = null;
    }
}
