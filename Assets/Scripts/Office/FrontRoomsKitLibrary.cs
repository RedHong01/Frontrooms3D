using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads the Blender-built prop kit (Tools/Blender/frontrooms_kit) from
/// Resources/Props/Models: one FBX per asset plus a JSON sidecar with its
/// bounds, support surfaces, anchors and collider boxes, all in Unity space.
/// The asset's front faces +Z and it stands on y = 0.
/// Submesh materials are named after kit slots ("Prop_WoodCherry"); the
/// importer remaps them to Resources/Surfaces/&lt;slot&gt;.mat, and Spawn
/// repeats that remap at runtime as a safety net.
/// </summary>
public static class FrontRoomsKitLibrary
{
    public const string ModelFolder = "Props/Models/";

    [Serializable]
    public class Support { public string name; public float[] centre; public float[] size; }

    [Serializable]
    public class BoxDef { public float[] centre; public float[] size; }

    [Serializable]
    public class Anchor { public string name; public float[] pos; }

    /// <summary>How the furniture pile may use an asset (kitlib Kit.pile()).</summary>
    [Serializable]
    public class PileDef
    {
        public string cls;          // Seat Table Case Soft Tall Screen Crate Small
        public int mass;            // 0 light .. 3 heavy
        public string[] states;     // Upright Back Front Side Inverted EdgeLean
        public float tolerance;     // max own-volume fraction inside other pieces
        public string palette;      // domestic70s office90s storage hotel
        public bool topper;
    }

    [Serializable]
    public class Info
    {
        public PileDef pile;
        public string name;
        public float[] boundsMin;
        public float[] boundsMax;
        public Support[] supports;
        public BoxDef[] colliders;
        public Anchor[] anchors;
        public string[] tags;
        public string[] slots;
        public int triangles;
        public int trianglesLod1;
        public string frontAxis;          // "+Z": the asset's front faces local +Z
        public float[] footprintCentre;   // physical footprint (colliders), Unity x/z
        public float[] footprintSize;
        public string placement;          // Floor | Wall | DeskTop | Ceiling
        public float service;             // clear depth needed in front (wall units)
        public float minCeiling;          // ceiling height the piece needs
        public float[] lodDistances;      // authored switch distances; 0 means never cull

        public Vector3 Min => V(boundsMin);
        public Vector3 Max => V(boundsMax);
        public Vector3 Size => Max - Min;
        /// <summary>The physical footprint (collider union; cables and overhangs excluded) as an x/z rect.</summary>
        public Rect Footprint => footprintSize != null && footprintSize.Length >= 2 && footprintCentre != null && footprintCentre.Length >= 2
            ? new Rect(footprintCentre[0] - footprintSize[0] * .5f, footprintCentre[1] - footprintSize[1] * .5f, footprintSize[0], footprintSize[1])
            : Rect.MinMaxRect(Min.x, Min.z, Max.x, Max.z);

        /// <summary>Footprint half extents about the origin (x, z).</summary>
        public Vector2 HalfFootprint
        {
            get
            {
                var f = Footprint;
                return new Vector2(Mathf.Max(-f.xMin, f.xMax), Mathf.Max(-f.yMin, f.yMax));
            }
        }

        /// <summary>Physical depth (z) of the footprint.</summary>
        public float Depth => Footprint.height;
        public float Height => Max.y;

        public bool TryAnchor(string anchorName, out Vector3 position)
        {
            if (anchors != null)
                foreach (var a in anchors)
                    if (a != null && a.name == anchorName) { position = V(a.pos); return true; }
            position = Vector3.zero;
            return false;
        }

        public bool TrySupport(string supportName, out Vector3 centre, out Vector2 size)
        {
            if (supports != null)
                foreach (var s in supports)
                    if (s != null && (supportName == null || s.name == supportName)) { centre = V(s.centre); size = new Vector2(s.size[0], s.size[1]); return true; }
            centre = Vector3.zero;
            size = Vector2.zero;
            return false;
        }

        public bool HasTag(string tag) => tags != null && Array.IndexOf(tags, tag) >= 0;
        public bool IsPilePiece => pile != null && !string.IsNullOrEmpty(pile.cls);
    }

    static readonly Dictionary<string, GameObject> Models = new Dictionary<string, GameObject>();
    static readonly Dictionary<string, Info> Infos = new Dictionary<string, Info>();
    static readonly Dictionary<string, Material[]> MaterialSets = new Dictionary<string, Material[]>();
    static readonly HashSet<string> Missing = new HashSet<string>();

    static Vector3 V(float[] a) => a == null || a.Length < 3 ? Vector3.zero : new Vector3(a[0], a[1], a[2]);

    public static bool Has(string assetName) => Model(assetName) != null;

    static List<string> allNames;

    /// <summary>
    /// Forget every cached model, sidecar, name list and material. The editor calls
    /// this when a kit FBX/JSON or a surface material is re-imported, so palettes,
    /// footprints and Validate see the new export without a script reload.
    /// </summary>
    public static void ClearCache()
    {
        Models.Clear();
        Infos.Clear();
        MaterialSets.Clear();
        PreparedKits.Clear();
        Missing.Clear();
        ProjectMaterials.Clear();
        allNames = null;
    }

    /// <summary>
    /// Load what the first Spawn of each kit would otherwise load mid-game:
    /// every model and sidecar, the project slot materials each model's
    /// renderers are remapped to (Resources/Surfaces, with their textures),
    /// and the per-kit shadow and collider set-up. Changes nothing that gets
    /// built. Call once while nothing is on screen (e.g. from the map's Prewarm).
    /// </summary>
    public static void Prewarm()
    {
        foreach (var name in AllNames())
        {
            GetInfo(name);
            var model = Model(name);
            if (model != null) Prepare(name, model);
        }
    }

    /// <summary>Every kit asset name in Resources/Props/Models, sorted (stable for seeding).</summary>
    public static IReadOnlyList<string> AllNames()
    {
        if (allNames != null) return allNames;
        allNames = new List<string>();
        foreach (var model in Resources.LoadAll<GameObject>(ModelFolder.TrimEnd('/')))
            if (model != null && !model.name.StartsWith("Kit_AxisProbe") && !IsSyncCopy(model.name) && !allNames.Contains(model.name)) allNames.Add(model.name);
        allNames.Sort(StringComparer.Ordinal);
        return allNames;
    }

    /// <summary>"Kit_CRTMonitor 2": a file-sync conflict copy (the project sits on an
    /// iCloud Desktop), never a kit. Keeps it out of piles and seeded orders.</summary>
    static bool IsSyncCopy(string name)
    {
        var i = name.LastIndexOf(' ');
        if (i < 0 || i == name.Length - 1) return false;
        for (var k = i + 1; k < name.Length; k++)
            if (!char.IsDigit(name[k])) return false;
        return true;
    }

    public static GameObject Model(string assetName)
    {
        if (string.IsNullOrEmpty(assetName)) return null;
        if (Models.TryGetValue(assetName, out var model)) return model;
        model = Resources.Load<GameObject>(ModelFolder + assetName);
        Models[assetName] = model;
        if (model == null && Missing.Add(assetName))
            Debug.LogWarning("[FrontRoomsKit] Missing model Resources/" + ModelFolder + assetName);
        return model;
    }

    public static Info GetInfo(string assetName)
    {
        if (string.IsNullOrEmpty(assetName)) return null;
        if (Infos.TryGetValue(assetName, out var info)) return info;
        var text = Resources.Load<TextAsset>(ModelFolder + assetName);
        info = text != null ? JsonUtility.FromJson<Info>(text.text) : null;
        if (info == null)
        {
            // No sidecar: derive bounds from the mesh so placement still works.
            var model = Model(assetName);
            var filter = model != null ? model.GetComponentInChildren<MeshFilter>() : null;
            if (filter != null && filter.sharedMesh != null)
            {
                var b = filter.sharedMesh.bounds;
                info = new Info { name = assetName, boundsMin = new[] { b.min.x, b.min.y, b.min.z }, boundsMax = new[] { b.max.x, b.max.y, b.max.z } };
            }
        }
        Infos[assetName] = info;
        return info;
    }

    /// <summary>
    /// Instantiate an asset under <paramref name="parent"/> at a local
    /// position and yaw (degrees, 0 = front faces the parent's +Z).
    /// Scale may be non-uniform (the furniture pile uses it sparingly).
    /// </summary>
    public static GameObject Spawn(string assetName, Transform parent, Vector3 localPosition, float yaw, Vector3? scale = null, bool colliders = true, string label = null)
    {
        return Spawn(assetName, parent, localPosition, Quaternion.Euler(0f, yaw, 0f), scale, colliders, label);
    }

    public static GameObject Spawn(string assetName, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3? scale = null, bool colliders = true, string label = null)
    {
        var model = Model(assetName);
        if (model == null) return null;
        var instance = UnityEngine.Object.Instantiate(model, parent, false);
        instance.name = label ?? assetName;
        var t = instance.transform;
        t.localPosition = localPosition;
        t.localRotation = localRotation;
        // The clone already has the model's scale; write only when it differs (bit for bit).
        var s = scale ?? Vector3.one;
        var current = t.localScale;
        if (current.x != s.x || current.y != s.y || current.z != s.z) t.localScale = s;
        ApplyMaterials(assetName, model, instance);
        if (colliders) AddColliders(assetName, model, instance);
        return instance;
    }

    static readonly Dictionary<string, Material> ProjectMaterials = new Dictionary<string, Material>();

    /// <summary>
    /// What Spawn does to every instance of one asset, worked out once from the
    /// model (an instance's renderers are clones of the model's, in the same
    /// order): the material set per renderer (null when the importer already
    /// assigned exactly those materials), whether the shadow settings need
    /// writing, and the collider boxes.
    /// </summary>
    sealed class Prepared
    {
        public int renderers;
        public Material[][] sets;
        public bool[] writeShadows, writeReceive;
        public UnityEngine.Rendering.ShadowCastingMode shadows;
        public Vector3[] boxCentres, boxSizes;
    }

    static readonly Dictionary<string, Prepared> PreparedKits = new Dictionary<string, Prepared>();
    static readonly List<MeshRenderer> RendererScratch = new List<MeshRenderer>();

    static Prepared Prepare(string assetName, GameObject model)
    {
        if (PreparedKits.TryGetValue(assetName, out var prep)) return prep;
        var info = GetInfo(assetName);
        var castShadows = info == null || Mathf.Max(info.Size.x, Mathf.Max(info.Size.y, info.Size.z)) > .3f;
        var shadows = castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
        var renderers = model.GetComponentsInChildren<MeshRenderer>(true);
        prep = new Prepared
        {
            renderers = renderers.Length,
            sets = new Material[renderers.Length][],
            writeShadows = new bool[renderers.Length],
            writeReceive = new bool[renderers.Length],
            shadows = shadows,
        };
        for (var r = 0; r < renderers.Length; r++)
        {
            var renderer = renderers[r];
            var set = MaterialSet(assetName, renderer);
            var imported = renderer.sharedMaterials;
            var same = imported.Length == set.Length;
            for (var i = 0; same && i < set.Length; i++) same = ReferenceEquals(imported[i], set[i]);
            prep.sets[r] = same ? null : set;
            prep.writeShadows[r] = renderer.shadowCastingMode != shadows;
            prep.writeReceive[r] = !renderer.receiveShadows;
        }
        if (info?.colliders != null && info.colliders.Length > 0)
        {
            prep.boxCentres = new Vector3[info.colliders.Length];
            prep.boxSizes = new Vector3[info.colliders.Length];
            for (var i = 0; i < info.colliders.Length; i++)
            {
                prep.boxCentres[i] = V(info.colliders[i].centre);
                prep.boxSizes[i] = V(info.colliders[i].size);
            }
        }
        PreparedKits[assetName] = prep;
        return prep;
    }

    /// <summary>The project slot materials for one renderer, cached by asset and renderer name.</summary>
    static Material[] MaterialSet(string assetName, MeshRenderer renderer)
    {
        if (MaterialSets.TryGetValue(assetName + "/" + renderer.name, out var set)) return set;
        var imported = renderer.sharedMaterials;
        set = new Material[imported.Length];
        for (var i = 0; i < imported.Length; i++)
        {
            var slot = imported[i] != null ? imported[i].name.Replace(" (Instance)", string.Empty) : null;
            Material project = null;
            if (slot != null && !ProjectMaterials.TryGetValue(slot, out project))
                ProjectMaterials[slot] = project = FrontRoomsSurfaces.TryGet(slot);
            set[i] = project != null ? project : imported[i];
        }
        MaterialSets[assetName + "/" + renderer.name] = set;
        return set;
    }

    /// <summary>
    /// Remap every renderer (LOD0 and LOD1) to the project slot materials and
    /// keep shadows only on props larger than 0.3 m (LEVEL_MODULE_SPEC §8).
    /// Writes are skipped where the instance already holds the wanted value.
    /// </summary>
    static void ApplyMaterials(string assetName, GameObject model, GameObject instance)
    {
        var prep = Prepare(assetName, model);
        instance.GetComponentsInChildren(true, RendererScratch);
        if (RendererScratch.Count != prep.renderers)
        {
            // Not shaped like its model (never expected): set everything, as before.
            foreach (var renderer in RendererScratch)
            {
                renderer.sharedMaterials = MaterialSet(assetName, renderer);
                renderer.shadowCastingMode = prep.shadows;
                renderer.receiveShadows = true;
            }
            RendererScratch.Clear();
            return;
        }
        for (var r = 0; r < RendererScratch.Count; r++)
        {
            var renderer = RendererScratch[r];
            if (prep.sets[r] != null) renderer.sharedMaterials = prep.sets[r];
            if (prep.writeShadows[r]) renderer.shadowCastingMode = prep.shadows;
            if (prep.writeReceive[r]) renderer.receiveShadows = true;
        }
        RendererScratch.Clear();
    }

    static void AddColliders(string assetName, GameObject model, GameObject instance)
    {
        var prep = Prepare(assetName, model);
        if (prep.boxCentres == null) return;
        for (var i = 0; i < prep.boxCentres.Length; i++)
        {
            var collider = instance.AddComponent<BoxCollider>();
            collider.center = prep.boxCentres[i];
            collider.size = prep.boxSizes[i];
        }
    }
}
