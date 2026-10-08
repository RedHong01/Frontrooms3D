using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

/// <summary>
/// A room being furnished a little at a time (FrontRoomsOfficeKit.BeginDress,
/// FrontRoomsFurniturePile.BeginBuild / BeginBuildPile).
///
/// The dresser first solves the layout (pure arithmetic, in small resumable
/// units) and records what to create — group objects, kit instances, column
/// blocks — in creation order; Step then creates them, interleaved with the
/// solving, until the frame's budget is spent. The objects, their order under
/// every parent, positions, rotations and materials are exactly what the
/// synchronous Dress / Build call makes (those calls run the same job to
/// completion), whatever budget is used.
///
/// Usage:
/// <code>
/// var job = FrontRoomsOfficeKit.BeginDress(parent, floor, ceiling, seed, keepClear, obstacles);
/// // every frame:
/// if (job.Step(budgetMs)) { /* finished */ }
/// </code>
/// Rule: one open job per parent, and nothing else adds children to that
/// parent until the job is done. The office root is created by BeginDress,
/// but a pile root only once the pile's layout is solved (a few steps later),
/// so anything added to the parent meanwhile would land before it, where the
/// synchronous Build puts it after. Jobs under different parents may be
/// stepped in any order.
///
/// If the parent is destroyed before the job finishes, the next Step stops it
/// (Cancelled) without creating anything else.
/// </summary>
public abstract class FrontRoomsDressJob
{
    /// <summary>All work is done (or the job was cancelled).</summary>
    public bool Done { get; protected set; }

    /// <summary>The parent was destroyed (or Cancel was called) before the job finished.</summary>
    public bool Cancelled { get; protected set; }

    /// <summary>Objects created so far (groups, kit instances, blocks).</summary>
    public int Created => build != null ? build.executed : 0;

    /// <summary>Wall-clock milliseconds spent inside Step/Complete so far.</summary>
    public double ElapsedMs { get; private set; }

    /// <summary>Calls to Step so far.</summary>
    public int Steps { get; private set; }

    /// <summary>
    /// The longest single unit seen inside Step so far, in milliseconds: a
    /// solver unit (one placement attempt) and a create unit (one object).
    /// </summary>
    public double MaxSolveUnitMs => maxSolveTicks * TickMs;
    public double MaxCreateUnitMs => maxCreateTicks * TickMs;

    static readonly Stopwatch Clock = Stopwatch.StartNew();
    static readonly double TickMs = 1000.0 / Stopwatch.Frequency;

    internal FrontRoomsDressBuild build;
    IEnumerator<bool> planner;
    bool planned;
    bool lastUnitCreated;
    long maxSolveTicks, maxCreateTicks;
    // What the next solver / create unit is expected to cost: the longest
    // recent unit of that kind, losing a quarter per unit (see Step).
    long solveEstimate, createEstimate;

    internal void Start(IEnumerator<bool> plan, FrontRoomsDressBuild into)
    {
        planner = plan;
        build = into;
        planned = plan == null;
        if (planned && build == null) Done = true;
    }

    /// <summary>
    /// Do work for up to <paramref name="budgetMs"/> milliseconds and return
    /// true when the job is finished. A step always does at least one unit (one
    /// placement attempt or one object), then stops before a unit that would
    /// probably end past the budget: it keeps, per kind of unit, the longest
    /// recent one (a longer unit replaces it, otherwise it loses a quarter per
    /// unit) and stops when less time is left than that. The guess counts for
    /// at most half the budget, so one unit the OS stalled cannot cut later
    /// steps down to one unit each. A step can still overrun when a single unit
    /// is longer than its guess (the first spawn of a kit nothing has loaded:
    /// see <see cref="WarmUp"/>). How the work is split never changes what is built.
    /// </summary>
    public bool Step(float budgetMs)
    {
        if (Done) return true;
        Steps++;
        var start = Clock.ElapsedTicks;
        var budget = (long)(Mathf.Max(0f, budgetMs) / TickMs);
        var limit = start + budget;
        var cap = budget / 2;
        var before = start;
        while (Unit())
        {
            var now = Clock.ElapsedTicks;
            var took = now - before;
            before = now;
            if (lastUnitCreated)
            {
                if (took > maxCreateTicks) maxCreateTicks = took;
                createEstimate = took > createEstimate ? took : createEstimate - (createEstimate >> 2);
            }
            else
            {
                if (took > maxSolveTicks) maxSolveTicks = took;
                solveEstimate = took > solveEstimate ? took : solveEstimate - (solveEstimate >> 2);
            }
            var next = build != null && build.executed < build.Count ? createEstimate : solveEstimate;
            if (now + (next < cap ? next : cap) >= limit) break;
        }
        ElapsedMs += (Clock.ElapsedTicks - start) * TickMs;
        return Done;
    }

    /// <summary>Finish everything now (what the synchronous Dress / Build calls do).</summary>
    public void Complete()
    {
        if (Done) return;
        var start = Clock.ElapsedTicks;
        while (Unit()) { }
        ElapsedMs += (Clock.ElapsedTicks - start) * TickMs;
    }

    /// <summary>Stop without creating anything more. Objects already created stay.</summary>
    public void Cancel()
    {
        if (Done) return;
        Cancelled = true;
        Done = true;
        planner = null;
    }

    /// <summary>One unit of work. False when the job is finished.</summary>
    bool Unit()
    {
        if (Done) return false;
        if (build != null && build.Lost)
        {
            Cancel();
            return false;
        }
        // Create what the solver has recorded so far, in order, before solving more.
        if (build != null && build.executed < build.Count)
        {
            lastUnitCreated = true;
            build.ExecuteNext();
            return true;
        }
        lastUnitCreated = false;
        if (!planned)
        {
            if (planner.MoveNext()) return true;
            planned = true;
            planner = null;
            OnPlanned();
            if (build != null && build.executed < build.Count) return true;
        }
        Done = true;
        return false;
    }

    /// <summary>The solver finished (before the last objects are created).</summary>
    protected virtual void OnPlanned() { }

    /// <summary>
    /// First-use costs the first dressed rooms would otherwise pay in front of
    /// the player: every kit model, sidecar and slot material
    /// (<see cref="FrontRoomsKitLibrary.Prewarm"/>), and the first run (JIT) of
    /// the dressers' code, synchronous and stepped, every pile tableau included.
    /// The rooms are built under an inactive object that is destroyed at once.
    /// Every dress call seeds its own random stream and the caches only hold
    /// loaded assets, so nothing built later changes. Call once while nothing
    /// is on screen (the map's Prewarm, before the title).
    /// </summary>
    public static void WarmUp()
    {
        FrontRoomsKitLibrary.Prewarm();
        var scratch = new GameObject("dress warm-up") { hideFlags = HideFlags.HideAndDontSave };
        scratch.SetActive(false);
        try
        {
            var t = scratch.transform;
            var floor = new Rect(.08f, .08f, 11.84f, 11.84f);
            var clear = new[] { new Rect(4.5f, .08f, 1.2f, 1f), new Rect(.08f, 7.5f, 1f, 1.2f) };
            var none = new Rect[0];
            // The map's overload (pods, wall units, wall rows), the look-dev one (own columns), a small room (two wall rows).
            FrontRoomsOfficeKit.Dress(t, floor, 2.9f, 1, clear, none);
            FrontRoomsOfficeKit.Dress(t, floor, 2.9f, 2, clear);
            FrontRoomsOfficeKit.Dress(t, new Rect(.08f, .08f, 5.84f, 8.84f), 2.4f, 3, new[] { new Rect(2.4f, .08f, 1.2f, 1f) }, none);
            FrontRoomsFurniturePile.Build(t, new Vector3(6f, 0f, 6f), 3.2f, 5.4f, 1);
            foreach (FrontRoomsFurniturePile.Tableau tableau in System.Enum.GetValues(typeof(FrontRoomsFurniturePile.Tableau)))
                FrontRoomsFurniturePile.BuildPile(t, new Vector3(6f, 0f, 6f), 2.4f, 2.9f, 4, tableau);
            // The stepped path the map drives.
            var office = FrontRoomsOfficeKit.BeginDress(t, floor, 2.9f, 5, clear, none);
            while (!office.Step(1f)) { }
            var pile = FrontRoomsFurniturePile.BeginBuild(t, new Vector3(6f, 0f, 6f), 3.2f, 2.9f, 6);
            while (!pile.Step(1f)) { }
        }
        finally
        {
            Object.DestroyImmediate(scratch);
        }
    }
}

/// <summary>
/// The objects a dresser decided to create, in creation order: groups (empty
/// GameObjects), kit instances (FrontRoomsKitLibrary.Spawn) and primitive
/// blocks. Node -1 is the job's root; a node's parent is always created first.
/// </summary>
internal sealed class FrontRoomsDressBuild
{
    public enum Kind : byte { Group, Kit, Block }

    struct Node
    {
        public Kind kind;
        public int parent;
        public string name;          // group / block name, or the kit asset
        public string label;         // kit instance name (null: the asset name)
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 size;         // block size
        public bool colliders;
        public Material material;    // block material
        public System.Action<GameObject> then;   // kit: run on the instance right after Spawn (null: nothing)
    }

    readonly List<Node> nodes = new List<Node>();
    readonly List<Transform> made = new List<Transform>();
    Transform root, watched;
    bool watching;
    public int executed;

    public FrontRoomsDressBuild(Transform root) { this.root = root; }

    /// <summary>The transform node -1 stands for.</summary>
    public void SetRoot(Transform r) => root = r;

    /// <summary>Stop the job when this transform is destroyed (the root, or the parent before the root exists).</summary>
    public void Watch(Transform t)
    {
        watched = t;
        watching = t != null;
    }

    public int Count => nodes.Count;

    /// <summary>The watched transform was destroyed.</summary>
    public bool Lost => watching && watched == null;

    public int Group(int parent, string name, Vector3 localPosition, Quaternion localRotation)
    {
        nodes.Add(new Node { kind = Kind.Group, parent = parent, name = name, position = localPosition, rotation = localRotation });
        return nodes.Count - 1;
    }

    /// <summary>A kit instance: FrontRoomsKitLibrary.Spawn(asset, parent, position, rotation, null, colliders, label).</summary>
    public void Kit(int parent, string asset, Vector3 localPosition, Quaternion localRotation, bool colliders = true, string label = null)
    {
        nodes.Add(new Node { kind = Kind.Kit, parent = parent, name = asset, label = label, position = localPosition, rotation = localRotation, colliders = colliders });
    }

    /// <summary>A kit instance turned by yaw degrees (the Spawn(…, float yaw, …) overload).</summary>
    public void Kit(int parent, string asset, Vector3 localPosition, float yaw, bool colliders = true, string label = null)
    {
        Kit(parent, asset, localPosition, Quaternion.Euler(0f, yaw, 0f), colliders, label);
    }

    /// <summary>
    /// A kit instance turned by yaw degrees, with <paramref name="then"/> run on
    /// the spawned instance (or null, if the model is missing) in the same step,
    /// right after Spawn and before the next object: what the one-call code did
    /// with the GameObject Spawn returned.
    /// </summary>
    public void Kit(int parent, string asset, Vector3 localPosition, float yaw, bool colliders, string label, System.Action<GameObject> then)
    {
        nodes.Add(new Node { kind = Kind.Kit, parent = parent, name = asset, label = label, position = localPosition, rotation = Quaternion.Euler(0f, yaw, 0f), colliders = colliders, then = then });
    }

    public void Block(int parent, string name, Vector3 localPosition, Vector3 size, Material material, bool collider)
    {
        nodes.Add(new Node { kind = Kind.Block, parent = parent, name = name, position = localPosition, size = size, material = material, colliders = collider });
    }

    public void ExecuteNext()
    {
        var n = nodes[executed];
        var parent = n.parent < 0 ? root : made[n.parent];
        Transform t = null;
        if (parent != null)
            switch (n.kind)
            {
                case Kind.Group:
                    t = new GameObject(n.name).transform;
                    t.SetParent(parent, false);
                    t.localPosition = n.position;
                    t.localRotation = n.rotation;
                    break;
                case Kind.Kit:
                    var go = FrontRoomsKitLibrary.Spawn(n.name, parent, n.position, n.rotation, null, n.colliders, n.label);
                    n.then?.Invoke(go);
                    t = go != null ? go.transform : null;
                    break;
                case Kind.Block:
                    t = MakeBlock(parent, n.name, n.position, n.size, n.material, n.colliders).transform;
                    break;
            }
        made.Add(t);
        executed++;
    }

    static GameObject MakeBlock(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool collider)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = size;
        if (!collider)
        {
            var c = go.GetComponent<Collider>();
            if (Application.isPlaying) Object.Destroy(c); else Object.DestroyImmediate(c);
        }
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }
}
